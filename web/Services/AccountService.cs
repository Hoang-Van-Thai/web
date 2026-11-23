
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using web.Models.Entities;
//using web.ViewModels;

//namespace web.Services
//{
//    public class AccountService : IAccountService
//    {
//        private readonly RetailFlowDBContext _db;
//        private readonly IPasswordHasher<User> _passwordHasher;

//        public AccountService(
//            RetailFlowDBContext db,
//            IPasswordHasher<User> passwordHasher)
//        {
//            _db = db;
//            _passwordHasher = passwordHasher;
//        }

//        public async Task<ProfileViewModel?> AuthenticateAsync(
//            string username,
//            string password)
//        {
//            username = username.Trim();

//            var user = await _db.Users
//                .Include(x => x.Role)
//                .FirstOrDefaultAsync(x => x.Username == username);

//            if (user == null || !user.IsActive)
//                return null;

//            if (string.IsNullOrWhiteSpace(user.PasswordHash))
//                return null;

//            var result = _passwordHasher.VerifyHashedPassword(
//                user,
//                user.PasswordHash,
//                password
//            );

//            if (result == PasswordVerificationResult.Failed)
//                return null;

//            // Nâng cấp hash nếu thuật toán/định dạng cần rehash.
//            if (result == PasswordVerificationResult.SuccessRehashNeeded)
//            {
//                user.PasswordHash = _passwordHasher.HashPassword(user, password);
//                await _db.SaveChangesAsync();
//            }

//            return MapProfile(user);
//        }

//        public async Task<ProfileViewModel?> GetProfileAsync(int userId)
//        {
//            var user = await _db.Users
//                .Include(x => x.Role)
//                .FirstOrDefaultAsync(x => x.UserId == userId);

//            return user == null ? null : MapProfile(user);
//        }

//        public async Task<bool> UpdateProfileAsync(
//            int userId,
//            string fullName,
//            string? email)
//        {
//            var user = await _db.Users
//                .FirstOrDefaultAsync(x => x.UserId == userId);

//            if (user == null)
//                return false;

//            fullName = fullName.Trim();
//            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

//            if (string.IsNullOrWhiteSpace(fullName))
//                return false;

//            if (email != null)
//            {
//                var emailExists = await _db.Users.AnyAsync(x =>
//                    x.UserId != userId &&
//                    x.Email == email);

//                if (emailExists)
//                    return false;
//            }

//            user.FullName = fullName;
//            user.Email = email;

//            await _db.SaveChangesAsync();
//            return true;
//        }

//        public async Task<(bool Success, string Message)> ChangePasswordAsync(
//            int userId,
//            string currentPassword,
//            string newPassword)
//        {
//            var user = await _db.Users
//                .FirstOrDefaultAsync(x => x.UserId == userId);

//            if (user == null)
//                return (false, "Không tìm thấy tài khoản.");

//            if (string.IsNullOrWhiteSpace(user.PasswordHash))
//                return (false, "Tài khoản chưa có mật khẩu hợp lệ.");

//            var verify = _passwordHasher.VerifyHashedPassword(
//                user,
//                user.PasswordHash,
//                currentPassword
//            );

//            if (verify == PasswordVerificationResult.Failed)
//                return (false, "Mật khẩu hiện tại không chính xác.");

//            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
//                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

//            if (currentPassword == newPassword)
//                return (false, "Mật khẩu mới phải khác mật khẩu hiện tại.");

//            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

//            await _db.SaveChangesAsync();

//            return (true, "Đổi mật khẩu thành công.");
//        }

