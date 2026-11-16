using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class CustomerIndexViewModel
    {
        public string? Keyword { get; set; }

        public int TotalCustomers { get; set; }
        public int CustomersWithOrders { get; set; }
        public int CustomersWithoutOrders { get; set; }

        public List<CustomerRowViewModel> Items { get; set; } = new();
    }

    public class CustomerRowViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class CustomerFormViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khách hàng.")]
        [StringLength(50)]
        public string CustomerCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string FullName { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }

        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }

        public List<CustomerOrderRowViewModel> RecentOrders { get; set; } = new();
    }

    public class CustomerOrderRowViewModel
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
    }
}