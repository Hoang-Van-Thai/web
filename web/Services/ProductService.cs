
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class ProductService : IProductService
    {
        private readonly RetailFlowDBContext _context;

        public ProductService(RetailFlowDBContext context)
        {
            _context = context;
        }

        // Danh sach san pham
        public async Task<List<Product>> GetProductsAsync(
            string? keyword,
            int? categoryId,
            bool? isActive,
            string? sort,
            int page,
            int pageSize)
        {
            var query = BuildQuery(keyword, categoryId, isActive);

            query = sort switch
            {
                "name" => query.OrderBy(p => p.ProductName),
                "price_asc" => query.OrderBy(p => p.SellingPrice),
                "price_desc" => query.OrderByDescending(p => p.SellingPrice),
                "oldest" => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Dem san pham theo dieu kien
        public async Task<int> CountProductsAsync(
            string? keyword,
            int? categoryId,
            bool? isActive)
        {
            return await BuildQuery(keyword, categoryId, isActive)
                .CountAsync();
        }

        private IQueryable<Product> BuildQuery(
            string? keyword,
            int? categoryId,
            bool? isActive)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(p =>
                    p.ProductCode.Contains(keyword) ||
                    p.ProductName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            return query;
        }

        // Lay chi tiet san pham
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        // Lay danh sach danh muc
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        // Kiem tra trung ma san pham
        public async Task<bool> IsProductCodeExistsAsync(
            string productCode,
            int? excludeId = null)
        {
            var query = _context.Products
                .Where(p => p.ProductCode == productCode.Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.ProductId != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // Them san pham
        public async Task CreateAsync(ProductViewModel model)
        {
            var product = new Product
            {
                CategoryId = model.CategoryId,
                ProductCode = model.ProductCode.Trim(),
                ProductName = model.ProductName.Trim(),
                Unit = model.Unit?.Trim(),
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                MinStock = model.MinStock,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        // Cap nhat san pham
        public async Task UpdateAsync(ProductViewModel model)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            product.CategoryId = model.CategoryId;
            product.ProductCode = model.ProductCode.Trim();
            product.ProductName = model.ProductName.Trim();
            product.Unit = model.Unit?.Trim();
            product.CostPrice = model.CostPrice;
            product.SellingPrice = model.SellingPrice;
            product.MinStock = model.MinStock;
            product.Description = model.Description?.Trim();
            product.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
        }

        // Kiem tra san pham da phat sinh giao dich
        public async Task<bool> HasTransactionsAsync(int id)
        {
            return await _context.OrderDetails.AnyAsync(x => x.ProductId == id)
                || await _context.InventoryTransactions.AnyAsync(x => x.ProductId == id)
                || await _context.InventoryBalances.AnyAsync(x => x.ProductId == id)
                || await _context.PurchaseReceiptDetails.AnyAsync(x => x.ProductId == id)
                || await _context.SalesDailies.AnyAsync(x => x.ProductId == id)
                || await _context.ForecastDetails.AnyAsync(x => x.ProductId == id);
        }

        // Ngung hoat dong san pham thay vi xoa vat ly
        public async Task<bool> DeactivateAsync(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return false;

            if (await HasTransactionsAsync(id))
                return false;

            product.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }

        // Du lieu xuat CSV
        public async Task<List<Product>> GetProductsForExportAsync(
            string? keyword,
            int? categoryId,
            bool? isActive)
        {
            return await BuildQuery(keyword, categoryId, isActive)
                .OrderBy(p => p.ProductCode)
                .ToListAsync();
        }
    }
}
