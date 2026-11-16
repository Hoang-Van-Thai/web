
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class OrderService : IOrderService
    {
        private readonly RetailFlowDBContext _db;

        public OrderService(RetailFlowDBContext db)
        {
            _db = db;
        }

        // =====================================================
        // 1. DANH SACH DON HANG
        // =====================================================

        public async Task<(IReadOnlyList<Order> Items, int Total)>
            SearchAsync(
                string? keyword,
                string? status,
                DateTime? fromDate,
                DateTime? toDate,
                int page,
                int pageSize)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Include(x => x.Store)
                .Include(x => x.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.OrderCode.Contains(keyword) ||
                    (x.Customer != null &&
                     x.Customer.FullName.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.OrderDate < endDate);
            }

            int total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.OrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        // =====================================================
        // 2. CHI TIET DON HANG
        // =====================================================

        public async Task<Order?> GetAsync(long id)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(x => x.Store)
                .Include(x => x.Customer)
                .Include(x => x.User)
                .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.OrderId == id);
        }

        // =====================================================
        // 3. DU LIEU CHO FORM TAO DON
        // =====================================================

        public async Task<OrderFormViewModel> BuildFormAsync(
            OrderFormViewModel? model = null)
        {
            model ??= new OrderFormViewModel();

            model.Stores = await _db.Stores
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StoreName)
                .Select(x => new SelectListItem
                {
                    Value = x.StoreId.ToString(),
                    Text = x.StoreName
                })
                .ToListAsync();

            model.Warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.StoreId == model.StoreId)
                .OrderBy(x => x.WarehouseName)
                .Select(x => new SelectListItem
                {
                    Value = x.WarehouseId.ToString(),
                    Text = x.WarehouseName
                })
                .ToListAsync();

            model.Customers = await _db.Customers
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .Select(x => new SelectListItem
                {
                    Value = x.CustomerId.ToString(),
                    Text = x.FullName + " - " + x.CustomerCode
                })
                .ToListAsync();

            model.Products = await _db.Products
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProductName)
                .Select(x => new SelectListItem
                {
                    Value = x.ProductId.ToString(),
                    Text = x.ProductName + " - " +
                           x.ProductCode + " (" +
                           x.SellingPrice.ToString() + ")"
                })
                .ToListAsync();

            if (model.Items == null || model.Items.Count == 0)
            {
                model.Items = new List<OrderItemInputViewModel>
                {
                    new OrderItemInputViewModel
                    {
                        Quantity = 1
                    }
                };
            }

            return model;
        }

        // =====================================================
        // 4. TAO DON HANG
        // =====================================================

        public async Task<(bool Success, string Message, long? OrderId)>
            CreateAsync(OrderFormViewModel model, int? userId)
        {
            if (string.IsNullOrWhiteSpace(model.OrderCode))
                return (false, "Mã đơn hàng không được để trống.", null);

            if (model.Items == null || model.Items.Count == 0)
                return (false, "Đơn hàng phải có ít nhất một sản phẩm.", null);

            if (model.Items.Any(x =>
                x.ProductId <= 0 ||
                x.Quantity <= 0 ||
                x.DiscountAmount < 0))
            {
                return (false, "Thông tin sản phẩm không hợp lệ.", null);
            }

            if (model.DiscountAmount < 0)
                return (false, "Giảm giá không hợp lệ.", null);

            // Không cho chọn trùng sản phẩm trong một đơn.
            if (model.Items.GroupBy(x => x.ProductId).Any(g => g.Count() > 1))
            {
                return (false,
                    "Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn hàng.",
                    null);
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Kiểm tra mã đơn
                bool codeExists = await _db.Orders
                    .AnyAsync(x => x.OrderCode == model.OrderCode.Trim());

                if (codeExists)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Mã đơn hàng đã tồn tại.",
                        null);
                }

                // Kiểm tra cửa hàng
                var store = await _db.Stores
                    .FirstOrDefaultAsync(x =>
                        x.StoreId == model.StoreId &&
                        x.IsActive);

                if (store == null)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Cửa hàng không tồn tại hoặc đã ngừng hoạt động.",
                        null);
                }

                // Kiểm tra kho
                var warehouse = await _db.Warehouses
                    .FirstOrDefaultAsync(x =>
                        x.WarehouseId == model.WarehouseId &&
                        x.StoreId == model.StoreId &&
                        x.IsActive);

                if (warehouse == null)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Kho không hợp lệ.",
                        null);
                }

                // Kiểm tra khách hàng
                if (model.CustomerId.HasValue)
                {
                    bool customerExists = await _db.Customers
                        .AnyAsync(x =>
                            x.CustomerId == model.CustomerId.Value);

                    if (!customerExists)
                    {
                        await transaction.RollbackAsync();

                        return (false,
                            "Khách hàng không tồn tại.",
                            null);
                    }
                }

                decimal subTotal = 0;
                decimal totalDiscount = model.DiscountAmount;

                var orderDetails = new List<OrderDetail>();

                foreach (var item in model.Items)
                {
                    // Lấy sản phẩm từ database
                    var product = await _db.Products
                        .FirstOrDefaultAsync(x =>
                            x.ProductId == item.ProductId &&
                            x.IsActive);

                    if (product == null)
                    {
                        await transaction.RollbackAsync();

                        return (false,
                            "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.",
                            null);
                    }

                    // Lấy giá bán từ database
                    decimal unitPrice = product.SellingPrice;

                    decimal lineSubTotal =
                        unitPrice * item.Quantity;

                    if (item.DiscountAmount > lineSubTotal)
                    {
                        await transaction.RollbackAsync();

                        return (false,
                            "Giảm giá sản phẩm vượt quá thành tiền.",
                            null);
                    }

                    // Kiểm tra tồn kho
                    var inventory = await _db.InventoryBalances
                        .FirstOrDefaultAsync(x =>
                            x.WarehouseId == model.WarehouseId &&
                            x.ProductId == item.ProductId);

                    if (inventory == null ||
                        inventory.Quantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();

                        return (false,
                            "Không đủ tồn kho sản phẩm: " +
                            product.ProductName,
                            null);
                    }

                    decimal lineTotal =
                        lineSubTotal - item.DiscountAmount;

                    subTotal += lineSubTotal;
                    totalDiscount += item.DiscountAmount;

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        DiscountAmount = item.DiscountAmount,
                        LineTotal = lineTotal
                    });
                }

                if (totalDiscount > subTotal)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Tổng giảm giá không được lớn hơn tổng tiền hàng.",
                        null);
                }

                // Tạo đơn hàng
                var order = new Order
                {
                    OrderCode = model.OrderCode.Trim(),
                    StoreId = model.StoreId,
                    CustomerId = model.CustomerId,
                    UserId = userId,
                    OrderDate = model.OrderDate,
                    SubTotal = subTotal,
                    DiscountAmount = totalDiscount,
                    TotalAmount = subTotal - totalDiscount,
                    Status = "Completed",
                    Note = model.Note?.Trim()
                };

                _db.Orders.Add(order);

                await _db.SaveChangesAsync();

                // Lưu chi tiết đơn hàng và xuất kho
                foreach (var detail in orderDetails)
                {
                    detail.OrderId = order.OrderId;

                    _db.OrderDetails.Add(detail);

                    var inventory = await _db.InventoryBalances
                        .FirstAsync(x =>
                            x.WarehouseId == model.WarehouseId &&
                            x.ProductId == detail.ProductId);

                    // Trừ tồn kho
                    inventory.Quantity -= detail.Quantity;
                    inventory.UpdatedAt = DateTime.Now;

                    // Ghi lịch sử xuất kho
                    _db.InventoryTransactions.Add(
                        new InventoryTransaction
                        {
                            WarehouseId = model.WarehouseId,
                            ProductId = detail.ProductId,
                            UserId = userId,
                            TransactionType = "OUT",
                            Quantity = detail.Quantity,
                            ReferenceCode = order.OrderCode,
                            TransactionDate = DateTime.Now,
                            Note = "Xuất kho cho đơn hàng " +
                                   order.OrderCode
                        });
                }

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true,
                    "Tạo đơn hàng thành công.",
                    order.OrderId);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return (false,
                    "Lỗi lưu dữ liệu. Vui lòng kiểm tra mã đơn và database.",
                    null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // 5. HUY DON HANG VA HOAN KHO
        // =====================================================

        public async Task<(bool Success, string Message)>
            CancelAsync(long id, int? userId)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var order = await _db.Orders
                    .FirstOrDefaultAsync(x => x.OrderId == id);

                if (order == null)
                {
                    await transaction.RollbackAsync();

                    return (false, "Không tìm thấy đơn hàng.");
                }

                if (order.Status == "Cancelled")
                {
                    await transaction.RollbackAsync();

                    return (false, "Đơn hàng đã được hủy trước đó.");
                }

                // Lấy chi tiết đơn
                var details = await _db.OrderDetails
                    .Where(x => x.OrderId == id)
                    .ToListAsync();

                // Lấy lịch sử xuất kho
                var transactions = await _db.InventoryTransactions
                    .Where(x =>
                        x.ReferenceCode == order.OrderCode &&
                        x.TransactionType == "OUT")
                    .ToListAsync();

                if (transactions.Count == 0)
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Không tìm thấy lịch sử xuất kho. Không thể hoàn kho tự động.");
                }

                // Kiểm tra lịch sử xuất kho có khớp chi tiết đơn không.
                var expected = details
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.Quantity));

                var actual = transactions
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.Quantity));

                if (expected.Count != actual.Count ||
                    expected.Any(x =>
                        !actual.ContainsKey(x.Key) ||
                        actual[x.Key] != x.Value))
                {
                    await transaction.RollbackAsync();

                    return (false,
                        "Lịch sử xuất kho không khớp chi tiết đơn hàng. Không thể hoàn kho an toàn.");
                }

                foreach (var item in transactions)
                {
                    var inventory = await _db.InventoryBalances
                        .FirstOrDefaultAsync(x =>
                            x.WarehouseId == item.WarehouseId &&
                            x.ProductId == item.ProductId);

                    if (inventory == null)
                    {
                        await transaction.RollbackAsync();

                        return (false,
                            "Không tìm thấy tồn kho để hoàn sản phẩm.");
                    }

                    // Hoàn lại tồn kho
                    inventory.Quantity += item.Quantity;
                    inventory.UpdatedAt = DateTime.Now;

                    // Ghi lịch sử nhập kho
                    _db.InventoryTransactions.Add(
                        new InventoryTransaction
                        {
                            WarehouseId = item.WarehouseId,
                            ProductId = item.ProductId,
                            UserId = userId,
                            TransactionType = "IN",
                            Quantity = item.Quantity,
                            ReferenceCode = order.OrderCode,
                            TransactionDate = DateTime.Now,
                            Note = "Hoàn kho do hủy đơn " +
                                   order.OrderCode
                        });
                }

                order.Status = "Cancelled";

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true,
                    "Hủy đơn hàng và hoàn kho thành công.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // 6. LAY DANH SACH KHO THEO CUA HANG
        // =====================================================

        public async Task<List<object>> GetWarehousesAsync(int storeId)
        {
            var warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(x =>
                    x.StoreId == storeId &&
                    x.IsActive)
                .OrderBy(x => x.WarehouseName)
                .Select(x => new
                {
                    id = x.WarehouseId,
                    name = x.WarehouseName
                })
                .ToListAsync();

            return warehouses
                .Select(x => (object)x)
                .ToList();
        }
    }
}
