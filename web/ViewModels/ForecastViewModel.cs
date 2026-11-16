using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace web.ViewModels
{
    public class ForecastIndexViewModel
    {
        public List<ForecastRunListItemViewModel> ForecastRuns { get; set; } = new();
    }

    public class ForecastCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn cửa hàng.")]
        [Display(Name = "Cửa hàng")]
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        [Display(Name = "Ngày bắt đầu dự báo")]
        public DateOnly StartDate { get; set; } =
            DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required]
        [Range(7, 28, ErrorMessage = "Chỉ hỗ trợ 7, 14 hoặc 28 ngày.")]
        [Display(Name = "Số ngày dự báo")]
        public int HorizonDays { get; set; } = 7;

        [StringLength(150)]
        [Display(Name = "Tên phiên dự báo")]
        public string? ForecastName { get; set; }

        public List<SelectListItem> Stores { get; set; } = new();
        public string? ActiveModelName { get; set; }
    }

    public class ForecastRunListItemViewModel
    {
        public long ForecastRunId { get; set; }

        public string? ForecastName { get; set; }

        public string ForecastType { get; set; } = string.Empty;

        public string ModelVersionName { get; set; } = string.Empty;

        public string StoreNames { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int HorizonDays { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public int ProductCount { get; set; }
    }

    public class ForecastDetailViewModel
    {
        public long ForecastRunId { get; set; }

        public string? ForecastName { get; set; }

        public string ModelVersionName { get; set; } = string.Empty;

        public string StoreName { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int HorizonDays { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalPredictedQuantity { get; set; }

        public List<ForecastProductDailyViewModel> Details { get; set; } = new();
    }

    public class ForecastProductDailyViewModel
    {
        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public DateOnly ForecastDate { get; set; }

        public decimal PredictedQuantity { get; set; }

        public decimal? ActualQuantity { get; set; }
    }
}