//        private static ProfileViewModel MapProfile(User user)
//        {
//            return new ProfileViewModel
//            {
//                UserId = user.UserId,
//                FullName = user.FullName ?? "",
//                Username = user.Username ?? "",
//                Email = user.Email,
//                RoleName = user.Role?.RoleName ?? "Chưa phân quyền",
//                CreatedAt = user.CreatedAt
//            };
//        }
//    }
//}
using System.Security.Cryptography;
using System.Text;
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
        private readonly IEmailService _emailService;

        public AccountService(
            RetailFlowDBContext db,
            IPasswordHasher<User> passwordHasher,
            IEmailService emailService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }


        // ============================================================
        // LOGIN
        // ============================================================

        public async Task<ProfileViewModel?> AuthenticateAsync(
            string username,
            string password)
        {
            username = username.Trim();

            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.Username == username);

            if (user == null || !user.IsActive)
                return null;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (result == PasswordVerificationResult.Failed)
                return null;

            if (result ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        password);

                await _db.SaveChangesAsync();
            }

            return MapProfile(user);
        }


        // ============================================================
        // REGISTER
        // ============================================================

        public async Task<(bool Success, string Message)> RegisterAsync(
            RegisterViewModel model)
        {
            var username = model.Username.Trim();
            var email = model.Email.Trim();
            var fullName = model.FullName.Trim();

            // Kiểm tra username
            var usernameExists = await _db.Users
                .AnyAsync(x => x.Username == username);

            if (usernameExists)
            {
                return (
                    false,
                    "Tên đăng nhập đã tồn tại."
                );
            }

            // Kiểm tra email
            var emailExists = await _db.Users
                .AnyAsync(x =>
                    x.Email != null &&
                    x.Email == email);

            if (emailExists)
            {
                return (
                    false,
                    "Email đã được sử dụng."
                );
            }

            // Tài khoản đăng ký mặc định là Staff
            var staffRole = await _db.Roles
                .FirstOrDefaultAsync(
                    x => x.RoleName == "Staff");

            if (staffRole == null)
            {
                return (
                    false,
                    "Không tìm thấy quyền Staff trong hệ thống."
                );
            }

            var user = new User
            {
                RoleId = staffRole.RoleId,

                FullName = fullName,

                Username = username,

                Email = email,

                IsActive = true,

                CreatedAt = DateTime.Now
            };

            // Hash password
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    model.Password);

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            return (
                true,
                "Đăng ký tài khoản thành công."
            );
        }


        // ============================================================
        // GET PROFILE
        // ============================================================

        public async Task<ProfileViewModel?> GetProfileAsync(
            int userId)
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

            return user == null
                ? null
                : MapProfile(user);
        }


        // ============================================================
        // UPDATE PROFILE
        // ============================================================

        public async Task<bool> UpdateProfileAsync(
            int userId,
            string fullName,
            string? email)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

            if (user == null)
                return false;

            fullName = fullName.Trim();

            email = string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            if (email != null)
            {
                var emailExists = await _db.Users
                    .AnyAsync(x =>
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


        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        public async Task<(bool Success, string Message)>
            ChangePasswordAsync(
                int userId,
                string currentPassword,
                string newPassword)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

            if (user == null)
            {
                return (
                    false,
                    "Không tìm thấy tài khoản."
                );
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return (
                    false,
                    "Tài khoản chưa có mật khẩu hợp lệ."
                );
            }

            var verify =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    currentPassword);

            if (verify ==
                PasswordVerificationResult.Failed)
            {
                return (
                    false,
                    "Mật khẩu hiện tại không chính xác."
                );
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 6)
            {
                return (
                    false,
                    "Mật khẩu mới phải có ít nhất 6 ký tự."
                );
            }

            if (currentPassword == newPassword)
            {
                return (
                    false,
                    "Mật khẩu mới phải khác mật khẩu hiện tại."
                );
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword);

            await _db.SaveChangesAsync();

            return (
                true,
                "Đổi mật khẩu thành công."
            );
        }


        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        public async Task<(bool Success, string Message)>
            CreatePasswordResetTokenAsync(
                string email,
                string resetUrl)
        {
            email = email.Trim();

            var user = await _db.Users
                .FirstOrDefaultAsync(
                    x => x.Email == email &&
                         x.IsActive);

            // Không tiết lộ email có tồn tại hay không
            if (user == null)
            {
                return (
                    true,
                    "Nếu email tồn tại trong hệ thống, " +
                    "liên kết đặt lại mật khẩu đã được gửi."
                );
            }

            // Vô hiệu hóa token cũ
            var oldTokens = await _db.PasswordResetTokens
                .Where(x =>
                    x.UserId == user.UserId &&
                    x.UsedAt == null)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
            {
                oldToken.UsedAt = DateTime.Now;
            }


            // Tạo token ngẫu nhiên
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");


            // Chỉ lưu hash của token vào database
            var tokenHash = ComputeSha256(token);

            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,

                TokenHash = tokenHash,

                ExpiresAt = DateTime.Now.AddMinutes(30),

                CreatedAt = DateTime.Now
            };

            _db.PasswordResetTokens.Add(resetToken);

            await _db.SaveChangesAsync();


            // Tạo link reset password
            var finalResetUrl =
                $"{resetUrl}?token={Uri.EscapeDataString(token)}";


            // Nội dung email
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>

