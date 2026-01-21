using System.Text.Json;
using Backend.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/physics")]
public class PhysicsController : ControllerBase
{
    private readonly PhysicsService.PhysicsServiceClient _grpcClient;
    private readonly ILogger<PhysicsController> _logger;

    public PhysicsController(PhysicsService.PhysicsServiceClient grpcClient, ILogger<PhysicsController> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    [HttpGet("lorenz")]
    public async Task GetLorenz(CancellationToken cancellationToken, [FromQuery] double sigma = 10, [FromQuery] double rho = 28, [FromQuery] double beta = 2.6667)
    {
        var grpcRequest = new LorenzRequest
        {
            Sigma = sigma,
            Rho = rho,
            Beta = beta,
            Dt = 0.01,
            MaxIterations = 10000
        };

        Response.ContentType = "application/x-ndjson";

        try
        {
            using var call = _grpcClient.GenerateLorenz(grpcRequest, cancellationToken: cancellationToken);

            await foreach (var point in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(new { x = point.X, y = point.Y, z = point.Z });
                await Response.WriteAsync(json + "\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error generating lorenz");
        }
    }
}
