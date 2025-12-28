using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using GrpcServer;

namespace GrpcClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Get gRPC server address from environment variable, default to Envoy proxy
            var grpcAddress = Environment.GetEnvironmentVariable("GRPC_SERVER_ADDRESS") ?? "http://localhost:8080";
            
            Console.WriteLine($"Connecting to gRPC server at: {grpcAddress}");
            Console.WriteLine("Using production-ready gRPC channel with retry policies...\n");

            // Production-ready gRPC channel configuration with retry policy
            // This replaces the hacky manual retry loop with proper gRPC retry semantics
            var methodConfig = new MethodConfig
            {
                Names = { MethodName.Default }, // Apply to all methods
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromMilliseconds(500),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 2,
                    // Retry on these status codes (standard retryable errors)
                    RetryableStatusCodes = 
                    { 
                        StatusCode.Unavailable,      // Service unavailable
                        StatusCode.DeadlineExceeded, // Timeout
                        StatusCode.Aborted,          // Operation aborted
                        StatusCode.Internal,         // Internal error (use with caution)
                        StatusCode.ResourceExhausted // Rate limiting (with backoff)
                    }
                }
            };

            var channel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
            {
                // Enable service config for retry policies
                ServiceConfig = new ServiceConfig
                {
                    MethodConfigs = { methodConfig }
                },
                
                // HTTP handler configuration for better connection management
                HttpHandler = new SocketsHttpHandler
                {
                    // Connection pooling - keep connections alive
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    
                    // Keep-alive to detect dead connections
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                    
                    // Enable multiple HTTP/2 connections for high throughput
                    EnableMultipleHttp2Connections = true,
                    
                    // Connection timeout
                    ConnectTimeout = TimeSpan.FromSeconds(10)
                },
                
                // Max retry buffer size (important for large messages)
                MaxRetryBufferSize = 16 * 1024 * 1024,      // 16MB
                MaxRetryBufferPerCallSize = 1 * 1024 * 1024  // 1MB per call
            });

            var client = new Greeter.GreeterClient(channel);

            // Wait for service readiness using a simple probe
            // Note: With Envoy health checks, this should succeed quickly
            Console.WriteLine("Checking service readiness...");
            await WaitForServiceReady(client, maxWaitSeconds: 30);

            Console.WriteLine("\nConnected! Starting load balancing demo - making 10 requests...\n");

            // Make multiple requests to demonstrate load balancing
            // Retries are now handled automatically by the gRPC channel
            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    var request = new HelloRequest { Name = $"Client-Request-{i}" };
                    
                    // Set per-call options (deadline, headers, etc.)
                    var callOptions = new CallOptions()
                        .WithDeadline(DateTime.UtcNow.AddSeconds(30))
                        .WithHeaders(new Metadata
                        {
                            { "x-request-id", Guid.NewGuid().ToString() }
                        });
                    
                    var reply = await client.SayHelloAsync(request, callOptions);
                    Console.WriteLine($"[{i:D2}] Response: {reply.Message}");
                }
                catch (RpcException ex)
                {
                    // This only fires if ALL retries failed
                    Console.WriteLine($"[{i:D2}] Failed after all retries: {ex.Status.StatusCode} - {ex.Status.Detail}");
                }
                
                await Task.Delay(500); // Small delay between requests for demo
            }

            Console.WriteLine("\nDemo completed!");
            
            // Graceful shutdown
            await channel.ShutdownAsync();
            
            // Only wait for key if running interactively
            if (Environment.GetEnvironmentVariable("DOCKER_CONTAINER") != "true")
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Wait for the gRPC service to become ready.
        /// With Envoy health checks enabled, this primarily handles initial startup timing.
        /// </summary>
        private static async Task WaitForServiceReady(Greeter.GreeterClient client, int maxWaitSeconds = 30)
        {
            var deadline = DateTime.UtcNow.AddSeconds(maxWaitSeconds);
            var attemptDelay = TimeSpan.FromSeconds(2);
            int attempt = 0;

            while (DateTime.UtcNow < deadline)
            {
                attempt++;
                try
                {
                    // Short timeout for connectivity check
                    var options = new CallOptions()
                        .WithDeadline(DateTime.UtcNow.AddSeconds(5));
                    
                    await client.SayHelloAsync(
                        new HelloRequest { Name = "HealthCheck" }, 
                        options);
                    
                    Console.WriteLine($"Service ready after {attempt} attempt(s)");
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || 
                                               ex.StatusCode == StatusCode.DeadlineExceeded)
                {
                    Console.WriteLine($"Waiting for service... attempt {attempt} ({ex.StatusCode})");
                    await Task.Delay(attemptDelay);
                }
            }

            throw new TimeoutException($"Service not ready after {maxWaitSeconds} seconds");
        }
    }
}
