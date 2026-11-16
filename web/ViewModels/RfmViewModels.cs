namespace web.ViewModels
{
    public class RfmIndexViewModel
    {
        public string? SegmentFilter { get; set; }

        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }

        public int Champions { get; set; }
        public int LoyalCustomers { get; set; }
        public int PotentialCustomers { get; set; }
        public int AtRiskCustomers { get; set; }
        public int HibernatingCustomers { get; set; }

        public List<RfmCustomerViewModel> Items { get; set; } = new();
    }

    public class RfmCustomerViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }

        public DateTime? LastOrderDate { get; set; }

        public int? Recency { get; set; }
        public int Frequency { get; set; }
        public decimal Monetary { get; set; }

        public int RScore { get; set; }
        public int FScore { get; set; }
        public int MScore { get; set; }

        public string RfmCode { get; set; } = "";
        public string Segment { get; set; } = "";
    }
}