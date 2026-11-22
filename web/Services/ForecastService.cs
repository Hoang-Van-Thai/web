using System.Globalization;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;

namespace web.Services;

public class ForecastService : IForecastService
{
    private const int RequiredHistoryDays = 63;

    private readonly RetailFlowDBContext _db;
    private readonly IForecastApiClient _apiClient;
    private readonly ILogger<ForecastService> _logger;

    public ForecastService(
        RetailFlowDBContext db,
        IForecastApiClient apiClient,
        ILogger<ForecastService> logger)
    {
        _db = db;
        _apiClient = apiClient;
        _logger = logger;
    }

    // Lấy danh sách sản phẩm
    public async Task<List<ForecastOption>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => new ForecastOption
            {
                Id = p.ProductId,
                Name = p.ProductName
            })
            .ToListAsync(cancellationToken);
    }

    // Lấy danh sách cửa hàng
    public async Task<List<ForecastOption>> GetStoresAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Stores
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.StoreName)
            .Select(s => new ForecastOption
            {
                Id = s.StoreId,
                Name = s.StoreName
            })
            .ToListAsync(cancellationToken);
    }

    // Kiểm tra Python API
    public Task<ForecastApiHealthResponse> CheckApiAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetHealthAsync(cancellationToken);
    }

    // Thực hiện dự báo
    public async Task<ForecastExecutionResult> RunForecastAsync(
        int productId,
        int storeId,
        int days,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
            throw new ArgumentException("Sản phẩm không hợp lệ.");

        if (storeId <= 0)
            throw new ArgumentException("Cửa hàng không hợp lệ.");

        if (days is not (7 or 14 or 28))
            throw new ArgumentException(
                "Số ngày dự báo chỉ được là 7, 14 hoặc 28.");

        // 1. Kiểm tra API và model
        var health = await _apiClient.GetHealthAsync(
            cancellationToken);

        if (!string.Equals(
                health.Status,
                "healthy",
                StringComparison.OrdinalIgnoreCase)
            || !health.ModelLoaded)
        {
            throw new InvalidOperationException(
                "Python API chưa sẵn sàng hoặc chưa load được model.");
        }

        // 2. Lấy sản phẩm
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ProductId == productId && p.IsActive,
                cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy sản phẩm hoặc sản phẩm đã ngừng hoạt động.");
        }

        // 3. Lấy cửa hàng
        var store = await _db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.StoreId == storeId && s.IsActive,
                cancellationToken);

        if (store == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy cửa hàng hoặc cửa hàng đã ngừng hoạt động.");
        }

        // 4. Lấy lịch sử bán hàng đúng sản phẩm và cửa hàng
        var salesRows = await _db.SalesDailies
            .AsNoTracking()
            .Where(x =>
                x.ProductId == productId &&
                x.StoreId == storeId)
            .OrderBy(x => x.SalesDate)
            .ToListAsync(cancellationToken);

        if (salesRows.Count == 0)
        {
            throw new InvalidOperationException(
                "Sản phẩm này chưa có lịch sử bán hàng tại cửa hàng đã chọn.");
        }

        // Ngày bán hàng cuối cùng
        var lastDate = salesRows.Max(x => x.SalesDate);

        // Cần 63 ngày lịch
        var firstRequiredDate = lastDate.AddDays(-62);

        // Không tự tạo dữ liệu giả để đủ 63 ngày
        if (salesRows.Min(x => x.SalesDate) > firstRequiredDate)
        {
            var availableDays =
                lastDate.DayNumber -
                salesRows.Min(x => x.SalesDate).DayNumber + 1;

            throw new InvalidOperationException(
                $"Chỉ có {availableDays} ngày lịch sử. " +
                "API yêu cầu ít nhất 63 ngày lịch sử. " +
                "Hãy bổ sung dữ liệu SalesDaily trước khi dự báo.");
        }

        // Gom dữ liệu theo ngày
        var salesByDate = salesRows
            .GroupBy(x => x.SalesDate)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.SalesDailyId).Last());

        // 5. Tạo đúng 63 ngày lịch sử
        var history = new List<ForecastHistoryItem>();

        double previousPrice =
            Convert.ToDouble(product.SellingPrice);

        for (int i = 0; i < RequiredHistoryDays; i++)
        {
            var date = firstRequiredDate.AddDays(i);

            if (salesByDate.TryGetValue(date, out var row))
            {
                var rowPrice =
                    Convert.ToDouble(row.AverageSellingPrice);

                if (rowPrice > 0)
                    previousPrice = rowPrice;

                history.Add(new ForecastHistoryItem
                {
                    ProductId = productId.ToString(
                        CultureInfo.InvariantCulture),

                    Date = date.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                    Quantity = Convert.ToDouble(row.QuantitySold),

                    Price = rowPrice > 0
                        ? rowPrice
                        : previousPrice
                });
            }
            else
            {
                // Ngày không có dữ liệu bán hàng
                history.Add(new ForecastHistoryItem
                {
                    ProductId = productId.ToString(
                        CultureInfo.InvariantCulture),

                    Date = date.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),

                    Quantity = 0,

                    Price = previousPrice
                });
            }
        }

        // 6. Tạo request gửi Python API
        var request = new ForecastApiRequest
        {
            ProductId = productId.ToString(
                CultureInfo.InvariantCulture),

            History = history,

            Days = days
        };

        // 7. Gọi Python API
        var apiResult = await _apiClient.ForecastAsync(
            request,
            cancellationToken);

        if (apiResult.Forecast.Count != days)
        {
            throw new InvalidOperationException(
                $"API trả về {apiResult.Forecast.Count}/{days} ngày dự báo.");
        }

        // 8. Kiểm tra và chuẩn hóa kết quả
        var parsedForecast = apiResult.Forecast.Select(item =>
        {
            if (!DateOnly.TryParseExact(
                item.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var forecastDate))
            {
                throw new InvalidOperationException(
                    $"Ngày dự báo không hợp lệ: {item.Date}");
            }

            if (double.IsNaN(item.Prediction) ||
                double.IsInfinity(item.Prediction))
            {
                throw new InvalidOperationException(
                    "Model trả về giá trị dự báo không hợp lệ.");
            }

            return new
            {
                Item = item,
                Date = forecastDate,
                Quantity = Math.Max(
                    0,
                    Convert.ToDecimal(item.Prediction))
            };
        })
        .OrderBy(x => x.Date)
        .ToList();

        if (parsedForecast
            .Select(x => x.Date)
            .Distinct()
            .Count() != parsedForecast.Count)
        {
            throw new InvalidOperationException(
                "API trả về ngày dự báo bị trùng.");
        }

        // 9. Xác định phiên bản model
        var activeModelName =
            apiResult.ActiveModel
            ?? health.ActiveModel
            ?? "M5-BASE-001";

        activeModelName = activeModelName.Trim();

        if (string.IsNullOrWhiteSpace(activeModelName))
            activeModelName = "M5-BASE-001";

        if (activeModelName.Length > 50)
            activeModelName = activeModelName[..50];

        // 10. Lưu kết quả vào database
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var modelVersion = await _db.ModelVersions
                .FirstOrDefaultAsync(
                    x => x.VersionName == activeModelName,
                    cancellationToken);

            if (modelVersion == null)
            {
                modelVersion = new ModelVersion
                {
                    VersionName = activeModelName,
                    ModelName = "RetailFlow LightGBM",
                    ModelFilePath = "Managed by Python Forecast API",
                    Framework = "LightGBM",
                    IsActive = true,
                    Description =
                        "Model version được nhận từ Python Forecast API."
                };

                _db.ModelVersions.Add(modelVersion);
            }

            // Chỉ đánh dấu phiên bản model hiện tại là Active
            var oldVersions = await _db.ModelVersions
                .Where(x =>
                    x.IsActive &&
                    x.VersionName != activeModelName)
                .ToListAsync(cancellationToken);

            foreach (var version in oldVersions)
            {
                version.IsActive = false;
            }

            modelVersion.IsActive = true;

            await _db.SaveChangesAsync(cancellationToken);

            // Tạo phiên dự báo
            var forecastRun = new ForecastRun
            {
                ModelVersionId = modelVersion.ModelVersionId,
                UserId = userId,

                ForecastName =
                    $"Dự báo {product.ProductName} - {store.StoreName}",

                ForecastType = "Product",

                StartDate = parsedForecast.First().Date,

                EndDate = parsedForecast.Last().Date,

                HorizonDays = days,

                Status = "Completed"
            };

            _db.ForecastRuns.Add(forecastRun);

            await _db.SaveChangesAsync(cancellationToken);

            // Lưu chi tiết dự báo từng ngày
            foreach (var item in parsedForecast)
            {
                var detail = new ForecastDetail
                {
                    ForecastRunId = forecastRun.ForecastRunId,

                    ProductId = productId,

                    StoreId = storeId,

                    ForecastDate = item.Date,

                    PredictedQuantity =
                        decimal.Round(item.Quantity, 3),

                    LowerBound = null,

                    UpperBound = null,

                    ActualQuantity = null
                };

                _db.ForecastDetails.Add(detail);
            }

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // 11. Tạo thống kê
            var predictions = parsedForecast
                .Select(x => x.Item.Prediction)
                .ToList();

            var statistics = apiResult.Statistics
                ?? new ForecastApiStatistics
                {
                    Minimum = predictions.Min(),
                    Maximum = predictions.Max(),
                    Mean = predictions.Average(),
                    Total = predictions.Sum()
                };

            return new ForecastExecutionResult
            {
                ForecastRunId = forecastRun.ForecastRunId,

                ProductName = product.ProductName,

                StoreName = store.StoreName,

                ActiveModel = activeModelName,

                HistoryDays = history.Count,

                ForecastDays = parsedForecast.Count,

                LastHistoryDate = lastDate.ToDateTime(
                    TimeOnly.MinValue),

                Statistics = statistics,

                Forecast = parsedForecast
                    .Select(x => x.Item)
                    .ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Lấy lại kết quả dự báo đã lưu
    public async Task<ForecastExecutionResult?> GetForecastDetailsAsync(
        long forecastRunId,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.ForecastRuns
            .AsNoTracking()
            .Include(x => x.ModelVersion)
            .Include(x => x.ForecastDetails)
                .ThenInclude(x => x.Product)
            .Include(x => x.ForecastDetails)
                .ThenInclude(x => x.Store)
            .FirstOrDefaultAsync(
                x => x.ForecastRunId == forecastRunId,
                cancellationToken);

        if (run == null)
            return null;

        var details = run.ForecastDetails
            .OrderBy(x => x.ForecastDate)
            .ToList();

        var predictions = details
            .Select(x => Convert.ToDouble(x.PredictedQuantity))
            .ToList();

        var forecastItems = details.Select(x =>
            new ForecastApiItem
            {
                ProductId = x.ProductId.ToString(
                    CultureInfo.InvariantCulture),

                Date = x.ForecastDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                Prediction = Convert.ToDouble(
                    x.PredictedQuantity)
            })
            .ToList();

        var statistics = new ForecastApiStatistics();

        if (predictions.Count > 0)
        {
            statistics.Minimum = predictions.Min();
            statistics.Maximum = predictions.Max();
            statistics.Mean = predictions.Average();
            statistics.Total = predictions.Sum();
        }

        var firstDetail = details.FirstOrDefault();

        return new ForecastExecutionResult
        {
            ForecastRunId = run.ForecastRunId,

            ProductName =
                firstDetail?.Product?.ProductName
                ?? run.ForecastName
                ?? "Dự báo sản phẩm",

            StoreName =
                firstDetail?.Store?.StoreName
                ?? string.Empty,

            ActiveModel = run.ModelVersion?.VersionName,

            HistoryDays = RequiredHistoryDays,

            ForecastDays = run.HorizonDays,

            LastHistoryDate = run.StartDate
                .AddDays(-1)
                .ToDateTime(TimeOnly.MinValue),

            Statistics = statistics,

            Forecast = forecastItems
        };
    }
}