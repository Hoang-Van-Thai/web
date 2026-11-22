using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web.Services;
using web.ViewModels;

namespace web.Controllers;

public class ForecastController : Controller
{
    private readonly IForecastService _forecastService;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(
        IForecastService forecastService,
        ILogger<ForecastController> logger)
    {
        _forecastService = forecastService;
        _logger = logger;
    }

    // Trang chính Forecast
    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = new ForecastViewModel();

        await LoadOptionsAsync(model, cancellationToken);

        try
        {
            var health = await _forecastService.CheckApiAsync(
                cancellationToken);

            model.ApiAvailable =
                string.Equals(
                    health.Status,
                    "healthy",
                    StringComparison.OrdinalIgnoreCase)
                && health.ModelLoaded;

            model.ActiveModel = health.ActiveModel;

            if (!model.ApiAvailable)
            {
                model.ErrorMessage =
                    "Python API chưa sẵn sàng hoặc chưa load model.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Không kết nối được Forecast API.");

            model.ApiAvailable = false;

            model.ErrorMessage =
                "Không thể kết nối Python API. " +
                "Hãy kiểm tra server Python tại http://127.0.0.1:8000.";
        }

        return View(model);
    }

    // Chuyển về trang Index
    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index));
    }

    // Thực hiện dự báo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(
        ForecastViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.ProductId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.ProductId),
                "Vui lòng chọn sản phẩm.");
        }

        if (model.StoreId <= 0)
        {
            ModelState.AddModelError(
                nameof(model.StoreId),
                "Vui lòng chọn cửa hàng.");
        }

        if (model.Days is not (7 or 14 or 28))
        {
            ModelState.AddModelError(
                nameof(model.Days),
                "Chỉ hỗ trợ dự báo 7, 14 hoặc 28 ngày.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(model, cancellationToken);

            return View("Index", model);
        }

        try
        {
            int? userId = null;

            var claimValue =
                User.FindFirstValue("UserId")
                ?? User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (int.TryParse(claimValue, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var result = await _forecastService.RunForecastAsync(
                model.ProductId,
                model.StoreId,
                model.Days,
                userId,
                cancellationToken);

            TempData["SuccessMessage"] =
                "Dự báo thành công và đã lưu vào hệ thống.";

            return RedirectToAction(
                nameof(Details),
                new { id = result.ForecastRunId });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Thực hiện dự báo thất bại.");

            model.ErrorMessage = ex.Message;

            await LoadOptionsAsync(model, cancellationToken);

            return View("Index", model);
        }
    }

    // Xem kết quả dự báo
    [HttpGet]
    public async Task<IActionResult> Details(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest();

        var result =
            await _forecastService.GetForecastDetailsAsync(
                id,
                cancellationToken);

        if (result == null)
            return NotFound();

        return View(result);
    }

    // Nạp danh sách sản phẩm và cửa hàng
    private async Task LoadOptionsAsync(
        ForecastViewModel model,
        CancellationToken cancellationToken)
    {
        var products =
            await _forecastService.GetProductsAsync(
                cancellationToken);

        var stores =
            await _forecastService.GetStoresAsync(
                cancellationToken);

        model.Products = products.Select(x =>
            new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = x.Id == model.ProductId
            })
            .ToList();

        model.Stores = stores.Select(x =>
            new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name,
                Selected = x.Id == model.StoreId
            })
            .ToList();
    }
}