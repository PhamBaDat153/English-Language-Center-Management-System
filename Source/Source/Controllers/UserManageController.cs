using Source.Models;
using Source.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class UserManageController : Controller
    {
        // Khai báo DBContext
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        /// <summary>Chuyển các exception chưa được xử lý sang trang lỗi 500 dùng chung.</summary>
        protected override void OnException(ExceptionContext filterContext)
        {
            // Đánh dấu exception đã được MVC xử lý.
            filterContext.ExceptionHandled = true;
            // Hiển thị trang lỗi 500 dùng chung.
            filterContext.Result = View("~/Views/Shared/Error_500.cshtml");
        }

        // GET: Lấy toàn bộ danh sách người dùng và trả về trang User Index.
        // Nếu gặp lỗi thì trả về trang Error.
        [HttpGet]
        /// <summary>Hiển thị danh sách user, hỗ trợ lọc, thống kê và phân trang.</summary>
        /// <param name="username">Tên đăng nhập cần lọc.</param>
        /// <param name="email">Email cần lọc.</param>
        /// <param name="phone">Số điện thoại cần lọc.</param>
        /// <param name="role">Role cần lọc.</param>
        /// <param name="status">Trạng thái tài khoản cần lọc.</param>
        /// <param name="page">Số trang hiện tại.</param>
        /// <returns>View danh sách user hoặc trang lỗi 500.</returns>
        public ActionResult Index(
            string username,
            string email,
            string phone,
            string role,
            string status,
            int page = 1)
        {
            try
            {
                // Quy định số lượng user hiển thị trên mỗi trang.
                const int pageSize = 15;

                page = Math.Max(page, 1);

                // Lấy toàn bộ user để tính thống kê tổng quan.
                var users = db.Users.ToList();
                // Khởi tạo truy vấn để lần lượt thêm các điều kiện lọc.
                var filteredUsers = db.Users.AsQueryable();

                // Lọc theo tên đăng nhập.
                if (!string.IsNullOrWhiteSpace(username))
                {
                    filteredUsers = filteredUsers.Where(
                        u => u.username.Contains(username));
                }

                // Lọc theo email.
                if (!string.IsNullOrWhiteSpace(email))
                {
                    filteredUsers = filteredUsers.Where(
                        u => u.email.Contains(email));
                }

                // Lọc theo số điện thoại.
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    filteredUsers = filteredUsers.Where(
                        u => u.phone_number.Contains(phone));
                }

                // Lọc theo trạng thái tài khoản.
                if (!string.IsNullOrWhiteSpace(status))
                {
                    filteredUsers = filteredUsers.Where(
                        u => u.active_status == status);
                }

                // Lọc theo role được chọn.
                if (!string.IsNullOrWhiteSpace(role))
                {
                    filteredUsers = filteredUsers.Where(
                        u =>
                            u.Roles.Any(r => r.role_name == role) &&
                            (
                                role == "Administrator" ||
                                (
                                    role == "Teacher" &&
                                    !u.Roles.Any(
                                        r => r.role_name == "Administrator")
                                ) ||
                                (
                                    role == "Student" &&
                                    !u.Roles.Any(
                                        r =>
                                            r.role_name == "Administrator" ||
                                            r.role_name == "Teacher")
                                )
                            ));
                }

                // Summary dùng toàn bộ user;
                // table dùng toàn bộ kết quả sau filter.
                // Tính các số liệu tổng quan cho các metric card.
                var totalUsers = users.Count;

                var totalStudents = users.Count(
                    u =>
                        u.Roles.Any(r => r.role_name == "Student") &&
                        !u.Roles.Any(
                            r =>
                                r.role_name == "Administrator" ||
                                r.role_name == "Teacher"));

                var totalActiveUsers = users.Count(
                    u => u.active_status == "Active");

                var totalInactiveUsers = users.Count(
                    u => u.active_status == "Inactive");

                // Thực thi truy vấn lọc để lấy tổng số user phù hợp.
                var filteredUserCount = filteredUsers.Count();

                var totalPages = (int)Math.Ceiling(
                    (double)filteredUserCount / pageSize);

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                // Chỉ phân trang sau khi đã filter.
                // Sắp xếp và phân trang sau khi đã áp dụng toàn bộ bộ lọc.
                var result = filteredUsers
                    .OrderBy(u => u.full_name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Giữ lại bộ lọc và thông tin phân trang khi render view.
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

                // Trả dữ liệu trang hiện tại về UserManage/Index.
                return View(result);
            }
            catch (Exception ex)
            {
                // Có lỗi khi truy vấn hoặc xử lý dữ liệu thì hiển thị trang lỗi 500.
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        [HttpGet]
        /// <summary>Mở form tạo tài khoản và nạp danh sách role khả dụng.</summary>
        /// <returns>View tạo tài khoản hoặc trang lỗi 500.</returns>
        public ActionResult Create()
        {
            try
            {
                // Lấy các role đang khả dụng để hiển thị trong combobox.
                ViewBag.Roles = new SelectList(
                    db.Roles
                        .Where(r => r.available_status == "Available")
                        .OrderBy(r => r.role_name),
                    "role_id",
                    "role_name");

                return View();
            }
            catch (Exception ex)
            {
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>Kiểm tra và tạo tài khoản cùng role/profile liên quan.</summary>
        /// <param name="user">Dữ liệu tài khoản từ form.</param>
        /// <param name="role_id">ID role được chọn.</param>
        /// <returns>Form có lỗi, redirect về danh sách hoặc trang lỗi 500.</returns>
        public ActionResult Create(User user, Guid? role_id)
        {
            // Kiểm tra username bắt buộc và không được trùng.
            if (string.IsNullOrWhiteSpace(user.username))
            {
                ModelState.AddModelError(
                    "username",
                    "Tên đăng nhập không được để trống.");
            }
            else if (db.Users.Any(u => u.username == user.username))
            {
                ModelState.AddModelError(
                    "username",
                    "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra email bắt buộc và không được trùng.
            if (string.IsNullOrWhiteSpace(user.email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email không được để trống.");
            }
            else if (db.Users.Any(u => u.email == user.email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email đã được sử dụng.");
            }

            if (string.IsNullOrWhiteSpace(user.hashed_password))
            {
                ModelState.AddModelError(
                    "hashed_password",
                    "Mật khẩu không được để trống.");
            }

            // Nếu dữ liệu không hợp lệ, nạp lại role và trả lại form.
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    db.Roles
                        .Where(r => r.available_status == "Available")
                        .OrderBy(r => r.role_name),
                    "role_id",
                    "role_name",
                    role_id);

                return View(user);
            }

            // Tìm role được chọn trong các role đang khả dụng.
            var role = role_id.HasValue
                ? db.Roles.SingleOrDefault(
                    r =>
                        r.role_id == role_id.Value &&
                        r.available_status == "Available")
                : null;

            if (role == null)
            {
                ModelState.AddModelError(
                    "role_id",
                    "Vui lòng chọn vai trò.");

                ViewBag.Roles = new SelectList(
                    db.Roles
                        .Where(r => r.available_status == "Available")
                        .OrderBy(r => r.role_name),
                    "role_id",
                    "role_name",
                    role_id);

                return View(user);
            }

            // Khởi tạo ID và các giá trị mặc định cho user mới.
            user.user_id = Guid.NewGuid();
            user.full_name = user.username;
            user.created_at = DateTime.Now;
            user.updated_at = DateTime.Now;
            user.active_status =
                string.IsNullOrWhiteSpace(user.active_status)
                    ? "Active"
                    : user.active_status;

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
                .Where(
                    r =>
                        roleHierarchy.ContainsKey(r.role_name) &&
                        roleHierarchy[r.role_name] <= selectedLevel)
                .ToList();

            // Thêm user và các role vào Entity Framework context.
            db.Users.Add(user);

            foreach (var assignedRole in rolesToAssign)
            {
                user.Roles.Add(assignedRole);
            }

            var now = DateTime.Now;

            // Student_Profile chỉ được tạo khi role được chọn là Student.
            if (role.role_name == "Student")
            {
                db.Student_Profile.Add(
                    new Student_Profile
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

            if (
                role.role_name == "Teacher" ||
                role.role_name == "Administrator")
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
                    db.Teacher_profile.Add(
                        new Teacher_profile
                        {
                            teacher_id = Guid.NewGuid(),
                            staff_id = staff.staff_id,
                            created_at = now,
                            updated_at = now,
                            available_status = "Available"
                        });
                }
            }

            // Lưu user, role và profile xuống database.
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        /// <summary>Hiển thị thông tin chi tiết của một user.</summary>
        /// <param name="userId">ID user cần xem.</param>
        /// <returns>View chi tiết hoặc 404 nếu không tìm thấy.</returns>
        public ActionResult Details(Guid userId)
        {
            // Nạp user cùng các profile và role liên quan.
            var user = LoadUser(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Chuyển entity sang ViewModel trước khi hiển thị.
            return View(ToDetailsViewModel(user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>Cập nhật thông tin user và các profile liên quan.</summary>
        /// <param name="model">Dữ liệu chi tiết gửi từ form.</param>
        /// <returns>View có lỗi, redirect về chi tiết hoặc trang lỗi 500.</returns>
        public ActionResult Details(UserDetailsViewModel model)
        {
            // Tìm user cần cập nhật từ ID trong form.
            var user = LoadUser(model.UserId);

            if (user == null)
            {
                return HttpNotFound();
            }

            if (
                db.Users.Any(
                    u =>
                        u.user_id != model.UserId &&
                        u.username == model.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại.");
            }

            if (
                db.Users.Any(
                    u =>
                        u.user_id != model.UserId &&
                        u.email == model.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email đã được sử dụng.");
            }

            ValidateChoice(
                model.Gender,
                new[]
                {
                    "Male",
                    "Female",
                    "Other",
                    "PreferNotToSay"
                },
                "Gender",
                "Giới tính");

            ValidateChoice(
                model.ActiveStatus,
                new[]
                {
                    "Active",
                    "Inactive"
                },
                "ActiveStatus",
                "Trạng thái tài khoản");

            ValidateChoice(
                model.SelectedRole,
                new[]
                {
                    "Student",
                    "Teacher",
                    "Administrator"
                },
                "SelectedRole",
                "Vai trò");

            var selectedRole = db.Roles.SingleOrDefault(
                r =>
                    r.role_name == model.SelectedRole &&
                    r.available_status == "Available");

            if (selectedRole == null)
            {
                ModelState.AddModelError(
                    "SelectedRole",
                    "Vai trò không hợp lệ.");
            }

            for (
                var i = 0;
                i < (model.StudentProfiles ??
                     new List<StudentProfileViewModel>()).Count;
                i++)
            {
                var item = model.StudentProfiles[i];

                ValidateChoice(
                    item.StudentStatus,
                    new[]
                    {
                        "Active",
                        "Inactive",
                        "Graduated",
                        "Suspended"
                    },
                    "StudentProfiles[" + i + "].StudentStatus",
                    "Trạng thái học viên");

                ValidateChoice(
                    item.CurrentLevel,
                    new[]
                    {
                        "Beginner",
                        "Elementary",
                        "Intermediate",
                        "UpperIntermediate",
                        "Advanced",
                        "IELTS",
                        "TOEIC"
                    },
                    "StudentProfiles[" + i + "].CurrentLevel",
                    "Trình độ");

                ValidateChoice(
                    item.AvailableStatus,
                    new[]
                    {
                        "Available",
                        "Unavailable"
                    },
                    "StudentProfiles[" + i + "].AvailableStatus",
                    "Trạng thái sử dụng");
            }

            for (
                var i = 0;
                i < (model.StaffProfiles ??
                     new List<StaffProfileViewModel>()).Count;
                i++)
            {
                var item = model.StaffProfiles[i];

                ValidateChoice(
                    item.EmploymentType,
                    new[]
                    {
                        "FullTime",
                        "PartTime",
                        "Contract",
                        "Intern"
                    },
                    "StaffProfiles[" + i + "].EmploymentType",
                    "Loại hình làm việc");

                ValidateChoice(
                    item.EmploymentStatus,
                    new[]
                    {
                        "Active",
                        "OnLeave",
                        "Resigned",
                        "Terminated"
                    },
                    "StaffProfiles[" + i + "].EmploymentStatus",
                    "Trạng thái làm việc");

                ValidateChoice(
                    item.AvailableStatus,
                    new[]
                    {
                        "Available",
                        "Unavailable"
                    },
                    "StaffProfiles[" + i + "].AvailableStatus",
                    "Trạng thái sử dụng");

                for (
                    var j = 0;
                    j < (item.Teachers ??
                         new List<TeacherProfileViewModel>()).Count;
                    j++)
                {
                    var teacher = item.Teachers[j];

                    ValidateChoice(
                        teacher.AvailableStatus,
                        new[]
                        {
                            "Available",
                            "Unavailable"
                        },
                        "StaffProfiles[" + i + "].Teachers[" + j + "].AvailableStatus",
                        "Trạng thái giáo viên");

                    for (
                        var k = 0;
                        k < (teacher.Qualifications ??
                             new List<QualificationViewModel>()).Count;
                        k++)
                    {
                        var qualification = teacher.Qualifications[k];

                        ValidateChoice(
                            qualification.AvailableStatus,
                            new[]
                            {
                                "Available",
                                "Unavailable"
                            },
                            "StaffProfiles[" + i + "].Teachers[" + j +
                            "].Qualifications[" + k + "].AvailableStatus",
                            "Trạng thái chứng chỉ");
                    }
                }
            }

            var studentIds =
                model.StudentProfiles == null
                    ? new HashSet<Guid>()
                    : new HashSet<Guid>(
                        model.StudentProfiles
                            .Where(x => x.StudentId != Guid.Empty)
                            .Select(x => x.StudentId));

            var staffIds =
                model.StaffProfiles == null
                    ? new HashSet<Guid>()
                    : new HashSet<Guid>(
                        model.StaffProfiles
                            .Where(x => x.StaffId != Guid.Empty)
                            .Select(x => x.StaffId));

            var teacherIds = new HashSet<Guid>(
                (model.StaffProfiles ??
                 new List<StaffProfileViewModel>())
                    .SelectMany(
                        x =>
                            x.Teachers ??
                            new List<TeacherProfileViewModel>())
                    .Where(x => x.TeacherId != Guid.Empty)
                    .Select(x => x.TeacherId));

            var qualificationIds = new HashSet<Guid>(
                (model.StaffProfiles ??
                 new List<StaffProfileViewModel>())
                    .SelectMany(
                        x =>
                            x.Teachers ??
                            new List<TeacherProfileViewModel>())
                    .SelectMany(
                        x =>
                            x.Qualifications ??
                            new List<QualificationViewModel>())
                    .Where(x => x.QualificationId != Guid.Empty)
                    .Select(x => x.QualificationId));

            var userTeacherIds = new HashSet<Guid>(
                user.Staff_Profile
                    .SelectMany(x => x.Teacher_profile)
                    .Select(x => x.teacher_id));

            var userQualificationIds = new HashSet<Guid>(
                user.Staff_Profile
                    .SelectMany(x => x.Teacher_profile)
                    .SelectMany(x => x.Teacher_qualification)
                    .Select(x => x.qualification_id));

            if (
                studentIds.Any(
                    id =>
                        !user.Student_Profile.Any(
                            x => x.student_id == id)) ||
                staffIds.Any(
                    id =>
                        !user.Staff_Profile.Any(
                            x => x.staff_id == id)) ||
                teacherIds.Any(
                    id => !userTeacherIds.Contains(id)) ||
                qualificationIds.Any(
                    id => !userQualificationIds.Contains(id)))
            {
                ModelState.AddModelError(
                    "",
                    "Dữ liệu profile không thuộc người dùng này.");
            }

            if (!ModelState.IsValid)
            {
                model.Roles = HighestRole(user);
                model.SelectedRole = HighestRole(user);
                model.PasswordStatus = "Đã thiết lập";

                return View(model);
            }

            // Cập nhật thông tin cơ bản của user.
            user.username = model.Username;
            user.email = model.Email;
            user.full_name = model.FullName;
            user.date_of_birth = model.DateOfBirth;
            user.phone_number = model.PhoneNumber;
            user.gender = model.Gender;
            user.native_language = model.NativeLanguage;
            user.emergency_contact_number =
                model.EmergencyContactNumber;
            user.emergency_contact_name =
                model.EmergencyContactName;
            user.active_status = model.ActiveStatus;
            user.updated_at = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.hashed_password =
                    BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            // Đồng bộ role mới được chọn.
            user.Roles.Clear();
            user.Roles.Add(selectedRole);

            // Tạo hoặc cập nhật profile theo role.
            ApplyRoleProfiles(user, model.SelectedRole);

            foreach (
                var item in model.StudentProfiles ??
                new List<StudentProfileViewModel>())
            {
                var profile = user.Student_Profile.Single(
                    x => x.student_id == item.StudentId);

                profile.student_code =
                    string.IsNullOrWhiteSpace(item.StudentCode)
                        ? profile.student_code
                        : item.StudentCode;

                profile.student_status =
                    KeepValue(
                        item.StudentStatus,
                        profile.student_status);

                profile.enrollment_date = item.EnrollmentDate;

                profile.current_level =
                    KeepValue(
                        item.CurrentLevel,
                        profile.current_level);

                profile.learning_goal = item.LearningGoal;

                profile.available_status =
                    KeepValue(
                        item.AvailableStatus,
                        profile.available_status);

                db.Entry(profile)
                    .Property(x => x.student_status)
                    .IsModified = true;

                db.Entry(profile)
                    .Property(x => x.current_level)
                    .IsModified = true;

                db.Entry(profile)
                    .Property(x => x.available_status)
                    .IsModified = true;

                profile.updated_at = DateTime.Now;
            }

            foreach (
                var item in model.StaffProfiles ??
                new List<StaffProfileViewModel>())
            {
                var profile = user.Staff_Profile.Single(
                    x => x.staff_id == item.StaffId);

                profile.staff_code =
                    string.IsNullOrWhiteSpace(item.StaffCode)
                        ? profile.staff_code
                        : item.StaffCode;

                profile.job_title = item.JobTitle;

                profile.employment_type =
                    KeepValue(
                        item.EmploymentType,
                        profile.employment_type);

                profile.employment_status =
                    KeepValue(
                        item.EmploymentStatus,
                        profile.employment_status);

                profile.department = item.Department;
                profile.salary = item.Salary;
                profile.hire_date = item.HireDate;
                profile.termination_date = item.TerminationDate;

                profile.available_status =
                    KeepValue(
                        item.AvailableStatus,
                        profile.available_status);

                db.Entry(profile)
                    .Property(x => x.employment_type)
                    .IsModified = true;

                db.Entry(profile)
                    .Property(x => x.employment_status)
                    .IsModified = true;

                db.Entry(profile)
                    .Property(x => x.available_status)
                    .IsModified = true;

                profile.updated_at = DateTime.Now;

                foreach (
                    var teacherItem in item.Teachers ??
                    new List<TeacherProfileViewModel>())
                {
                    var teacher = profile.Teacher_profile.Single(
                        x => x.teacher_id == teacherItem.TeacherId);

                    teacher.professional_title =
                        teacherItem.ProfessionalTitle;

                    teacher.years_of_experience =
                        teacherItem.YearsOfExperience;

                    teacher.teaching_bio =
                        teacherItem.TeachingBio;

                    teacher.specializations =
                        teacherItem.Specializations;

                    teacher.available_status =
                        KeepValue(
                            teacherItem.AvailableStatus,
                            teacher.available_status);

                    db.Entry(teacher)
                        .Property(x => x.available_status)
                        .IsModified = true;

                    teacher.updated_at = DateTime.Now;

                    foreach (
                        var qualificationItem in teacherItem.Qualifications ??
                        new List<QualificationViewModel>())
                    {
                        if (qualificationItem.QualificationId == Guid.Empty)
                        {
                            if (
                                !qualificationItem.IsDeleted &&
                                !string.IsNullOrWhiteSpace(
                                    qualificationItem.QualificationName))
                            {
                                teacher.Teacher_qualification.Add(
                                    new Teacher_qualification
                                    {
                                        qualification_id = Guid.NewGuid(),
                                        teacher_id = teacher.teacher_id,
                                        qualification_name =
                                            qualificationItem
                                                .QualificationName
                                                .Trim(),
                                        institution =
                                            qualificationItem.Institution,
                                        issued_date =
                                            qualificationItem.IssuedDate,
                                        expiry_date =
                                            qualificationItem.ExpiryDate,
                                        available_status = "Available",
                                        created_at = DateTime.Now,
                                        updated_at = DateTime.Now
                                    });
                            }

                            continue;
                        }

                        var qualification =
                            teacher.Teacher_qualification.Single(
                                x =>
                                    x.qualification_id ==
                                    qualificationItem.QualificationId);

                        if (qualificationItem.PermanentDelete)
                        {
                            db.Teacher_qualification.Remove(qualification);
                            continue;
                        }

                        qualification.qualification_name =
                            string.IsNullOrWhiteSpace(
                                qualificationItem.QualificationName)
                                ? qualification.qualification_name
                                : qualificationItem.QualificationName;

                        qualification.institution =
                            qualificationItem.Institution;

                        qualification.issued_date =
                            qualificationItem.IssuedDate;

                        qualification.expiry_date =
                            qualificationItem.ExpiryDate;

                        qualification.available_status =
                            KeepValue(
                                qualificationItem.AvailableStatus,
                                qualification.available_status);

                        db.Entry(qualification)
                            .Property(x => x.available_status)
                            .IsModified = true;

                        qualification.updated_at = DateTime.Now;
                    }
                }
            }

            try
            {
                // Lưu toàn bộ thay đổi vào database.
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã lưu thông tin người dùng.";

                return RedirectToAction(
                    "Details",
                    new { userId = model.UserId });
            }
            catch (DbEntityValidationException ex)
            {
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Kiểm tra một giá trị có thuộc danh sách lựa chọn hợp lệ hay không.</summary>
        private void ValidateChoice(
            string value,
            string[] choices,
            string key,
            string label)
        {
            // Thêm lỗi nếu giá trị không nằm trong danh sách được phép.
            if (
                !string.IsNullOrWhiteSpace(value) &&
                !choices.Contains(value))
            {
                ModelState.AddModelError(
                    key,
                    label + " không hợp lệ.");
            }
        }

        /// <summary>Giữ giá trị cũ nếu giá trị mới rỗng, ngược lại trả về giá trị đã trim.</summary>
        private static string KeepValue(
            string submitted,
            string current)
        {
            // Giữ giá trị hiện tại nếu dữ liệu mới rỗng; ngược lại loại bỏ khoảng trắng.
            return string.IsNullOrWhiteSpace(submitted)
                ? current
                : submitted.Trim();
        }

        /// <summary>Đồng bộ trạng thái student/staff/teacher profile theo role của user.</summary>
        private void ApplyRoleProfiles(
            User user,
            string roleName)
        {
            // Đồng bộ trạng thái các profile theo role mới của user.
            var now = DateTime.Now;

            var student =
                user.Student_Profile.FirstOrDefault();

            var staff =
                user.Staff_Profile.FirstOrDefault();

            var teacher =
                staff == null
                    ? null
                    : staff.Teacher_profile.FirstOrDefault();

            if (roleName == "Student")
            {
                if (student == null)
                {
                    student = new Student_Profile
                    {
                        student_id = Guid.NewGuid(),
                        user_id = user.user_id,
                        student_code = "STU-" + user.username,
                        student_status = "Active",
                        available_status = "Available",
                        created_at = now,
                        updated_at = now
                    };

                    db.Student_Profile.Add(student);
                }

                student.available_status = "Available";
                student.updated_at = now;

                if (staff != null)
                {
                    staff.available_status = "Unavailable";
                    staff.updated_at = now;
                }

                if (teacher != null)
                {
                    teacher.available_status = "Unavailable";
                    teacher.updated_at = now;
                }
            }
            else if (roleName == "Teacher")
            {
                EnsureStaffAndTeacher(
                    user,
                    ref staff,
                    ref teacher,
                    now);

                staff.available_status = "Available";
                staff.updated_at = now;

                teacher.available_status = "Available";
                teacher.updated_at = now;

                if (student != null)
                {
                    student.available_status = "Unavailable";
                    student.updated_at = now;
                }
            }
            else if (roleName == "Administrator")
            {
                EnsureStaffAndTeacher(
                    user,
                    ref staff,
                    ref teacher,
                    now);

                staff.available_status = "Available";
                staff.updated_at = now;

                if (student != null)
                {
                    student.available_status = "Unavailable";
                    student.updated_at = now;
                }

                if (teacher != null)
                {
                    teacher.available_status = "Unavailable";
                    teacher.updated_at = now;
                }
            }
        }

        /// <summary>Tạo staff profile và teacher profile còn thiếu cho user.</summary>
        private void EnsureStaffAndTeacher(
            User user,
            ref Staff_Profile staff,
            ref Teacher_profile teacher,
            DateTime now)
        {
            // Tạo staff profile và teacher profile nếu chúng chưa tồn tại.
            if (staff == null)
            {
                staff = new Staff_Profile
                {
                    staff_id = Guid.NewGuid(),
                    user_id = user.user_id,
                    staff_code = "STF-" + user.username,
                    employment_status = "Active",
                    available_status = "Available",
                    created_at = now,
                    updated_at = now
                };

                db.Staff_Profile.Add(staff);
            }

            if (teacher == null)
            {
                teacher = new Teacher_profile
                {
                    teacher_id = Guid.NewGuid(),
                    staff_id = staff.staff_id,
                    available_status = "Available",
                    created_at = now,
                    updated_at = now
                };

                db.Teacher_profile.Add(teacher);
            }
        }

        [HttpGet]
        /// <summary>Xóa user và toàn bộ dữ liệu liên quan trong một transaction.</summary>
        /// <param name="id">ID user cần xóa.</param>
        /// <returns>Redirect về danh sách, 404 hoặc trang lỗi 500.</returns>
        public ActionResult Delete(Guid id)
        {
            // Tìm user và nạp các dữ liệu liên quan trước khi xóa.
            var user = db.Users
                .Include("Student_Profile.Enrollments.AssessmentScores")
                .Include("Student_Profile.Enrollments.AttendanceRecords")
                .Include("Student_Profile.Enrollments.Payments")
                .Include("Staff_Profile.Teacher_profile.Teacher_qualification")
                .Include("Roles")
                .SingleOrDefault(u => u.user_id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Lấy ID học viên để tìm các enrollment phụ thuộc.
                    var studentIds = user.Student_Profile
                        .Select(s => s.student_id)
                        .ToList();

                    var enrollmentIds = db.Enrollments
                        .Where(e => studentIds.Contains(e.student_id))
                        .Select(e => e.enrollment_id)
                        .ToList();

                    // Xóa điểm, điểm danh và thanh toán trước enrollment.
                    db.AssessmentScores.RemoveRange(
                        db.AssessmentScores.Where(
                            x =>
                                enrollmentIds.Contains(x.enrollment_id) ||
                                x.entered_by == id));

                    db.AttendanceRecords.RemoveRange(
                        db.AttendanceRecords.Where(
                            x =>
                                enrollmentIds.Contains(x.enrollment_id) ||
                                x.recorded_by == id));

                    db.Payments.RemoveRange(
                        db.Payments.Where(
                            x =>
                                enrollmentIds.Contains(x.enrollment_id) ||
                                x.recorded_by == id));

                    // Xóa enrollment và hồ sơ học viên.
                    db.Enrollments.RemoveRange(
                        db.Enrollments.Where(
                            x => enrollmentIds.Contains(x.enrollment_id)));

                    db.Student_Profile.RemoveRange(
                        user.Student_Profile);

                    // Lấy ID giáo viên thuộc user.
                    var teacherIds = user.Staff_Profile
                        .SelectMany(s => s.Teacher_profile)
                        .Select(t => t.teacher_id)
                        .ToList();

                    var teacherProfiles = user.Staff_Profile
                        .SelectMany(s => s.Teacher_profile)
                        .ToList();

                    // Gỡ giáo viên khỏi lớp trước khi xóa hồ sơ giáo viên.
                    db.Classes
                        .Where(
                            c =>
                                c.teacher_id.HasValue &&
                                teacherIds.Contains(c.teacher_id.Value))
                        .ToList()
                        .ForEach(c => c.teacher_id = null);

                    // Xóa chứng chỉ, teacher profile và staff profile.
                    db.Teacher_qualification.RemoveRange(
                        db.Teacher_qualification.Where(
                            q => teacherIds.Contains(q.teacher_id)));

                    db.Teacher_profile.RemoveRange(
                        teacherProfiles);

                    db.Staff_Profile.RemoveRange(
                        user.Staff_Profile);

                    // Xóa thông báo, liên kết role và user chính.
                    db.Notifications.RemoveRange(
                        db.Notifications.Where(
                            x => x.user_id == id));

                    user.Roles.Clear();

                    db.Users.Remove(user);

                    // Lưu và commit toàn bộ transaction xóa.
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Đã xóa tài khoản và các dữ liệu liên quan.";

                    return RedirectToAction("Index");
                }
                catch
                {
                    // Hoàn tác nếu một thao tác xóa thất bại.
                    transaction.Rollback();

                    return View("~/Views/Shared/Error_500.cshtml");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>Kiểm tra và upload avatar của user lên Cloudinary.</summary>
        /// <param name="userId">ID user cần cập nhật avatar.</param>
        /// <returns>JSON kết quả upload hoặc trang lỗi 500.</returns>
        public ActionResult UploadAvatar(Guid userId)
        {
            // Tìm user và lấy file avatar từ request.
            var user = db.Users.SingleOrDefault(
                u => u.user_id == userId);

            var file = Request.Files["avatar"];

            var cloudName =
                ConfigurationManager.AppSettings["CloudinaryCloudName"];

            var apiKey =
                ConfigurationManager.AppSettings["CloudinaryApiKey"];

            var apiSecret =
                ConfigurationManager.AppSettings["CloudinaryApiSecret"];

            var folder =
                ConfigurationManager.AppSettings["CloudinaryUploadFolder"]
                ?? "user-avatars";

            // Từ chối request nếu user không tồn tại hoặc file rỗng.
            if (
                user == null ||
                file == null ||
                file.ContentLength == 0)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "Vui lòng chọn ảnh."
                    });
            }

            // Giới hạn dung lượng file tối đa 5 MB.
            if (file.ContentLength > 5 * 1024 * 1024)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "Ảnh không được vượt quá 5 MB."
                    });
            }

            if (
                string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "Cloudinary chưa được cấu hình."
                    });
            }

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (
                extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".webp")
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP."
                    });
            }

            // Tạo multipart request để gửi ảnh lên Cloudinary.
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            using (var stream = file.InputStream)
            {
                var timestamp =
                    DateTimeOffset.UtcNow
                        .ToUnixTimeSeconds()
                        .ToString();

                var signature = CreateCloudinarySignature(
                    folder,
                    timestamp,
                    apiSecret);

                var image = new StreamContent(stream);

                image.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        file.ContentType ??
                        "application/octet-stream");

                content.Add(
                    image,
                    "file",
                    Path.GetFileName(file.FileName));

                content.Add(
                    new StringContent(apiKey),
                    "api_key");

                content.Add(
                    new StringContent(folder),
                    "folder");

                content.Add(
                    new StringContent(timestamp),
                    "timestamp");

                content.Add(
                    new StringContent(signature),
                    "signature");

                var response = client
                    .PostAsync(
                        "https://api.cloudinary.com/v1_1/" +
                        cloudName +
                        "/image/upload",
                        content)
                    .Result;

                if (!response.IsSuccessStatusCode)
                {
                    var cloudinaryError =
                        response.Content
                            .ReadAsStringAsync()
                            .Result;

                    return Json(
                        new
                        {
                            success = false,
                            message =
                                "Không thể tải ảnh lên Cloudinary: " +
                                cloudinaryError
                        });
                }

                var result =
                    Newtonsoft.Json.JsonConvert
                        .DeserializeObject<CloudinaryUploadResult>(
                            response.Content
                                .ReadAsStringAsync()
                                .Result);

                if (
                    result == null ||
                    string.IsNullOrWhiteSpace(result.secure_url))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message =
                                "Cloudinary không trả về đường dẫn ảnh hợp lệ."
                        });
                }

                // Lưu URL ảnh Cloudinary vào tài khoản user.
                user.avatar_url = result.secure_url;
                user.updated_at = DateTime.Now;

                db.Entry(user)
                    .Property(x => x.avatar_url)
                    .IsModified = true;

                db.Entry(user)
                    .Property(x => x.updated_at)
                    .IsModified = true;

                try
                {
                    db.SaveChanges();

                    return Json(
                        new
                        {
                            success = true,
                            url = result.secure_url
                        });
                }
                catch (DbEntityValidationException ex)
                {
                    return View(
                        "~/Views/Shared/Error_500.cshtml");
                }
                catch (DbUpdateException ex)
                {
                    return View(
                        "~/Views/Shared/Error_500.cshtml");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>Ẩn hoặc xóa vĩnh viễn qualification của giáo viên.</summary>
        /// <param name="qualificationId">ID qualification cần xử lý.</param>
        /// <param name="permanent">True để xóa khỏi database, false để ẩn.</param>
        /// <returns>JSON kết quả hoặc trang lỗi 500.</returns>
        public ActionResult DeleteQualification(
            Guid qualificationId,
            bool permanent = false)
        {
            try
            {
                // Tìm chứng chỉ theo ID được gửi từ client.
                var qualification =
                    db.Teacher_qualification
                        .SingleOrDefault(
                            q => q.qualification_id == qualificationId);

                if (qualification == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Không tìm thấy chứng chỉ."
                        });
                }

                // Xóa vĩnh viễn hoặc chỉ đánh dấu không khả dụng.
                if (permanent)
                {
                    db.Teacher_qualification.Remove(qualification);
                }
                else
                {
                    qualification.available_status =
                        "Unavailable";

                    qualification.updated_at =
                        DateTime.Now;
                }

                // Lưu thay đổi và trả kết quả JSON cho client.
                db.SaveChanges();

                return Json(
                    new
                    {
                        success = true,
                        message = permanent
                            ? "Đã xóa chứng chỉ."
                            : "Đã ẩn chứng chỉ."
                    });
            }
            catch (Exception)
            {
                return View(
                    "~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Chuyển lỗi validation của Entity Framework thành chuỗi hiển thị được.</summary>
        private static string ValidationMessage(
            DbEntityValidationException exception)
        {
            // Gom các lỗi validation của Entity Framework thành một chuỗi dễ đọc.
            return string.Join(
                "; ",
                exception.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(
                        x =>
                            x.PropertyName +
                            ": " +
                            x.ErrorMessage));
        }

        private class CloudinaryUploadResult
        {
            // Ánh xạ URL ảnh trả về từ response JSON của Cloudinary.
            public string secure_url { get; set; }
        }

        /// <summary>Tạo chữ ký SHA1 dùng để xác thực request upload Cloudinary.</summary>
        private static string CreateCloudinarySignature(
            string folder,
            string timestamp,
            string apiSecret)
        {
            // Ghép tham số theo chuẩn Cloudinary trước khi băm SHA1.
            var parameters =
                "folder=" +
                folder +
                "&timestamp=" +
                timestamp +
                apiSecret;

            using (var sha1 = SHA1.Create())
            {
                var hash =
                    sha1.ComputeHash(
                        Encoding.UTF8.GetBytes(parameters));

                var builder =
                    new StringBuilder(hash.Length * 2);

                foreach (var value in hash)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>Load user cùng các navigation property cần cho trang chi tiết.</summary>
        /// <param name="userId">ID user cần load.</param>
        /// <returns>User tìm được hoặc null.</returns>
        private User LoadUser(Guid userId)
        {
            // Load user cùng các navigation property phục vụ trang chi tiết.
            return db.Users
                .Include("Student_Profile")
                .Include("Staff_Profile.Teacher_profile.Teacher_qualification")
                .Include("Roles")
                .SingleOrDefault(
                    u => u.user_id == userId);
        }

        /// <summary>Chuyển entity User thành ViewModel dùng bởi form chi tiết.</summary>
        /// <param name="user">Entity user cần chuyển đổi.</param>
        /// <returns>ViewModel chứa thông tin user và các profile.</returns>
        private static UserDetailsViewModel ToDetailsViewModel(
            User user)
        {
            // Chuyển entity User và các profile con thành ViewModel cho form Details.
            return new UserDetailsViewModel
            {
                UserId = user.user_id,
                CreatedAt = user.created_at,
                UpdatedAt = user.updated_at,

                PasswordStatus =
                    string.IsNullOrWhiteSpace(user.hashed_password)
                        ? "Chưa thiết lập"
                        : "Đã thiết lập",

                Roles = HighestRole(user),
                SelectedRole = HighestRole(user),

                Username = user.username,
                Email = user.email,

                AuthProvider = user.auth_provider,
                AuthProviderKey = user.auth_provider_key,

                FullName = user.full_name,
                DateOfBirth = user.date_of_birth,
                PhoneNumber = user.phone_number,
                Gender = user.gender,
                AvatarUrl = user.avatar_url,

                NativeLanguage = user.native_language,

                EmergencyContactNumber =
                    user.emergency_contact_number,

                EmergencyContactName =
                    user.emergency_contact_name,

                ActiveStatus = user.active_status,

                StudentProfiles = user.Student_Profile
                    .Select(
                        p =>
                            new StudentProfileViewModel
                            {
                                StudentId = p.student_id,
                                UserId = p.user_id,
                                StudentCode = p.student_code,
                                StudentStatus = p.student_status,
                                EnrollmentDate = p.enrollment_date,
                                CurrentLevel = p.current_level,
                                LearningGoal = p.learning_goal,
                                CreatedAt = p.created_at,
                                UpdatedAt = p.updated_at,
                                AvailableStatus = p.available_status
                            })
                    .ToList(),

                StaffProfiles = user.Staff_Profile
                    .Select(
                        p =>
                            new StaffProfileViewModel
                            {
                                StaffId = p.staff_id,
                                UserId = p.user_id,
                                StaffCode = p.staff_code,
                                JobTitle = p.job_title,
                                EmploymentType = p.employment_type,
                                EmploymentStatus = p.employment_status,
                                Department = p.department,
                                Salary = p.salary,
                                HireDate = p.hire_date,
                                TerminationDate = p.termination_date,
                                CreatedAt = p.created_at,
                                UpdatedAt = p.updated_at,
                                AvailableStatus = p.available_status,

                                Teachers = p.Teacher_profile
                                    .Select(
                                        t =>
                                            new TeacherProfileViewModel
                                            {
                                                TeacherId = t.teacher_id,
                                                StaffId = t.staff_id,
                                                ProfessionalTitle =
                                                    t.professional_title,
                                                YearsOfExperience =
                                                    t.years_of_experience,
                                                TeachingBio =
                                                    t.teaching_bio,
                                                Specializations =
                                                    t.specializations,
                                                CreatedAt = t.created_at,
                                                UpdatedAt = t.updated_at,
                                                AvailableStatus =
                                                    t.available_status,

                                                Qualifications =
                                                    t.Teacher_qualification
                                                        .Select(
                                                            q =>
                                                                new QualificationViewModel
                                                                {
                                                                    QualificationId =
                                                                        q.qualification_id,
                                                                    TeacherId =
                                                                        q.teacher_id,
                                                                    QualificationName =
                                                                        q.qualification_name,
                                                                    Institution =
                                                                        q.institution,
                                                                    IssuedDate =
                                                                        q.issued_date,
                                                                    ExpiryDate =
                                                                        q.expiry_date,
                                                                    CreatedAt =
                                                                        q.created_at,
                                                                    UpdatedAt =
                                                                        q.updated_at,
                                                                    AvailableStatus =
                                                                        q.available_status,
                                                                    IsDeleted =
                                                                        q.available_status ==
                                                                        "Unavailable"
                                                                })
                                                        .ToList()
                                            })
                                    .ToList()
                            })
                    .ToList()
            };
        }

        /// <summary>Lấy role cao nhất theo thứ tự Administrator, Teacher, Student.</summary>
        /// <param name="user">User cần kiểm tra role.</param>
        /// <returns>Tên role cao nhất.</returns>
        private static string HighestRole(User user)
        {
            // Ưu tiên Administrator, sau đó Teacher, cuối cùng là Student.
            if (
                user.Roles.Any(
                    r => r.role_name == "Administrator"))
            {
                return "Administrator";
            }

            if (
                user.Roles.Any(
                    r => r.role_name == "Teacher"))
            {
                return "Teacher";
            }

            return "Student";
        }
    }
}
