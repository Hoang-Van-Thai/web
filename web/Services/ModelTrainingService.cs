using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services;

public class ModelTrainingService : IModelTrainingService
{
    private const int MinHistoryDays = 100;
    private const long MaxCsvBytes = 100 * 1024 * 1024;

    private readonly RetailFlowDBContext _db;
    private readonly IModelTrainingApiClient _api;
    private readonly ILogger<ModelTrainingService> _logger;

    public ModelTrainingService(
        RetailFlowDBContext db,
        IModelTrainingApiClient api,
        ILogger<ModelTrainingService> logger)
    {
        _db = db;
        _api = api;
        _logger = logger;
    }

    // =====================================================
    // INDEX
    // =====================================================

    public async Task<ModelTrainingViewModel> GetIndexAsync(
        CancellationToken cancellationToken = default)
    {
        string? apiError = null;
        PythonHealthDto? health = null;
        RetrainStatusDto? status = null;

        try
        {
            health = await _api.GetHealthAsync(cancellationToken);
            status = await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            apiError = ex.Message;
            _logger.LogWarning(ex, "Cannot connect to Python API.");
        }

        var models = await _db.ModelVersions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ModelVersionItemViewModel
            {
                ModelVersionId = x.ModelVersionId,
                VersionName = x.VersionName,
                ModelName = x.ModelName,
                ModelFilePath = x.ModelFilePath,
                Framework = x.Framework,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                Description = x.Description,
                TrainingRunCount = x.ModelTrainingRuns.Count
            })
            .ToListAsync(cancellationToken);

