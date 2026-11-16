
using System.ComponentModel.DataAnnotations;
using web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace web.ViewModels
{
    // Dữ liệu một sản phẩm trong đơn hàng
    public class OrderItemInputViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }

        [Range(typeof(decimal), "0.001", "999999999",
            ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal Quantity { get; set; } = 1;

        [Range(typeof(decimal), "0", "999999999999",
            ErrorMessage = "Giảm giá không hợp lệ")]
        public decimal DiscountAmount { get; set; } = 0;
    }

    // Form tạo đơn hàng
    public class OrderFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng")]
        [StringLength(50)]
        public string OrderCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn cửa hàng")]
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kho")]
        public int WarehouseId { get; set; }

        public int? CustomerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Range(typeof(decimal), "0", "9999999999999999",
            ErrorMessage = "Giảm giá không hợp lệ")]
        public decimal DiscountAmount { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public List<OrderItemInputViewModel> Items { get; set; } = new();

        public List<SelectListItem> Stores { get; set; } = new();

        public List<SelectListItem> Warehouses { get; set; } = new();

        public List<SelectListItem> Customers { get; set; } = new();

        public List<SelectListItem> Products { get; set; } = new();
    }

    // Danh sách đơn hàng
    public class OrderIndexViewModel
    {
        public IReadOnlyList<Order> Orders { get; set; }
            = Array.Empty<Order>();

        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }
    }
}
