using Microsoft.AspNetCore.Mvc;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;
using StudentDiary.Presentation.Helpers;

namespace StudentDiary.Presentation.Controllers
{
    [RequireAuthentication]
    public class ProfileController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(IAuthService authService, IWebHostEnvironment environment)
        {
            _authService = authService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var profile = await _authService.GetUserProfileAsync(userId.Value);
            if (profile == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction("Index", "Diary");
            }

            ViewBag.CurrentUser = profile;
            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var profile = await _authService.GetUserProfileAsync(userId.Value);
            if (profile == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction("Index", "Diary");
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email
            };

            ViewBag.CurrentUser = profile;
            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProfileDto updateDto)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
                return View(updateDto);
            }

            var result = await _authService.UpdateProfileAsync(userId.Value, updateDto);

            if (result.Success)
            {
                // Update session data with new profile info
                var updatedProfile = await _authService.GetUserProfileAsync(userId.Value);
                if (updatedProfile != null)
                {
                    AuthenticationHelper.SignIn(HttpContext, updatedProfile);
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid image file.";
                return RedirectToAction("Index");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, and GIF files are allowed.";
                return RedirectToAction("Index");
            }

            // Validate file size (max 5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size must be less than 5MB.";
                return RedirectToAction("Index");
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Delete old profile picture if exists
                var currentUser = await _authService.GetUserProfileAsync(userId.Value);
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.ProfilePicturePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, currentUser.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                // Update database with relative path
                var relativePath = $"/uploads/profiles/{fileName}";
                var result = await _authService.UpdateProfilePictureAsync(userId.Value, relativePath);

                if (result.Success)
                {
                    // Update session data
                    var updatedProfile = await _authService.GetUserProfileAsync(userId.Value);
                    if (updatedProfile != null)
                    {
                        AuthenticationHelper.SignIn(HttpContext, updatedProfile);
                    }

                    TempData["SuccessMessage"] = "Profile picture updated successfully.";
                }
                else
                {
                    // Delete the uploaded file if database update failed
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Get current profile picture path
                var currentUser = await _authService.GetUserProfileAsync(userId.Value);
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.ProfilePicturePath))
                {
                    // Delete file from disk
                    var filePath = Path.Combine(_environment.WebRootPath, currentUser.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Update database
                    var result = await _authService.UpdateProfilePictureAsync(userId.Value, null);
                    
                    if (result.Success)
                    {
                        // Update session data
                        var updatedProfile = await _authService.GetUserProfileAsync(userId.Value);
                        if (updatedProfile != null)
                        {
                            AuthenticationHelper.SignIn(HttpContext, updatedProfile);
                        }

                        TempData["SuccessMessage"] = "Profile picture removed successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message;
                    }
                }
                else
                {
                    TempData["InfoMessage"] = "No profile picture to remove.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error removing profile picture: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
