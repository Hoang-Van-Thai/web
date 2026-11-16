
using System.Data;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly RetailFlowDBContext _db;

        public InventoryService(RetailFlowDBContext db)
        {
            _db = db;
        }

        // =====================================================
        // 1. DANH SACH TON KHO
        // =====================================================
        public async Task<InventoryIndexViewModel> GetIndexAsync(
            string? keyword,
            int? warehouseId,
            string? stockStatus)
        {
            var model = new InventoryIndexViewModel
            {
                Keyword = keyword,
                WarehouseId = warehouseId,
                StockStatus = stockStatus
            };

            model.Warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.WarehouseName)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = x.WarehouseId.ToString(),
                    Text = x.WarehouseCode + " - " + x.WarehouseName
                })
                .ToListAsync();

            var query = _db.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.Product.ProductCode.Contains(keyword) ||
                    x.Product.ProductName.Contains(keyword));
            }

            if (stockStatus == "low")
            {
                query = query.Where(x =>
                    x.Quantity <= x.Product.MinStock);
            }
            else if (stockStatus == "normal")
            {
                query = query.Where(x =>
                    x.Quantity > x.Product.MinStock);
            }
            else if (stockStatus == "out")
            {
                query = query.Where(x => x.Quantity <= 0);
            }

            var balances = await query
                .OrderBy(x => x.Product.ProductName)
                .ThenBy(x => x.Warehouse.WarehouseName)
                .ToListAsync();

            model.Items = balances.Select(x => new InventoryRowViewModel
            {
                InventoryId = x.InventoryId,
                WarehouseId = x.WarehouseId,
                WarehouseName = x.Warehouse.WarehouseName,
                ProductId = x.ProductId,
                ProductCode = x.Product.ProductCode,
                ProductName = x.Product.ProductName,
                Unit = x.Product.Unit,
                Quantity = x.Quantity,
                MinStock = x.Product.MinStock,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            model.TotalProducts = model.Items
                .Select(x => x.ProductId)
                .Distinct()
                .Count();

            model.LowStockCount = model.Items
                .Count(x => x.IsLowStock);

            model.TotalQuantity = model.Items
                .Sum(x => x.Quantity);

            return model;
        }

        // =====================================================
        // 2. CHI TIET TON KHO THEO SAN PHAM
        // =====================================================
        public async Task<InventoryDetailViewModel?> GetDetailAsync(
            int productId)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (product == null)
                return null;

            var balances = await _db.InventoryBalances
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.Warehouse.WarehouseName)
                .ToListAsync();

            var transactions = await _db.InventoryTransactions
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.TransactionDate)
                .Take(100)
                .ToListAsync();

            return new InventoryDetailViewModel
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Unit = product.Unit,
                MinStock = product.MinStock,

                TotalQuantity = balances.Sum(x => x.Quantity),

                Warehouses = balances.Select(x => new InventoryRowViewModel
                {
                    InventoryId = x.InventoryId,
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.Warehouse.WarehouseName,
                    ProductId = x.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    Quantity = x.Quantity,
                    MinStock = product.MinStock,
                    UpdatedAt = x.UpdatedAt
                }).ToList(),

                Transactions = transactions
            };
        }

        // =====================================================
        // 3. TAO FORM NHAP / XUAT KHO
        // =====================================================
        public async Task<InventoryMovementViewModel> BuildMovementFormAsync(
            InventoryMovementViewModel? model = null)
        {
            model ??= new InventoryMovementViewModel();

            model.Warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.WarehouseName)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = x.WarehouseId.ToString(),
                    Text = x.WarehouseCode + " - " + x.WarehouseName
                })
                .ToListAsync();

            model.Products = await _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProductName)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = x.ProductId.ToString(),
                    Text = x.ProductCode + " - " + x.ProductName
                })
                .ToListAsync();

            if (model.WarehouseId > 0 && model.ProductId > 0)
            {
                model.CurrentStock = await _db.InventoryBalances
                    .Where(x =>
                        x.WarehouseId == model.WarehouseId &&
                        x.ProductId == model.ProductId)
                    .Select(x => (decimal?)x.Quantity)
                    .FirstOrDefaultAsync() ?? 0;
            }

            if (model.ProductId > 0)
            {
                model.ProductName = await _db.Products
                    .Where(x => x.ProductId == model.ProductId)
                    .Select(x => x.ProductName)
                    .FirstOrDefaultAsync() ?? "";
            }

            return model;
        }

        // =====================================================
        // 4. NHAP / XUAT KHO
        // =====================================================
        public async Task<(bool Success, string Message)> CreateMovementAsync(
            InventoryMovementViewModel model,
            int? userId)
        {
            if (model.WarehouseId <= 0)
                return (false, "Vui lòng chọn kho.");

            if (model.ProductId <= 0)
                return (false, "Vui lòng chọn sản phẩm.");

            if (model.Quantity <= 0)
                return (false, "Số lượng phải lớn hơn 0.");

            if (model.TransactionType != "IN" &&
                model.TransactionType != "OUT")
                return (false, "Loại giao dịch không hợp lệ.");

            if (model.Quantity > 999999999999999m)
                return (false, "Số lượng vượt giới hạn cho phép.");

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var warehouse = await _db.Warehouses
                    .FirstOrDefaultAsync(x =>
                        x.WarehouseId == model.WarehouseId &&
                        x.IsActive);

                if (warehouse == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "Kho không tồn tại hoặc đã ngừng hoạt động.");
                }

                var product = await _db.Products
                    .FirstOrDefaultAsync(x =>
                        x.ProductId == model.ProductId &&
                        x.IsActive);

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
                }

                var balance = await _db.InventoryBalances
                    .FirstOrDefaultAsync(x =>
                        x.WarehouseId == model.WarehouseId &&
                        x.ProductId == model.ProductId);

                decimal currentStock = balance?.Quantity ?? 0;

                if (model.TransactionType == "OUT" &&
                    model.Quantity > currentStock)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        $"Không đủ hàng trong kho. Tồn hiện tại: {currentStock:N3} {product.Unit}.");
                }

                if (balance == null)
                {
                    balance = new InventoryBalance
                    {
                        WarehouseId = model.WarehouseId,
                        ProductId = model.ProductId,
                        Quantity = 0,
                        UpdatedAt = DateTime.Now
                    };

                    _db.InventoryBalances.Add(balance);
                }

                if (model.TransactionType == "IN")
                    balance.Quantity += model.Quantity;
                else
                    balance.Quantity -= model.Quantity;

                balance.UpdatedAt = DateTime.Now;

                string prefix = model.TransactionType == "IN" ? "NK" : "XK";

                string referenceCode = string.IsNullOrWhiteSpace(model.ReferenceCode)
                    ? $"{prefix}{DateTime.Now:yyyyMMddHHmmssfff}"
                    : model.ReferenceCode.Trim();

                var inventoryTransaction = new InventoryTransaction
                {
                    WarehouseId = model.WarehouseId,
                    ProductId = model.ProductId,
                    UserId = userId,
                    TransactionType = model.TransactionType,
                    Quantity = model.Quantity,
                    ReferenceCode = referenceCode,
                    TransactionDate = DateTime.Now,
                    Note = model.Note?.Trim()
                };

                _db.InventoryTransactions.Add(inventoryTransaction);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true,
                    model.TransactionType == "IN"
                        ? "Nhập kho thành công."
                        : "Xuất kho thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // 5. LICH SU NHAP XUAT
        // =====================================================
        public async Task<InventoryHistoryViewModel> GetHistoryAsync(
            string? keyword,
            string? transactionType,
            int? warehouseId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var model = new InventoryHistoryViewModel
            {
                Keyword = keyword,
                TransactionType = transactionType,
                WarehouseId = warehouseId,
                FromDate = fromDate,
                ToDate = toDate
            };

            model.Warehouses = await _db.Warehouses
                .AsNoTracking()
                .OrderBy(x => x.WarehouseName)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = x.WarehouseId.ToString(),
                    Text = x.WarehouseCode + " - " + x.WarehouseName
                })
                .ToListAsync();

            var query = _db.InventoryTransactions
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.ReferenceCode.Contains(keyword) ||
                    x.Product.ProductCode.Contains(keyword) ||
                    x.Product.ProductName.Contains(keyword));
            }

            if (transactionType == "IN" || transactionType == "OUT")
            {
                query = query.Where(x =>
                    x.TransactionType == transactionType);
            }

            if (warehouseId.HasValue)
                query = query.Where(x =>
                    x.WarehouseId == warehouseId.Value);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.TransactionDate >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.TransactionDate < to);
            }

            model.Items = await query
                .OrderByDescending(x => x.TransactionDate)
                .Take(1000)
                .ToListAsync();

            return model;
        }
    }
}
