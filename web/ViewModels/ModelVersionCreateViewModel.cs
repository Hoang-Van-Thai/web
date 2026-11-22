using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace web.ViewModels;

public class ModelVersionCreateViewModel
{
    // =========================================
    // MODEL INFORMATION
    // =========================================

    [Required(ErrorMessage = "Vui lòng nhập tên phiên bản.")]
    [StringLength(50)]
    [Display(Name = "Tên phiên bản")]
    public string VersionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên model.")]
    [StringLength(100)]
    [Display(Name = "Tên model")]
    public string ModelName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Đường dẫn model")]
    public string? ModelFilePath { get; set; }

    [StringLength(50)]
    [Display(Name = "Framework")]
    public string? Framework { get; set; }

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // =========================================
    // TRAINING INFORMATION
    // =========================================

    [StringLength(150)]
    [Display(Name = "Tên phiên huấn luyện")]
    public string? TrainingName { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu dữ liệu")]
    public DateTime? DataStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc dữ liệu")]
    public DateTime? DataEndDate { get; set; }

    // Các thuộc tính cũ để tương thích nếu Controller
    // đăng ký model offline vẫn đang sử dụng chúng.

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    // =========================================
    // EVALUATION METRICS
    // =========================================

    [Range(0, double.MaxValue)]
    [Display(Name = "MAE")]
    public decimal? Mae { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "RMSE")]
    public decimal? Rmse { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "RMSSE")]
    public decimal? Rmsse { get; set; }

    // =========================================
    // LOG
    // =========================================

    [Display(Name = "Thông tin huấn luyện")]
    public string? LogMessage { get; set; }

    // =========================================
    // OPTIONAL OLD PROPERTIES
    // =========================================

    public List<SelectListItem> Products { get; set; } = new();

    public List<SelectListItem> Stores { get; set; } = new();

    public int? ProductId { get; set; }

    public int? StoreId { get; set; }
}