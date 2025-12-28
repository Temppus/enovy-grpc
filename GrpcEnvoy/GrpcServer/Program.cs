using GrpcServer.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;

            // Add gRPC services
            builder.Services.AddGrpc(options =>
            {
                // Enable detailed errors in development
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                
                // Set max message sizes for production
                options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
                options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4MB
            });

            // Add gRPC reflection for debugging tools like grpcurl
            builder.Services.AddGrpcReflection();

            // Configure health checks for Envoy to use
            builder.Services.AddGrpcHealthChecks()
                .AddCheck("greeter_service", () =>
                {
                    // Add custom health check logic here if needed
                    // e.g., check database connectivity, external service availability
                    return HealthCheckResult.Healthy($"Server {hostname} is healthy");
                });

            var app = builder.Build();

            // Map gRPC health check service (standard gRPC health protocol)
            // This is what Envoy's grpc_health_check will call
            app.MapGrpcHealthChecksService();

            // Map our gRPC services
            app.MapGrpcService<GreeterService>();

            // Map gRPC reflection service (for debugging with grpcurl, etc.)
            if (app.Environment.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            // HTTP health endpoint for container orchestrators (k8s, docker)
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", hostname }));
            
            // Readiness probe - indicates if service is ready to accept traffic
            app.MapGet("/ready", () => Results.Ok(new { status = "ready", hostname }));

            app.MapGet("/", () => $"gRPC Server [{hostname}] - Use a gRPC client to connect. Health: /health, Ready: /ready");

            Console.WriteLine($"gRPC Server starting on {hostname}");
            app.Run();
        }
    }
}
