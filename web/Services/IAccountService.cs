
//using web.ViewModels;

//namespace web.Services
//{
//    public interface IAccountService
//    {
//        Task<ProfileViewModel?> AuthenticateAsync(string username, string password);

//        Task<ProfileViewModel?> GetProfileAsync(int userId);

//        Task<bool> UpdateProfileAsync(int userId, string fullName, string? email);

//        Task<(bool Success, string Message)> ChangePasswordAsync(
//            int userId,
//            string currentPassword,
//            string newPassword);
//    }
//}
using web.ViewModels;

namespace web.Services
{
    public interface IAccountService
    {
        // Đăng nhập
        Task<ProfileViewModel?> AuthenticateAsync(
            string username,
            string password);

        // Profile
        Task<ProfileViewModel?> GetProfileAsync(int userId);

        Task<bool> UpdateProfileAsync(
            int userId,
            string fullName,
            string? email);

        // Đổi mật khẩu khi đã đăng nhập
        Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);

        // Đăng ký
        Task<(bool Success, string Message)> RegisterAsync(
            RegisterViewModel model);

        // Quên mật khẩu
        Task<(bool Success, string Message)> CreatePasswordResetTokenAsync(
            string email,
            string resetUrl);

        // Đặt lại mật khẩu
        Task<(bool Success, string Message)> ResetPasswordAsync(
            string token,
            string newPassword);
    }
}