using Grpc.Core;

namespace GrpcServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly string _hostname;

        private static readonly SemaphoreSlim Semaphore = new(1, 1);

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
            _hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        }

        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received request from {Name} on server {Hostname}", request.Name, _hostname);

            if (Random.Shared.NextInt64(0, 100) < 10)
            {
                throw new InvalidOperationException("Simulating GRPC request processing error");
            }

            await Semaphore.WaitAsync(context.CancellationToken);

            try
            {
                var delay = Random.Shared.Next(800, 1200); // avg processing time ~1s
                await Task.Delay(delay);

                _logger.LogInformation("Request processed in {ProcessingTimeMs}", delay);

                return new HelloReply
                {
                    Message = $"Hello {request.Name} from server [{_hostname}]"
                };
            }
            finally
            {
                Semaphore.Release(1);
            }
        }
    }
}
