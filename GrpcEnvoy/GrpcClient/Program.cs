using GrpcClient.Configuration;
using GrpcClient.Extensions;
using GrpcClient.Services;
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

        // Register gRPC client
        builder.Services.AddGreeterClient(grpcClientOptions);

        // Register the hosted service
        builder.Services.AddHostedService<GreeterClientWorker>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Connecting to gRPC server at: {Address}", grpcClientOptions.Address);
        logger.LogInformation("Using production-ready gRPC channel with retry policies");

        await host.RunAsync();
    }
}
