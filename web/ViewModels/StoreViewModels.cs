
using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class StoreIndexViewModel
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }

        public int TotalStores { get; set; }
        public int ActiveStores { get; set; }
        public int InactiveStores { get; set; }

        public List<StoreRowViewModel> Items { get; set; } = new();
    }

    public class StoreRowViewModel
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int WarehouseCount { get; set; }
        public int OrderCount { get; set; }
    }

    public class StoreFormViewModel
    {
        public int StoreId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã cửa hàng.")]
        [StringLength(30, ErrorMessage = "Mã cửa hàng không được vượt quá 30 ký tự.")]
        [Display(Name = "Mã cửa hàng")]
        public string StoreCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")]
        [StringLength(150, ErrorMessage = "Tên cửa hàng không được vượt quá 150 ký tự.")]
        [Display(Name = "Tên cửa hàng")]
        public string StoreName { get; set; } = "";

        [StringLength(300, ErrorMessage = "Địa chỉ không được vượt quá 300 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;
    }

    public class StoreDetailsViewModel
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int WarehouseCount { get; set; }
        public int OrderCount { get; set; }

        public List<StoreWarehouseViewModel> Warehouses { get; set; } = new();
        public List<StoreOrderViewModel> RecentOrders { get; set; } = new();
    }

    public class StoreWarehouseViewModel
    {
        public int WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = "";
        public string WarehouseName { get; set; } = "";
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }

    public class StoreOrderViewModel
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
    }
}
