using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class ModelTrainingViewModel
    {
        public List<ModelVersionItemViewModel> Models { get; set; } = new();

        public List<ModelTrainingRunItemViewModel> TrainingRuns { get; set; } = new();

        public int TotalModels { get; set; }
        public int ActiveModels { get; set; }
        public int TotalTrainingRuns { get; set; }
    }

    public class ModelVersionItemViewModel
    {
        public int ModelVersionId { get; set; }

        public string VersionName { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public string ModelFilePath { get; set; } = string.Empty;

        public string Framework { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Description { get; set; }

        public int TrainingRunCount { get; set; }
    }

    public class ModelTrainingRunItemViewModel
    {
        public long TrainingRunId { get; set; }

        public string? TrainingName { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? VersionName { get; set; }

        public DateOnly? DataStartDate { get; set; }

        public DateOnly? DataEndDate { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public decimal? Mae { get; set; }

        public decimal? Rmse { get; set; }

        public decimal? Rmsse { get; set; }

        public string? LogMessage { get; set; }
    }

    public class ModelVersionCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phiên bản.")]
        [StringLength(50)]
        [Display(Name = "Tên phiên bản")]
        public string VersionName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên model.")]
        [StringLength(100)]
        [Display(Name = "Tên model")]
        public string ModelName { get; set; } = "Global LightGBM";

        [Required(ErrorMessage = "Vui lòng nhập đường dẫn model.")]
        [StringLength(500)]
        [Display(Name = "Đường dẫn model")]
        public string ModelFilePath { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập framework.")]
        [StringLength(50)]
        [Display(Name = "Framework")]
        public string Framework { get; set; } = "LightGBM";

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Tên phiên huấn luyện")]
        [StringLength(150)]
        public string? TrainingName { get; set; }

        [Display(Name = "Ngày bắt đầu dữ liệu")]
        public DateOnly? DataStartDate { get; set; }

        [Display(Name = "Ngày kết thúc dữ liệu")]
        public DateOnly? DataEndDate { get; set; }

        [Display(Name = "MAE")]
        [Range(0, double.MaxValue)]
        public decimal? Mae { get; set; }

        [Display(Name = "RMSE")]
        [Range(0, double.MaxValue)]
        public decimal? Rmse { get; set; }

        [Display(Name = "RMSSE")]
        [Range(0, double.MaxValue)]
        public decimal? Rmsse { get; set; }

        [Display(Name = "Thông tin huấn luyện")]
        public string? LogMessage { get; set; }
    }
}