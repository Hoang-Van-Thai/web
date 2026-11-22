using Microsoft.AspNetCore.Http;
using web.ViewModels;

namespace web.Services;

public interface IModelTrainingService
{
    Task<ModelTrainingViewModel> GetIndexAsync(
        CancellationToken cancellationToken = default);

    Task<List<TrainingStoreOption>> GetStoresAsync(
        CancellationToken cancellationToken = default);

    Task<StoreTrainingPreviewViewModel> GetStorePreviewAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    Task<long> StartCsvTrainingAsync(
        IFormFile file,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<long> StartSqlTrainingAsync(
        int storeId,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<RetrainStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<ModelTrainingRunItemViewModel?> GetTrainingRunDetailsAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateModelAsync(
        int modelVersionId,
        CancellationToken cancellationToken = default);
}