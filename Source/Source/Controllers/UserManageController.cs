using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class UserManageController : Controller
    {
        //Khai Báo DBcontext
        private readonly NewGen_english_center_dbEntities db = new NewGen_english_center_dbEntities();

        // GET: Lấy toàn bộ danh sách người dùng và trả về trang User Index, nếu gặp lỗi trả về trang error
        public ActionResult Index(string username, string email, string phone, string role, string status, int page = 1)
        {
            try
            {
                const int pageSize = 15;
                page = Math.Max(page, 1);

                var users = db.Users.ToList();
                var filteredUsers = db.Users.AsQueryable();

                if (!string.IsNullOrWhiteSpace(username))
                    filteredUsers = filteredUsers.Where(u => u.username.Contains(username));

                if (!string.IsNullOrWhiteSpace(email))
                    filteredUsers = filteredUsers.Where(u => u.email.Contains(email));

                if (!string.IsNullOrWhiteSpace(phone))
                    filteredUsers = filteredUsers.Where(u => u.phone_number.Contains(phone));

                if (!string.IsNullOrWhiteSpace(status))
                    filteredUsers = filteredUsers.Where(u => u.active_status == status);

                if (!string.IsNullOrWhiteSpace(role))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        u.Roles.Any(r => r.role_name == role) &&
                        (
                            role == "Administrator" ||
                            (role == "Teacher" &&
                             !u.Roles.Any(r => r.role_name == "Administrator")) ||
                            (role == "Student" &&
                             !u.Roles.Any(r =>
                                 r.role_name == "Administrator" ||
                                 r.role_name == "Teacher"))
                        ));
                }

                // Summary dùng toàn bộ user; table dùng toàn bộ kết quả sau filter.
                var totalUsers = users.Count;

                var totalStudents = users.Count(u =>
                    u.Roles.Any(r => r.role_name == "Student") &&
                    !u.Roles.Any(r =>
                        r.role_name == "Administrator" ||
                        r.role_name == "Teacher"));

                var totalActiveUsers = users.Count(u =>
                    u.active_status == "Active");

                var totalInactiveUsers = users.Count(u =>
                    u.active_status == "Inactive");

                var filteredUserCount = filteredUsers.Count();
                var totalPages = (int)Math.Ceiling(
                    (double)filteredUserCount / pageSize);

                if (totalPages > 0 && page > totalPages)
                    page = totalPages;

                // Chỉ phân trang sau khi đã filter
                var result = filteredUsers
                    .OrderBy(u => u.full_name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Username = username;
                ViewBag.Email = email;
                ViewBag.Phone = phone;
                ViewBag.Role = role;
                ViewBag.Status = status;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalUsers = totalUsers;
                ViewBag.FilteredUserCount = filteredUserCount;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.TotalActiveUsers = totalActiveUsers;
                ViewBag.TotalInactiveUsers = totalInactiveUsers;
                ViewBag.PageSize = pageSize;

                return View(result);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Unable to load users.";
                return View("Error");
            }
        }
    }
}
