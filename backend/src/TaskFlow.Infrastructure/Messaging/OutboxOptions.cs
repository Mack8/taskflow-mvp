namespace TaskFlow.Infrastructure.Messaging;

public class OutboxOptions
{
    public const string SectionName = "Outbox";
    public int BatchSize { get; set; } = 25;
    public int PollIntervalSeconds { get; set; } = 2;
    public int RetryDelaySeconds { get; set; } = 5;
}
