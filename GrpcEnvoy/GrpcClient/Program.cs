using Grpc.Core;
using Grpc.Net.Client.Configuration;
using GrpcClient.Configuration;
using GrpcClient.Services;
using GrpcServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GrpcClient;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Bind configuration
        var grpcClientOptions = builder.Configuration
            .GetSection(GrpcClientOptions.SectionName)
            .Get<GrpcClientOptions>() ?? new GrpcClientOptions();

        // Override address from environment variable if set
        var envAddress = Environment.GetEnvironmentVariable("GRPC_SERVER_ADDRESS");
        if (!string.IsNullOrEmpty(envAddress))
        {
            grpcClientOptions.Address = envAddress;
        }

        // Register the gRPC client using the client factory
        builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
        {
            options.Address = new Uri(grpcClientOptions.Address);
        })
        .ConfigureChannel(channelOptions =>
        {
            // Configure retry policy
            var methodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = grpcClientOptions.Retry.MaxAttempts,
                    InitialBackoff = TimeSpan.FromMilliseconds(grpcClientOptions.Retry.InitialBackoffMs),
                    MaxBackoff = TimeSpan.FromSeconds(grpcClientOptions.Retry.MaxBackoffSeconds),
                    BackoffMultiplier = grpcClientOptions.Retry.BackoffMultiplier,
                    RetryableStatusCodes =
                    {
                        StatusCode.Unavailable,
                        StatusCode.DeadlineExceeded,
                        StatusCode.Aborted,
                        StatusCode.Internal,
                        StatusCode.ResourceExhausted
                    }
                }
            };

            channelOptions.ServiceConfig = new ServiceConfig
            {
                MethodConfigs = { methodConfig }
            };

            channelOptions.MaxRetryBufferSize = 16 * 1024 * 1024;      // 16MB
            channelOptions.MaxRetryBufferPerCallSize = 1 * 1024 * 1024; // 1MB per call
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var connectionOptions = grpcClientOptions.Connection;

            return new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(connectionOptions.PooledConnectionLifetimeMinutes),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(connectionOptions.PooledConnectionIdleTimeoutMinutes),
                KeepAlivePingDelay = TimeSpan.FromSeconds(connectionOptions.KeepAlivePingDelaySeconds),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(connectionOptions.KeepAlivePingTimeoutSeconds),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(connectionOptions.ConnectTimeoutSeconds)
            };
        });

        // Register the hosted service
        builder.Services.AddHostedService<GreeterWorker>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Connecting to gRPC server at: {Address}", grpcClientOptions.Address);
        logger.LogInformation("Using production-ready gRPC channel with retry policies");

        await host.RunAsync();
    }
}
