using Grpc.Core;
using GrpcServer;

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

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received request from {Name} on server {Hostname}", request.Name, _hostname);
            
            return Task.FromResult(new HelloReply
            {
                Message = $"Hello {request.Name} from server [{_hostname}]"
            });
        }
    }
}
