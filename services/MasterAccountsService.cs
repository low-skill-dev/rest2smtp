using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace services;

/* Данный Singleton-сервис служит для валидации мастер-аккаунтов.
 */
public sealed class MasterAccountsService
{
	private class MasterAccountParsed
	{
		public required byte[] Hash { get; init; }
		public required DateTime? NotBefore { get; init; }
		public required DateTime? NotAfter { get; init; }

		public bool IsValidNow =>
			(NotBefore ?? DateTime.MinValue) < DateTime.UtcNow && 
			DateTime.UtcNow < (NotAfter ?? DateTime.MaxValue);
	}

	private readonly MasterAccountParsed[] _mastersAccounts;
	private readonly ILogger<LoadBalancerService> _logger;

	public MasterAccountsService(SettingsProviderService settingsProvider, ILogger<LoadBalancerService> logger)
	{
		_logger = logger;

		_mastersAccounts = settingsProvider.MasterAccounts.Select(x =>
		{
			var bytes = Convert.FromBase64String(x.KeyHashBase64);

			var c = CultureInfo.InvariantCulture;
			var s = DateTimeStyles.None;

			DateTime? notBefore = x.NotBeforeUtcIso8601 is not null
				? DateTime.Parse(x.NotBeforeUtcIso8601, c, s) : null;

			DateTime? notAfter = x.NotAfterUtcIso8601 is not null
				? DateTime.Parse(x.NotAfterUtcIso8601, c, s) : null;

			return new MasterAccountParsed
			{
				Hash = bytes,
				NotBefore = notBefore,
				NotAfter = notAfter,
			};
		}).ToArray();

		_logger.LogInformation($"Parsed {_mastersAccounts.Length} access keys in total.");
	}

	/// <returns>true if passed key is valid, false otherwise.</returns>
	public bool IsValid(string keyBase64)
	{
		byte[] search = SHA512.HashData(Convert.FromBase64String(keyBase64));
		return _mastersAccounts.Any(k =>
		{
			var result = k.IsValidNow && k.Hash.SequenceEqual(search);

			if(result && _logger.IsEnabled(LogLevel.Information))
			{
				var usedKeyHashStr = Convert.ToBase64String(search);
				var len = usedKeyHashStr.Length;

				var keyHashStart = usedKeyHashStr.Substring(0, 4);
				var keyHashEnd = usedKeyHashStr.Substring(len - 4, 4);

				var keyShortNotation = $"\'{keyHashStart}...{keyHashEnd}\'";

				_logger.LogInformation($"The next key was used for sending email: {keyShortNotation}.");
			}

			return result;
		});
	}
}
