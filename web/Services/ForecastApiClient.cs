using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace web.Services
{
    public class ForecastApiClient : IForecastApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ForecastApiOptions _options;

        public ForecastApiClient(
            HttpClient httpClient,
            IOptions<ForecastApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<ForecastApiResponse> PredictAsync(
            ForecastApiRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                _options.PredictEndpoint,
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(
                    cancellationToken
                );

                throw new HttpRequestException(
                    $"Python Forecast API trả về lỗi " +
                    $"{(int)response.StatusCode}: {error}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<ForecastApiResponse>(
                cancellationToken: cancellationToken
            );

            if (result == null || result.Predictions == null ||
                result.Predictions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Python API không trả về dữ liệu dự báo."
                );
            }

            return result;
        }
    }
}