namespace web.Services;

public interface IForecastApiClient
{
    Task<ForecastApiHealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default);

    Task<ForecastApiResponse> ForecastAsync(
        ForecastApiRequest request,
        CancellationToken cancellationToken = default);
}