using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace web.ViewModels;

public class ModelTrainingViewModel
{
    public List<ModelVersionItemViewModel> Models { get; set; } = new();
    public List<ModelTrainingRunItemViewModel> TrainingRuns { get; set; } = new();
    public List<SelectListItem> Stores { get; set; } = new();

    public int TotalModels { get; set; }
    public int ActiveModels { get; set; }
    public int TotalTrainingRuns { get; set; }

    public bool ApiAvailable { get; set; }
    public string? ActivePythonModel { get; set; }
    public string? ApiError { get; set; }

    public RetrainStatusViewModel RetrainStatus { get; set; } = new();
}

public class StoreTrainingPreviewViewModel
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;

    public int TotalProducts { get; set; }
    public int EligibleProducts { get; set; }
    public int IneligibleProducts => TotalProducts - EligibleProducts;

    public int TotalRecords { get; set; }

    public DateOnly? DataStartDate { get; set; }
    public DateOnly? DataEndDate { get; set; }

    public List<StoreTrainingProductViewModel> Products { get; set; } = new();
}

public class StoreTrainingProductViewModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public DateOnly FirstSaleDate { get; set; }
    public DateOnly LastSaleDate { get; set; }

    // Số ngày lịch từ lần bán đầu tiên đến ngày cuối cùng
    public int CalendarDays { get; set; }

    public bool IsEligible => CalendarDays >= 100;
}

public class ModelVersionItemViewModel
{
    public int ModelVersionId { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelFilePath { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public int TrainingRunCount { get; set; }
    public decimal? Mae { get; set; }
    public decimal? Rmse { get; set; }
}

public class ModelTrainingRunItemViewModel
{
    public long TrainingRunId { get; set; }
    public string? TrainingName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? VersionName { get; set; }

    public DateOnly? DataStartDate { get; set; }
    public DateOnly? DataEndDate { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public decimal? Mae { get; set; }
    public decimal? Rmse { get; set; }
    public decimal? Rmsse { get; set; }

    public string? LogMessage { get; set; }
}

public class RetrainStatusViewModel
{
    public string Status { get; set; } = "idle";
    public string? Message { get; set; }
    public int Progress { get; set; }
    public string? Error { get; set; }
    public string? ActiveModel { get; set; }
    public string? Winner { get; set; }

    public double? OldRmse { get; set; }
    public double? NewRmse { get; set; }
    public double? OldMae { get; set; }
    public double? NewMae { get; set; }

    public double? RmseImprovementPercent { get; set; }
    public double? MaeImprovementPercent { get; set; }

    public int TrainingRows { get; set; }
    public int ValidationRows { get; set; }
    public int Products { get; set; }
}

public class TrainingStoreOption
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
}