<body style='font-family: Arial, sans-serif;'>

    <h2>RetailFlow - Đặt lại mật khẩu</h2>

    <p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>

    <p>
        Hệ thống nhận được yêu cầu đặt lại mật khẩu
        cho tài khoản RetailFlow của bạn.
    </p>

    <p>
        Liên kết này sẽ hết hạn sau <strong>30 phút</strong>.
    </p>

    <p style='margin: 30px 0;'>

        <a href='{finalResetUrl}'
           style='
                background-color:#0d6efd;
                color:white;
                padding:12px 20px;
                text-decoration:none;
                border-radius:6px;
                display:inline-block;
           '>
            Đặt lại mật khẩu
        </a>

    </p>

    <p>
        Nếu bạn không yêu cầu đặt lại mật khẩu,
        hãy bỏ qua email này.
    </p>

    <hr>

    <p style='color:#777;'>
        RetailFlow - Retail Management
    </p>

</body>
</html>
";


            try
            {
                await _emailService.SendAsync(
                    user.Email!,
                    "RetailFlow - Đặt lại mật khẩu",
                    htmlBody);

                return (
                    true,
                    "Nếu email tồn tại trong hệ thống, " +
                    "liên kết đặt lại mật khẩu đã được gửi."
                );
            }
            catch
            {
                // Email gửi thất bại
                resetToken.UsedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return (
                    false,
                    "Không thể gửi email. " +
                    "Vui lòng kiểm tra cấu hình email."
                );
            }
        }


        // ============================================================
        // RESET PASSWORD
        // ============================================================

        public async Task<(bool Success, string Message)>
            ResetPasswordAsync(
                string token,
                string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (
                    false,
                    "Liên kết đặt lại mật khẩu không hợp lệ."
                );
            }

            var tokenHash = ComputeSha256(token);

            var resetToken =
                await _db.PasswordResetTokens
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x =>
                        x.TokenHash == tokenHash &&
                        x.UsedAt == null);

            if (resetToken == null)
            {
                return (
                    false,
                    "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã được sử dụng."
                );
            }

            // Kiểm tra hết hạn
            if (resetToken.ExpiresAt < DateTime.Now)
            {
                resetToken.UsedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return (
                    false,
                    "Liên kết đặt lại mật khẩu đã hết hạn."
                );
            }

            var user = resetToken.User;

            if (user == null || !user.IsActive)
            {
                return (
                    false,
                    "Tài khoản không hợp lệ."
                );
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 6)
            {
                return (
                    false,
                    "Mật khẩu mới phải có ít nhất 6 ký tự."
                );
            }

            // Hash password mới
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword);

            // Đánh dấu token đã sử dụng
            resetToken.UsedAt = DateTime.Now;


            // Vô hiệu hóa tất cả token reset cũ khác
            var otherTokens =
                await _db.PasswordResetTokens
                    .Where(x =>
                        x.UserId == user.UserId &&
                        x.PasswordResetTokenId !=
                            resetToken.PasswordResetTokenId &&
                        x.UsedAt == null)
                    .ToListAsync();

            foreach (var item in otherTokens)
            {
                item.UsedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return (
                true,
                "Đặt lại mật khẩu thành công."
            );
        }


        // ============================================================
        // SHA256
        // ============================================================

        private static string ComputeSha256(string value)
        {
            var bytes =
                Encoding.UTF8.GetBytes(value);

            var hash =
                SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }


        // ============================================================
        // MAP USER -> PROFILE
        // ============================================================

        private static ProfileViewModel MapProfile(
            User user)
        {
            return new ProfileViewModel
            {
                UserId = user.UserId,

                FullName = user.FullName ?? "",

                Username = user.Username ?? "",

                Email = user.Email,

                RoleName =
                    user.Role?.RoleName ??
                    "Chưa phân quyền",

                CreatedAt = user.CreatedAt
            };
        }
    }
}