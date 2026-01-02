using Google.Protobuf;
using Grpc.Core;
using GrpcServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GrpcClient.Services;

public sealed class GreeterClientWorker : BackgroundService
{
    private readonly Greeter.GreeterClient _client;
    private readonly ILogger<GreeterClientWorker> _logger;
    private readonly TimeSpan _requestInterval = TimeSpan.FromMilliseconds(1000);

    private static readonly ByteString RequestData = ByteString.CopyFrom(new byte[1024 * 1024]); // 1MB

    public GreeterClientWorker(Greeter.GreeterClient client, ILogger<GreeterClientWorker> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting request loop with {Interval}ms interval", _requestInterval.TotalMilliseconds);

        var requestNumber = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            requestNumber++;

            try
            {
                var request = new HelloRequest
                {
                    Name = $"Worker-Request-{requestNumber}",
                    Data = RequestData
                };

                var callOptions = new CallOptions(cancellationToken: stoppingToken)
                    .WithDeadline(DateTime.UtcNow.AddSeconds(5))
                    .WithHeaders(new Metadata
                    {
                        { "x-request-id", Guid.NewGuid().ToString() }
                    });

                var reply = await _client.SayHelloAsync(request, callOptions);

                _logger.LogInformation("[{RequestNumber:D4}] " +
                                       "ResponseMessage: {Message}, " +
                                       "Data size: {DataSize} bytes",
                    requestNumber, reply.Message, reply.Data.Length);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogInformation("Request cancelled due to shutdown");
                break;
            }
            catch (RpcException ex)
            {
                _logger.LogWarning("[{RequestNumber:D4}] gRPC call failed: {StatusCode} - {Detail}",
                    requestNumber, ex.Status.StatusCode, ex.Status.Detail);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker stopping due to cancellation");
                break;
            }
        }

        _logger.LogInformation("GreeterWorker stopped after {RequestCount} requests", requestNumber);
    }
}

