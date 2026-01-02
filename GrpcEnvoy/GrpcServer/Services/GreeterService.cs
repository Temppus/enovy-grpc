using Google.Protobuf;
using Grpc.Core;

namespace GrpcServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly string _hostname;
        private static readonly ByteString ResponseData = ByteString.CopyFrom(new byte[200 * 1024]); // 200kB

        private static readonly SemaphoreSlim Semaphore = new(1, 1);

        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
            _hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        }

        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            // Decode ByteString to byte array and log size
            var incomingData = request.Data.ToByteArray();

            _logger.LogInformation("Received request from {Name} on server {Hostname}, incoming data size: {DataSize} bytes", 
                request.Name, _hostname, incomingData.Length);

            await Semaphore.WaitAsync(context.CancellationToken);

            try
            {
                var delay = Random.Shared.Next(800, 1200); // avg processing time ~1s
                //await Task.Delay(delay);

                _logger.LogInformation("Request processed in {ProcessingTimeMs}ms, response data size: {ResponseSize} bytes", 
                    delay, ResponseData.Length);

                return new HelloReply
                {
                    Message = $"Hello {request.Name} from server [{_hostname}]",
                    Data = ResponseData
                };
            }
            finally
            {
                Semaphore.Release(1);
            }
        }
    }
}
