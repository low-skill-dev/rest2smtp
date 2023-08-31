namespace vdb_node_api.Models.Runtime;

public class MasterAccount
{
    public string KeyHashBase64 { get; init; } = null!;
    public string? NotBeforeUtcIso8601 { get; init; } = null!;
    public string? NotAfterUtcIso8601 { get; init; } = null!;
}
