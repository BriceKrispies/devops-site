using System.ComponentModel.DataAnnotations;

namespace DevOpsSite.Adapters.DynamoDb;

/// <summary>
/// Configuration for DynamoDB user/role tables (shared with old DevOps site).
/// Constitution §11: typed, validated at startup.
/// </summary>
public sealed class DynamoDbConfig
{
    [Required(ErrorMessage = "DynamoDb:UsersTableName is required.")]
    public string UsersTableName { get; set; } = string.Empty;

    [Required(ErrorMessage = "DynamoDb:RolesTableName is required.")]
    public string RolesTableName { get; set; } = string.Empty;

    [Required(ErrorMessage = "DynamoDb:Region is required.")]
    public string Region { get; set; } = "us-east-1";

    public int TimeoutMs { get; set; } = 5000;
}
