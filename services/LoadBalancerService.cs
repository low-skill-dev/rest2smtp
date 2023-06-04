using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using services.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace services;

/* Балансировщик нагрузки отвечает за распределение отправляемых сообщений
 * между предоставленными SMTP-relay серверами. Балансировщик ничего не знает 
 * о самих серверах. Он использует Id в качестве идентификатора.
 * 
 * Удаление учтенных запросов проиводится в фоне с определенным интервалом.
 */



public class LoadBalancerService : BackgroundService
{
	private class ServerInfo
	{
		public int Id;
		public int RequestsCount;
		public int RequestsLimit;

		public ServerInfo(int id, int requestsLimit)
		{
			this.Id = id;
			this.RequestsLimit = requestsLimit;
		}
	}


	public const int SafetyGap = 1;
	private const int slicesPerInterval = 24;
	private const int countIntervalSecond = 60 * 60 * 24;

	private readonly ServerInfo[] _relaysInfo;
	private readonly ILogger<LoadBalancerService> _logger;

	public int TotalLimitPerDay => _relaysInfo.Sum(x => x.RequestsLimit);
	public int CanSendImmediately => _relaysInfo.Sum(x => x.RequestsLimit - x.RequestsCount - SafetyGap);

	public LoadBalancerService(SmtpServiceSettings settings, ILogger<LoadBalancerService> logger)
	{
		int i = 0;
		this._relaysInfo = settings.SmtpRelays.Select(x => new ServerInfo(i++, x.MaxMailsPerDay)).ToArray();

		_logger = logger;
		_logger.LogInformation($"Created {nameof(LoadBalancerService)}.");
	}

	private bool CanUseServer(ServerInfo server)
		=> server.RequestsCount < (server.RequestsLimit - SafetyGap);

	/// <returns>
	/// Zero-based index of the SMTP-relay server which
	/// limit is still not reached, -1 if such not found.
	/// </returns>
	public int SelectAndCount(params int[] ignoreServerIds)
	{
		for(int i = 0; i < _relaysInfo.Length; i++) {
			var curr = _relaysInfo[i];
			if(!ignoreServerIds.Contains(curr.Id) && CanUseServer(curr)) {
				_logger.LogInformation($"SMTP-relay server with id={i} was selected for the next sending. " +
					$"Current count is {curr.RequestsCount}, limit is {curr.RequestsLimit}.");
				curr.RequestsCount++;
				return i;
			}
		}

		_logger.LogInformation($"None of {_relaysInfo.Length} SMTP-relay servers are " +
			$"currently able to serve the request. Returning -1...");
		return -1;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var sliceInterval = countIntervalSecond / slicesPerInterval;
		_logger.LogInformation($"Begin ExecuteAsync... Slice interval is {sliceInterval} seconds.");

		while(!stoppingToken.IsCancellationRequested) {
			for(int i =0; i < _relaysInfo.Length; i++) {
				var curr = _relaysInfo[i];
				curr.RequestsCount -= curr.RequestsLimit / slicesPerInterval;
				if(curr.RequestsCount < 0 ) curr.RequestsCount = 0;

				_logger.LogInformation($"For server '{i}' current count was decreased by " +
					$"{curr.RequestsLimit / slicesPerInterval} and now equals to {curr.RequestsCount}.");
			}

			_logger.LogInformation($"ExecuteAsync is delayed for {sliceInterval} seconds.");
			await Task.Delay(sliceInterval * 1000);
		}
	}
}
