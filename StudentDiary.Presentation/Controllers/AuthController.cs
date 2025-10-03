using Microsoft.AspNetCore.Mvc;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;
using StudentDiary.Presentation.Helpers;

namespace StudentDiary.Presentation.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [RequireGuest]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [RequireGuest]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return View(registerDto);
            }

            var result = await _authService.RegisterAsync(registerDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", result.Message);
            return View(registerDto);
        }

        [RequireGuest]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [RequireGuest]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

            var result = await _authService.LoginAsync(loginDto);

            if (result.Success)
            {
                AuthenticationHelper.SignIn(HttpContext, result.User);
                TempData["SuccessMessage"] = "Welcome back!";
                return RedirectToAction("Index", "Diary");
            }

            ModelState.AddModelError("", result.Message);
            return View(loginDto);
        }

        [RequireAuthentication]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            AuthenticationHelper.SignOut(HttpContext);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [RequireGuest]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [RequireGuest]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return View(forgotPasswordDto);
            }

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            
            TempData["InfoMessage"] = result.Message;
            return View();
        }

        [RequireGuest]
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordDto { Token = token };
            return View(model);
        }

        [RequireGuest]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordDto);
            }

            var result = await _authService.ResetPasswordAsync(resetPasswordDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Password reset successful! Please login with your new password.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", result.Message);
            return View(resetPasswordDto);
        }

        [RequireAuthentication]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
