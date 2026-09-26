namespace Authorization.API.Models;

/// <summary>
/// Hierarchical security levels used by Mandatory Access Control (MAC).
/// Higher rank = more sensitive / higher clearance.
/// </summary>
public static class SecurityLevel
{
    public const string Public = "Public";
    public const string Internal = "Internal";
    public const string Confidential = "Confidential";
    public const string Restricted = "Restricted";
    public const string Secret = "Secret";
    public const string TopSecret = "TopSecret";

    private static readonly Dictionary<string, int> Rank = new(StringComparer.OrdinalIgnoreCase)
    {
        [Public] = 0,
        [Internal] = 1,
        [Confidential] = 2,
        [Restricted] = 3,
        [Secret] = 4,
        [TopSecret] = 5
    };

    public static int GetRank(string? level)
    {
        if (string.IsNullOrWhiteSpace(level)) return 0;
        return Rank.TryGetValue(level.Trim(), out var r) ? r : 0;
    }

    public static bool IsValid(string? level) =>
        !string.IsNullOrWhiteSpace(level) && Rank.ContainsKey(level.Trim());

    public static IReadOnlyList<string> AllLevels => Rank.Keys.OrderBy(k => Rank[k]).ToList();
}
