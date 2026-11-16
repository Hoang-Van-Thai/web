
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Controllers
{
    public class StoresController : Controller
    {
        private readonly RetailFlowDBContext _db;

        public StoresController(RetailFlowDBContext db)
        {
            _db = db;
        }

        // =====================================================
        // DANH SACH CUA HANG
        // GET: /Stores
        // =====================================================
        public async Task<IActionResult> Index(
            string? keyword,
            string? status)
        {
            var query = _db.Stores
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.StoreCode.Contains(keyword) ||
                    x.StoreName.Contains(keyword) ||
                    (x.Phone != null && x.Phone.Contains(keyword)) ||
                    (x.Address != null && x.Address.Contains(keyword)));
            }

            if (status == "active")
                query = query.Where(x => x.IsActive);
            else if (status == "inactive")
                query = query.Where(x => !x.IsActive);

            var stores = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new StoreRowViewModel
                {
                    StoreId = x.StoreId,
                    StoreCode = x.StoreCode,
                    StoreName = x.StoreName,
                    Address = x.Address,
                    Phone = x.Phone,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    WarehouseCount = x.Warehouses.Count(),
                    OrderCount = x.Orders.Count()
                })
                .ToListAsync();

            var model = new StoreIndexViewModel
            {
                Keyword = keyword,
                Status = status,
                Items = stores,
                TotalStores = await _db.Stores.CountAsync(),
                ActiveStores = await _db.Stores.CountAsync(x => x.IsActive),
                InactiveStores = await _db.Stores.CountAsync(x => !x.IsActive)
            };

            return View(model);
        }

        // =====================================================
        // CHI TIET CUA HANG
        // GET: /Stores/Details/1
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var store = await _db.Stores
                .AsNoTracking()
                .Include(x => x.Warehouses)
                .Include(x => x.Orders)
                .FirstOrDefaultAsync(x => x.StoreId == id);

            if (store == null)
                return NotFound();

            var model = new StoreDetailsViewModel
            {
                StoreId = store.StoreId,
                StoreCode = store.StoreCode,
                StoreName = store.StoreName,
                Address = store.Address,
                Phone = store.Phone,
                IsActive = store.IsActive,
                CreatedAt = store.CreatedAt,

                WarehouseCount = store.Warehouses.Count,
                OrderCount = store.Orders.Count,

                Warehouses = store.Warehouses
                    .OrderBy(x => x.WarehouseName)
                    .Select(x => new StoreWarehouseViewModel
                    {
                        WarehouseId = x.WarehouseId,
                        WarehouseCode = x.WarehouseCode,
                        WarehouseName = x.WarehouseName,
                        Address = x.Address,
                        IsActive = x.IsActive
                    })
                    .ToList(),

                RecentOrders = store.Orders
                    .OrderByDescending(x => x.OrderDate)
                    .Take(10)
                    .Select(x => new StoreOrderViewModel
                    {
                        OrderId = x.OrderId,
                        OrderCode = x.OrderCode,
                        OrderDate = x.OrderDate,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status
                    })
                    .ToList()
            };

            return View(model);
        }

        // =====================================================
        // TAO CUA HANG
        // GET: /Stores/Create
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new StoreFormViewModel
            {
                IsActive = true
            });
        }

        // POST: /Stores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoreFormViewModel model)
        {
            model.StoreCode = model.StoreCode?.Trim() ?? "";
            model.StoreName = model.StoreName?.Trim() ?? "";
            model.Address = model.Address?.Trim();
            model.Phone = model.Phone?.Trim();

            if (!string.IsNullOrWhiteSpace(model.StoreCode))
            {
                bool codeExists = await _db.Stores
                    .AnyAsync(x => x.StoreCode == model.StoreCode);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        nameof(model.StoreCode),
                        "Mã cửa hàng đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var store = new Store
            {
                StoreCode = model.StoreCode,
                StoreName = model.StoreName,
                Address = model.Address,
                Phone = model.Phone,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _db.Stores.Add(store);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thêm cửa hàng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CAP NHAT CUA HANG
        // GET: /Stores/Edit/1
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var store = await _db.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StoreId == id);

            if (store == null)
                return NotFound();

            var model = new StoreFormViewModel
            {
                StoreId = store.StoreId,
                StoreCode = store.StoreCode,
                StoreName = store.StoreName,
                Address = store.Address,
                Phone = store.Phone,
                IsActive = store.IsActive
            };

            return View(model);
        }

        // POST: /Stores/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            StoreFormViewModel model)
        {
            if (id != model.StoreId)
                return BadRequest();

            model.StoreCode = model.StoreCode?.Trim() ?? "";
            model.StoreName = model.StoreName?.Trim() ?? "";
            model.Address = model.Address?.Trim();
            model.Phone = model.Phone?.Trim();

            if (!string.IsNullOrWhiteSpace(model.StoreCode))
            {
                bool codeExists = await _db.Stores.AnyAsync(x =>
                    x.StoreCode == model.StoreCode &&
                    x.StoreId != id);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        nameof(model.StoreCode),
                        "Mã cửa hàng đã được sử dụng.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var store = await _db.Stores
                .FirstOrDefaultAsync(x => x.StoreId == id);

            if (store == null)
                return NotFound();

            store.StoreCode = model.StoreCode;
            store.StoreName = model.StoreName;
            store.Address = model.Address;
            store.Phone = model.Phone;
            store.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật cửa hàng thành công.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // NGUNG HOAT DONG / KICH HOAT LAI
        // POST: /Stores/ToggleStatus/1
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var store = await _db.Stores
                .FirstOrDefaultAsync(x => x.StoreId == id);

            if (store == null)
                return NotFound();

            store.IsActive = !store.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = store.IsActive
                ? "Đã kích hoạt cửa hàng."
                : "Đã ngừng hoạt động cửa hàng.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // XOA CUA HANG
        // Chỉ cho xóa nếu chưa phát sinh kho hoặc đơn hàng.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var store = await _db.Stores
                .Include(x => x.Warehouses)
                .Include(x => x.Orders)
                .FirstOrDefaultAsync(x => x.StoreId == id);

            if (store == null)
                return NotFound();

            if (store.Warehouses.Any() || store.Orders.Any())
            {
                TempData["Error"] =
                    "Không thể xóa cửa hàng đã có kho hàng hoặc đơn hàng. " +
                    "Bạn có thể ngừng hoạt động cửa hàng thay thế.";

                return RedirectToAction(nameof(Details), new { id });
            }

            _db.Stores.Remove(store);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Xóa cửa hàng thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
