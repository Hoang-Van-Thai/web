
using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm")]
        [StringLength(50)]
        [Display(Name = "Mã sản phẩm")]
        public string ProductCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [StringLength(50)]
        [Display(Name = "Đơn vị tính")]
        public string? Unit { get; set; }

        [Range(0, 999999999999, ErrorMessage = "Giá nhập không hợp lệ")]
        [Display(Name = "Giá nhập")]
        public decimal CostPrice { get; set; }

        [Range(0, 999999999999, ErrorMessage = "Giá bán không hợp lệ")]
        [Display(Name = "Giá bán")]
        public decimal SellingPrice { get; set; }

        [Range(0, 100000000, ErrorMessage = "Tồn kho tối thiểu không hợp lệ")]
        [Display(Name = "Tồn kho tối thiểu")]
        public int MinStock { get; set; }

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }
}
