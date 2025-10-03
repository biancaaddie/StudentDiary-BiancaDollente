using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StudentDiary.Services.DTOs;
using System.Text.Json;

namespace StudentDiary.Presentation.Helpers
{
    public static class AuthenticationHelper
    {
        private const string SessionKeyUserId = "UserId";
        private const string SessionKeyUserData = "UserData";

        public static void SignIn(HttpContext context, UserProfileDto user)
        {
            context.Session.SetInt32(SessionKeyUserId, user.Id);
            context.Session.SetString(SessionKeyUserData, JsonSerializer.Serialize(user));
        }

        public static void SignOut(HttpContext context)
        {
            context.Session.Remove(SessionKeyUserId);
            context.Session.Remove(SessionKeyUserData);
            context.Session.Clear();
        }

        public static bool IsAuthenticated(HttpContext context)
        {
            return context.Session.GetInt32(SessionKeyUserId).HasValue;
        }

        public static int? GetUserId(HttpContext context)
        {
            return context.Session.GetInt32(SessionKeyUserId);
        }

        public static UserProfileDto? GetCurrentUser(HttpContext context)
        {
            var userDataJson = context.Session.GetString(SessionKeyUserData);
            if (string.IsNullOrEmpty(userDataJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<UserProfileDto>(userDataJson);
            }
            catch
            {
                return null;
            }
        }
    }

    public class RequireAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!AuthenticationHelper.IsAuthenticated(context.HttpContext))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }
            base.OnActionExecuting(context);
        }
    }

    public class RequireGuestAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (AuthenticationHelper.IsAuthenticated(context.HttpContext))
            {
                context.Result = new RedirectToActionResult("Index", "Diary", null);
            }
            base.OnActionExecuting(context);
        }
    }
}
