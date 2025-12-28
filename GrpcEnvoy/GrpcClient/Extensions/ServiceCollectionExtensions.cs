using Grpc.Core;
using Grpc.Net.Client.Configuration;
using GrpcClient.Configuration;
using GrpcServer;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcClient.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGreeterClient(this IServiceCollection services, GrpcClientOptions options)
    {
        services.AddGrpcClient<Greeter.GreeterClient>(clientOptions =>
        {
            clientOptions.Address = new Uri(options.Address);
        })
        .ConfigureChannel(channelOptions =>
        {
            // Configure retry policy
            var methodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = options.Retry.MaxAttempts,
                    InitialBackoff = TimeSpan.FromMilliseconds(options.Retry.InitialBackoffMs),
                    MaxBackoff = TimeSpan.FromSeconds(options.Retry.MaxBackoffSeconds),
                    BackoffMultiplier = options.Retry.BackoffMultiplier,
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
            var connectionOptions = options.Connection;

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

        return services;
    }
}

