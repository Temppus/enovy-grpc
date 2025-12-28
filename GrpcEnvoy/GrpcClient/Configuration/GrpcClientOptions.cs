namespace GrpcClient.Configuration;

public sealed class GrpcClientOptions
{
    public const string SectionName = "GrpcClient";

    public string Address { get; set; } = "http://localhost:8080";
    public ConnectionOptions Connection { get; set; } = new();
}

public sealed class ConnectionOptions
{
    public int PooledConnectionLifetimeMinutes { get; set; } = 5;
    public int PooledConnectionIdleTimeoutMinutes { get; set; } = 2;
    public int KeepAlivePingDelaySeconds { get; set; } = 60;
    public int KeepAlivePingTimeoutSeconds { get; set; } = 30;
    public int ConnectTimeoutSeconds { get; set; } = 10;
}

