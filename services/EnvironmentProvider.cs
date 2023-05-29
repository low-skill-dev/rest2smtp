using Microsoft.Extensions.Logging;

namespace services;

public sealed class EnvironmentProvider
{
	private const string ENV_IGNORE_UNATHORIZED = "REST2WG_IGNORE_UNAUTHORIZED";


	public bool? IGNORE_UNAUTHORIZED { get; init; }



	private readonly ILogger<EnvironmentProvider> _logger;

	public EnvironmentProvider(ILogger<EnvironmentProvider> logger)
	{
		_logger = logger;

		IGNORE_UNAUTHORIZED = ParseBoolValue(ENV_IGNORE_UNATHORIZED);
	}

	private string GetIncorrectIgnoredMessage(string EnvName)
	{
		return $"Incorrect value of {EnvName} environment variable was ignored.";
	}

	private bool? ParseBoolValue(string EnvName)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (str.Equals("true", StringComparison.InvariantCultureIgnoreCase))
			{
				_logger.LogInformation($"{EnvName}={true}.");
				return true;
			}
			if (str.Equals("false", StringComparison.InvariantCultureIgnoreCase))
			{
				_logger.LogInformation($"{EnvName}={false}.");
				return false;
			}
			_logger.LogError(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger.LogInformation($"{EnvName} was not present.");
		return null;
	}
	private int? ParseIntValue(string EnvName, int minValue = int.MinValue)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (int.TryParse(str, out int val))
			{
				_logger.LogInformation($"{EnvName}={val}.");
				return val;
			}
			_logger.LogError(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger.LogInformation($"{EnvName} was not present.");
		return null;
	}

	private string? ParseStringValue(string EnvName, Func<string, bool> valueValidator)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (valueValidator(str))
			{
				_logger.LogInformation($"{EnvName}={str}.");
				return str;
			}
			_logger.LogError(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger.LogInformation($"{EnvName} was not present.");
		return null;
	}
}


