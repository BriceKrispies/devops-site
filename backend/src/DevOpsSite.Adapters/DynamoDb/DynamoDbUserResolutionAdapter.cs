using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DevOpsSite.Application.Authorization;
using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.DynamoDb;

/// <summary>
/// Resolves user identity and permissions from the legacy DynamoDB tables
/// shared with the old DevOps site. READ-ONLY — never writes to shared tables.
///
/// Table schemas (owned by old site):
///   Users: userid (PK), username (email), roleid, createdAt
///   Roles: RoleID (PK), RoleName, Perms (list of numbers), active, createdAt
///
/// Constitution §10: vendor-specific errors mapped to internal taxonomy.
/// </summary>
public sealed class DynamoDbUserResolutionAdapter : IUserResolutionPort
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ITelemetryPort _telemetry;
    private readonly DynamoDbConfig _config;
    private const string DependencyName = "dynamodb-users";

    public DynamoDbUserResolutionAdapter(
        IAmazonDynamoDB dynamoDb,
        ITelemetryPort telemetry,
        DynamoDbConfig config)
    {
        _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<ResolvedUser?> ResolveByEmailAsync(string email, CancellationToken ct = default)
    {
        using var span = _telemetry.StartSpan($"{DependencyName}.ResolveByEmail", Guid.NewGuid().ToString("N"));
        span.SetAttribute("externalTarget", DependencyName);

        try
        {
            var user = await FindUserByEmailAsync(email, ct);
            if (user is null)
            {
                span.SetResult("user_not_found");
                _telemetry.IncrementCounter("auth.user_resolution", new Dictionary<string, string>
                {
                    ["result"] = "user_not_found"
                });
                return null;
            }

            var role = await GetRoleAsync(user.RoleId, ct);
            if (role is null)
            {
                _telemetry.LogWarn(DependencyName, span.SpanId,
                    $"User '{email}' references role '{user.RoleId}' which does not exist.");
                span.SetResult("role_not_found");
                _telemetry.IncrementCounter("auth.user_resolution", new Dictionary<string, string>
                {
                    ["result"] = "role_not_found"
                });
                return null;
            }

            var permissions = LegacyPermissionMap.Resolve(role.PermNumbers);

            span.SetResult("success");
            _telemetry.IncrementCounter("auth.user_resolution", new Dictionary<string, string>
            {
                ["result"] = "success"
            });

            return new ResolvedUser
            {
                UserId = user.UserId,
                Username = user.Username,
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Permissions = permissions
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            span.SetError("dynamodb_error", ex.Message);
            _telemetry.LogError(DependencyName, span.SpanId,
                $"DynamoDB error resolving user '{email}'.", "DEPENDENCY_UNAVAILABLE", DependencyName);
            _telemetry.IncrementCounter("auth.user_resolution", new Dictionary<string, string>
            {
                ["result"] = "error"
            });
            throw;
        }
    }

    private async Task<UserDto?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        // Scan by username field (email). The old site uses username as the login identity.
        var request = new ScanRequest
        {
            TableName = _config.UsersTableName,
            FilterExpression = "username = :email",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":email"] = new() { S = email }
            },
            Limit = 1
        };

        var response = await _dynamoDb.ScanAsync(request, ct);

        if (response.Items.Count == 0)
            return null;

        var item = response.Items[0];
        return new UserDto
        {
            UserId = GetString(item, "userid"),
            Username = GetString(item, "username"),
            RoleId = GetString(item, "roleid")
        };
    }

    private async Task<RoleDto?> GetRoleAsync(string roleId, CancellationToken ct)
    {
        var request = new GetItemRequest
        {
            TableName = _config.RolesTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["RoleID"] = new() { S = roleId }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, ct);

        if (response.Item is null || response.Item.Count == 0)
            return null;

        var item = response.Item;
        var perms = ParsePerms(item);

        return new RoleDto
        {
            RoleId = GetString(item, "RoleID"),
            RoleName = GetString(item, "RoleName"),
            PermNumbers = perms
        };
    }

    /// <summary>
    /// Parse the Perms field from the DynamoDB item.
    /// The old site stores this as a DynamoDB list of numbers (N type).
    /// Some roles may store them as strings or a JSON string — handle all cases.
    /// </summary>
    private static List<int> ParsePerms(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("Perms", out var permsAttr))
            return [];

        // Case 1: DynamoDB List type (L) — most common
        if (permsAttr.L is { Count: > 0 })
        {
            var result = new List<int>();
            foreach (var elem in permsAttr.L)
            {
                if (elem.N is not null && int.TryParse(elem.N, out var n))
                    result.Add(n);
                else if (elem.S is not null && int.TryParse(elem.S, out var s))
                    result.Add(s);
            }
            return result;
        }

        // Case 2: String containing JSON array — e.g. "[\"1\",\"2\",\"3\"]"
        if (permsAttr.S is not null)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(permsAttr.S);
                if (parsed is not null)
                {
                    return parsed
                        .Select(e => e.ValueKind == JsonValueKind.Number
                            ? e.GetInt32()
                            : int.TryParse(e.GetString(), out var n) ? n : -1)
                        .Where(n => n > 0)
                        .ToList();
                }
            }
            catch (JsonException)
            {
                // Malformed — return empty
            }
        }

        // Case 3: Number set (NS) — unlikely but handle
        if (permsAttr.NS is { Count: > 0 })
        {
            return permsAttr.NS
                .Select(s => int.TryParse(s, out var n) ? n : -1)
                .Where(n => n > 0)
                .ToList();
        }

        return [];
    }

    private static string GetString(Dictionary<string, AttributeValue> item, string key) =>
        item.TryGetValue(key, out var attr) ? attr.S ?? string.Empty : string.Empty;

    // Internal DTOs — never leave this file
    private sealed record UserDto
    {
        public required string UserId { get; init; }
        public required string Username { get; init; }
        public required string RoleId { get; init; }
    }

    private sealed record RoleDto
    {
        public required string RoleId { get; init; }
        public required string RoleName { get; init; }
        public required List<int> PermNumbers { get; init; }
    }
}
