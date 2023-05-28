namespace api.Models;

public class LimitationsResponse
{
	public int TotalPerDayLimit { get; init; }
	public int SendedLast24Hours { get; init; }
	public int CanSendImmediately { get; init; }
}
