namespace TaskFlow.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "TaskFlow";
    public string Audience { get; set; } = "TaskFlowClient";
    public int ExpiryMinutes { get; set; } = 120;
}
