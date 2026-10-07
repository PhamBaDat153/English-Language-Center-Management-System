using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Source.ViewModels;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using System.IO;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;
using System.Security.Cryptography;
using System.Text;

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


        [HttpGet]
        public ActionResult Details(Guid userId)
        {
            var user = LoadUser(userId);

            if (user == null)
                return HttpNotFound();

            return View(ToDetailsViewModel(user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(UserDetailsViewModel model)
        {
            var user = LoadUser(model.UserId);
            if (user == null)
                return HttpNotFound();

            if (db.Users.Any(u => u.user_id != model.UserId && u.username == model.Username))
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            if (db.Users.Any(u => u.user_id != model.UserId && u.email == model.Email))
                ModelState.AddModelError("Email", "Email đã được sử dụng.");

            ValidateChoice(model.Gender, new[] { "Male", "Female", "Other", "PreferNotToSay" }, "Gender", "Giới tính");
            ValidateChoice(model.ActiveStatus, new[] { "Active", "Inactive" }, "ActiveStatus", "Trạng thái tài khoản");
            ValidateChoice(model.SelectedRole, new[] { "Student", "Teacher", "Administrator" }, "SelectedRole", "Vai trò");
            var selectedRole = db.Roles.SingleOrDefault(r => r.role_name == model.SelectedRole && r.available_status == "Available");
            if (selectedRole == null)
                ModelState.AddModelError("SelectedRole", "Vai trò không hợp lệ.");
            for (var i = 0; i < (model.StudentProfiles ?? new List<StudentProfileViewModel>()).Count; i++)
            {
                var item = model.StudentProfiles[i];
                ValidateChoice(item.StudentStatus, new[] { "Active", "Inactive", "Graduated", "Suspended" }, "StudentProfiles[" + i + "].StudentStatus", "Trạng thái học viên");
                ValidateChoice(item.CurrentLevel, new[] { "Beginner", "Elementary", "Intermediate", "UpperIntermediate", "Advanced", "IELTS", "TOEIC" }, "StudentProfiles[" + i + "].CurrentLevel", "Trình độ");
                ValidateChoice(item.AvailableStatus, new[] { "Available", "Unavailable" }, "StudentProfiles[" + i + "].AvailableStatus", "Trạng thái sử dụng");
            }
            for (var i = 0; i < (model.StaffProfiles ?? new List<StaffProfileViewModel>()).Count; i++)
            {
                var item = model.StaffProfiles[i];
                ValidateChoice(item.EmploymentType, new[] { "FullTime", "PartTime", "Contract", "Intern" }, "StaffProfiles[" + i + "].EmploymentType", "Loại hình làm việc");
                ValidateChoice(item.EmploymentStatus, new[] { "Active", "OnLeave", "Resigned", "Terminated" }, "StaffProfiles[" + i + "].EmploymentStatus", "Trạng thái làm việc");
                ValidateChoice(item.AvailableStatus, new[] { "Available", "Unavailable" }, "StaffProfiles[" + i + "].AvailableStatus", "Trạng thái sử dụng");
                for (var j = 0; j < (item.Teachers ?? new List<TeacherProfileViewModel>()).Count; j++)
                {
                    var teacher = item.Teachers[j];
                    ValidateChoice(teacher.AvailableStatus, new[] { "Available", "Unavailable" }, "StaffProfiles[" + i + "].Teachers[" + j + "].AvailableStatus", "Trạng thái giáo viên");
                    for (var k = 0; k < (teacher.Qualifications ?? new List<QualificationViewModel>()).Count; k++)
                    {
                        var qualification = teacher.Qualifications[k];
                        ValidateChoice(qualification.AvailableStatus, new[] { "Available", "Unavailable" }, "StaffProfiles[" + i + "].Teachers[" + j + "].Qualifications[" + k + "].AvailableStatus", "Trạng thái chứng chỉ");
                    }
                }
            }

            var studentIds = model.StudentProfiles == null ? new HashSet<Guid>() : new HashSet<Guid>(model.StudentProfiles.Where(x => x.StudentId != Guid.Empty).Select(x => x.StudentId));
            var staffIds = model.StaffProfiles == null ? new HashSet<Guid>() : new HashSet<Guid>(model.StaffProfiles.Where(x => x.StaffId != Guid.Empty).Select(x => x.StaffId));
            var teacherIds = new HashSet<Guid>((model.StaffProfiles ?? new List<StaffProfileViewModel>()).SelectMany(x => x.Teachers ?? new List<TeacherProfileViewModel>()).Where(x => x.TeacherId != Guid.Empty).Select(x => x.TeacherId));
            var qualificationIds = new HashSet<Guid>((model.StaffProfiles ?? new List<StaffProfileViewModel>()).SelectMany(x => x.Teachers ?? new List<TeacherProfileViewModel>()).SelectMany(x => x.Qualifications ?? new List<QualificationViewModel>()).Where(x => x.QualificationId != Guid.Empty).Select(x => x.QualificationId));
            var userTeacherIds = new HashSet<Guid>(user.Staff_Profile.SelectMany(x => x.Teacher_profile).Select(x => x.teacher_id));
            var userQualificationIds = new HashSet<Guid>(user.Staff_Profile.SelectMany(x => x.Teacher_profile).SelectMany(x => x.Teacher_qualification).Select(x => x.qualification_id));
            if (studentIds.Any(id => !user.Student_Profile.Any(x => x.student_id == id)) ||
                staffIds.Any(id => !user.Staff_Profile.Any(x => x.staff_id == id)) ||
                teacherIds.Any(id => !userTeacherIds.Contains(id)) ||
                qualificationIds.Any(id => !userQualificationIds.Contains(id)))
                ModelState.AddModelError("", "Dữ liệu profile không thuộc người dùng này.");

            if (!ModelState.IsValid)
            {
                model.Roles = HighestRole(user);
                model.SelectedRole = HighestRole(user);
                model.PasswordStatus = "Đã thiết lập";
                return View(model);
            }

            user.username = model.Username;
            user.email = model.Email;
            user.full_name = model.FullName;
            user.date_of_birth = model.DateOfBirth;
            user.phone_number = model.PhoneNumber;
            user.gender = model.Gender;
            user.native_language = model.NativeLanguage;
            user.emergency_contact_number = model.EmergencyContactNumber;
            user.emergency_contact_name = model.EmergencyContactName;
            user.active_status = model.ActiveStatus;
            user.updated_at = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.hashed_password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            user.Roles.Clear();
            user.Roles.Add(selectedRole);

            ApplyRoleProfiles(user, model.SelectedRole);

            foreach (var item in model.StudentProfiles ?? new List<StudentProfileViewModel>())
            {
                var profile = user.Student_Profile.Single(x => x.student_id == item.StudentId);
                profile.student_code = string.IsNullOrWhiteSpace(item.StudentCode) ? profile.student_code : item.StudentCode;
                profile.student_status = KeepValue(item.StudentStatus, profile.student_status);
                profile.enrollment_date = item.EnrollmentDate;
                profile.current_level = KeepValue(item.CurrentLevel, profile.current_level);
                profile.learning_goal = item.LearningGoal;
                profile.available_status = KeepValue(item.AvailableStatus, profile.available_status);
                db.Entry(profile).Property(x => x.student_status).IsModified = true;
                db.Entry(profile).Property(x => x.current_level).IsModified = true;
                db.Entry(profile).Property(x => x.available_status).IsModified = true;
                profile.updated_at = DateTime.Now;
            }
            foreach (var item in model.StaffProfiles ?? new List<StaffProfileViewModel>())
            {
                var profile = user.Staff_Profile.Single(x => x.staff_id == item.StaffId);
                profile.staff_code = string.IsNullOrWhiteSpace(item.StaffCode) ? profile.staff_code : item.StaffCode;
                profile.job_title = item.JobTitle;
                profile.employment_type = KeepValue(item.EmploymentType, profile.employment_type);
                profile.employment_status = KeepValue(item.EmploymentStatus, profile.employment_status);
                profile.department = item.Department;
                profile.salary = item.Salary;
                profile.hire_date = item.HireDate;
                profile.termination_date = item.TerminationDate;
                profile.available_status = KeepValue(item.AvailableStatus, profile.available_status);
                db.Entry(profile).Property(x => x.employment_type).IsModified = true;
                db.Entry(profile).Property(x => x.employment_status).IsModified = true;
                db.Entry(profile).Property(x => x.available_status).IsModified = true;
                profile.updated_at = DateTime.Now;
                foreach (var teacherItem in item.Teachers ?? new List<TeacherProfileViewModel>())
                {
                    var teacher = profile.Teacher_profile.Single(x => x.teacher_id == teacherItem.TeacherId);
                    teacher.professional_title = teacherItem.ProfessionalTitle;
                    teacher.years_of_experience = teacherItem.YearsOfExperience;
                    teacher.teaching_bio = teacherItem.TeachingBio;
                    teacher.specializations = teacherItem.Specializations;
                    teacher.available_status = KeepValue(teacherItem.AvailableStatus, teacher.available_status);
                    db.Entry(teacher).Property(x => x.available_status).IsModified = true;
                    teacher.updated_at = DateTime.Now;
                    foreach (var qualificationItem in teacherItem.Qualifications ?? new List<QualificationViewModel>())
                    {
                        if (qualificationItem.QualificationId == Guid.Empty)
                        {
                            if (!qualificationItem.IsDeleted && !string.IsNullOrWhiteSpace(qualificationItem.QualificationName))
                                teacher.Teacher_qualification.Add(new Teacher_qualification { qualification_id = Guid.NewGuid(), teacher_id = teacher.teacher_id, qualification_name = qualificationItem.QualificationName.Trim(), institution = qualificationItem.Institution, issued_date = qualificationItem.IssuedDate, expiry_date = qualificationItem.ExpiryDate, available_status = "Available", created_at = DateTime.Now, updated_at = DateTime.Now });
                            continue;
                        }
                        var qualification = teacher.Teacher_qualification.Single(x => x.qualification_id == qualificationItem.QualificationId);
                        if (qualificationItem.PermanentDelete)
                        {
                            db.Teacher_qualification.Remove(qualification);
                            continue;
                        }
                        qualification.qualification_name = string.IsNullOrWhiteSpace(qualificationItem.QualificationName) ? qualification.qualification_name : qualificationItem.QualificationName;
                        qualification.institution = qualificationItem.Institution;
                        qualification.issued_date = qualificationItem.IssuedDate;
                        qualification.expiry_date = qualificationItem.ExpiryDate;
                        qualification.available_status = KeepValue(qualificationItem.AvailableStatus, qualification.available_status);
                        db.Entry(qualification).Property(x => x.available_status).IsModified = true;
                        qualification.updated_at = DateTime.Now;
                    }
                }
            }

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã lưu thông tin người dùng.";
                return RedirectToAction("Details", new { userId = model.UserId });
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entity in ex.EntityValidationErrors)
                    foreach (var error in entity.ValidationErrors)
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                model.Roles = HighestRole(user);
                model.PasswordStatus = "Đã thiết lập";
                return View(model);
            }
        }

        private void ValidateChoice(string value, string[] choices, string key, string label)
        {
            if (!string.IsNullOrWhiteSpace(value) && !choices.Contains(value))
                ModelState.AddModelError(key, label + " không hợp lệ.");
        }

        private static string KeepValue(string submitted, string current)
        {
            return string.IsNullOrWhiteSpace(submitted) ? current : submitted.Trim();
        }

        private void ApplyRoleProfiles(User user, string roleName)
        {
            var now = DateTime.Now;
            var student = user.Student_Profile.FirstOrDefault();
            var staff = user.Staff_Profile.FirstOrDefault();
            var teacher = staff == null ? null : staff.Teacher_profile.FirstOrDefault();

            if (roleName == "Student")
            {
                if (student == null)
                {
                    student = new Student_Profile
                    {
                        student_id = Guid.NewGuid(), user_id = user.user_id,
                        student_code = "STU-" + user.username, student_status = "Active",
                        available_status = "Available", created_at = now, updated_at = now
                    };
                    db.Student_Profile.Add(student);
                }
                student.available_status = "Available";
                student.updated_at = now;
                if (staff != null) { staff.available_status = "Unavailable"; staff.updated_at = now; }
                if (teacher != null) { teacher.available_status = "Unavailable"; teacher.updated_at = now; }
            }
            else if (roleName == "Teacher")
            {
                EnsureStaffAndTeacher(user, ref staff, ref teacher, now);
                staff.available_status = "Available"; staff.updated_at = now;
                teacher.available_status = "Available"; teacher.updated_at = now;
                if (student != null) { student.available_status = "Unavailable"; student.updated_at = now; }
            }
            else if (roleName == "Administrator")
            {
                EnsureStaffAndTeacher(user, ref staff, ref teacher, now);
                staff.available_status = "Available"; staff.updated_at = now;
                if (student != null) { student.available_status = "Unavailable"; student.updated_at = now; }
                if (teacher != null) { teacher.available_status = "Unavailable"; teacher.updated_at = now; }
            }
        }

        private void EnsureStaffAndTeacher(User user, ref Staff_Profile staff, ref Teacher_profile teacher, DateTime now)
        {
            if (staff == null)
            {
                staff = new Staff_Profile
                {
                    staff_id = Guid.NewGuid(), user_id = user.user_id,
                    staff_code = "STF-" + user.username, employment_status = "Active",
                    available_status = "Available", created_at = now, updated_at = now
                };
                db.Staff_Profile.Add(staff);
            }
            if (teacher == null)
            {
                teacher = new Teacher_profile
                {
                    teacher_id = Guid.NewGuid(), staff_id = staff.staff_id,
                    available_status = "Available", created_at = now, updated_at = now
                };
                db.Teacher_profile.Add(teacher);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAvatar(Guid userId)
        {
            var user = db.Users.SingleOrDefault(u => u.user_id == userId);
            var file = Request.Files["avatar"];
            var cloudName = ConfigurationManager.AppSettings["CloudinaryCloudName"];
            var apiKey = ConfigurationManager.AppSettings["CloudinaryApiKey"];
            var apiSecret = ConfigurationManager.AppSettings["CloudinaryApiSecret"];
            var folder = ConfigurationManager.AppSettings["CloudinaryUploadFolder"] ?? "user-avatars";

            if (user == null || file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "Vui lòng chọn ảnh." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "Ảnh không được vượt quá 5 MB." });
            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                return Json(new { success = false, message = "Cloudinary chưa được cấu hình." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
                return Json(new { success = false, message = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP." });

            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            using (var stream = file.InputStream)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var signature = CreateCloudinarySignature(folder, timestamp, apiSecret);
                var image = new StreamContent(stream);
                image.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                content.Add(image, "file", Path.GetFileName(file.FileName));
                content.Add(new StringContent(apiKey), "api_key");
                content.Add(new StringContent(folder), "folder");
                content.Add(new StringContent(timestamp), "timestamp");
                content.Add(new StringContent(signature), "signature");
                var response = client.PostAsync("https://api.cloudinary.com/v1_1/" + cloudName + "/image/upload", content).Result;
                if (!response.IsSuccessStatusCode)
                {
                    var cloudinaryError = response.Content.ReadAsStringAsync().Result;
                    return Json(new { success = false, message = "Không thể tải ảnh lên Cloudinary: " + cloudinaryError });
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<CloudinaryUploadResult>(response.Content.ReadAsStringAsync().Result);
                if (result == null || string.IsNullOrWhiteSpace(result.secure_url))
                    return Json(new { success = false, message = "Cloudinary không trả về đường dẫn ảnh hợp lệ." });
                user.avatar_url = result.secure_url;
                user.updated_at = DateTime.Now;
                db.Entry(user).Property(x => x.avatar_url).IsModified = true;
                db.Entry(user).Property(x => x.updated_at).IsModified = true;

                try
                {
                    db.SaveChanges();
                    return Json(new { success = true, url = result.secure_url });
                }
                catch (DbEntityValidationException ex)
                {
                    return Json(new { success = false, message = "Không thể lưu URL ảnh: " + ValidationMessage(ex) });
                }
                catch (DbUpdateException ex)
                {
                    return Json(new { success = false, message = "Không thể lưu URL ảnh vào cơ sở dữ liệu: " + ex.GetBaseException().Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteQualification(Guid qualificationId, bool permanent = false)
        {
            var qualification = db.Teacher_qualification.SingleOrDefault(q => q.qualification_id == qualificationId);
            if (qualification == null)
                return Json(new { success = false, message = "Không tìm thấy chứng chỉ." });

            if (permanent)
                db.Teacher_qualification.Remove(qualification);
            else
            {
                qualification.available_status = "Unavailable";
                qualification.updated_at = DateTime.Now;
            }

            db.SaveChanges();
            return Json(new { success = true, message = permanent ? "Đã xóa chứng chỉ." : "Đã ẩn chứng chỉ." });
        }

        private static string ValidationMessage(DbEntityValidationException exception)
        {
            return string.Join("; ", exception.EntityValidationErrors
                .SelectMany(x => x.ValidationErrors)
                .Select(x => x.PropertyName + ": " + x.ErrorMessage));
        }

        private class CloudinaryUploadResult
        {
            public string secure_url { get; set; }
        }

        private static string CreateCloudinarySignature(string folder, string timestamp, string apiSecret)
        {
            var parameters = "folder=" + folder + "&timestamp=" + timestamp + apiSecret;
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(parameters));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash)
                    builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }

        private User LoadUser(Guid userId)
        {
            return db.Users
                .Include("Student_Profile")
                .Include("Staff_Profile.Teacher_profile.Teacher_qualification")
                .Include("Roles")
                .SingleOrDefault(u => u.user_id == userId);
        }

        private static UserDetailsViewModel ToDetailsViewModel(User user)
        {
            return new UserDetailsViewModel
            {
                UserId = user.user_id, CreatedAt = user.created_at, UpdatedAt = user.updated_at,
                PasswordStatus = string.IsNullOrWhiteSpace(user.hashed_password) ? "Chưa thiết lập" : "Đã thiết lập",
                Roles = HighestRole(user), SelectedRole = HighestRole(user), Username = user.username, Email = user.email,
                AuthProvider = user.auth_provider, AuthProviderKey = user.auth_provider_key, FullName = user.full_name,
                DateOfBirth = user.date_of_birth, PhoneNumber = user.phone_number, Gender = user.gender, AvatarUrl = user.avatar_url,
                NativeLanguage = user.native_language, EmergencyContactNumber = user.emergency_contact_number,
                EmergencyContactName = user.emergency_contact_name, ActiveStatus = user.active_status,
                StudentProfiles = user.Student_Profile.Select(p => new StudentProfileViewModel
                {
                    StudentId = p.student_id, UserId = p.user_id, StudentCode = p.student_code, StudentStatus = p.student_status,
                    EnrollmentDate = p.enrollment_date, CurrentLevel = p.current_level, LearningGoal = p.learning_goal,
                    CreatedAt = p.created_at, UpdatedAt = p.updated_at, AvailableStatus = p.available_status
                }).ToList(),
                StaffProfiles = user.Staff_Profile.Select(p => new StaffProfileViewModel
                {
                    StaffId = p.staff_id, UserId = p.user_id, StaffCode = p.staff_code, JobTitle = p.job_title,
                    EmploymentType = p.employment_type, EmploymentStatus = p.employment_status, Department = p.department,
                    Salary = p.salary, HireDate = p.hire_date, TerminationDate = p.termination_date,
                    CreatedAt = p.created_at, UpdatedAt = p.updated_at, AvailableStatus = p.available_status,
                    Teachers = p.Teacher_profile.Select(t => new TeacherProfileViewModel
                    {
                        TeacherId = t.teacher_id, StaffId = t.staff_id, ProfessionalTitle = t.professional_title,
                        YearsOfExperience = t.years_of_experience, TeachingBio = t.teaching_bio, Specializations = t.specializations,
                        CreatedAt = t.created_at, UpdatedAt = t.updated_at, AvailableStatus = t.available_status,
                        Qualifications = t.Teacher_qualification.Select(q => new QualificationViewModel
                        {
                            QualificationId = q.qualification_id, TeacherId = q.teacher_id, QualificationName = q.qualification_name,
                            Institution = q.institution, IssuedDate = q.issued_date, ExpiryDate = q.expiry_date,
                            CreatedAt = q.created_at, UpdatedAt = q.updated_at, AvailableStatus = q.available_status,
                            IsDeleted = q.available_status == "Unavailable"
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        private static string HighestRole(User user)
        {
            if (user.Roles.Any(r => r.role_name == "Administrator")) return "Administrator";
            if (user.Roles.Any(r => r.role_name == "Teacher")) return "Teacher";
            return "Student";
        }
    } 
}
