using Backend.Api.Clients.Dto;
using Refit;

namespace Backend.Api.Clients;

public interface IOptimizerClient
{
    [Post("/api/optimize")]
    Task<OptimizeResponse> OptimizeAsync([Body] OptimizeRequest request, CancellationToken cancellationToken = default);

    [Get("/api/health")]
    Task<Dictionary<string, string>> HealthCheckAsync(CancellationToken cancellationToken = default);
}
