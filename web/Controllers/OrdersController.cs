
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using web.Models.Entities;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly RetailFlowDBContext _db;

        public OrdersController(
            IOrderService orderService,
            RetailFlowDBContext db)
        {
            _orderService = orderService;
            _db = db;
        }

        // =====================================================
        // 1. DANH SACH DON HANG
        // =====================================================

        public async Task<IActionResult> Index(
            string? keyword,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 10)
        {
            page = Math.Max(1, page);

            if (pageSize != 10 &&
                pageSize != 20 &&
                pageSize != 50)
            {
                pageSize = 10;
            }

            var result = await _orderService.SearchAsync(
                keyword,
                status,
                fromDate,
                toDate,
                page,
                pageSize);

            var model = new OrderIndexViewModel
            {
                Orders = result.Items,
                Keyword = keyword,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize,
                TotalItems = result.Total,
                TotalPages = (int)Math.Ceiling(
                    result.Total / (double)pageSize)
            };

            return View(model);
        }

        // =====================================================
        // 2. CHI TIET DON HANG
        // =====================================================

        public async Task<IActionResult> Details(long id)
        {
            var order = await _orderService.GetAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // =====================================================
        // 3. TAO DON HANG - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new OrderFormViewModel
            {
                OrderCode = "DH" +
                    DateTime.Now.ToString("yyyyMMddHHmmss"),

                OrderDate = DateTime.Now,

                Items = new List<OrderItemInputViewModel>
                {
                    new OrderItemInputViewModel
                    {
                        Quantity = 1
                    }
                }
            };

            model = await _orderService.BuildFormAsync(model);

            return View(model);
        }

        // =====================================================
        // 4. TAO DON HANG - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OrderFormViewModel model)
        {
            if (model.Items == null ||
                model.Items.Count == 0)
            {
                ModelState.AddModelError(
                    "Items",
                    "Vui lòng thêm ít nhất một sản phẩm.");
            }

            if (!ModelState.IsValid)
            {
                model = await _orderService.BuildFormAsync(model);

                return View(model);
            }

            int? userId = null;

            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int id))
            {
                userId = id;
            }

            var result = await _orderService.CreateAsync(
                model,
                userId);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                model = await _orderService.BuildFormAsync(model);

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(
                nameof(Details),
                new { id = result.OrderId });
        }

        // =====================================================
        // 5. HUY DON HANG
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            int? userId = null;

            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int uid))
            {
                userId = uid;
            }

            var result = await _orderService.CancelAsync(
                id,
                userId);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =====================================================
        // 6. LAY KHO THEO CUA HANG - AJAX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetWarehouses(int storeId)
        {
            var warehouses =
                await _orderService.GetWarehousesAsync(storeId);

            return Json(warehouses);
        }

        // =====================================================
        // 7. XUAT DANH SACH DON HANG CSV
        // =====================================================

        public async Task<IActionResult> Export(
            string? keyword,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var result = await _orderService.SearchAsync(
                keyword,
                status,
                fromDate,
                toDate,
                1,
                100000);

            var builder = new StringBuilder();

            builder.AppendLine(
                "Ma don,Ngay tao,Cua hang,Khach hang,Tam tinh,Giam gia,Tong tien,Trang thai");

            static string Escape(string? value)
            {
                return "\"" +
                    (value ?? "").Replace("\"", "\"\"") +
                    "\"";
            }

            foreach (var order in result.Items)
            {
                builder.AppendLine(string.Join(",",
                    Escape(order.OrderCode),
                    Escape(order.OrderDate.ToString(
                        "dd/MM/yyyy HH:mm")),
                    Escape(order.Store?.StoreName),
                    Escape(order.Customer?.FullName ?? "Khách lẻ"),
                    order.SubTotal.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    order.DiscountAmount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    order.TotalAmount.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    Escape(order.Status)
                ));
            }

            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(
                    builder.ToString()))
                .ToArray();

            return File(
                bytes,
                "text/csv; charset=utf-8",
                "Orders.csv");
        }
    }
}
