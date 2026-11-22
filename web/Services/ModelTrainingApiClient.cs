using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace web.Services;

public class ModelTrainingApiClient : IModelTrainingApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ModelTrainingApiClient(
        HttpClient httpClient,
        IOptions<ForecastApiOptions> options)
    {
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(
            options.Value.BaseUrl.TrimEnd('/') + "/");

        _httpClient.Timeout = TimeSpan.FromSeconds(
            Math.Max(options.Value.TimeoutSeconds, 300));
    }

    // ========================================================
    // HEALTH
    // ========================================================

    public Task<PythonHealthDto> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<PythonHealthDto>(
            "health",
            cancellationToken);
    }

    // ========================================================
    // RETRAIN STATUS
    // ========================================================

    public Task<RetrainStatusDto> GetRetrainStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<RetrainStatusDto>(
            "retrain/status",
            cancellationToken);
    }

    // ========================================================
    // MODEL REGISTRY
    // ========================================================

    public Task<ModelRegistryResponseDto> GetModelsAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<ModelRegistryResponseDto>(
            "models",
            cancellationToken);
    }

    // ========================================================
    // START RETRAIN
    // ========================================================

    public async Task StartRetrainAsync(
        byte[] csvBytes,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (csvBytes == null || csvBytes.Length == 0)
        {
            throw new InvalidOperationException(
                "Dữ liệu CSV không hợp lệ.");
        }

        using var form = new MultipartFormDataContent();

        using var fileContent = new ByteArrayContent(csvBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("text/csv");

        form.Add(
            fileContent,
            "file",
            fileName);

        using var response = await _httpClient.PostAsync(
            "retrain/csv",
            form,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                ReadApiError(content, (int)response.StatusCode));
        }

        var result =
            JsonSerializer.Deserialize<RetrainStartResponseDto>(
                content,
                JsonOptions);

        if (result == null || !result.Success)
        {
            throw new InvalidOperationException(
                result?.Message ??
                "Python API không xác nhận bắt đầu retrain.");
        }
    }

    // ========================================================
    // SELECT ACTIVE MODEL
    // ========================================================

    public async Task SelectModelAsync(
        string version,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException(
                "Phiên bản model không hợp lệ.");
        }

        using var response = await _httpClient.PostAsync(
            $"models/select/{Uri.EscapeDataString(version)}",
            null,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                ReadApiError(content, (int)response.StatusCode));
        }
    }

    // ========================================================
    // GENERIC GET
    // ========================================================

    private async Task<T> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            endpoint,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                ReadApiError(content, (int)response.StatusCode));
        }

        return JsonSerializer.Deserialize<T>(
            content,
            JsonOptions)
            ?? throw new InvalidOperationException(
                $"Không đọc được phản hồi Python API: {endpoint}");
    }

    // ========================================================
    // READ ERROR
    // ========================================================

    private static string ReadApiError(
        string content,
        int statusCode)
    {
        try
        {
            using var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty(
                "detail",
                out var detail))
            {
                return $"Python API lỗi {statusCode}: {detail}";
            }

            if (json.RootElement.TryGetProperty(
                "message",
                out var message))
            {
                return $"Python API lỗi {statusCode}: {message}";
            }
        }
        catch
        {
            // Không phải JSON.
        }

        return $"Python API lỗi {statusCode}: {content}";
    }
}