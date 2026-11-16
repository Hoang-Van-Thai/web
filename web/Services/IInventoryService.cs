
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public interface IInventoryService
    {
        Task<InventoryIndexViewModel> GetIndexAsync(
            string? keyword,
            int? warehouseId,
            string? stockStatus);

        Task<InventoryDetailViewModel?> GetDetailAsync(int productId);

        Task<InventoryMovementViewModel> BuildMovementFormAsync(
            InventoryMovementViewModel? model = null);

        Task<(bool Success, string Message)> CreateMovementAsync(
            InventoryMovementViewModel model,
            int? userId);

        Task<InventoryHistoryViewModel> GetHistoryAsync(
            string? keyword,
            string? transactionType,
            int? warehouseId,
            DateTime? fromDate,
            DateTime? toDate);
    }
}
