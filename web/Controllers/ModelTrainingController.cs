using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class ModelTrainingController : Controller
    {
        private readonly IModelTrainingService _service;

        public ModelTrainingController(IModelTrainingService service)
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ModelVersionCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int? userId = GetCurrentUserId();

            var result = await _service.CreateModelVersionAsync(
                model,
                userId
            );

            if (!result)
            {
                ModelState.AddModelError(
                    nameof(model.VersionName),
                    "Tên phiên bản đã tồn tại hoặc khoảng ngày dữ liệu không hợp lệ."
                );

                return View(model);
            }

            TempData["Success"] = "Đăng ký model thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> TrainingRunDetails(long id)
        {
            var model = await _service.GetTrainingRunDetailsAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _service.ActivateModelAsync(id);

            if (!result)
            {
                TempData["Error"] = "Không tìm thấy model.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã kích hoạt model thành công.";
            return RedirectToAction(nameof(Index));
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