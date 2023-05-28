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
		using var client = new SmtpClient() {
			SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
		};

		List<int> ignoreList = new();
		while(ignoreList.Count < _serverInfos.Length) { // safe counter
			var node = _loadBalancerService.SelectAndCount();
			if(node == -1) break;

			var nodeInfo = _serverInfos[node];

			try {
				await client.ConnectAsync(nodeInfo.SmtpHost, nodeInfo.SmtpPort, SecureSocketOptions.StartTls);
				await client.AuthenticateAsync(nodeInfo.Login, nodeInfo.Password);

				_logger.LogInformation($"Sending mail. To: {message.To}. From: {message.From}. " +
					$"Using server: {nodeInfo.SmtpHost}:{nodeInfo.SmtpPort}.");

				await client.SendAsync(message);
				return true;
			} catch {
				_logger.LogWarning($"Sending failed.");
			}

			ignoreList.Add(node);
		}

		_logger.LogError($"None of the servers was able to handle the request.");
		return false;
	}

	public async Task<bool> Send(
		string from,
		string fromName,
		string to,
		string subject,
		string body
		)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(fromName, from));
		message.To.Add(new MailboxAddress("recipient", to));
		message.Subject = subject;
		message.Body = new MimeKit.TextPart("plain") { Text = body };

		return await Send(message);
	}

	public async Task<bool> Send(SendMailRequest request) => await
		Send(request.From, request.FromName, request.To, request.Subject, request.Body);
}
