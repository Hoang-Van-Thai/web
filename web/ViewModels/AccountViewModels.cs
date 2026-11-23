
//using System.ComponentModel.DataAnnotations;

//namespace web.ViewModels
//{
//    public class LoginViewModel
//    {
//        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
//        public string Username { get; set; } = "";

//        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
//        [DataType(DataType.Password)]
//        public string Password { get; set; } = "";

//        public bool RememberMe { get; set; }

//        public string? ReturnUrl { get; set; }
//    }

//    public class ProfileViewModel
//    {
//        public int UserId { get; set; }

//        public string FullName { get; set; } = "";

//        public string Username { get; set; } = "";

//        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
//        public string? Email { get; set; }

//        public string RoleName { get; set; } = "";

//        public DateTime CreatedAt { get; set; }
//    }

//    public class ChangePasswordViewModel
//    {
//        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
//        [DataType(DataType.Password)]
//        public string CurrentPassword { get; set; } = "";

//        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
//        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
//        [DataType(DataType.Password)]
//        public string NewPassword { get; set; } = "";

//        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
//        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
//        [DataType(DataType.Password)]
//        public string ConfirmPassword { get; set; } = "";
//    }
//}
using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    // ============================================================
    // LOGIN
    // ============================================================

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }


    // ============================================================
    // REGISTER
    // ============================================================

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150, ErrorMessage = "Email không được quá 150 ký tự")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(Password),
            ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }


    // ============================================================
    // PROFILE
    // ============================================================

    public class ProfileViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        public string Username { get; set; } = "";

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string RoleName { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }


    // ============================================================
    // CHANGE PASSWORD
    // ============================================================

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }


    // ============================================================
    // FORGOT PASSWORD
    // ============================================================

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";
    }


    // ============================================================
    // RESET PASSWORD
    // ============================================================

    public class ResetPasswordViewModel
    {
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6,
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}