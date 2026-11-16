using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class ForecastController : Controller
    {
        private readonly IForecastService _service;

        public ForecastController(IForecastService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _service.GetIndexAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _service.GetCreateViewModelAsync();

            if (model.Stores.Count == 0)
            {
                TempData["Error"] = "Chưa có cửa hàng đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.ActiveModelName))
            {
                TempData["Error"] =
                    "Bạn cần đăng ký và kích hoạt Model trước khi dự báo.";

                return RedirectToAction("Index", "ModelTraining");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ForecastCreateViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var formData = await _service.GetCreateViewModelAsync();
                model.Stores = formData.Stores;
                model.ActiveModelName = formData.ActiveModelName;

                return View(model);
            }

            int? userId = GetCurrentUserId();

            try
            {
                var forecastRunId = await _service.CreateForecastAsync(
                    model,
                    userId,
                    cancellationToken
                );

                TempData["Success"] = "Tạo dự báo thành công.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = forecastRunId }
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var formData = await _service.GetCreateViewModelAsync();
                model.Stores = formData.Stores;
                model.ActiveModelName = formData.ActiveModelName;

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var model = await _service.GetDetailsAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }
}