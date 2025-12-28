using Grpc.Core;

namespace GrpcServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly string _hostname;

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
            _hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        }

        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received request from {Name} on server {Hostname}", request.Name, _hostname);

            var delay = Random.Shared.Next(50, 200);
            await Task.Delay(delay);

            _logger.LogInformation("Request processed in {ProcessingTimeMs}", delay);

            return new HelloReply
            {
                Message = $"Hello {request.Name} from server [{_hostname}]"
            };
        }
    }
}
