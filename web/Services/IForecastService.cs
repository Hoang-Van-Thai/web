using web.ViewModels;

namespace web.Services
{
    public interface IForecastService
    {
        Task<ForecastIndexViewModel> GetIndexAsync();

        Task<ForecastCreateViewModel> GetCreateViewModelAsync();

        Task<long> CreateForecastAsync(
            ForecastCreateViewModel model,
            int? userId,
            CancellationToken cancellationToken = default
        );

        Task<ForecastDetailViewModel?> GetDetailsAsync(long id);
    }
}