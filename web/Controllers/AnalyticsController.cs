using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly RetailFlowDBContext _db;

        public AnalyticsController(RetailFlowDBContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var startDate = (fromDate ?? DateTime.Today.AddDays(-29)).Date;
            var endDate = (toDate ?? DateTime.Today).Date;

            if (startDate > endDate)
            {
                TempData["Error"] =
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.";

                return View(new AnalyticsViewModel
                {
                    FromDate = startDate,
                    ToDate = endDate
                });
            }

            // Ngày kết thúc được tính trọn ngày.
            var endExclusive = endDate.AddDays(1);

            var ordersQuery = _db.Orders
                .AsNoTracking()
                .Where(x =>
                    x.Status == "Completed" &&
                    x.OrderDate >= startDate &&
                    x.OrderDate < endExclusive);

            var totalRevenue = await ordersQuery
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

            var totalOrders = await ordersQuery.CountAsync();

            var totalCustomers = await ordersQuery
                .Where(x => x.CustomerId != null)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();

            var averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0;

            // Doanh thu theo ngày.
            var orderDates = await ordersQuery
                .Select(x => new
                {
                    x.OrderDate,
                    x.TotalAmount
                })
                .ToListAsync();

            var dailyRevenue = orderDates
                .GroupBy(x => x.OrderDate.Date)
                .Select(g => new DailyRevenueViewModel
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Sản phẩm bán chạy.
            var topProducts = await _db.OrderDetails
                .AsNoTracking()
                .Where(x =>
                    x.Order.Status == "Completed" &&
                    x.Order.OrderDate >= startDate &&
                    x.Order.OrderDate < endExclusive)
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.Product.ProductCode,
                    x.Product.ProductName
                })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToListAsync();

            // Doanh thu theo cửa hàng.
            var storeRevenues = await ordersQuery
                .GroupBy(x => new
                {
                    x.StoreId,
                    x.Store.StoreCode,
                    x.Store.StoreName
                })
                .Select(g => new StoreRevenueViewModel
                {
                    StoreId = g.Key.StoreId,
                    StoreCode = g.Key.StoreCode,
                    StoreName = g.Key.StoreName,
                    Revenue = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var model = new AnalyticsViewModel
            {
                FromDate = startDate,
                ToDate = endDate,

                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalCustomers = totalCustomers,
                AverageOrderValue = averageOrderValue,

                DailyRevenue = dailyRevenue,
                TopProducts = topProducts,
                StoreRevenues = storeRevenues
            };

            return View(model);
        }
    }
}