        var runs = await _db.ModelTrainingRuns
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new ModelTrainingRunItemViewModel
            {
                TrainingRunId = x.TrainingRunId,
                TrainingName = x.TrainingName,
                Status = x.Status,

                VersionName = x.ModelVersion != null
                    ? x.ModelVersion.VersionName
                    : null,

                DataStartDate = x.DataStartDate,
                DataEndDate = x.DataEndDate,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                Mae = x.Mae,
                Rmse = x.Rmse,
                Rmsse = x.Rmsse,
                LogMessage = x.LogMessage
            })
            .ToListAsync(cancellationToken);

        var stores = await GetStoresAsync(cancellationToken);

        return new ModelTrainingViewModel
        {
            Models = models,
            TrainingRuns = runs,

            Stores = stores.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = x.StoreId.ToString(),
                Text = x.StoreName
            }).ToList(),

            TotalModels = models.Count,
            ActiveModels = models.Count(x => x.IsActive),
            TotalTrainingRuns = runs.Count,

            ApiAvailable =
                health != null &&
                string.Equals(
                    health.Status,
                    "healthy",
                    StringComparison.OrdinalIgnoreCase) &&
                health.ModelLoaded,

            ActivePythonModel = health?.ActiveModel,
            ApiError = apiError,

            RetrainStatus = MapStatus(status)
        };
    }

    // =====================================================
    // GET STORES
    // =====================================================

    public async Task<List<TrainingStoreOption>> GetStoresAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Stores
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.StoreName)
            .Select(x => new TrainingStoreOption
            {
                StoreId = x.StoreId,
                StoreName = x.StoreName
            })
            .ToListAsync(cancellationToken);
    }

    // =====================================================
    // PREVIEW SQL DATA
    // =====================================================

    public async Task<StoreTrainingPreviewViewModel> GetStorePreviewAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        if (storeId <= 0)
            throw new InvalidOperationException("Vui lòng chọn cửa hàng.");

        var store = await _db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.StoreId == storeId && x.IsActive,
                cancellationToken);

        if (store == null)
            throw new InvalidOperationException(
                "Không tìm thấy cửa hàng hoặc cửa hàng đã ngừng hoạt động.");

        var query = _db.SalesDailies
            .AsNoTracking()
            .Where(x => x.StoreId == storeId);

        var totalRecords = await query.CountAsync(cancellationToken);

        if (totalRecords == 0)
        {
            return new StoreTrainingPreviewViewModel
            {
                StoreId = storeId,
                StoreName = store.StoreName,
                TotalRecords = 0
            };
        }

        var dataStartDate = await query
            .MinAsync(x => x.SalesDate, cancellationToken);

        var dataEndDate = await query
            .MaxAsync(x => x.SalesDate, cancellationToken);

        var products = await query
            .GroupBy(x => new
            {
                x.ProductId,
                x.Product.ProductCode,
                x.Product.ProductName
            })
            .Select(g => new StoreTrainingProductViewModel
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                FirstSaleDate = g.Min(x => x.SalesDate),
                LastSaleDate = g.Max(x => x.SalesDate)
            })
            .OrderBy(x => x.ProductCode)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CalendarDays =
                product.LastSaleDate.DayNumber -
                product.FirstSaleDate.DayNumber + 1;
        }

        return new StoreTrainingPreviewViewModel
        {
            StoreId = storeId,
            StoreName = store.StoreName,

            TotalProducts = products.Count,
            EligibleProducts = products.Count(x => x.IsEligible),

            TotalRecords = totalRecords,

            DataStartDate = dataStartDate,
            DataEndDate = dataEndDate,

            Products = products
        };
    }

    // =====================================================
    // TRAIN FROM CSV
    // =====================================================

    public async Task<long> StartCsvTrainingAsync(
        IFormFile file,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Vui lòng chọn file CSV.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ file CSV.");
        }

        if (file.Length > MaxCsvBytes)
            throw new InvalidOperationException(
                "File CSV không được vượt quá 100 MB.");

        byte[] bytes;

        await using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, cancellationToken);
            bytes = stream.ToArray();
        }

        ValidateCsvHeader(bytes);

        await EnsureApiReadyAsync(cancellationToken);

        var run = new ModelTrainingRun
        {
            UserId = userId,
            TrainingName = $"Retrain CSV - {Path.GetFileName(file.FileName)}",
            StartedAt = DateTime.Now,
            Status = "Training",
            LogMessage = "Nguồn dữ liệu: CSV upload."
        };

        _db.ModelTrainingRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _api.StartRetrainAsync(
                bytes,
                Path.GetFileName(file.FileName),
                cancellationToken);

            return run.TrainingRunId;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.CompletedAt = DateTime.Now;
            run.LogMessage = ex.Message;

            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateCsvHeader(byte[] bytes)
    {
        var content = Encoding.UTF8.GetString(bytes);

        using var reader = new StringReader(content);

        var header = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(header))
            throw new InvalidOperationException("File CSV không có dữ liệu.");

        header = header.Trim().TrimStart('\uFEFF');

        var columns = header
            .Split(',')
            .Select(x => x.Trim().ToLowerInvariant())
            .ToArray();

        var required = new[]
        {
            "product_id",
            "date",
            "quantity",
            "price"
        };

        if (!columns.SequenceEqual(required))
        {
            throw new InvalidOperationException(
                "CSV không đúng định dạng. Header phải là: " +
                "product_id,date,quantity,price");
        }
    }

    // =====================================================
    // TRAIN FROM SQL SERVER
    // =====================================================

    public async Task<long> StartSqlTrainingAsync(
        int storeId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureApiReadyAsync(cancellationToken);

        // Lấy thống kê và danh sách sản phẩm thực tế.
        var preview = await GetStorePreviewAsync(
            storeId,
            cancellationToken);

        if (preview.TotalRecords == 0)
            throw new InvalidOperationException(
                "Cửa hàng chưa có dữ liệu SalesDaily.");

        if (preview.EligibleProducts == 0)
        {
            throw new InvalidOperationException(
                "Không có sản phẩm nào đủ tối thiểu 100 ngày dữ liệu. " +
                "Không thể retrain.");
        }

        var store = await _db.Stores
            .AsNoTracking()
            .FirstAsync(x => x.StoreId == storeId, cancellationToken);

        var eligibleProductIds = preview.Products
            .Where(x => x.IsEligible)
            .Select(x => x.ProductId)
            .ToList();

        var firstDate = preview.DataStartDate!.Value;
        var lastDate = preview.DataEndDate!.Value;

        // Lấy tất cả bản ghi của sản phẩm đủ điều kiện.
        var sales = await _db.SalesDailies
            .AsNoTracking()
            .Where(x =>
                x.StoreId == storeId &&
                eligibleProductIds.Contains(x.ProductId) &&
                x.SalesDate >= firstDate &&
                x.SalesDate <= lastDate)
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.SalesDate)
            .Select(x => new SqlSalesRow
            {
                ProductId = x.ProductId,
                Date = x.SalesDate,
                Quantity = x.QuantitySold,
                Revenue = x.Revenue,
                AveragePrice = x.AverageSellingPrice
            })
            .ToListAsync(cancellationToken);

        var productIds = eligibleProductIds;

        var products = await _db.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new ProductTrainingInfo
            {
                ProductId = x.ProductId,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                SellingPrice = x.SellingPrice
            })
            .ToDictionaryAsync(
                x => x.ProductId,
                cancellationToken);

        var csv = BuildSqlCsv(
            sales,
            products,
            storeId,
            lastDate,
            out var includedProducts,
            out var trainingStartDate);

        if (includedProducts == 0)
            throw new InvalidOperationException(
                "Không tạo được dữ liệu huấn luyện hợp lệ.");

        var bytes = new UTF8Encoding(true).GetBytes(csv);

        var run = new ModelTrainingRun
        {
            UserId = userId,
            TrainingName = $"Retrain SQL - {store.StoreName}",
            DataStartDate = trainingStartDate,
            DataEndDate = lastDate,
            StartedAt = DateTime.Now,
            Status = "Training",

            LogMessage =
                $"Nguồn dữ liệu: SQL Server / SalesDaily.{Environment.NewLine}" +
                $"Cửa hàng: {store.StoreName} (ID: {storeId}).{Environment.NewLine}" +
                $"Khoảng dữ liệu cửa hàng: {firstDate:yyyy-MM-dd} đến {lastDate:yyyy-MM-dd}.{Environment.NewLine}" +
                $"Tổng sản phẩm có bán hàng: {preview.TotalProducts}.{Environment.NewLine}" +
                $"Sản phẩm đủ điều kiện: {includedProducts}.{Environment.NewLine}" +
                $"Sản phẩm không đủ điều kiện: {preview.IneligibleProducts}.{Environment.NewLine}" +
                $"Tổng bản ghi SalesDaily: {preview.TotalRecords}.{Environment.NewLine}" +
                "Sản phẩm không đủ 100 ngày lịch đã bị loại khỏi CSV huấn luyện. " +
                "Ngày không có giao dịch trong chuỗi được điền quantity = 0."
        };

        _db.ModelTrainingRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _api.StartRetrainAsync(
                bytes,
                $"retailflow_store_{storeId}_{DateTime.Now:yyyyMMddHHmmss}.csv",
                cancellationToken);

            return run.TrainingRunId;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.CompletedAt = DateTime.Now;
            run.LogMessage += Environment.NewLine + ex.Message;

            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // =====================================================
    // BUILD CSV
    // =====================================================
    private static string BuildSqlCsv(
    List<SqlSalesRow> sales,
    Dictionary<int, ProductTrainingInfo> products,
    int storeId,
    DateOnly endDate,
    out int includedProducts,
    out DateOnly earliestDate)
    {
        var builder = new StringBuilder();

        builder.AppendLine("product_id,date,quantity,price");

        includedProducts = 0;
        earliestDate = endDate;

        var groups = sales
            .GroupBy(x => x.ProductId)
            .OrderBy(x => x.Key);

        foreach (var group in groups)
        {
            var rows = group
                .OrderBy(x => x.Date)
                .ToList();

            if (rows.Count == 0)
                continue;

            // Ngày bán đầu tiên và cuối cùng của chính sản phẩm này.
            var firstDate = rows.First().Date;
            var productEndDate = rows.Last().Date;

            // Tính số ngày lịch có dữ liệu của sản phẩm.
            var calendarDays =
                productEndDate.DayNumber -
                firstDate.DayNumber + 1;

            // Chỉ loại sản phẩm có dưới 100 ngày.
            // Không giới hạn số ngày được lấy.
            if (calendarDays < MinHistoryDays)
                continue;

            if (!products.TryGetValue(group.Key, out var product))
                continue;

            if (firstDate < earliestDate)
                earliestDate = firstDate;

            var seriesId = $"P{product.ProductCode}_S{storeId}";

            var lookup = rows.ToDictionary(x => x.Date);

            var firstKnownPrice = rows
                .Select(x => GetPrice(x, product.SellingPrice))
                .FirstOrDefault(x => x > 0);

            if (firstKnownPrice <= 0)
            {
                throw new InvalidOperationException(
                    $"Sản phẩm {product.ProductCode} không có giá hợp lệ.");
            }

            var lastPrice = firstKnownPrice;

            // Lấy toàn bộ lịch sử từ ngày đầu tiên
            // đến ngày cuối cùng của sản phẩm.
            for (
                var date = firstDate;
                date <= productEndDate;
                date = date.AddDays(1))
            {
                decimal quantity = 0;
                decimal price = lastPrice;

                if (lookup.TryGetValue(date, out var row))
                {
                    quantity = Math.Max(0, row.Quantity);

                    var currentPrice = GetPrice(
                        row,
                        product.SellingPrice);

                    if (currentPrice > 0)
                        price = currentPrice;
                }

                lastPrice = price;

                builder.Append(seriesId);
                builder.Append(',');
                builder.Append(date.ToString("yyyy-MM-dd"));
                builder.Append(',');
                builder.Append(quantity.ToString(
                    CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(price.ToString(
                    CultureInfo.InvariantCulture));

                builder.AppendLine();
            }

            includedProducts++;
        }

        return builder.ToString();
    }
    //private static string BuildSqlCsv(
    //    List<SqlSalesRow> sales,
    //    Dictionary<int, ProductTrainingInfo> products,
    //    int storeId,
    //    DateOnly endDate,
    //    out int includedProducts,
    //    out DateOnly earliestDate)
    //{
    //    var builder = new StringBuilder();

    //    builder.AppendLine("product_id,date,quantity,price");

    //    includedProducts = 0;
    //    earliestDate = endDate;

    //    var groups = sales
    //        .GroupBy(x => x.ProductId)
    //        .OrderBy(x => x.Key);

    //    foreach (var group in groups)
    //    {
    //        var rows = group
    //            .OrderBy(x => x.Date)
    //            .ToList();

    //        if (rows.Count == 0)
    //            continue;

    //        var firstDate = rows.First().Date;

    //        var calendarDays =
    //            endDate.DayNumber - firstDate.DayNumber + 1;

    //        if (calendarDays < MinHistoryDays)
    //            continue;

    //        if (firstDate < earliestDate)
    //            earliestDate = firstDate;

    //        if (!products.TryGetValue(group.Key, out var product))
    //            continue;

    //        var seriesId = $"P{product.ProductCode}_S{storeId}";

    //        var lookup = rows.ToDictionary(x => x.Date);

    //        var firstKnownPrice = rows
    //            .Select(x => GetPrice(x, product.SellingPrice))
    //            .FirstOrDefault(x => x > 0);

    //        if (firstKnownPrice <= 0)
    //        {
    //            throw new InvalidOperationException(
    //                $"Sản phẩm {product.ProductCode} không có giá hợp lệ.");
    //        }

    //        var lastPrice = firstKnownPrice;

    //        for (var date = firstDate; date <= endDate; date = date.AddDays(1))
    //        {
    //            decimal quantity = 0;
    //            decimal price = lastPrice;

    //            if (lookup.TryGetValue(date, out var row))
    //            {
    //                quantity = Math.Max(0, row.Quantity);

    //                var currentPrice = GetPrice(
    //                    row,
    //                    product.SellingPrice);

    //                if (currentPrice > 0)
    //                    price = currentPrice;
    //            }

    //            lastPrice = price;

    //            builder.Append(seriesId);
    //            builder.Append(',');
    //            builder.Append(date.ToString("yyyy-MM-dd"));
    //            builder.Append(',');
    //            builder.Append(quantity.ToString(
    //                CultureInfo.InvariantCulture));
    //            builder.Append(',');
    //            builder.Append(price.ToString(
    //                CultureInfo.InvariantCulture));
    //            builder.AppendLine();
    //        }

    //        includedProducts++;
    //    }

    //    return builder.ToString();
    //}

    private static decimal GetPrice(
        SqlSalesRow row,
        decimal defaultPrice)
    {
        if (row.Quantity > 0 && row.Revenue > 0)
            return row.Revenue / row.Quantity;

        if (row.AveragePrice > 0)
            return row.AveragePrice;

        return defaultPrice;
    }

    // =====================================================
    // STATUS
    // =====================================================

    public async Task<RetrainStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var status = await _api.GetRetrainStatusAsync(
            cancellationToken);

        await SyncTrainingResultAsync(
            status,
            cancellationToken);

        return status;
    }

    private async Task SyncTrainingResultAsync(
        RetrainStatusDto status,
        CancellationToken cancellationToken)
    {
        var run = await _db.ModelTrainingRuns
            .Where(x => x.Status == "Training")
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null)
            return;

        if (string.Equals(
            status.Status,
            "training",
            StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(
            status.Status,
            "failed",
            StringComparison.OrdinalIgnoreCase))
        {
            run.Status = "Failed";
            run.CompletedAt = DateTime.Now;
            run.LogMessage += Environment.NewLine +
                "Python retrain thất bại: " + status.Error;

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!string.Equals(
            status.Status,
            "completed",
            StringComparison.OrdinalIgnoreCase))
            return;

        var result = status.Metrics;

        if (result == null ||
            string.IsNullOrWhiteSpace(result.NewModel))
            return;

        var registry = await _api.GetModelsAsync(cancellationToken);

        var registryItem = registry.Models.FirstOrDefault(
            x => string.Equals(
                x.Version,
                result.NewModel,
                StringComparison.OrdinalIgnoreCase));

        if (registryItem == null)
            return;

        var modelVersion = await _db.ModelVersions
            .FirstOrDefaultAsync(
                x => x.VersionName == result.NewModel,
                cancellationToken);

        if (modelVersion == null)
        {
            modelVersion = new ModelVersion
            {
                VersionName = result.NewModel,
                ModelName = "DemandForecast",
                ModelFilePath = registryItem.ModelFile ?? string.Empty,
                Framework = "LightGBM",
                CreatedAt = DateTime.Now,
                Description = $"Nguồn: {registryItem.Source ?? "Retrain"}",
                IsActive = false
            };

            _db.ModelVersions.Add(modelVersion);
        }
        else
        {
            modelVersion.ModelFilePath =
                registryItem.ModelFile ?? modelVersion.ModelFilePath;
        }

        var activeVersion = registry.ActiveModel;

        var allVersions = await _db.ModelVersions
            .ToListAsync(cancellationToken);

        foreach (var item in allVersions)
        {
            item.IsActive = string.Equals(
                item.VersionName,
                activeVersion,
                StringComparison.OrdinalIgnoreCase);
        }

        modelVersion.IsActive = string.Equals(
            modelVersion.VersionName,
            activeVersion,
            StringComparison.OrdinalIgnoreCase);

        run.ModelVersion = modelVersion;
        run.Status = "Completed";
        run.CompletedAt = DateTime.Now;

        run.DataStartDate =
            ParseDate(result.TrainStartDate) ?? run.DataStartDate;

        run.DataEndDate =
            ParseDate(result.TrainEndDate) ?? run.DataEndDate;

        run.Mae = registryItem.ValidationMetrics?.Mae.HasValue == true
            ? Convert.ToDecimal(registryItem.ValidationMetrics.Mae.Value)
            : null;

        run.Rmse = registryItem.ValidationMetrics?.Rmse.HasValue == true
            ? Convert.ToDecimal(registryItem.ValidationMetrics.Rmse.Value)
            : null;

        run.Rmsse = null;

        var comparison = result.Comparison;

        run.LogMessage += Environment.NewLine +
            $"New model: {result.NewModel}" +
            Environment.NewLine +
            $"Active model: {result.ActiveModel}" +
            Environment.NewLine +
            $"Winner: {result.Winner}" +
            Environment.NewLine +
            $"Training rows: {result.TrainingRows}" +
            Environment.NewLine +
            $"Validation rows: {result.ValidationRows}" +
            Environment.NewLine +
            $"Products: {result.Products}" +
            Environment.NewLine +
            $"Old RMSE: {comparison?.OldRmse}" +
            Environment.NewLine +
            $"New RMSE: {comparison?.NewRmse}" +
            Environment.NewLine +
            $"Old MAE: {comparison?.OldMae}" +
            Environment.NewLine +
            $"New MAE: {comparison?.NewMae}";

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (DateOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result))
            return result;

        return null;
    }

    // =====================================================
    // ACTIVATE MODEL
    // =====================================================

    public async Task<bool> ActivateModelAsync(
        int modelVersionId,
        CancellationToken cancellationToken = default)
    {
        var selected = await _db.ModelVersions
            .FirstOrDefaultAsync(
                x => x.ModelVersionId == modelVersionId,
                cancellationToken);

        if (selected == null)
            return false;

        await _api.SelectModelAsync(
            selected.VersionName,
            cancellationToken);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            await _db.ModelVersions
                .Where(x => x.IsActive)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.IsActive,
                        false),
                    cancellationToken);

            selected.IsActive = true;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<ModelTrainingRunItemViewModel?>
        GetTrainingRunDetailsAsync(
            long id,
            CancellationToken cancellationToken = default)
    {
        return await _db.ModelTrainingRuns
            .AsNoTracking()
            .Where(x => x.TrainingRunId == id)
            .Select(x => new ModelTrainingRunItemViewModel
            {
                TrainingRunId = x.TrainingRunId,
                TrainingName = x.TrainingName,
                Status = x.Status,

                VersionName = x.ModelVersion != null
                    ? x.ModelVersion.VersionName
                    : null,

                DataStartDate = x.DataStartDate,
                DataEndDate = x.DataEndDate,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                Mae = x.Mae,
                Rmse = x.Rmse,
                Rmsse = x.Rmsse,
                LogMessage = x.LogMessage
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    // =====================================================
    // API READY
    // =====================================================

    private async Task EnsureApiReadyAsync(
        CancellationToken cancellationToken)
    {
        var health = await _api.GetHealthAsync(cancellationToken);

        if (!string.Equals(
            health.Status,
            "healthy",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Python API chưa hoạt động.");
        }

        if (!health.ModelLoaded)
        {
            throw new InvalidOperationException(
                "Python API chưa load model gốc. Không thể retrain.");
        }

        var status = await _api.GetRetrainStatusAsync(cancellationToken);

        if (string.Equals(
            status.Status,
            "training",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Python đang retrain một phiên khác. Vui lòng chờ.");
        }
    }

    private static RetrainStatusViewModel MapStatus(
        RetrainStatusDto? status)
    {
        if (status == null)
            return new RetrainStatusViewModel();

        var comparison =
            status.Metrics?.Comparison ?? status.Comparison;

        return new RetrainStatusViewModel
        {
            Status = status.Status,
            Message = status.Message,
            Progress = status.Progress,
            Error = status.Error,

            ActiveModel = status.Metrics?.ActiveModel,
            Winner = status.Metrics?.Winner ?? comparison?.Winner,

            OldRmse = comparison?.OldRmse,
            NewRmse = comparison?.NewRmse,
            OldMae = comparison?.OldMae,
            NewMae = comparison?.NewMae,

            RmseImprovementPercent =
                comparison?.RmseImprovementPercent,

            MaeImprovementPercent =
                comparison?.MaeImprovementPercent,

            TrainingRows =
                status.Metrics?.TrainingRows ?? status.TrainingRows,

            ValidationRows =
                status.Metrics?.ValidationRows ?? 0,

            Products =
                status.Metrics?.Products ?? status.Products
        };
    }

    // =====================================================
    // INTERNAL MODELS
    // =====================================================

    private class SqlSalesRow
    {
        public int ProductId { get; set; }
        public DateOnly Date { get; set; }
        public decimal Quantity { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    private class ProductTrainingInfo
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
    }
}