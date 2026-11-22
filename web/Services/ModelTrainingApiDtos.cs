using System.Text.Json.Serialization;

namespace web.Services;

// ========================================================
// PYTHON HEALTH
// ========================================================

public class PythonHealthDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("model_loaded")]
    public bool ModelLoaded { get; set; }

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("model_features")]
    public int ModelFeatures { get; set; }

    [JsonPropertyName("retrain_status")]
    public string? RetrainStatus { get; set; }
}

// ========================================================
// RETRAIN STATUS
// ========================================================

public class RetrainStatusDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "idle";

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("finished_at")]
    public string? FinishedAt { get; set; }

    [JsonPropertyName("metrics")]
    public RetrainResultDto? Metrics { get; set; }

    [JsonPropertyName("comparison")]
    public ModelComparisonDto? Comparison { get; set; }

    [JsonPropertyName("training_rows")]
    public int TrainingRows { get; set; }

    [JsonPropertyName("products")]
    public int Products { get; set; }
}

// ========================================================
// RETRAIN RESULT
// ========================================================

public class RetrainResultDto
{
    [JsonPropertyName("old_model")]
    public string? OldModel { get; set; }

    [JsonPropertyName("new_model")]
    public string? NewModel { get; set; }

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("active_changed")]
    public bool ActiveChanged { get; set; }

    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("comparison")]
    public ModelComparisonDto? Comparison { get; set; }

    [JsonPropertyName("training_rows")]
    public int TrainingRows { get; set; }

    [JsonPropertyName("validation_rows")]
    public int ValidationRows { get; set; }

    [JsonPropertyName("products")]
    public int Products { get; set; }

    [JsonPropertyName("train_start_date")]
    public string? TrainStartDate { get; set; }

    [JsonPropertyName("train_end_date")]
    public string? TrainEndDate { get; set; }

    [JsonPropertyName("validation_start_date")]
    public string? ValidationStartDate { get; set; }

    [JsonPropertyName("validation_end_date")]
    public string? ValidationEndDate { get; set; }

    [JsonPropertyName("new_model_file")]
    public string? NewModelFile { get; set; }

    [JsonPropertyName("metadata_file")]
    public string? MetadataFile { get; set; }

    [JsonPropertyName("feature_importance_file")]
    public string? FeatureImportanceFile { get; set; }
}

// ========================================================
// MODEL COMPARISON
// ========================================================

public class ModelComparisonDto
{
    [JsonPropertyName("old_model")]
    public string? OldModel { get; set; }

    [JsonPropertyName("new_model")]
    public string? NewModel { get; set; }

    [JsonPropertyName("old_RMSE")]
    public double? OldRmse { get; set; }

    [JsonPropertyName("new_RMSE")]
    public double? NewRmse { get; set; }

    [JsonPropertyName("old_MAE")]
    public double? OldMae { get; set; }

    [JsonPropertyName("new_MAE")]
    public double? NewMae { get; set; }

    [JsonPropertyName("RMSE_improvement_percent")]
    public double? RmseImprovementPercent { get; set; }

    [JsonPropertyName("MAE_improvement_percent")]
    public double? MaeImprovementPercent { get; set; }

    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("decision")]
    public string? Decision { get; set; }
}

// ========================================================
// MODEL REGISTRY
// ========================================================

public class ModelRegistryResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("active_model")]
    public string? ActiveModel { get; set; }

    [JsonPropertyName("models")]
    public List<ModelRegistryItemDto> Models { get; set; } = new();
}

public class ModelRegistryItemDto
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("model_file")]
    public string? ModelFile { get; set; }

    [JsonPropertyName("metadata_file")]
    public string? MetadataFile { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("training_rows")]
    public int? TrainingRows { get; set; }

    [JsonPropertyName("validation_rows")]
    public int? ValidationRows { get; set; }

    [JsonPropertyName("products")]
    public int? Products { get; set; }

    [JsonPropertyName("training_metrics")]
    public ModelMetricDto? TrainingMetrics { get; set; }

    [JsonPropertyName("validation_metrics")]
    public ModelMetricDto? ValidationMetrics { get; set; }

    [JsonPropertyName("comparison")]
    public ModelComparisonDto? Comparison { get; set; }
}

public class ModelMetricDto
{
    [JsonPropertyName("MAE")]
    public double? Mae { get; set; }

    [JsonPropertyName("RMSE")]
    public double? Rmse { get; set; }
}

// ========================================================
// START RETRAIN RESPONSE
// ========================================================

public class RetrainStartResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}