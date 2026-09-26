using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class RadacEvaluateRequest
{
    public Guid? ResourceId { get; set; }

    [Required]
    public string Action { get; set; } = "Read";

    // Risk context (client / gateway can supply these)
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
    public bool IsManagedDevice { get; set; }
    public bool IsTrustedNetwork { get; set; }
    public string? CountryCode { get; set; }
    public string AuthMethod { get; set; } = "Password";
    public int FailedLoginsLastHour { get; set; }
    public bool IsAnomalousLocation { get; set; }
    public bool IsOffHours { get; set; }

    /// <summary>0-100 operational / mission need</summary>
    [Range(0, 100)]
    public double OperationalNeed { get; set; } = 50;
}

public class CreateRiskPolicyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double NormalRiskThreshold { get; set; } = 30;
    public double MaxAcceptableRisk { get; set; } = 70;
    public double CriticalNeedThreshold { get; set; } = 80;
}
