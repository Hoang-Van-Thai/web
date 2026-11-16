
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public interface IOrderService
    {
        Task<(IReadOnlyList<Order> Items, int Total)> SearchAsync(
            string? keyword,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);

        Task<Order?> GetAsync(long id);

        Task<OrderFormViewModel> BuildFormAsync(
            OrderFormViewModel? model = null);

        Task<(bool Success, string Message, long? OrderId)>
            CreateAsync(OrderFormViewModel model, int? userId);

        Task<(bool Success, string Message)>
            CancelAsync(long id, int? userId);

        Task<List<object>> GetWarehousesAsync(int storeId);
    }
}
