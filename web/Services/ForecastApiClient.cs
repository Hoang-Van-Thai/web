using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace web.Services;

public class ForecastApiClient : IForecastApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ForecastApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ForecastApiClient(
        HttpClient httpClient,
        IOptions<ForecastApiOptions> options,
        ILogger<ForecastApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = options.Value.BaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "PythonApi:BaseUrl chưa được cấu hình.");
        }

        _httpClient.BaseAddress =
            new Uri(baseUrl.TrimEnd('/') + "/");

        _httpClient.Timeout = TimeSpan.FromSeconds(
            options.Value.TimeoutSeconds > 0
                ? options.Value.TimeoutSeconds
                : 120);
    }

    // Kiểm tra trạng thái Python API
    public async Task<ForecastApiHealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "health",
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Python API /health lỗi " +
                $"{(int)response.StatusCode}: {content}");
        }

        return JsonSerializer.Deserialize<ForecastApiHealthResponse>(
            content,
            JsonOptions)
            ?? throw new InvalidOperationException(
                "Không đọc được dữ liệu health từ Python API.");
    }

    // Gửi dữ liệu sang Python để dự báo
    public async Task<ForecastApiResponse> ForecastAsync(
        ForecastApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "forecast",
            request,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Forecast API trả về {StatusCode}: {Content}",
                (int)response.StatusCode,
                content);

            var message = content;

            try
            {
                using var json = JsonDocument.Parse(content);

                if (json.RootElement.TryGetProperty(
                    "detail",
                    out var detail))
                {
                    message = detail.ToString();
                }
            }
            catch (JsonException)
            {
                // Giữ nguyên nội dung lỗi gốc.
            }

            throw new InvalidOperationException(
                $"Python Forecast API lỗi " +
                $"{(int)response.StatusCode}: {message}");
        }

        var result = JsonSerializer.Deserialize<ForecastApiResponse>(
            content,
            JsonOptions);

        if (result == null || !result.Success)
        {
            throw new InvalidOperationException(
                "Python API không trả về kết quả dự báo hợp lệ.");
        }

        if (result.Forecast == null || result.Forecast.Count == 0)
        {
            throw new InvalidOperationException(
                "API không trả về ngày dự báo nào.");
        }

        return result;
    }
}