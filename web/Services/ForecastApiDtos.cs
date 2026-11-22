using System.Text.Json.Serialization;

namespace web.Services;

// Một ngày lịch sử bán hàng
public class ForecastHistoryItem
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public double Quantity { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }
}

// Request gửi sang Python API
public class ForecastApiRequest
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public List<ForecastHistoryItem> History { get; set; } = new();

    [JsonPropertyName("days")]
    public int Days { get; set; } = 7;
}

// Một ngày dự báo
public class ForecastApiItem
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("prediction")]
    public double Prediction { get; set; }
}

// Thống kê kết quả dự báo
public class ForecastApiStatistics
{
    [JsonPropertyName("minimum")]
    public double Minimum { get; set; }

    [JsonPropertyName("maximum")]
    public double Maximum { get; set; }

    [JsonPropertyName("mean")]
    public double Mean { get; set; }

    [JsonPropertyName("total")]
    public double Total { get; set; }
}

// Response nhận từ Python API
public class ForecastApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("history_days")]
    public int HistoryDays { get; set; }

    [JsonPropertyName("forecast_days")]
    public int ForecastDays { get; set; }

    [JsonPropertyName("last_history_date")]
    public string? LastHistoryDate { get; set; }

    [JsonPropertyName("forecast")]
    public List<ForecastApiItem> Forecast { get; set; } = new();

    [JsonPropertyName("statistics")]
    public ForecastApiStatistics? Statistics { get; set; }
}

// Response GET /health
public class ForecastApiHealthResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("model_loaded")]
    public bool ModelLoaded { get; set; }

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("model_features")]
    public int ModelFeatures { get; set; }

    [JsonPropertyName("retrain_status")]
    public string? RetrainStatus { get; set; }
}

// Dữ liệu lựa chọn sản phẩm và cửa hàng
public class ForecastOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

// Kết quả dự báo sau khi lưu vào SQL Server
public class ForecastExecutionResult
{
    public long ForecastRunId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string? ActiveModel { get; set; }

    public int HistoryDays { get; set; }

    public int ForecastDays { get; set; }

    public DateTime LastHistoryDate { get; set; }

    public ForecastApiStatistics Statistics { get; set; } = new();

    public List<ForecastApiItem> Forecast { get; set; } = new();
}