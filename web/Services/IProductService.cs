
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync(
            string? keyword,
            int? categoryId,
            bool? isActive,
            string? sort,
            int page,
            int pageSize);

        Task<int> CountProductsAsync(
            string? keyword,
            int? categoryId,
            bool? isActive);

        Task<Product?> GetByIdAsync(int id);

        Task<List<Category>> GetCategoriesAsync();

        Task<bool> IsProductCodeExistsAsync(
            string productCode,
            int? excludeId = null);

        Task CreateAsync(ProductViewModel model);

        Task UpdateAsync(ProductViewModel model);

        Task<bool> DeactivateAsync(int id);

        Task<bool> HasTransactionsAsync(int id);

        Task<List<Product>> GetProductsForExportAsync(
            string? keyword,
            int? categoryId,
            bool? isActive);
    }
}
