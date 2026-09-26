using System.ComponentModel.DataAnnotations;

namespace Authorization.API.DTOs;

public class CreateAccessRuleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;
    public string Effect { get; set; } = "Allow";
    public string? ResourceType { get; set; }
    public string? Action { get; set; }
    public string? SourceIpAllowList { get; set; }
    public string? SourceIpDenyList { get; set; }
    public int? TimeStartHour { get; set; }
    public int? TimeEndHour { get; set; }
    public string? DaysOfWeek { get; set; }
    public string? RequiredDepartment { get; set; }
    public string? RequiredRole { get; set; }
    public int RateLimitPerHour { get; set; }
}

public class RubacEvaluateRequest
{
    public string? ResourceType { get; set; }
    public string Action { get; set; } = "Read";
    public string? SourceIp { get; set; }
}
