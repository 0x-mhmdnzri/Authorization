using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreateContextPolicyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = "*";
    public string Action { get; set; } = "*";
    public string Effect { get; set; } = "Allow";
    public int Priority { get; set; } = 100;

    public int? AllowedHourStart { get; set; }
    public int? AllowedHourEnd { get; set; }
    public string? AllowedDaysOfWeek { get; set; }
    public bool RequireTrustedNetwork { get; set; }
    public bool RequireManagedDevice { get; set; }
    public string? AllowedCountries { get; set; }
    public string? BlockedCountries { get; set; }
    public string? MinimumAuthMethod { get; set; }
    public bool BlockAnomalousLocation { get; set; }
}

public class CbacEvaluateRequest
{
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string Action { get; set; } = "Read";

    public bool IsTrustedNetwork { get; set; }
    public bool IsManagedDevice { get; set; }
    public string? CountryCode { get; set; }
    public bool IsAnomalousLocation { get; set; }
    public string AuthMethod { get; set; } = "Password";
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
}
