
//using Microsoft.EntityFrameworkCore;
//using web.Models.Entities;
//using web.ViewModels;

//namespace web.Services
//{
//    public class DashboardService : IDashboardService
//    {
//        private readonly RetailFlowDBContext _db;

//        public DashboardService(RetailFlowDBContext db)
//        {
//            _db = db;
//        }

//        public async Task<DashboardViewModel> GetDashboardAsync()
//        {
//            var today = DateTime.Today;
//            var tomorrow = today.AddDays(1);
//            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
//            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

//            var completedOrders = _db.Orders
//                .AsNoTracking()
//                .Where(x => x.Status == "Completed");

//            var todayOrdersQuery = completedOrders.Where(x =>
//                x.OrderDate >= today &&
//                x.OrderDate < tomorrow);

//            var monthOrdersQuery = completedOrders.Where(x =>
//                x.OrderDate >= firstDayOfMonth &&
//                x.OrderDate < firstDayOfNextMonth);

//            var model = new DashboardViewModel
//            {
//                TodayRevenue = await todayOrdersQuery
//                    .SumAsync(x => (decimal?)x.TotalAmount) ?? 0,

//                MonthRevenue = await monthOrdersQuery
//                    .SumAsync(x => (decimal?)x.TotalAmount) ?? 0,

//                TodayOrders = await todayOrdersQuery.CountAsync(),

//                MonthOrders = await monthOrdersQuery.CountAsync(),

//                TotalProducts = await _db.Products
//                    .CountAsync(x => x.IsActive),

//                TotalCustomers = await _db.Customers.CountAsync()
//            };

//            // Tồn kho thấp: tổng tồn của sản phẩm ở tất cả kho
//            var stockByProduct = _db.InventoryBalances
//                .AsNoTracking()
//                .GroupBy(x => x.ProductId)
//                .Select(g => new
//                {
//                    ProductId = g.Key,
//                    CurrentStock = g.Sum(x => x.Quantity)
//                });

//            var lowStockQuery =
//                from product in _db.Products.AsNoTracking()
//                join stock in stockByProduct
//                    on product.ProductId equals stock.ProductId into stockJoin
//                from stock in stockJoin.DefaultIfEmpty()
//                where product.IsActive &&
//                      (stock == null
//                          ? product.MinStock > 0
//                          : stock.CurrentStock <= product.MinStock)
//                select new DashboardLowStockViewModel
//                {
//                    ProductId = product.ProductId,
//                    ProductCode = product.ProductCode,
//                    ProductName = product.ProductName,
//                    CurrentStock = stock == null ? 0 : stock.CurrentStock,
//                    MinStock = product.MinStock
//                };

//            model.LowStockProductCount = await lowStockQuery.CountAsync();

//            model.LowStockProducts = await lowStockQuery
//                .OrderBy(x => x.CurrentStock)
//                .Take(8)
//                .ToListAsync();

//            // Doanh thu 7 ngày gần nhất, bao gồm cả ngày không có đơn
//            var sevenDaysAgo = today.AddDays(-6);

//            var revenueRows = await completedOrders
//                .Where(x =>
//                    x.OrderDate >= sevenDaysAgo &&
//                    x.OrderDate < tomorrow)
//                .GroupBy(x => x.OrderDate.Date)
//                .Select(g => new
//                {
//                    Date = g.Key,
//                    Revenue = g.Sum(x => x.TotalAmount)
//                })
//                .ToListAsync();

//            var revenueDictionary = revenueRows.ToDictionary(
//                x => x.Date,
//                x => x.Revenue
//            );

//            model.DailyRevenue = Enumerable.Range(0, 7)
//                .Select(i =>
//                {
//                    var date = sevenDaysAgo.AddDays(i);

//                    return new DashboardRevenueViewModel
//                    {
//                        Date = date,
//                        Revenue = revenueDictionary.TryGetValue(date, out var revenue)
//                            ? revenue
//                            : 0
//                    };
//                })
//                .ToList();

//            // 8 đơn hàng gần đây
//            model.RecentOrders = await completedOrders
//                .AsNoTracking()
//                .OrderByDescending(x => x.OrderDate)
//                .Take(8)
//                .Select(x => new DashboardOrderViewModel
//                {
//                    OrderId = x.OrderId,
//                    OrderCode = x.OrderCode,
//                    CustomerName = x.Customer != null
//                        ? x.Customer.FullName
//                        : "Khách lẻ",
//                    StoreName = x.Store.StoreName,
//                    OrderDate = x.OrderDate,
//                    TotalAmount = x.TotalAmount,
//                    Status = x.Status
//                })
//                .ToListAsync();

//            // 5 sản phẩm bán chạy theo số lượng đã bán
//            var topProductsQuery = _db.OrderDetails
//                .AsNoTracking()
//                .Where(x => x.Order.Status == "Completed")
//                .GroupBy(x => new
//                {
//                    x.ProductId,
//                    x.Product.ProductCode,
//                    x.Product.ProductName
//                })
//                .Select(g => new DashboardProductViewModel
//                {
//                    ProductId = g.Key.ProductId,
//                    ProductCode = g.Key.ProductCode,
//                    ProductName = g.Key.ProductName,
//                    QuantitySold = g.Sum(x => x.Quantity),
//                    Revenue = g.Sum(x => x.LineTotal)
//                });

