
namespace web.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }

        public int TodayOrders { get; set; }
        public int MonthOrders { get; set; }

        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockProductCount { get; set; }

        public List<DashboardRevenueViewModel> DailyRevenue { get; set; } = new();
        public List<DashboardOrderViewModel> RecentOrders { get; set; } = new();
        public List<DashboardProductViewModel> TopProducts { get; set; } = new();
        public List<DashboardLowStockViewModel> LowStockProducts { get; set; } = new();
    }

    public class DashboardRevenueViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardOrderViewModel
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string StoreName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
    }

    public class DashboardProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardLowStockViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal CurrentStock { get; set; }
        public decimal MinStock { get; set; }
    }
}
