namespace web.Services
{
    public interface IForecastApiClient
    {
        Task<ForecastApiResponse> PredictAsync(
            ForecastApiRequest request,
            CancellationToken cancellationToken = default
        );
    }
}