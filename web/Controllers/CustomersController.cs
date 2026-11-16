using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Controllers
{
    public class CustomersController : Controller
    {
        private readonly RetailFlowDBContext _db;

        public CustomersController(RetailFlowDBContext db)
        {
            _db = db;
        }

        // GET: /Customers
        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _db.Customers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.CustomerCode.Contains(keyword) ||
                    x.FullName.Contains(keyword) ||
                    (x.Phone != null && x.Phone.Contains(keyword)) ||
                    (x.Email != null && x.Email.Contains(keyword)));
            }

            var items = await query
                .OrderByDescending(x => x.CustomerId)
                .Select(x => new CustomerRowViewModel
                {
                    CustomerId = x.CustomerId,
                    CustomerCode = x.CustomerCode,
                    FullName = x.FullName,
                    Phone = x.Phone,
                    Email = x.Email,

                    OrderCount = x.Orders.Count(o =>
                        o.Status == "Completed"),

                    TotalSpent = x.Orders
                        .Where(o => o.Status == "Completed")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0,

                    LastOrderDate = x.Orders
                        .Where(o => o.Status == "Completed")
                        .Max(o => (DateTime?)o.OrderDate)
                })
                .ToListAsync();

            var model = new CustomerIndexViewModel
            {
                Keyword = keyword,
                Items = items,

                TotalCustomers = await _db.Customers.CountAsync(),

                CustomersWithOrders = await _db.Customers
                    .CountAsync(x => x.Orders.Any(
                        o => o.Status == "Completed")),

                CustomersWithoutOrders = await _db.Customers
                    .CountAsync(x => !x.Orders.Any(
                        o => o.Status == "Completed"))
            };

            return View(model);
        }

        // GET: /Customers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .Include(x => x.Orders)
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
                return NotFound();

            var completedOrders = customer.Orders
                .Where(x => x.Status == "Completed")
                .ToList();

            var model = new CustomerDetailsViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                Gender = customer.Gender,
                DateOfBirth = customer.DateOfBirth,
                CreatedAt = customer.CreatedAt,

                OrderCount = completedOrders.Count,
                TotalSpent = completedOrders.Sum(x => x.TotalAmount),

                LastOrderDate = completedOrders
                    .Select(x => (DateTime?)x.OrderDate)
                    .Max(),

                RecentOrders = completedOrders
                    .OrderByDescending(x => x.OrderDate)
                    .Take(10)
                    .Select(x => new CustomerOrderRowViewModel
                    {
                        OrderId = x.OrderId,
                        OrderCode = x.OrderCode,
                        OrderDate = x.OrderDate,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status
                    })
                    .ToList()
            };

            return View(model);
        }

        // GET: /Customers/Create
        public IActionResult Create()
        {
            return View(new CustomerFormViewModel());
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel model)
        {
            Normalize(model);

            if (await _db.Customers.AnyAsync(
                x => x.CustomerCode == model.CustomerCode))
            {
                ModelState.AddModelError(
                    nameof(model.CustomerCode),
                    "Mã khách hàng đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var customer = new Customer
            {
                CustomerCode = model.CustomerCode,
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                CreatedAt = DateTime.Now
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thêm khách hàng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Customers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
                return NotFound();

            var model = new CustomerFormViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                Gender = customer.Gender,
                DateOfBirth = customer.DateOfBirth
            };

            return View(model);
        }

        // POST: /Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CustomerFormViewModel model)
        {
            if (id != model.CustomerId)
                return BadRequest();

            Normalize(model);

            if (await _db.Customers.AnyAsync(x =>
                x.CustomerCode == model.CustomerCode &&
                x.CustomerId != id))
            {
                ModelState.AddModelError(
                    nameof(model.CustomerCode),
                    "Mã khách hàng đã được sử dụng.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var customer = await _db.Customers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
                return NotFound();

            customer.CustomerCode = model.CustomerCode;
            customer.FullName = model.FullName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;
            customer.Gender = model.Gender;
            customer.DateOfBirth = model.DateOfBirth;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật khách hàng thành công.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers
                .Include(x => x.Orders)
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
                return NotFound();

            if (customer.Orders.Any())
            {
                TempData["Error"] =
                    "Không thể xóa khách hàng đã phát sinh đơn hàng. " +
                    "Bạn nên giữ lại hồ sơ khách hàng để bảo toàn lịch sử giao dịch.";

                return RedirectToAction(nameof(Details), new { id });
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Xóa khách hàng thành công.";

            return RedirectToAction(nameof(Index));
        }

        private static void Normalize(CustomerFormViewModel model)
        {
            model.CustomerCode = model.CustomerCode?.Trim() ?? "";
            model.FullName = model.FullName?.Trim() ?? "";
            model.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null : model.Phone.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email)
                ? null : model.Email.Trim();
            model.Address = string.IsNullOrWhiteSpace(model.Address)
                ? null : model.Address.Trim();
            model.Gender = string.IsNullOrWhiteSpace(model.Gender)
                ? null : model.Gender.Trim();
        }
    }
}