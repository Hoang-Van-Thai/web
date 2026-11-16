using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class ModelTrainingService : IModelTrainingService
    {
        private readonly RetailFlowDBContext _db;

        public ModelTrainingService(RetailFlowDBContext db)
        {
            _db = db;
        }

        public async Task<ModelTrainingViewModel> GetIndexAsync()
        {
            var models = await _db.ModelVersions
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ModelVersionItemViewModel
                {
                    ModelVersionId = x.ModelVersionId,
                    VersionName = x.VersionName,
                    ModelName = x.ModelName,
                    ModelFilePath = x.ModelFilePath,
                    Framework = x.Framework,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    Description = x.Description,
                    TrainingRunCount = x.ModelTrainingRuns.Count
                })
                .ToListAsync();

            var runs = await _db.ModelTrainingRuns
                .AsNoTracking()
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new ModelTrainingRunItemViewModel
                {
                    TrainingRunId = x.TrainingRunId,
                    TrainingName = x.TrainingName,
                    Status = x.Status,
                    VersionName = x.ModelVersion != null
                        ? x.ModelVersion.VersionName
                        : null,
                    DataStartDate = x.DataStartDate,
                    DataEndDate = x.DataEndDate,
                    StartedAt = x.StartedAt,
                    CompletedAt = x.CompletedAt,
                    Mae = x.Mae,
                    Rmse = x.Rmse,
                    Rmsse = x.Rmsse,
                    LogMessage = x.LogMessage
                })
                .ToListAsync();

            return new ModelTrainingViewModel
            {
                Models = models,
                TrainingRuns = runs,
                TotalModels = models.Count,
                ActiveModels = models.Count(x => x.IsActive),
                TotalTrainingRuns = runs.Count
            };
        }

        public Task<ModelVersionCreateViewModel?> GetCreateViewModelAsync()
        {
            return Task.FromResult<ModelVersionCreateViewModel?>(
                new ModelVersionCreateViewModel()
            );
        }

        public async Task<bool> CreateModelVersionAsync(
            ModelVersionCreateViewModel model,
            int? userId)
        {
            if (await _db.ModelVersions.AnyAsync(
                x => x.VersionName == model.VersionName.Trim()))
            {
                return false;
            }

            if (model.DataStartDate.HasValue &&
                model.DataEndDate.HasValue &&
                model.DataStartDate > model.DataEndDate)
            {
                return false;
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var version = new ModelVersion
                {
                    VersionName = model.VersionName.Trim(),
                    ModelName = model.ModelName.Trim(),
                    ModelFilePath = model.ModelFilePath.Trim(),
                    Framework = model.Framework.Trim(),
                    Description = model.Description?.Trim(),
                    IsActive = false,
                    CreatedAt = DateTime.Now
                };

                _db.ModelVersions.Add(version);
                await _db.SaveChangesAsync();

                // Đây là thông tin phiên huấn luyện offline đã hoàn tất.
                // ASP.NET không thực hiện huấn luyện model.
                var run = new ModelTrainingRun
                {
                    ModelVersionId = version.ModelVersionId,
                    UserId = userId,
                    TrainingName = string.IsNullOrWhiteSpace(model.TrainingName)
                        ? $"Import model {version.VersionName}"
                        : model.TrainingName.Trim(),
                    DataStartDate = model.DataStartDate,
                    DataEndDate = model.DataEndDate,
                    StartedAt = DateTime.Now,
                    CompletedAt = DateTime.Now,
                    Status = "Completed",
                    Mae = model.Mae,
                    Rmse = model.Rmse,
                    Rmsse = model.Rmsse,
                    LogMessage = string.IsNullOrWhiteSpace(model.LogMessage)
                        ? "Model được huấn luyện offline bằng Python và đăng ký vào RetailFlow."
                        : model.LogMessage.Trim()
                };

                _db.ModelTrainingRuns.Add(run);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ModelTrainingRunItemViewModel?> GetTrainingRunDetailsAsync(
            long id)
        {
            return await _db.ModelTrainingRuns
                .AsNoTracking()
                .Where(x => x.TrainingRunId == id)
                .Select(x => new ModelTrainingRunItemViewModel
                {
                    TrainingRunId = x.TrainingRunId,
                    TrainingName = x.TrainingName,
                    Status = x.Status,
                    VersionName = x.ModelVersion != null
                        ? x.ModelVersion.VersionName
                        : null,
                    DataStartDate = x.DataStartDate,
                    DataEndDate = x.DataEndDate,
                    StartedAt = x.StartedAt,
                    CompletedAt = x.CompletedAt,
                    Mae = x.Mae,
                    Rmse = x.Rmse,
                    Rmsse = x.Rmsse,
                    LogMessage = x.LogMessage
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ActivateModelAsync(int modelVersionId)
        {
            var model = await _db.ModelVersions
                .FirstOrDefaultAsync(x =>
                    x.ModelVersionId == modelVersionId);

            if (model == null)
                return false;

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                await _db.ModelVersions
                    .Where(x => x.IsActive)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(x => x.IsActive, false));

                model.IsActive = true;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}