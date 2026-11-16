
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id) ? id : null;
        }

        // GET: /Inventory
        public async Task<IActionResult> Index(
            string? keyword,
            int? warehouseId,
            string? stockStatus)
        {
            var model = await _inventoryService.GetIndexAsync(
                keyword,
                warehouseId,
                stockStatus);

            return View(model);
        }

        // GET: /Inventory/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var model = await _inventoryService.GetDetailAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        // GET: /Inventory/Movement
        [HttpGet]
        public async Task<IActionResult> Movement(
            string type = "IN",
            int? warehouseId = null,
            int? productId = null)
        {
            if (type != "IN" && type != "OUT")
                type = "IN";

            var model = new InventoryMovementViewModel
            {
                TransactionType = type,
                WarehouseId = warehouseId ?? 0,
                ProductId = productId ?? 0
            };

            model = await _inventoryService.BuildMovementFormAsync(model);

            return View(model);
        }

        // POST: /Inventory/Movement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Movement(
            InventoryMovementViewModel model)
        {
            if (model.WarehouseId <= 0)
                ModelState.AddModelError(
                    nameof(model.WarehouseId),
                    "Vui lòng chọn kho.");

            if (model.ProductId <= 0)
                ModelState.AddModelError(
                    nameof(model.ProductId),
                    "Vui lòng chọn sản phẩm.");

            if (model.Quantity <= 0)
                ModelState.AddModelError(
                    nameof(model.Quantity),
                    "Số lượng phải lớn hơn 0.");

            if (model.TransactionType != "IN" &&
                model.TransactionType != "OUT")
            {
                ModelState.AddModelError(
                    nameof(model.TransactionType),
                    "Loại giao dịch không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                model = await _inventoryService.BuildMovementFormAsync(model);
                return View(model);
            }

            var result = await _inventoryService.CreateMovementAsync(
                model,
                GetCurrentUserId());

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model = await _inventoryService.BuildMovementFormAsync(model);
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(
                nameof(Details),
                new { id = model.ProductId });
        }

        // GET: /Inventory/History
        public async Task<IActionResult> History(
            string? keyword,
            string? transactionType,
            int? warehouseId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var model = await _inventoryService.GetHistoryAsync(
                keyword,
                transactionType,
                warehouseId,
                fromDate,
                toDate);

            return View(model);
        }
    }
}
