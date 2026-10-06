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
        [HttpGet]
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
            catch (Exception ex)
            {
                return View("Error");
            }
        }


        [HttpGet]
        public ActionResult Create() {
            try
            {
                ViewBag.Roles = new SelectList(
                    db.Roles.Where(r => r.available_status == "Available").OrderBy(r => r.role_name),
                    "role_id",
                    "role_name");
                return View();
            } 
            catch (Exception ex) 
            {
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user, Guid? role_id)
        {
            if (string.IsNullOrWhiteSpace(user.username))
                ModelState.AddModelError("username", "Tên đăng nhập không được để trống.");
            else if (db.Users.Any(u => u.username == user.username))
                ModelState.AddModelError("username", "Tên đăng nhập đã tồn tại.");

            if (string.IsNullOrWhiteSpace(user.email))
                ModelState.AddModelError("email", "Email không được để trống.");
            else if (db.Users.Any(u => u.email == user.email))
                ModelState.AddModelError("email", "Email đã được sử dụng.");

            if (string.IsNullOrWhiteSpace(user.hashed_password))
                ModelState.AddModelError("hashed_password", "Mật khẩu không được để trống.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    db.Roles.Where(r => r.available_status == "Available").OrderBy(r => r.role_name),
                    "role_id",
                    "role_name",
                    role_id);
                return View(user);
            }

            var role = role_id.HasValue
                ? db.Roles.SingleOrDefault(r => r.role_id == role_id.Value && r.available_status == "Available")
                : null;

            if (role == null)
            {
                ModelState.AddModelError("role_id", "Vui lòng chọn vai trò.");
                ViewBag.Roles = new SelectList(
                    db.Roles.Where(r => r.available_status == "Available").OrderBy(r => r.role_name),
                    "role_id",
                    "role_name",
                    role_id);
                return View(user);
            }

            user.user_id = Guid.NewGuid();
            user.full_name = user.username;
            user.created_at = DateTime.Now;
            user.updated_at = DateTime.Now;
            user.active_status = string.IsNullOrWhiteSpace(user.active_status) ? "Active" : user.active_status;

            var roleHierarchy = new Dictionary<string, int>
            {
                { "Student", 1 },
                { "Teacher", 2 },
                { "Administrator", 3 }
            };

            var selectedLevel = roleHierarchy[role.role_name];
            var rolesToAssign = db.Roles
                .Where(r => r.available_status == "Available")
                .ToList()
                .Where(r => roleHierarchy.ContainsKey(r.role_name)
                         && roleHierarchy[r.role_name] <= selectedLevel)
                .ToList();

            db.Users.Add(user);
            foreach (var assignedRole in rolesToAssign)
                user.Roles.Add(assignedRole);

            var now = DateTime.Now;

            // Student_Profile is created only for a user whose selected role is Student.
            if (role.role_name == "Student")
            {
                db.Student_Profile.Add(new Student_Profile
                {
                    student_id = Guid.NewGuid(),
                    user_id = user.user_id,
                    student_code = "STU-" + user.username,
                    student_status = "Active",
                    created_at = now,
                    updated_at = now,
                    available_status = "Available"
                });
            }

            if (role.role_name == "Teacher" || role.role_name == "Administrator")
            {
                var staff = new Staff_Profile
                {
                    staff_id = Guid.NewGuid(),
                    user_id = user.user_id,
                    staff_code = "STF-" + user.username,
                    employment_status = "Active",
                    created_at = now,
                    updated_at = now,
                    available_status = "Available"
                };

                db.Staff_Profile.Add(staff);

                if (role.role_name == "Teacher")
                {
                    db.Teacher_profile.Add(new Teacher_profile
                    {
                        teacher_id = Guid.NewGuid(),
                        staff_id = staff.staff_id,
                        created_at = now,
                        updated_at = now,
                        available_status = "Available"
                    });
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }


    }
}
