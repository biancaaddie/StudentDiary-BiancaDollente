using Microsoft.AspNetCore.Mvc;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;
using StudentDiary.Presentation.Helpers;

namespace StudentDiary.Presentation.Controllers
{
    [RequireAuthentication]
    public class DiaryController : Controller
    {
        private readonly IDiaryService _diaryService;

        public DiaryController(IDiaryService diaryService)
        {
            _diaryService = diaryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var entries = await _diaryService.GetUserEntriesAsync(userId.Value);
            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(entries);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDiaryEntryDto createDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
                return View(createDto);
            }

            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _diaryService.CreateEntryAsync(userId.Value, createDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(createDto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var entry = await _diaryService.GetEntryByIdAsync(id, userId.Value);
            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to view it.";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(entry);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var entry = await _diaryService.GetEntryByIdAsync(id, userId.Value);
            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to edit it.";
                return RedirectToAction("Index");
            }

            var updateDto = new UpdateDiaryEntryDto
            {
                Id = entry.Id,
                Title = entry.Title,
                Content = entry.Content
            };

            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateDiaryEntryDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
                return View(updateDto);
            }

            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _diaryService.UpdateEntryAsync(userId.Value, updateDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Details", new { id = updateDto.Id });
            }

            ModelState.AddModelError("", result.Message);
            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(updateDto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var entry = await _diaryService.GetEntryByIdAsync(id, userId.Value);
            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to delete it.";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUser = AuthenticationHelper.GetCurrentUser(HttpContext);
            return View(entry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _diaryService.DeleteEntryAsync(id, userId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
