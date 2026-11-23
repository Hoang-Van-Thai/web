
//using System.Security.Claims;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using web.Services;
//using web.ViewModels;

//namespace web.Controllers
//{
//    public class AccountController : Controller
//    {
//        private readonly IAccountService _accountService;

//        public AccountController(IAccountService accountService)
//        {
//            _accountService = accountService;
//        }

//        [AllowAnonymous]
//        [HttpGet]
//        public IActionResult Login(string? returnUrl = null)
//        {
//            if (User.Identity?.IsAuthenticated == true)
//                return RedirectToAction("Index", "Home");

//            return View(new LoginViewModel
//            {
//                ReturnUrl = returnUrl
//            });
//        }

//        [AllowAnonymous]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Login(LoginViewModel model)
//        {
//            if (!ModelState.IsValid)
//                return View(model);

//            var profile = await _accountService.AuthenticateAsync(
//                model.Username,
//                model.Password
//            );

//            if (profile == null)
//            {
//                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
//                return View(model);
//            }

//            var claims = CreateClaims(profile);

//            var identity = new ClaimsIdentity(
//                claims,
//                CookieAuthenticationDefaults.AuthenticationScheme
//            );

//            var principal = new ClaimsPrincipal(identity);

//            await HttpContext.SignInAsync(
//                CookieAuthenticationDefaults.AuthenticationScheme,
//                principal,
//                new AuthenticationProperties
//                {
//                    IsPersistent = model.RememberMe,
//                    AllowRefresh = true,
//                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(
//                        model.RememberMe ? 24 : 8
//                    )
//                }
//            );

//            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
//                Url.IsLocalUrl(model.ReturnUrl))
//            {
//                return Redirect(model.ReturnUrl);
//            }

//            return RedirectToAction("Index", "Home");
//        }

//        [Authorize]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Logout()
//        {
//            await HttpContext.SignOutAsync(
//                CookieAuthenticationDefaults.AuthenticationScheme
//            );

//            return RedirectToAction(nameof(Login));
//        }

//        [Authorize]
//        [HttpGet]
//        public async Task<IActionResult> Profile()
//        {
//            var userId = GetCurrentUserId();
//            if (userId == null)
//                return RedirectToAction(nameof(Login));

//            var profile = await _accountService.GetProfileAsync(userId.Value);

//            if (profile == null)
//                return NotFound();

//            return View(profile);
//        }

//        [Authorize]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Profile(ProfileViewModel model)
//        {
//            var userId = GetCurrentUserId();
//            if (userId == null)
//                return RedirectToAction(nameof(Login));

//            if (!ModelState.IsValid)
//            {
//                var current = await _accountService.GetProfileAsync(userId.Value);
//                if (current == null)
//                    return NotFound();

//                model.Username = current.Username;
//                model.RoleName = current.RoleName;
//                model.CreatedAt = current.CreatedAt;
//                return View(model);
//            }

//            var success = await _accountService.UpdateProfileAsync(
//                userId.Value,
//                model.FullName,
//                model.Email
//            );

//            if (!success)
//            {
//                ModelState.AddModelError("", "Không thể cập nhật. Email có thể đã được sử dụng.");

//                var current = await _accountService.GetProfileAsync(userId.Value);
//                if (current == null)
//                    return NotFound();

//                model.Username = current.Username;
//                model.RoleName = current.RoleName;
//                model.CreatedAt = current.CreatedAt;
//                return View(model);
//            }

//            // Lấy lại dữ liệu từ DB, không lấy RoleName từ form gửi lên.
//            var updatedProfile = await _accountService.GetProfileAsync(userId.Value);

//            if (updatedProfile == null)
//                return NotFound();

//            var identity = new ClaimsIdentity(
//                CreateClaims(updatedProfile),
//                CookieAuthenticationDefaults.AuthenticationScheme
//            );

//            await HttpContext.SignInAsync(
//                CookieAuthenticationDefaults.AuthenticationScheme,
//                new ClaimsPrincipal(identity)
//            );

//            TempData["Success"] = "Cập nhật thông tin thành công.";
//            return RedirectToAction(nameof(Profile));
//        }

//        [Authorize]
//        [HttpGet]
//        public IActionResult ChangePassword()
//        {
//            return View(new ChangePasswordViewModel());
//        }

//        [Authorize]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
//        {
//            if (!ModelState.IsValid)
//                return View(model);

//            var userId = GetCurrentUserId();
//            if (userId == null)
//                return RedirectToAction(nameof(Login));

//            var result = await _accountService.ChangePasswordAsync(
//                userId.Value,
//                model.CurrentPassword,
//                model.NewPassword
//            );

//            if (!result.Success)
//            {
//                ModelState.AddModelError("", result.Message);
//                return View(model);
//            }

//            TempData["Success"] = result.Message;
//            return RedirectToAction(nameof(ChangePassword));
//        }

