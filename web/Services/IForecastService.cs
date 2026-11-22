namespace web.Services;

public interface IForecastService
{
    Task<List<ForecastOption>> GetProductsAsync(
        CancellationToken cancellationToken = default);

    Task<List<ForecastOption>> GetStoresAsync(
        CancellationToken cancellationToken = default);

    Task<ForecastApiHealthResponse> CheckApiAsync(
        CancellationToken cancellationToken = default);

    Task<ForecastExecutionResult> RunForecastAsync(
        int productId,
        int storeId,
        int days,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<ForecastExecutionResult?> GetForecastDetailsAsync(
        long forecastRunId,
        CancellationToken cancellationToken = default);
}