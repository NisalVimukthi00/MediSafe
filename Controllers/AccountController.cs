using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using MedSafe.Web.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Controllers
{
    /// <summary>Sign in, sign out and the demo account shortcuts on the login screen.</summary>
    public class AccountController : BaseController
    {
        public AccountController(ANDtrDbContext db, CurrentUserService current, SearchService search)
            : base(db, current, search) { }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (Me != null) return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.DemoUsers = Db.Users.AsNoTracking()
                .OrderBy(u => u.Role).ThenBy(u => u.Name)
                .Take(3)
                .ToList();

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = Db.Users.FirstOrDefault(u => u.Email.ToLower() == email);

            if (user == null || !PasswordHasher.Verify(model.Password ?? string.Empty, user.PasswordHash))
            {
                // same message for unknown email and wrong password
                ModelState.AddModelError(string.Empty,
                    "Those credentials were not recognised. Check the email address and password and try again.");
                return View(model);
            }

            Current.SignIn(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Current.SignOut();
            Notify("You have been signed out. Your reviews, selection and comments are saved and will be here when you sign back in.");
            return RedirectToAction("Login");
        }

        /// <summary>Fills the demo credentials from the login screen (progressive enhancement).</summary>
        [HttpGet]
        public IActionResult Demo(string email, string password)
        {
            return RedirectToAction("Login");
        }
    }

    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Enter your email address.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string? Email { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Enter your password.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string? Password { get; set; }
    }
}
