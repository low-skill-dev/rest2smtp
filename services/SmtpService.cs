using Microsoft.Extensions.Logging;
using services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.Net.Security;
using System.Security.Authentication;
using MailKit.Security;

namespace services;
public class SmtpService
{
	private readonly LoadBalancerService _loadBalancerService;
	private readonly SmtpRelayInfo[] _serverInfos;
	private readonly ILogger<SmtpService> _logger;

	public SmtpService(SmtpServiceSettings settings, LoadBalancerService balancer, ILogger<SmtpService> logger)
	{
		_serverInfos = settings.SmtpRelays.ToArray();
		_loadBalancerService = balancer;
		_logger = logger;

		_logger.LogInformation($"Created {nameof(SmtpServiceSettings)}.");
		_logger.LogInformation($"Loaded {_serverInfos.Length} smtp-relay servers: [{string.Join(',', _serverInfos.Select(x => x.SmtpHost))}].");
	}

	public async Task<bool> Send(MimeMessage message)
	{
		using var client = new SmtpClient()
		{
			SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
			Timeout = 5000
		};

		int safeCounter = 0;
		List<int> usedRelays = new();
		while(usedRelays.Count < _serverInfos.Length && safeCounter++ < 1000)
		{
			/* The logic here:
			 * This method considered stupid. If 'SelectAndCount' will tell it to
			 * use the same client 1k time, it will do it no thinking. And this
			 * is correct. We don't need two methods to think about one thing.
			 */
			var node = _loadBalancerService.SelectAndCount();
			if(node == -1) break;

			var nodeInfo = _serverInfos[node];

			try
			{
				await client.ConnectAsync(nodeInfo.SmtpHost, nodeInfo.SmtpPort, SecureSocketOptions.StartTls);
				await client.AuthenticateAsync(nodeInfo.Login, nodeInfo.Password);

				_logger.LogInformation($"Sending mail. To: {message.To}. From: {message.From}. " +
					$"Using server: {nodeInfo.SmtpHost}:{nodeInfo.SmtpPort}.");

				await client.SendAsync(message);

				return true;
			}
			catch(Exception ex)
			{
				_logger.LogWarning($"Sending failed : {ex.Message}");
				await client.DisconnectAsync(true);
			}

			usedRelays.Add(node);
		}

		_logger.LogError($"None of the servers was able to handle the request.");
		return false;
	}

	public async Task<bool> Send(
		string from,
		string fromName,
		string to,
		string subject,
		string body,
		string? htmlBody
		)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(fromName, from));
		message.To.Add(new MailboxAddress("recipient", to));
		message.Subject = subject;
		message.Body = new BodyBuilder
		{
			HtmlBody = htmlBody ?? $"<h2>{body}</h2>",
			TextBody = body
		}.ToMessageBody();

		return await Send(message);
	}

	public async Task<bool> Send(SendMailRequest request) => await
		Send(request.From, request.FromName, request.To, request.Subject, request.Body, request.HtmlBody);
}
