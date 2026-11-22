using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[Authorize]
public class ModelTrainingController : Controller
{
    private readonly IModelTrainingService _service;
    private readonly ILogger<ModelTrainingController> _logger;

    public ModelTrainingController(
        IModelTrainingService service,
        ILogger<ModelTrainingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = await _service.GetIndexAsync(cancellationToken);
        return View(model);
    }

    // =====================================================
    // PREVIEW SQL DATA WHEN STORE CHANGES
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> PreviewStore(
        int storeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetStorePreviewAsync(
                storeId,
                cancellationToken);

            return Json(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot preview store training data.");

            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // =====================================================
    // DOWNLOAD EXAMPLE CSV
    // =====================================================

    [HttpGet]
    public IActionResult DownloadExampleCsv()
    {
        var csv = new StringBuilder();

        csv.AppendLine("product_id,date,quantity,price");
        csv.AppendLine("P001,2026-01-01,10,150000");
        csv.AppendLine("P001,2026-01-02,12,150000");
        csv.AppendLine("P001,2026-01-03,0,150000");
        csv.AppendLine("P002,2026-01-01,5,200000");
        csv.AppendLine("P002,2026-01-02,8,200000");
        csv.AppendLine("P002,2026-01-03,6,200000");

        var bytes = new UTF8Encoding(true).GetBytes(csv.ToString());

        return File(
            bytes,
            "text/csv",
            "example.csv");
    }

    // =====================================================
    // UPLOAD CSV
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(105 * 1024 * 1024)]
    public async Task<IActionResult> UploadCsv(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var runId = await _service.StartCsvTrainingAsync(
                file,
                GetCurrentUserId(),
                cancellationToken);

            TempData["Success"] =
                $"Đã gửi CSV đến Python. Mã phiên retrain: #{runId}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV retrain failed.");
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // TRAIN FROM SQL
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrainFromSql(
        int storeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var runId = await _service.StartSqlTrainingAsync(
                storeId,
                GetCurrentUserId(),
                cancellationToken);

            TempData["Success"] =
                $"Đã gửi dữ liệu SQL đến Python. Mã phiên: #{runId}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL retrain failed.");
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // RETRAIN STATUS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Status(
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await _service.GetStatusAsync(
                cancellationToken);

            var comparison =
                status.Metrics?.Comparison ?? status.Comparison;

            return Json(new
            {
                success = status.Success,
                status = status.Status,
                message = status.Message,
                progress = status.Progress,
                error = status.Error,

                activeModel = status.Metrics?.ActiveModel,

                winner = status.Metrics?.Winner ?? comparison?.Winner,

                comparison,

                trainingRows =
                    status.Metrics?.TrainingRows ?? status.TrainingRows,

                validationRows =
                    status.Metrics?.ValidationRows ?? 0,

                products =
                    status.Metrics?.Products ?? status.Products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get retrain status.");

            return Json(new
            {
                success = false,
                status = "unavailable",
                message = "Không thể kết nối Python API.",
                error = ex.Message,
                progress = 0
            });
        }
    }

    // =====================================================
    // TRAINING RUN DETAILS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> TrainingRunDetails(
        long id,
        CancellationToken cancellationToken)
    {
        var model = await _service.GetTrainingRunDetailsAsync(
            id,
            cancellationToken);

        if (model == null)
            return NotFound();

        return View(model);
    }

    // =====================================================
    // ACTIVATE MODEL
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ActivateModelAsync(
                id,
                cancellationToken);

            if (!result)
            {
                TempData["Error"] = "Không tìm thấy model.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Đã kích hoạt model thành công trên Python API và SQL Server.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Activate model failed.");
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // CURRENT USER
    // =====================================================

    private int? GetCurrentUserId()
    {
        var value =
            User.FindFirstValue("UserId") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}