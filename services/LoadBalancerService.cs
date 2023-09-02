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
	#region private data models

	private class ServerInfo
	{
		public readonly int Id;
		public readonly int RequestsLimit;

		public int RequestsCount;

		public ServerInfo(int id, int requestsLimit)
		{
			this.Id = id;
			this.RequestsLimit = requestsLimit;
		}
	}

	#endregion

	#region public api props

	// we need -1 coz we use '<' and not '<=' in 'SelectAndCount' method.
	public int TotalLimitPerDay => _relaysInfo.Sum(x => x.RequestsLimit - 1);
	public int CanSendImmediately => _relaysInfo.Sum(x => x.RequestsLimit - x.RequestsCount - 1);

	#endregion

	private readonly ServerInfo[] _relaysInfo;
	private readonly ILogger<LoadBalancerService> _logger;

	public LoadBalancerService(SmtpServiceSettings settings, ILogger<LoadBalancerService> logger)
	{
		int i = 0;
		_relaysInfo = settings.SmtpRelays.Select(x => new ServerInfo(i++, x.MaxMailsPerDay)).ToArray();

		_logger = logger;
		_logger.LogInformation($"Created {nameof(LoadBalancerService)}.");
	}



	private static readonly Mutex parallelSelectionPreventer = new Mutex();
	private static int prevSelectedServer = -1;

	/// <returns>
	/// Zero-based index of the SMTP-relay server which
	/// limit is still not reached, -1 if no such found.
	/// </returns>
	public int SelectAndCount()
	{
		parallelSelectionPreventer.WaitOne(100);
		try
		{
			/* This logic of this loop is next:
			 * We are constantly iterating over our clients array. When exiting the method, our state
			 * is being saved into the 'prevSelectedServer' static variable. Therefore we need mutex
			 * here, to not allow modify this from another instance, what will break those logic.
			 * This method is like very fast, zero-allocation. Since this, we don't need to make mutex
			 * wait more then 100 ms. So the function exits when 'totalPassed' has reached the array
			 * length. 
			 * 
			 * Once again: we are iterating over all the array loopingly and the index is shared
			 * over all the instances, but every instance passes all the array not more than 1 time.
			 */
			for(int i = prevSelectedServer + 1, totalPassed = 0; totalPassed < _relaysInfo.Length; i++, totalPassed++)
			{
				if(i == _relaysInfo.Length) i = 0;

				var testedServer = _relaysInfo[i];

				if(testedServer.RequestsCount < testedServer.RequestsLimit)
				{
					_logger.LogInformation($"SMTP-relay server with id={i} was selected for the next sending. " +
						$"Current count is {testedServer.RequestsCount}, limit is {testedServer.RequestsLimit}.");

					testedServer.RequestsCount++;
					prevSelectedServer = i;
					return i;
				}
			}

			_logger.LogInformation($"None of {_relaysInfo.Length} SMTP-relay servers are " +
				$"currently able to serve the request.");
			return -1;
		}
		finally
		{
			parallelSelectionPreventer.ReleaseMutex();
		}
	}


	// how many times 'RequestsCount' will be decreased during the 'countIntervalSecond'
	private const int slicesPerInterval = 24;
	private const int countIntervalSecond = 60 * 60 * 24;
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var sliceInterval = countIntervalSecond / slicesPerInterval;
		_logger.LogInformation($"Begin ExecuteAsync... Slice interval is {sliceInterval} seconds.");

		while(!stoppingToken.IsCancellationRequested)
		{
			for(int i = 0; i < _relaysInfo.Length; i++)
			{
				var currRelay = _relaysInfo[i];

				var sliceQuantity = currRelay.RequestsLimit / slicesPerInterval;

				currRelay.RequestsCount -= sliceQuantity;

				if(currRelay.RequestsCount < 0) currRelay.RequestsCount = 0;

				_logger.LogInformation($"For server [{i}] current count was decreased by " +
					$"{sliceQuantity} and now equals to {currRelay.RequestsCount}.");
			}

			_logger.LogInformation($"ExecuteAsync is delayed for {sliceInterval} seconds.");
			await Task.Delay(sliceInterval * 1000, stoppingToken);
		}
	}
}