//        [AllowAnonymous]
//        [HttpGet]
//        public IActionResult AccessDenied()
//        {
//            return View();
//        }

//        private int? GetCurrentUserId()
//        {
//            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            return int.TryParse(value, out var userId)
//                ? userId
//                : null;
//        }

//        private static List<Claim> CreateClaims(ProfileViewModel profile)
//        {
//            return new List<Claim>
//            {
//                new Claim(ClaimTypes.NameIdentifier, profile.UserId.ToString()),
//                new Claim(ClaimTypes.Name, profile.FullName),
//                new Claim("Username", profile.Username),
//                new Claim(ClaimTypes.Role, profile.RoleName)
//            };
//        }
//    }
//}
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Services;
using web.ViewModels;

namespace web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(
            IAccountService accountService)
        {
            _accountService = accountService;
        }


        // ============================================================
        // LOGIN
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var profile =
                await _accountService.AuthenticateAsync(
                    model.Username,
                    model.Password);

            if (profile == null)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không chính xác.");

                return View(model);
            }

            var claims = CreateClaims(profile);

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,

                    AllowRefresh = true,

                    ExpiresUtc =
                        DateTimeOffset.UtcNow.AddHours(
                            model.RememberMe ? 24 : 8)
                });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
                Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");
        }


        // ============================================================
        // REGISTER
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result =
                await _accountService.RegisterAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result.Message);

                return View(model);
            }

            TempData["Success"] =
                "Đăng ký tài khoản thành công. " +
                "Bạn có thể đăng nhập.";

            return RedirectToAction(
                nameof(Login));
        }


        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(
                new ForgotPasswordViewModel());
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                null,
                Request.Scheme);

            if (string.IsNullOrWhiteSpace(resetUrl))
            {
                ModelState.AddModelError(
                    "",
                    "Không thể tạo liên kết đặt lại mật khẩu.");

                return View(model);
            }

            var result =
                await _accountService
                    .CreatePasswordResetTokenAsync(
                        model.Email,
                        resetUrl);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result.Message);

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(
                nameof(ForgotPassword));
        }


        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "Liên kết đặt lại mật khẩu không hợp lệ.";

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            return View(
                new ResetPasswordViewModel
                {
                    Token = token
                });
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result =
                await _accountService.ResetPasswordAsync(
                    model.Token,
                    model.NewPassword);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result.Message);

                return View(model);
            }

            TempData["Success"] =
                "Đặt lại mật khẩu thành công. " +
                "Bạn có thể đăng nhập bằng mật khẩu mới.";

            return RedirectToAction(
                nameof(Login));
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                nameof(Login));
        }


        // ============================================================
        // PROFILE
        // ============================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction(nameof(Login));

            var profile =
                await _accountService.GetProfileAsync(
                    userId.Value);

            if (profile == null)
                return NotFound();

            return View(profile);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            ProfileViewModel model)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                var current =
                    await _accountService.GetProfileAsync(
                        userId.Value);

                if (current == null)
                    return NotFound();

                model.Username = current.Username;
                model.RoleName = current.RoleName;
                model.CreatedAt = current.CreatedAt;

                return View(model);
            }

            var success =
                await _accountService.UpdateProfileAsync(
                    userId.Value,
                    model.FullName,
                    model.Email);

            if (!success)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật. " +
                    "Email có thể đã được sử dụng.");

                var current =
                    await _accountService.GetProfileAsync(
                        userId.Value);

                if (current == null)
                    return NotFound();

                model.Username = current.Username;
                model.RoleName = current.RoleName;
                model.CreatedAt = current.CreatedAt;

                return View(model);
            }

            var updatedProfile =
                await _accountService.GetProfileAsync(
                    userId.Value);

            if (updatedProfile == null)
                return NotFound();

            var identity = new ClaimsIdentity(
                CreateClaims(updatedProfile),
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            TempData["Success"] =
                "Cập nhật thông tin thành công.";

            return RedirectToAction(
                nameof(Profile));
        }


        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(
                new ChangePasswordViewModel());
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            if (userId == null)
                return RedirectToAction(nameof(Login));

            var result =
                await _accountService.ChangePasswordAsync(
                    userId.Value,
                    model.CurrentPassword,
                    model.NewPassword);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result.Message);

                return View(model);
            }

            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(ChangePassword));
        }


        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // ============================================================
        // HELPER
        // ============================================================

        private int? GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.TryParse(
                value,
                out var userId)
                ? userId
                : null;
        }


        private static List<Claim> CreateClaims(
            ProfileViewModel profile)
        {
            return new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    profile.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    profile.FullName),

                new Claim(
                    "Username",
                    profile.Username),

                new Claim(
                    ClaimTypes.Role,
                    profile.RoleName)
            };
        }
    }
}