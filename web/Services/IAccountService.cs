
using web.ViewModels;

namespace web.Services
{
    public interface IAccountService
    {
        Task<ProfileViewModel?> AuthenticateAsync(string username, string password);

        Task<ProfileViewModel?> GetProfileAsync(int userId);

        Task<bool> UpdateProfileAsync(int userId, string fullName, string? email);

        Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);
    }
}
