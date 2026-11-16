using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public interface ICategoryService
    {
        // Danh sách danh mục
        Task<List<Category>> GetCategoriesAsync(
            string? keyword,
            string? sort,
            int page,
            int pageSize);

        // Đếm số danh mục
        Task<int> CountCategoriesAsync(
            string? keyword);

        // Chi tiết danh mục
        Task<Category?> GetByIdAsync(int id);

        // Kiểm tra mã danh mục trùng
        Task<bool> IsCategoryCodeExistsAsync(
            string categoryCode,
            int? excludeId = null);

        // Thêm danh mục
        Task CreateAsync(CategoryViewModel model);

        // Cập nhật danh mục
        Task UpdateAsync(CategoryViewModel model);

        // Kiểm tra danh mục có sản phẩm liên kết
        Task<bool> HasProductsAsync(int id);

        // Xóa danh mục
        Task<bool> DeleteAsync(int id);

        // Danh sách xuất Excel
        Task<List<Category>> GetCategoriesForExportAsync(
            string? keyword);
    }
}