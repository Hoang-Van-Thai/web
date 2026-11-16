using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        // =====================================================
        // 1. DANH SACH
        // =====================================================

        public async Task<IActionResult> Index(
            string? keyword,
            string? sort,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
                page = 1;

            var categories = await _service.GetCategoriesAsync(
                keyword,
                sort,
                page,
                pageSize);

            var totalItems = await _service.CountCategoriesAsync(
                keyword);

            ViewBag.Keyword = keyword;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            ViewBag.TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            return View(categories);
        }

        // =====================================================
        // 2. CHI TIET
        // =====================================================

        public async Task<IActionResult> Details(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            ViewBag.HasProducts = await _service.HasProductsAsync(id);

            return View(category);
        }

        // =====================================================
        // 3. HIEN FORM THEM
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        // =====================================================
        // 4. XU LY THEM
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CategoryViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.CategoryCode))
            {
                if (await _service.IsCategoryCodeExistsAsync(
                    model.CategoryCode))
                {
                    ModelState.AddModelError(
                        nameof(model.CategoryCode),
                        "Mã danh mục đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _service.CreateAsync(model);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryCode),
                    "Không thể lưu danh mục. Vui lòng kiểm tra lại dữ liệu.");

                return View(model);
            }

            TempData["Success"] = "Thêm danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 5. HIEN FORM SUA
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName
            };

            return View(model);
        }

        // =====================================================
        // 6. XU LY SUA
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CategoryViewModel model)
        {
            if (id != model.CategoryId)
                return BadRequest();

            if (!string.IsNullOrWhiteSpace(model.CategoryCode))
            {
                if (await _service.IsCategoryCodeExistsAsync(
                    model.CategoryCode, id))
                {
                    ModelState.AddModelError(
                        nameof(model.CategoryCode),
                        "Mã danh mục đã được sử dụng.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _service.UpdateAsync(model);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể cập nhật danh mục. Vui lòng kiểm tra dữ liệu.");

                return View(model);
            }

            TempData["Success"] = "Cập nhật danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 7. XAC NHAN XOA
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            ViewBag.HasProducts =
                await _service.HasProductsAsync(id);

            return View(category);
        }

        // =====================================================
        // 8. XU LY XOA
        // =====================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);

                if (!result)
                {
                    TempData["Error"] =
                        "Không thể xóa danh mục. Danh mục không tồn tại hoặc đang có sản phẩm liên kết.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa danh mục do dữ liệu đang được liên kết.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Xóa danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 9. XUAT CSV
        // =====================================================

        public async Task<IActionResult> Export(string? keyword)
        {
            var categories = await _service
                .GetCategoriesForExportAsync(keyword);

            var builder = new System.Text.StringBuilder();

            builder.AppendLine(
                "Ma danh muc,Ten danh muc");

            foreach (var category in categories)
            {
                var fields = new[]
                {
                    category.CategoryCode,
                    category.CategoryName
                };

                builder.AppendLine(
                    string.Join(",",
                        fields.Select(x =>
                            "\"" + (x ?? "").Replace("\"", "\"\"") + "\""))
                );
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(
                    System.Text.Encoding.UTF8.GetBytes(
                        builder.ToString()))
                .ToArray();

            return File(
                bytes,
                "text/csv",
                $"RetailFlow_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );
        }
    }
}