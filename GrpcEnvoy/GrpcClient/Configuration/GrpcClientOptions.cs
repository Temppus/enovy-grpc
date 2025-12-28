namespace GrpcClient.Configuration;

public sealed class GrpcClientOptions
{
    public const string SectionName = "GrpcClient";

    public string Address { get; set; } = "http://localhost:8080";
    public int TimeoutSeconds { get; set; } = 30;
    public RetryOptions Retry { get; set; } = new();
    public ConnectionOptions Connection { get; set; } = new();
}

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 5;
    public int InitialBackoffMs { get; set; } = 500;
    public int MaxBackoffSeconds { get; set; } = 5;
    public double BackoffMultiplier { get; set; } = 2;
}

public sealed class ConnectionOptions
{
    public int PooledConnectionLifetimeMinutes { get; set; } = 5;
    public int PooledConnectionIdleTimeoutMinutes { get; set; } = 2;
    public int KeepAlivePingDelaySeconds { get; set; } = 60;
    public int KeepAlivePingTimeoutSeconds { get; set; } = 30;
    public int ConnectTimeoutSeconds { get; set; } = 10;
}

