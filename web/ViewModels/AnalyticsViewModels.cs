namespace web.ViewModels
{
    public class AnalyticsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<DailyRevenueViewModel> DailyRevenue { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<StoreRevenueViewModel> StoreRevenues { get; set; } = new();
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class StoreRevenueViewModel
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = "";
        public string StoreName { get; set; } = "";
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}