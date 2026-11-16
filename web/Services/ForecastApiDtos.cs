namespace web.Services
{
    public class ForecastApiRequest
    {
        public int Horizon { get; set; }
        public DateOnly StartDate { get; set; }
        public List<ForecastHistoryItem> History { get; set; } = new();
    }

    public class ForecastHistoryItem
    {
        public string Product { get; set; } = string.Empty;
        public string Store { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public decimal Sales { get; set; }
        public decimal Price { get; set; }
    }

    public class ForecastApiResponse
    {
        public List<ForecastPredictionItem> Predictions { get; set; } = new();
    }

    public class ForecastPredictionItem
    {
        public string Product { get; set; } = string.Empty;
        public string Store { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public decimal Sales { get; set; }
    }
}