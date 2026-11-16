using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Controllers
{
    public class RfmController : Controller
    {
        private readonly RetailFlowDBContext _db;

        public RfmController(RetailFlowDBContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? segment)
        {
            var today = DateTime.Today;

            // Lấy thống kê đơn hàng hoàn thành theo khách hàng.
            var customerStats = await _db.Orders
                .AsNoTracking()
                .Where(x =>
                    x.CustomerId.HasValue &&
                    x.Status == "Completed")
                .GroupBy(x => x.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    LastOrderDate = g.Max(x => x.OrderDate),
                    Frequency = g.Count(),
                    Monetary = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var statsDictionary = customerStats
                .ToDictionary(x => x.CustomerId);

            // Lấy tất cả khách hàng, kể cả khách chưa từng mua.
            var customers = await _db.Customers
                .AsNoTracking()
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerCode,
                    x.FullName,
                    x.Phone
                })
                .ToListAsync();

            var rows = new List<RfmCustomerViewModel>();

            foreach (var customer in customers)
            {
                if (statsDictionary.TryGetValue(
                    customer.CustomerId, out var stats))
                {
                    var recency = Math.Max(
                        0,
                        (today - stats.LastOrderDate.Date).Days);

                    rows.Add(new RfmCustomerViewModel
                    {
                        CustomerId = customer.CustomerId,
                        CustomerCode = customer.CustomerCode,
                        FullName = customer.FullName,
                        Phone = customer.Phone,

                        LastOrderDate = stats.LastOrderDate,
                        Recency = recency,
                        Frequency = stats.Frequency,
                        Monetary = stats.Monetary
                    });
                }
                else
                {
                    rows.Add(new RfmCustomerViewModel
                    {
                        CustomerId = customer.CustomerId,
                        CustomerCode = customer.CustomerCode,
                        FullName = customer.FullName,
                        Phone = customer.Phone,

                        LastOrderDate = null,
                        Recency = null,
                        Frequency = 0,
                        Monetary = 0,

                        RScore = 1,
                        FScore = 1,
                        MScore = 1,

                        RfmCode = "111",
                        Segment = "Chưa mua hàng"
                    });
                }
            }

            // Chỉ chấm điểm các khách hàng đã có đơn hoàn thành.
            var purchasedCustomers = rows
                .Where(x => x.LastOrderDate.HasValue)
                .ToList();

            ApplyScores(purchasedCustomers);

            // Phân nhóm khách hàng.
            foreach (var item in purchasedCustomers)
            {
                item.RfmCode =
                    $"{item.RScore}{item.FScore}{item.MScore}";

                item.Segment = GetSegment(
                    item.RScore,
                    item.FScore,
                    item.MScore);
            }

            // Lọc theo nhóm nếu người dùng chọn.
            var filteredRows = string.IsNullOrWhiteSpace(segment)
                ? rows
                : rows.Where(x =>
                    x.Segment == segment).ToList();

            var model = new RfmIndexViewModel
            {
                SegmentFilter = segment,

                TotalCustomers = rows.Count,

                ActiveCustomers = purchasedCustomers.Count,

                InactiveCustomers = rows.Count -
                    purchasedCustomers.Count,

                Champions = rows.Count(x => x.Segment == "Champions"),

                LoyalCustomers = rows.Count(x => x.Segment == "Loyal"),

                PotentialCustomers = rows.Count(x => x.Segment == "Potential"),

                AtRiskCustomers = rows.Count(x => x.Segment == "At Risk"),

                HibernatingCustomers = rows.Count(
                    x => x.Segment == "Hibernating"),

                Items = filteredRows
                    .OrderByDescending(x => x.Monetary)
                    .ThenBy(x => x.Recency ?? int.MaxValue)
                    .ToList()
            };

            return View(model);
        }
        private static void ApplyScores(
    List<RfmCustomerViewModel> customers)
        {
            if (!customers.Any())
                return;

            // Recency: giá trị thấp hơn thì điểm cao hơn.
            var recencyValues = customers
                .Select(x => x.Recency!.Value)
                .ToList();

            // Frequency: chuyển int sang decimal.
            var frequencyValues = customers
                .Select(x => (decimal)x.Frequency)
                .ToList();

            // Monetary: giá trị cao hơn thì điểm cao hơn.
            var monetaryValues = customers
                .Select(x => x.Monetary)
                .ToList();

            foreach (var customer in customers)
            {
                customer.RScore = ScoreLowerIsBetter(
                    customer.Recency!.Value,
                    recencyValues);

                customer.FScore = ScoreHigherIsBetter(
                    (decimal)customer.Frequency,
                    frequencyValues);

                customer.MScore = ScoreHigherIsBetter(
                    customer.Monetary,
                    monetaryValues);
            }
        }
        

        private static int ScoreHigherIsBetter(
            decimal value,
            List<decimal> values)
        {
            var distinctValues = values
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (distinctValues.Count <= 1)
                return 3;

            var rank = distinctValues.IndexOf(value);

            var percentile = (double)rank /
                (distinctValues.Count - 1);

            return Math.Clamp(
                (int)Math.Ceiling(percentile * 4) + 1,
                1,
                5);
        }

        private static int ScoreLowerIsBetter(
            int value,
            List<int> values)
        {
            var distinctValues = values
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (distinctValues.Count <= 1)
                return 3;

            var rank = distinctValues.IndexOf(value);

            var percentile = (double)rank /
                (distinctValues.Count - 1);

            // Giá trị Recency càng thấp thì điểm càng cao.
            return Math.Clamp(
                5 - (int)Math.Ceiling(percentile * 4),
                1,
                5);
        }

        private static string GetSegment(
            int r,
            int f,
            int m)
        {
            if (r >= 4 && f >= 4)
                return "Champions";

            if (r >= 3 && f >= 4)
                return "Loyal";

            if (r >= 4 && f <= 2)
                return "New Customers";

            if (r >= 3 && f >= 3)
                return "Potential";

            if (r <= 2 && f >= 3)
                return "At Risk";

            if (r <= 2 && f <= 2)
                return "Hibernating";

            return "Needs Attention";
        }
    }
}