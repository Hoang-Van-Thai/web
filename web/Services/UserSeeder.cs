
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;

namespace web.Services
{
    public class UserSeeder
    {
        private readonly RetailFlowDBContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserSeeder(
            RetailFlowDBContext db,
            IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedUsersAsync()
        {
            var adminRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.RoleName == "Admin");

            var staffRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.RoleName == "Staff");

            if (adminRole == null || staffRole == null)
            {
                throw new Exception(
                    "Không tìm thấy quyền Admin hoặc Staff trong bảng Roles."
                );
            }

            var users = new[]
            {
                new
                {
                    FullName = "Quản trị viên",
                    Username = "admin",
                    Email = "admin@retailflow.com",
                    Password = "Admin@123",
                    RoleId = adminRole.RoleId
                },
                new
                {
                    FullName = "Nguyễn Văn An",
                    Username = "staff01",
                    Email = "staff01@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Trần Thị Bình",
                    Username = "staff02",
                    Email = "staff02@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Lê Minh Cường",
                    Username = "staff03",
                    Email = "staff03@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Phạm Thị Dung",
                    Username = "staff04",
                    Email = "staff04@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Hoàng Văn Nam",
                    Username = "staff05",
                    Email = "staff05@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Nguyễn Thị Hoa",
                    Username = "staff06",
                    Email = "staff06@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Trần Văn Hùng",
                    Username = "staff07",
                    Email = "staff07@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Võ Thị Mai",
                    Username = "staff08",
                    Email = "staff08@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                },
                new
                {
                    FullName = "Đặng Quốc Bảo",
                    Username = "staff09",
                    Email = "staff09@retailflow.com",
                    Password = "Staff@123",
                    RoleId = staffRole.RoleId
                }
            };

            foreach (var item in users)
            {
                bool exists = await _db.Users
                    .AnyAsync(x => x.Username == item.Username);

                if (exists)
                    continue;

                var user = new User
                {
                    RoleId = item.RoleId,
                    FullName = item.FullName,
                    Username = item.Username,
                    Email = item.Email,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    item.Password
                );

                _db.Users.Add(user);
            }

            await _db.SaveChangesAsync();
        }
    }
}
