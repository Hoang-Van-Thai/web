using web.ViewModels;

namespace web.Services
{
    public interface IModelTrainingService
    {
        Task<ModelTrainingViewModel> GetIndexAsync();

        Task<ModelVersionCreateViewModel?> GetCreateViewModelAsync();

        Task<bool> CreateModelVersionAsync(
            ModelVersionCreateViewModel model,
            int? userId
        );

        Task<ModelTrainingRunItemViewModel?> GetTrainingRunDetailsAsync(
            long id
        );

        Task<bool> ActivateModelAsync(int modelVersionId);
    }
}