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

