
using Microsoft.AspNetCore.Mvc.Rendering;
using web.Models.Entities;

namespace web.ViewModels
{
    public class InventoryIndexViewModel
    {
        public string? Keyword { get; set; }
        public int? WarehouseId { get; set; }
        public string? StockStatus { get; set; }

        public List<SelectListItem> Warehouses { get; set; } = new();
        public List<InventoryRowViewModel> Items { get; set; } = new();

        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class InventoryRowViewModel
    {
        public long InventoryId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public int MinStock { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsLowStock => Quantity <= MinStock;
    }

    public class InventoryMovementViewModel
    {
        public int WarehouseId { get; set; }
        public int ProductId { get; set; }

        // IN: nhập kho, OUT: xuất kho
        public string TransactionType { get; set; } = "IN";

        public decimal Quantity { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Note { get; set; }

        public decimal CurrentStock { get; set; }
        public string ProductName { get; set; } = "";

        public List<SelectListItem> Warehouses { get; set; } = new();
        public List<SelectListItem> Products { get; set; } = new();
    }

    public class InventoryDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public int MinStock { get; set; }

        public decimal TotalQuantity { get; set; }
        public List<InventoryRowViewModel> Warehouses { get; set; } = new();
        public List<InventoryTransaction> Transactions { get; set; } = new();
    }

    public class InventoryHistoryViewModel
    {
        public string? Keyword { get; set; }
        public string? TransactionType { get; set; }
        public int? WarehouseId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<SelectListItem> Warehouses { get; set; } = new();
        public List<InventoryTransaction> Items { get; set; } = new();
    }
}