//            model.TopProducts = await topProductsQuery
//                .OrderByDescending(x => x.QuantitySold)
//                .Take(5)
//                .ToListAsync();

//            return model;
//        }
//    }
//}

using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly RetailFlowDBContext _db;

        public DashboardService(RetailFlowDBContext db)
        {
            _db = db;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            // =========================================
            // 1. KHOẢNG THỜI GIAN
            // =========================================

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var firstDayOfMonth = new DateTime(
                today.Year,
                today.Month,
                1
            );

            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            var sevenDaysAgo = today.AddDays(-6);

            // =========================================
            // 2. ĐƠN HÀNG ĐÃ HOÀN THÀNH
            // =========================================

            var completedOrders = _db.Orders
                .AsNoTracking()
                .Where(x => x.Status == "Completed");

            var todayOrdersQuery = completedOrders
                .Where(x =>
                    x.OrderDate >= today &&
                    x.OrderDate < tomorrow
                );

            var monthOrdersQuery = completedOrders
                .Where(x =>
                    x.OrderDate >= firstDayOfMonth &&
                    x.OrderDate < firstDayOfNextMonth
                );

            // =========================================
            // 3. TỔNG QUAN DASHBOARD
            // =========================================

            var model = new DashboardViewModel
            {
                // Doanh thu hôm nay
                TodayRevenue = await todayOrdersQuery
                    .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m,

                // Doanh thu tháng này
                MonthRevenue = await monthOrdersQuery
                    .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m,

                // Số đơn hàng hôm nay
                TodayOrders = await todayOrdersQuery.CountAsync(),

                // Số đơn hàng tháng này
                MonthOrders = await monthOrdersQuery.CountAsync(),

                // Tổng sản phẩm đang hoạt động
                TotalProducts = await _db.Products
                    .CountAsync(x => x.IsActive),

                // Tổng khách hàng
                TotalCustomers = await _db.Customers
                    .CountAsync()
            };

            // =========================================
            // 4. SẢN PHẨM SẮP HẾT HÀNG
            // =========================================
            // Tổng tồn kho của sản phẩm ở tất cả các kho.
            // Nếu không có bản ghi tồn kho thì mặc định bằng 0.

            var lowStockQuery = _db.Products
                .AsNoTracking()
                .Where(product =>
                    product.IsActive &&
                    product.MinStock > 0 &&
                    (
                        _db.InventoryBalances
                            .Where(stock =>
                                stock.ProductId == product.ProductId
                            )
                            .Sum(stock => (decimal?)stock.Quantity) ?? 0m
                    ) <= product.MinStock
                )
                .Select(product => new DashboardLowStockViewModel
                {
                    ProductId = product.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,

                    CurrentStock =
                        _db.InventoryBalances
                            .Where(stock =>
                                stock.ProductId == product.ProductId
                            )
                            .Sum(stock => (decimal?)stock.Quantity) ?? 0m,

                    MinStock = product.MinStock
                });

            // Tổng số sản phẩm sắp hết hàng
            model.LowStockProductCount = await lowStockQuery.CountAsync();

            // Danh sách 8 sản phẩm sắp hết hàng
            model.LowStockProducts = await lowStockQuery
                .OrderBy(x => x.CurrentStock)
                .Take(8)
                .ToListAsync();

            // =========================================
            // 5. DOANH THU 7 NGÀY GẦN NHẤT
            // =========================================

            var revenueRows = await completedOrders
                .Where(x =>
                    x.OrderDate >= sevenDaysAgo &&
                    x.OrderDate < tomorrow
                )
                .GroupBy(x => x.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            // Chuyển kết quả thành Dictionary
            var revenueDictionary = revenueRows.ToDictionary(
                x => x.Date,
                x => x.Revenue
            );

            // Tạo đủ 7 ngày, kể cả ngày không có doanh thu
            model.DailyRevenue = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = sevenDaysAgo.AddDays(i);

                    return new DashboardRevenueViewModel
                    {
                        Date = date,

                        Revenue = revenueDictionary.TryGetValue(
                            date,
                            out var revenue
                        )
                            ? revenue
                            : 0m
                    };
                })
                .ToList();

            // =========================================
            // 6. 8 ĐƠN HÀNG GẦN ĐÂY
            // =========================================

            model.RecentOrders = await completedOrders
                .OrderByDescending(x => x.OrderDate)
                .Take(8)
                .Select(x => new DashboardOrderViewModel
                {
                    OrderId = x.OrderId,
                    OrderCode = x.OrderCode,

                    CustomerName = x.Customer != null
                        ? x.Customer.FullName
                        : "Khách lẻ",

                    StoreName = x.Store.StoreName,
                    OrderDate = x.OrderDate,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToListAsync();

            // =========================================
            // 7. TOP 5 SẢN PHẨM BÁN CHẠY
            // =========================================

            var topProductsQuery = _db.OrderDetails
                .AsNoTracking()
                .Where(x => x.Order.Status == "Completed")
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.Product.ProductCode,
                    x.Product.ProductName
                })
                .Select(g => new DashboardProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,

                    // Tổng số lượng đã bán
                    QuantitySold = g.Sum(x => x.Quantity),

                    // Tổng doanh thu
                    Revenue = g.Sum(x => x.LineTotal)
                });

            model.TopProducts = await topProductsQuery
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            // =========================================
            // 8. TRẢ KẾT QUẢ
            // =========================================

            return model;
        }
    }
}
