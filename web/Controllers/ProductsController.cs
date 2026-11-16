
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        // DANH SACH
        public async Task<IActionResult> Index(
            string? keyword,
            int? categoryId,
            bool? isActive,
            string? sort,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1) page = 1;

            var products = await _service.GetProductsAsync(
                keyword, categoryId, isActive, sort, page, pageSize);

            var totalItems = await _service.CountProductsAsync(
                keyword, categoryId, isActive);

            ViewBag.Categories = new SelectList(
                await _service.GetCategoriesAsync(),
                "CategoryId",
                "CategoryName",
                categoryId);

            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.IsActive = isActive;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            return View(products);
        }

        // CHI TIET
        public async Task<IActionResult> Details(int id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // HIEN FORM THEM
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();

            return View(new ProductViewModel());
        }

        // XU LY THEM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (model.SellingPrice < model.CostPrice)
            {
                ModelState.AddModelError(
                    nameof(model.SellingPrice),
                    "Giá bán không nên thấp hơn giá nhập.");
            }

            if (await _service.IsProductCodeExistsAsync(model.ProductCode))
            {
                ModelState.AddModelError(
                    nameof(model.ProductCode),
                    "Mã sản phẩm đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(model.CategoryId);
                return View(model);
            }

            await _service.CreateAsync(model);

            TempData["Success"] = "Thêm sản phẩm thành công.";

            return RedirectToAction(nameof(Index));
        }

        // HIEN FORM SUA
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                CategoryId = product.CategoryId,
                Unit = product.Unit,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                MinStock = product.MinStock,
                Description = product.Description,
                IsActive = product.IsActive
            };

            await LoadCategories(model.CategoryId);

            return View(model);
        }

        // XU LY SUA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductViewModel model)
        {
            if (id != model.ProductId)
                return BadRequest();

            if (model.SellingPrice < model.CostPrice)
            {
                ModelState.AddModelError(
                    nameof(model.SellingPrice),
                    "Giá bán không nên thấp hơn giá nhập.");
            }

            if (await _service.IsProductCodeExistsAsync(
                model.ProductCode, id))
            {
                ModelState.AddModelError(
                    nameof(model.ProductCode),
                    "Mã sản phẩm đã được sử dụng.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(model.CategoryId);
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

            TempData["Success"] = "Cập nhật sản phẩm thành công.";

            return RedirectToAction(nameof(Index));
        }

        // XAC NHAN NGUNG HOAT DONG
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound();

            ViewBag.HasTransactions =
                await _service.HasTransactionsAsync(id);

            return View(product);
        }

        // XU LY NGUNG HOAT DONG
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _service.DeactivateAsync(id);

            if (!result)
            {
                TempData["Error"] =
                    "Không thể ngừng sản phẩm. Sản phẩm không tồn tại hoặc đã phát sinh giao dịch.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã ngừng hoạt động sản phẩm.";

            return RedirectToAction(nameof(Index));
        }

        // XUAT CSV MO BANG EXCEL
        public async Task<IActionResult> Export(
            string? keyword,
            int? categoryId,
            bool? isActive)
        {
            var products = await _service.GetProductsForExportAsync(
                keyword, categoryId, isActive);

            var builder = new System.Text.StringBuilder();

            builder.AppendLine(
                "Ma san pham,Ten san pham,Danh muc,Don vi,Gia nhap,Gia ban,Ton toi thieu,Trang thai");

            foreach (var p in products)
            {
                var fields = new[]
                {
                    p.ProductCode,
                    p.ProductName,
                    p.Category?.CategoryName ?? "",
                    p.Unit ?? "",
                    p.CostPrice.ToString("0.00"),
                    p.SellingPrice.ToString("0.00"),
                    p.MinStock.ToString(),
                    p.IsActive ? "Hoat dong" : "Ngung hoat dong"
                };

                builder.AppendLine(string.Join(",",
                    fields.Select(x =>
                        "\"" + x.Replace("\"", "\"\"") + "\"")));
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString()))
                .ToArray();

            return File(bytes, "text/csv",
                $"RetailFlow_Products_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private async Task LoadCategories(int? selectedId = null)
        {
            ViewBag.Categories = new SelectList(
                await _service.GetCategoriesAsync(),
                "CategoryId",
                "CategoryName",
                selectedId);
        }
    }
}
