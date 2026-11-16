
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Models.Entities;
using web.ViewModels;

namespace web.Services
{
    public class AccountService : IAccountService
    {
        private readonly RetailFlowDBContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountService(
            RetailFlowDBContext db,
            IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task<ProfileViewModel?> AuthenticateAsync(
            string username,
            string password)
        {
            username = username.Trim();

            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Username == username);

            if (user == null || !user.IsActive)
                return null;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password
            );

            if (result == PasswordVerificationResult.Failed)
                return null;

            // Nâng cấp hash nếu thuật toán/định dạng cần rehash.
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _db.SaveChangesAsync();
            }

            return MapProfile(user);
        }

        public async Task<ProfileViewModel?> GetProfileAsync(int userId)
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return user == null ? null : MapProfile(user);
        }

        public async Task<bool> UpdateProfileAsync(
            int userId,
            string fullName,
            string? email)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                return false;

            fullName = fullName.Trim();
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            if (email != null)
            {
                var emailExists = await _db.Users.AnyAsync(x =>
                    x.UserId != userId &&
                    x.Email == email);

                if (emailExists)
                    return false;
            }

            user.FullName = fullName;
            user.Email = email;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                return (false, "Không tìm thấy tài khoản.");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return (false, "Tài khoản chưa có mật khẩu hợp lệ.");

            var verify = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword
            );

            if (verify == PasswordVerificationResult.Failed)
                return (false, "Mật khẩu hiện tại không chính xác.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            if (currentPassword == newPassword)
                return (false, "Mật khẩu mới phải khác mật khẩu hiện tại.");

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            await _db.SaveChangesAsync();

            return (true, "Đổi mật khẩu thành công.");
        }

        private static ProfileViewModel MapProfile(User user)
        {
            return new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName ?? "",
                Username = user.Username ?? "",
                Email = user.Email,
                RoleName = user.Role?.RoleName ?? "Chưa phân quyền",
                CreatedAt = user.CreatedAt
            };
        }
    }
}
