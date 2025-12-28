using Grpc.Net.Client;
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
            Console.WriteLine("Waiting for services to be ready...\n");

            using var channel = GrpcChannel.ForAddress(grpcAddress);
            var client = new Greeter.GreeterClient(channel);

            // Wait for the service to be ready with retry logic
            var maxRetries = 10;
            var connected = false;
            
            for (int retry = 1; retry <= maxRetries && !connected; retry++)
            {
                try
                {
                    var testRequest = new HelloRequest { Name = "ConnectionTest" };
                    await client.SayHelloAsync(testRequest);
                    connected = true;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Waiting for server... attempt {retry}/{maxRetries}");
                    await Task.Delay(2000);
                }
            }

            if (!connected)
            {
                Console.WriteLine("Failed to connect to server after multiple attempts.");
                return;
            }

            Console.WriteLine("Connected! Starting load balancing demo - making 10 requests...\n");

            // Make multiple requests to demonstrate load balancing
            for (int i = 1; i <= 10; i++)
            {
                var request = new HelloRequest { Name = $"Client-Request-{i}" };
                var reply = await client.SayHelloAsync(request);
                Console.WriteLine($"[{i:D2}] Response: {reply.Message}");
                
                await Task.Delay(500); // Small delay between requests
            }

            Console.WriteLine("\nDemo completed!");
            
            // Only wait for key if running interactively
            if (Environment.GetEnvironmentVariable("DOCKER_CONTAINER") != "true")
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
