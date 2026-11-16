using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly RetailFlowDBContext _context;

        public CategoryService(RetailFlowDBContext context)
        {
            _context = context;
        }

        // =====================================================
        // 1. DANH SACH DANH MUC
        // =====================================================

        public async Task<List<Category>> GetCategoriesAsync(
            string? keyword,
            string? sort,
            int page,
            int pageSize)
        {
            var query = BuildQuery(keyword);

            query = sort switch
            {
                "code" => query.OrderBy(c => c.CategoryCode),

                "name" => query.OrderBy(c => c.CategoryName),

                "name_desc" => query.OrderByDescending(
                    c => c.CategoryName),

                _ => query.OrderBy(c => c.CategoryId)
            };

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // =====================================================
        // 2. BUILD QUERY
        // =====================================================

        private IQueryable<Category> BuildQuery(string? keyword)
        {
            var query = _context.Categories
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(c =>
                    c.CategoryCode.Contains(keyword) ||
                    c.CategoryName.Contains(keyword));
            }

            return query;
        }

        // =====================================================
        // 3. DEM SO DANH MUC
        // =====================================================

        public async Task<int> CountCategoriesAsync(string? keyword)
        {
            return await BuildQuery(keyword).CountAsync();
        }

        // =====================================================
        // 4. LAY CHI TIET DANH MUC
        // =====================================================

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        // =====================================================
        // 5. KIEM TRA TRUNG MA DANH MUC
        // =====================================================

        public async Task<bool> IsCategoryCodeExistsAsync(
            string categoryCode,
            int? excludeId = null)
        {
            var query = _context.Categories
                .Where(c => c.CategoryCode == categoryCode.Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(
                    c => c.CategoryId != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // =====================================================
        // 6. THEM DANH MUC
        // =====================================================

        public async Task CreateAsync(CategoryViewModel model)
        {
            var category = new Category
            {
                CategoryCode = model.CategoryCode.Trim(),
                CategoryName = model.CategoryName.Trim()
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // 7. CAP NHAT DANH MUC
        // =====================================================

        public async Task UpdateAsync(CategoryViewModel model)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(
                    c => c.CategoryId == model.CategoryId);

            if (category == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy danh mục.");
            }

            category.CategoryCode = model.CategoryCode.Trim();
            category.CategoryName = model.CategoryName.Trim();

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // 8. KIEM TRA SAN PHAM LIEN KET
        // =====================================================

        public async Task<bool> HasProductsAsync(int id)
        {
            return await _context.Products
                .AnyAsync(p => p.CategoryId == id);
        }

        // =====================================================
        // 9. XOA DANH MUC
        // =====================================================

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return false;
            }

            // Không xóa nếu còn sản phẩm thuộc danh mục
            if (await HasProductsAsync(id))
            {
                return false;
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // 10. DU LIEU XUAT CSV
        // =====================================================

        public async Task<List<Category>> GetCategoriesForExportAsync(
            string? keyword)
        {
            return await BuildQuery(keyword)
                .OrderBy(c => c.CategoryCode)
                .ToListAsync();
        }
    }
}