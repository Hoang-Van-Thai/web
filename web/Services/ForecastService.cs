using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class ForecastService : IForecastService
    {
        private readonly RetailFlowDBContext _db;
        private readonly IForecastApiClient _apiClient;

        public ForecastService(
            RetailFlowDBContext db,
            IForecastApiClient apiClient)
        {
            _db = db;
            _apiClient = apiClient;
        }

        public async Task<ForecastIndexViewModel> GetIndexAsync()
        {
            var runs = await _db.ForecastRuns
                .AsNoTracking()
                .Include(x => x.ModelVersion)
                .Include(x => x.ForecastDetails)
                    .ThenInclude(x => x.Store)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = runs.Select(run => new ForecastRunListItemViewModel
            {
                ForecastRunId = run.ForecastRunId,
                ForecastName = run.ForecastName,
                ForecastType = run.ForecastType,
                ModelVersionName = run.ModelVersion.VersionName,
                StoreNames = string.Join(
                    ", ",
                    run.ForecastDetails
                        .Select(x => x.Store.StoreName)
                        .Distinct()
                ),
                StartDate = run.StartDate,
                EndDate = run.EndDate,
                HorizonDays = run.HorizonDays,
                CreatedAt = run.CreatedAt,
                Status = run.Status,
                ProductCount = run.ForecastDetails
                    .Select(x => x.ProductId)
                    .Distinct()
                    .Count()
            }).ToList();

            return new ForecastIndexViewModel
            {
                ForecastRuns = result
            };
        }

        public async Task<ForecastCreateViewModel> GetCreateViewModelAsync()
        {
            var activeModel = await _db.ModelVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);

            var stores = await _db.Stores
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StoreName)
                .Select(x => new SelectListItem
                {
                    Value = x.StoreId.ToString(),
                    Text = x.StoreCode + " - " + x.StoreName
                })
                .ToListAsync();

            return new ForecastCreateViewModel
            {
                Stores = stores,
                ActiveModelName = activeModel == null
                    ? null
                    : $"{activeModel.ModelName} ({activeModel.VersionName})"
            };
        }

        public async Task<long> CreateForecastAsync(
            ForecastCreateViewModel model,
            int? userId,
            CancellationToken cancellationToken = default)
        {
            if (model.HorizonDays != 7 &&
                model.HorizonDays != 14 &&
                model.HorizonDays != 28)
            {
                throw new InvalidOperationException(
                    "Số ngày dự báo chỉ được là 7, 14 hoặc 28."
                );
            }

            var modelVersion = await _db.ModelVersions
                .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);

            if (modelVersion == null)
            {
                throw new InvalidOperationException(
                    "Chưa có Model nào được kích hoạt. " +
                    "Vui lòng vào ModelTraining để đăng ký và kích hoạt Model."
                );
            }

            var store = await _db.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.StoreId == model.StoreId && x.IsActive,
                    cancellationToken
                );

            if (store == null)
            {
                throw new InvalidOperationException(
                    "Cửa hàng không tồn tại hoặc đã ngừng hoạt động."
                );
            }

            var startDate = model.StartDate;
            var endDate = startDate.AddDays(model.HorizonDays - 1);

            var forecastRun = new ForecastRun
            {
                ModelVersionId = modelVersion.ModelVersionId,
                UserId = userId,
                ForecastName = string.IsNullOrWhiteSpace(model.ForecastName)
                    ? $"Dự báo {store.StoreCode} - {startDate:dd/MM/yyyy}"
                    : model.ForecastName.Trim(),
                ForecastType = "Product",
                StartDate = startDate,
                EndDate = endDate,
                HorizonDays = model.HorizonDays,
                CreatedAt = DateTime.Now,
                Status = "Processing"
            };

            _db.ForecastRuns.Add(forecastRun);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var historyStartDate = startDate.AddDays(-90);
                var historyEndDate = startDate.AddDays(-1);

                var products = await _db.Products
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ProductId)
                    .ToListAsync(cancellationToken);

                var sales = await _db.SalesDailies
                    .AsNoTracking()
                    .Where(x =>
                        x.StoreId == store.StoreId &&
                        x.SalesDate >= historyStartDate &&
                        x.SalesDate <= historyEndDate
                    )
                    .ToListAsync(cancellationToken);

                var productSales = sales
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToDictionary(y => y.SalesDate, y => y)
                    );

                var history = new List<ForecastHistoryItem>();

                foreach (var product in products)
                {
                    // Không gửi sản phẩm chưa từng có lịch sử bán.
                    if (!productSales.TryGetValue(
                        product.ProductId,
                        out var dailySales))
                    {
                        continue;
                    }

                    decimal lastKnownPrice = product.SellingPrice;

                    for (
                        var date = historyStartDate;
                        date <= historyEndDate;
                        date = date.AddDays(1)
                    )
                    {
                        decimal quantity = 0;

                        if (dailySales.TryGetValue(date, out var sale))
                        {
                            quantity = sale.QuantitySold;

                            if (sale.AverageSellingPrice > 0)
                            {
                                lastKnownPrice = sale.AverageSellingPrice;
                            }
                        }

                        history.Add(new ForecastHistoryItem
                        {
                            Product = product.ProductCode,
                            Store = store.StoreCode,
                            Date = date,
                            Sales = quantity,
                            Price = lastKnownPrice
                        });
                    }
                }

                if (history.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Không có dữ liệu SalesDaily trong 90 ngày trước " +
                        "ngày dự báo. Hãy bổ sung lịch sử bán hàng trước."
                    );
                }

                var apiRequest = new ForecastApiRequest
                {
                    Horizon = model.HorizonDays,
                    StartDate = startDate,
                    History = history
                };

                var apiResponse = await _apiClient.PredictAsync(
                    apiRequest,
                    cancellationToken
                );

                var productMap = products.ToDictionary(
                    x => x.ProductCode,
                    x => x
                );

                var forecastDetails = new List<ForecastDetail>();

                foreach (var prediction in apiResponse.Predictions)
                {
                    if (!string.Equals(
                        prediction.Store,
                        store.StoreCode,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!productMap.TryGetValue(
                        prediction.Product,
                        out var product))
                    {
                        throw new InvalidOperationException(
                            $"API trả về ProductCode không tồn tại: " +
                            $"{prediction.Product}"
                        );
                    }

                    if (prediction.Date < startDate ||
                        prediction.Date > endDate)
                    {
                        throw new InvalidOperationException(
                            $"Ngày dự báo {prediction.Date} nằm ngoài khoảng yêu cầu."
                        );
                    }

                    forecastDetails.Add(new ForecastDetail
                    {
                        ForecastRunId = forecastRun.ForecastRunId,
                        ProductId = product.ProductId,
                        StoreId = store.StoreId,
                        ForecastDate = prediction.Date,
                        PredictedQuantity = Math.Max(0, prediction.Sales),
                        ActualQuantity = await _db.SalesDailies
                            .Where(x =>
                                x.ProductId == product.ProductId &&
                                x.StoreId == store.StoreId &&
                                x.SalesDate == prediction.Date
                            )
                            .Select(x => (decimal?)x.QuantitySold)
                            .FirstOrDefaultAsync(cancellationToken)
                    });
                }

                if (forecastDetails.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Không có kết quả dự báo phù hợp với sản phẩm và cửa hàng."
                    );
                }

                _db.ForecastDetails.AddRange(forecastDetails);

                forecastRun.Status = "Completed";

                await _db.SaveChangesAsync(cancellationToken);

                return forecastRun.ForecastRunId;
            }
            catch (Exception ex)
            {
                forecastRun.Status = "Failed";

                await _db.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException(
                    $"Không thể tạo dự báo: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<ForecastDetailViewModel?> GetDetailsAsync(long id)
        {
            var run = await _db.ForecastRuns
                .AsNoTracking()
                .Include(x => x.ModelVersion)
                .FirstOrDefaultAsync(x => x.ForecastRunId == id);

            if (run == null)
                return null;

            var details = await _db.ForecastDetails
                .AsNoTracking()
                .Where(x => x.ForecastRunId == id)
                .Include(x => x.Product)
                .Include(x => x.Store)
                .OrderBy(x => x.ForecastDate)
                .ThenBy(x => x.Product.ProductName)
                .ToListAsync();

            return new ForecastDetailViewModel
            {
                ForecastRunId = run.ForecastRunId,
                ForecastName = run.ForecastName,
                ModelVersionName = run.ModelVersion.VersionName,
                StoreName = string.Join(
                    ", ",
                    details.Select(x => x.Store.StoreName).Distinct()
                ),
                StartDate = run.StartDate,
                EndDate = run.EndDate,
                HorizonDays = run.HorizonDays,
                Status = run.Status,
                TotalPredictedQuantity = details.Sum(x => x.PredictedQuantity),

                Details = details.Select(x => new ForecastProductDailyViewModel
                {
                    ProductCode = x.Product.ProductCode,
                    ProductName = x.Product.ProductName,
                    Unit = x.Product.Unit ?? "",
                    ForecastDate = x.ForecastDate,
                    PredictedQuantity = x.PredictedQuantity,
                    ActualQuantity = x.ActualQuantity
                }).ToList()
            };
        }
    }
}