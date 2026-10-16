using Microsoft.Owin.Security;
using Source.Models;
using Source.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        private const string GoogleProvider = "Google";

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToHome();
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(
            LoginViewModel model,
            string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string identifier = model.Identifier.Trim().ToLower();

            User user = await db.Users
                .Include("Roles")
                .FirstOrDefaultAsync(u =>
                    (u.username != null && u.username.ToLower() == identifier) ||
                    (u.email != null && u.email.ToLower() == identifier));

            if (user == null ||
                user.active_status != "Active" ||
                string.IsNullOrWhiteSpace(user.hashed_password) ||
                !BCrypt.Net.BCrypt.Verify(
                    model.Password,
                    user.hashed_password))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username/email or password.");

                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            return RedirectToHome();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToHome();
            }

            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username = model.Username.Trim();
            string email = model.Email.Trim().ToLowerInvariant();

            if (await db.Users.AnyAsync(u =>
                u.username != null &&
                u.username.ToLower() == username.ToLower()))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng.");
            }

            if (await db.Users.AnyAsync(u =>
                u.email != null &&
                u.email.ToLower() == email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
            }

            Role studentRole = await GetStudentRoleAsync();
            if (studentRole == null)
            {
                ModelState.AddModelError("", "Hệ thống chưa được cấu hình vai trò học viên. Vui lòng liên hệ quản trị viên.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = new User
            {
                user_id = Guid.NewGuid(),
                username = username,
                email = email,
                hashed_password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                full_name = model.FullName.Trim(),
                auth_provider = null,
                auth_provider_key = null,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                active_status = "Active"
            };

            user.Roles.Add(studentRole);
            EnsureStudentProfile(user);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await SignInUserAsync(user, false);

            return RedirectToHome();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult GoogleLogin(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToHome();
            }

            AuthenticationProperties properties =
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action(
                        "GoogleCallback",
                        "Authentication",
                        new
                        {
                            returnUrl = returnUrl
                        })
                };

            HttpContext.GetOwinContext()
                .Authentication
                .Challenge(
                    properties,
                    "Google");

            return new HttpUnauthorizedResult();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GoogleCallback(
            string returnUrl)
        {
            IAuthenticationManager authentication =
                HttpContext.GetOwinContext().Authentication;

            AuthenticateResult externalResult =
                await authentication.AuthenticateAsync(
                    Source.Startup.ExternalCookieAuthenticationType);

            if (externalResult == null ||
                externalResult.Identity == null ||
                !externalResult.Identity.IsAuthenticated)
            {
                TempData["AuthenticationError"] =
                    "Google authentication failed.";

                return RedirectToAction("Login");
            }

            ClaimsIdentity externalIdentity =
                externalResult.Identity;

            string googleId =
                externalIdentity.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            string email =
                externalIdentity.FindFirst(
                    ClaimTypes.Email)?.Value;

            string fullName =
                externalIdentity.FindFirst(
                    ClaimTypes.Name)?.Value;

            string avatarUrl =
                externalIdentity.FindFirst(
                    "urn:google:picture")?.Value;

            if (string.IsNullOrWhiteSpace(googleId) ||
                string.IsNullOrWhiteSpace(email))
            {
                authentication.SignOut(
                    Source.Startup.ExternalCookieAuthenticationType);

                TempData["AuthenticationError"] =
                    "Google did not return a valid account.";

                return RedirectToAction("Login");
            }

            User user = db.Users
                .Include("Roles")
                .FirstOrDefault(u =>
                    u.auth_provider == GoogleProvider &&
                    u.auth_provider_key == googleId);

            if (user == null)
            {
                user = db.Users
                    .Include("Roles")
                    .FirstOrDefault(u =>
                        u.email == email);
            }

            if (user == null)
            {
                user = CreateGoogleUser(
                    googleId,
                    email,
                    fullName,
                    avatarUrl);
            }
            else
            {
                LinkGoogleAccount(
                    user,
                    googleId,
                    email,
                    fullName,
                    avatarUrl);

                if (user.Roles == null || !user.Roles.Any())
                {
                    Role studentRole = await GetStudentRoleAsync();
                    if (studentRole == null)
                    {
                        authentication.SignOut(
                            Source.Startup.ExternalCookieAuthenticationType);

                        TempData["AuthenticationError"] =
                            "Hệ thống chưa được cấu hình vai trò học viên.";

                        return RedirectToAction("Login");
                    }

                    user.Roles.Add(studentRole);
                }

                EnsureStudentProfile(user);
            }

            if (user.active_status != "Active")
            {
                authentication.SignOut(
                    Source.Startup.ExternalCookieAuthenticationType);

                TempData["AuthenticationError"] =
                    "This account is inactive.";

                return RedirectToAction("Login");
            }

            await db.SaveChangesAsync();

            authentication.SignOut(
                Source.Startup.ExternalCookieAuthenticationType);

            await SignInUserAsync(user, false);

            return RedirectToHome();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            HttpContext.GetOwinContext()
                .Authentication
                .SignOut(
                    Source.Startup.ApplicationCookieAuthenticationType,
                    Source.Startup.ExternalCookieAuthenticationType);

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        private User CreateGoogleUser(
            string googleId,
            string email,
            string fullName,
            string avatarUrl)
        {
            string username = GenerateUniqueUsername(email);

            Role studentRole = GetStudentRole();
            if (studentRole == null)
            {
                throw new InvalidOperationException(
                    "The Student role is not configured in the database.");
            }

            User user = new User
            {
                user_id = Guid.NewGuid(),
                username = username,
                email = email,
                hashed_password = null,
                auth_provider = GoogleProvider,
                auth_provider_key = googleId,
                full_name = string.IsNullOrWhiteSpace(fullName)
                    ? username
                    : fullName,
                avatar_url = avatarUrl,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                active_status = "Active"
            };

            user.Roles.Add(studentRole);
            EnsureStudentProfile(user);
            db.Users.Add(user);

            return user;
        }

        private void EnsureStudentProfile(User user)
        {
            if (db.Student_Profile.Any(p => p.user_id == user.user_id))
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            db.Student_Profile.Add(new Student_Profile
            {
                student_id = Guid.NewGuid(),
                user_id = user.user_id,
                student_code = GenerateUniqueStudentCode(user.username),
                student_status = "Active",
                enrollment_date = now,
                current_level = null,
                learning_goal = null,
                created_at = now,
                updated_at = now,
                available_status = "Available"
            });
        }

        private string GenerateUniqueStudentCode(string username)
        {
            string suffix = new string((username ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(suffix))
            {
                suffix = "USER";
            }

            string code = "STU-" + suffix;
            int number = 1;

            while (db.Student_Profile.Any(p => p.student_code == code))
            {
                number++;
                code = "STU-" + suffix + number;
            }

            return code;
        }

        private void LinkGoogleAccount(
            User user,
            string googleId,
            string email,
            string fullName,
            string avatarUrl)
        {
            user.auth_provider = GoogleProvider;
            user.auth_provider_key = googleId;

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                user.full_name = fullName;
            }

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.avatar_url = avatarUrl;
            }

            user.updated_at = DateTime.UtcNow;
        }

        private Role GetStudentRole()
        {
            return db.Roles.FirstOrDefault(r =>
                r.role_name == "Student" &&
                r.available_status == "Available");
        }

        private Task<Role> GetStudentRoleAsync()
        {
            return db.Roles.FirstOrDefaultAsync(r =>
                r.role_name == "Student" &&
                r.available_status == "Available");
        }

        private string GenerateUniqueUsername(string email)
        {
            string emailPrefix =
                email.Split('@')[0].Trim().ToLowerInvariant();

            string username = emailPrefix;
            int suffix = 1;

            while (db.Users.Any(u => u.username == username))
            {
                suffix++;
                username = emailPrefix + suffix;
            }

            return username;
        }

        private async Task SignInUserAsync(
            User user,
            bool rememberMe)
        {
            ClaimsIdentity identity = new ClaimsIdentity(
                Source.Startup.ApplicationCookieAuthenticationType);

            identity.AddClaim(
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.user_id.ToString()));

            identity.AddClaim(
                new Claim(
                    ClaimTypes.Name,
                    user.username ?? string.Empty));

            identity.AddClaim(
                new Claim(
                    ClaimTypes.Email,
                    user.email ?? string.Empty));

            identity.AddClaim(
                new Claim(
                    "FullName",
                    user.full_name ?? string.Empty));

            foreach (Role role in user.Roles)
            {
                if (!string.IsNullOrWhiteSpace(role.role_name))
                {
                    identity.AddClaim(
                        new Claim(
                            ClaimTypes.Role,
                            role.role_name));
                }
            }

            AuthenticationProperties properties =
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    AllowRefresh = true
                };

            HttpContext.GetOwinContext()
                .Authentication
                .SignIn(properties, identity);

            await Task.CompletedTask;
        }

        private ActionResult RedirectToHome()
        {
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}