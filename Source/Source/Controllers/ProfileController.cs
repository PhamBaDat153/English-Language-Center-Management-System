using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Source.Models;
using Source.ViewModels;
using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            User user = await CurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            return View(ToViewModel(user));
        }

        [HttpGet]
        public async Task<ActionResult> Timetable(DateTime? weekStart)
        {
            var user = await CurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Authentication");
            var model = ToViewModel(user, weekStart);
            var selectedDate = (weekStart ?? DateTime.Today).Date;
            var monday = selectedDate.AddDays(-(int)selectedDate.DayOfWeek + (int)DayOfWeek.Monday);
            if (selectedDate.DayOfWeek == DayOfWeek.Sunday) monday = selectedDate.AddDays(-6);
            model.WeekStart = monday;
            model.WeekEnd = monday.AddDays(6);
            model.PreviousWeek = monday.AddDays(-7);
            model.NextWeek = monday.AddDays(7);
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> MyClasses()
        {
            var user = await CurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Authentication");
            return View(ToViewModel(user));
        }

        [HttpGet]
        public async Task<ActionResult> ClassDetails(Guid? classId)
        {
            var user = await CurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Authentication");
            if (!classId.HasValue) return new HttpStatusCodeResult(400);

            var student = user.Student_Profile.FirstOrDefault(p => p.available_status == "Available");
            if (student == null) return HttpNotFound();

            var enrollment = await db.Enrollments
                .Include(e => e.Class.Course.SyllabusMaterials)
                .Include(e => e.Class.Room)
                .Include(e => e.Class.Teacher_profile.Staff_Profile.User)
                .Include(e => e.Class.ClassSessions.Select(s => s.TimeSlot))
                .Include(e => e.AttendanceRecords.Select(a => a.ClassSession.TimeSlot))
                .Include(e => e.AttendanceRecords.Select(a => a.ClassSession))
                .Include(e => e.AssessmentScores.Select(s => s.Assessment))
                .SingleOrDefaultAsync(e => e.class_id == classId.Value && e.student_id == student.student_id);

            if (enrollment == null) return HttpNotFound();

            var assessments = await db.Assessments
                .Where(a => a.class_id == enrollment.class_id)
                .ToListAsync();
            var scoreByAssessment = enrollment.AssessmentScores.ToDictionary(s => s.assessment_id);

            var model = new StudentClassDetailsViewModel
            {
                ClassId = enrollment.Class.class_id,
                ClassCode = enrollment.Class.class_code,
                CourseCode = enrollment.Class.Course.course_code,
                CourseName = enrollment.Class.Course.course_name,
                CourseLevel = enrollment.Class.Course.level,
                CourseDescription = enrollment.Class.Course.description,
                CourseDurationWeeks = enrollment.Class.Course.duration_weeks,
                TeacherName = enrollment.Class.Teacher_profile == null || enrollment.Class.Teacher_profile.Staff_Profile == null
                    ? "Chưa phân công" : enrollment.Class.Teacher_profile.Staff_Profile.User.full_name,
                RoomName = enrollment.Class.Room == null ? "Chưa xếp phòng" : enrollment.Class.Room.room_name,
                StartDate = enrollment.Class.start_date,
                EndDate = enrollment.Class.end_date,
                EnrollmentStatus = enrollment.enrollment_status,
                ClassStatus = enrollment.Class.class_status,
                TuitionAmount = enrollment.tuition_amount,
                Sessions = enrollment.Class.ClassSessions.OrderBy(s => s.session_date).ThenBy(s => s.TimeSlot.start_time).Select(s => new StudentClassSessionViewModel
                {
                    SessionDate = s.session_date,
                    TimeSlotName = s.TimeSlot == null ? "-" : s.TimeSlot.slot_name,
                    Topic = s.topic
                }).ToList(),
                Attendance = enrollment.AttendanceRecords.OrderByDescending(a => a.ClassSession.session_date).Select(a => new StudentAttendanceViewModel
                {
                    SessionDate = a.ClassSession.session_date,
                    TimeSlotName = a.ClassSession.TimeSlot == null ? "-" : a.ClassSession.TimeSlot.slot_name,
                    Status = a.attendance_status,
                    Remarks = a.remarks
                }).ToList(),
                Assessments = assessments.OrderByDescending(a => a.assessment_date).ThenBy(a => a.assessment_name).Select(a => new StudentAssessmentViewModel
                {
                    Name = a.assessment_name,
                    Type = a.assessment_type,
                    MaxScore = a.max_score,
                    AssessmentDate = a.assessment_date,
                    Score = scoreByAssessment.ContainsKey(a.assessment_id) ? (decimal?)scoreByAssessment[a.assessment_id].score : null,
                    Feedback = scoreByAssessment.ContainsKey(a.assessment_id) ? scoreByAssessment[a.assessment_id].feedback : null
                }).ToList(),
                Materials = enrollment.Class.Course.SyllabusMaterials
                    .Where(m => m.available_status == "Available")
                    .OrderBy(m => m.title)
                    .Select(m => new StudentMaterialViewModel { Title = m.title, Description = m.description, FileUrl = m.file_url })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadAvatar(HttpPostedFileBase avatar)
        {
            User user = await CurrentUserAsync();

            if (user == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
            }

            if (avatar == null || avatar.ContentLength == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh." });
            }

            if (avatar.ContentLength > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Ảnh không được vượt quá 5 MB." });
            }

            string extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            {
                return Json(new { success = false, message = "Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP." });
            }

            string cloudName = ConfigurationManager.AppSettings["CloudinaryCloudName"];
            string apiKey = ConfigurationManager.AppSettings["CloudinaryApiKey"];
            string apiSecret = ConfigurationManager.AppSettings["CloudinaryApiSecret"];
            string folder = ConfigurationManager.AppSettings["CloudinaryUploadFolder"] ?? "user-avatars";

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                return Json(new { success = false, message = "Cloudinary chưa được cấu hình." });
            }

            Account account = new Account(cloudName, apiKey, apiSecret);
            Cloudinary cloudinary = new Cloudinary(account);
            ImageUploadParams uploadParams = new ImageUploadParams
            {
                File = new FileDescription(Path.GetFileName(avatar.FileName), avatar.InputStream),
                Folder = folder,
                PublicId = "user-" + user.user_id,
                Overwrite = true,
                Transformation = new Transformation().Width(400).Height(400).Crop("fill").Gravity("face")
            };

            ImageUploadResult uploadResult = await Task.Run(() => cloudinary.Upload(uploadParams));
            if (uploadResult == null || uploadResult.Error != null || uploadResult.SecureUrl == null)
            {
                return Json(new
                {
                    success = false,
                    message = uploadResult?.Error?.Message ?? "Không thể tải ảnh lên Cloudinary."
                });
            }

            user.avatar_url = uploadResult.SecureUrl.ToString();
            user.updated_at = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Json(new { success = true, url = user.avatar_url });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(ProfileViewModel model)
        {
            User user = await CurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            if (!ModelState.IsValid)
            {
                FillReadOnlyFields(model, user);
                return View(model);
            }

            string email = user.email;
            user.full_name = model.FullName.Trim();
            user.date_of_birth = model.DateOfBirth;
            user.phone_number = model.PhoneNumber;
            user.gender = model.Gender;
            user.native_language = model.NativeLanguage;
            user.emergency_contact_number = model.EmergencyContactNumber;
            user.emergency_contact_name = model.EmergencyContactName;
            user.updated_at = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.hashed_password =
                    BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            Student_Profile student = user.Student_Profile.FirstOrDefault();
            if (student != null)
            {
                student.current_level = model.CurrentLevel;
                student.learning_goal = model.LearningGoal;
                student.updated_at = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("Index");
        }

        private async Task<User> CurrentUserAsync()
        {
            string userId = User.Identity is ClaimsIdentity identity
                ? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            Guid id;
            if (!Guid.TryParse(userId, out id))
            {
                return null;
            }

            return await db.Users
                .Include("Roles")
                .Include("Student_Profile")
                .Include("Staff_Profile.Teacher_profile.Teacher_qualification")
                .FirstOrDefaultAsync(u => u.user_id == id);
        }

        private ProfileViewModel ToViewModel(User user, DateTime? timetableWeekStart = null)
        {
            Student_Profile student = user.Student_Profile
                .FirstOrDefault(p => p.available_status == "Available");
            bool isStudent = student != null;
            bool isTeacher = user.Staff_Profile
                .SelectMany(s => s.Teacher_profile)
                .Any(t => t.available_status == "Available");
            bool isAdministrator = user.Roles.Any(r => r.role_name == "Administrator");
            string highestRole = user.Roles
                .Select(r => r.role_name)
                .OrderByDescending(RolePriority)
                .FirstOrDefault() ?? "Chưa phân quyền";

            var activeEnrollments = student == null
                ? new System.Collections.Generic.List<Enrollment>()
                : db.Enrollments
                    .Include(e => e.Class.Course)
                    .Include(e => e.Class.ClassSessions.Select(s => s.TimeSlot))
                    .Include(e => e.Class.Room)
                    .Include(e => e.Class.Teacher_profile.Staff_Profile.User)
                    .Where(e => e.student_id == student.student_id &&
                        e.Class != null &&
                        e.Class.class_status == "InProgress" &&
                        e.Class.ClassSessions.Any() &&
                        (e.enrollment_status == "Pending" ||
                         e.enrollment_status == "Confirmed" ||
                         e.enrollment_status == "Studying"))
                    .ToList();

            var myClasses = activeEnrollments.Select(e => new StudentClassViewModel
            {
                ClassId = e.Class.class_id,
                ClassCode = e.Class.class_code,
                CourseName = e.Class.Course == null ? "-" : e.Class.Course.course_name,
                TeacherName = e.Class.Teacher_profile == null || e.Class.Teacher_profile.Staff_Profile == null
                    ? "Chưa phân công" : e.Class.Teacher_profile.Staff_Profile.User.full_name,
                StartDate = e.Class.start_date,
                EndDate = e.Class.end_date,
                EnrollmentStatus = e.enrollment_status,
                ClassStatus = e.Class.class_status
            }).ToList();

            var timetableSessions = activeEnrollments
                .SelectMany(e => e.Class.ClassSessions
                    .Where(s => s.Class != null && s.Class.class_id == e.class_id && s.TimeSlot != null)
                    .Select(s => new { Enrollment = e, Session = s }))
                .Where(x => !timetableWeekStart.HasValue ||
                    (x.Session.session_date.Date >= timetableWeekStart.Value.Date &&
                     x.Session.session_date.Date <= timetableWeekStart.Value.Date.AddDays(6)))
                .ToList();

            var timetable = timetableSessions.Select(x => new StudentTimetableItemViewModel
                {
                    ClassCode = x.Enrollment.Class.class_code,
                    CourseName = x.Enrollment.Class.Course == null ? "-" : x.Enrollment.Class.Course.course_name,
                    ScheduleDate = x.Session.session_date,
                    WeekDay = x.Session.session_date.DayOfWeek.ToString(),
                    TimeSlotName = x.Session.TimeSlot.slot_name,
                    StartTime = x.Session.TimeSlot.start_time,
                    EndTime = x.Session.TimeSlot.end_time,
                    RoomName = x.Enrollment.Class.Room == null ? "Chưa xếp phòng" : x.Enrollment.Class.Room.room_name,
                    TeacherName = x.Enrollment.Class.Teacher_profile == null || x.Enrollment.Class.Teacher_profile.Staff_Profile == null
                        ? "Chưa phân công" : x.Enrollment.Class.Teacher_profile.Staff_Profile.User.full_name
                })
                .OrderBy(x => x.ScheduleDate)
                .ThenBy(x => x.StartTime)
                .ToList();

            var timetableSlots = db.TimeSlots
                .Where(t => t.available_status == "Available")
                .OrderBy(t => t.start_time)
                .Select(t => new TimetableSlotViewModel
                {
                    SlotName = t.slot_name,
                    StartTime = t.start_time,
                    EndTime = t.end_time
                })
                .ToList();

            return new ProfileViewModel
            {
                UserId = user.user_id,
                Username = user.username,
                Email = user.email,
                AuthProvider = string.IsNullOrWhiteSpace(user.auth_provider)
                    ? "Tài khoản mật khẩu"
                    : user.auth_provider,
                ActiveStatus = user.active_status,
                Roles = string.Join(", ", user.Roles.Select(r => r.role_name)),
                HighestRole = highestRole,
                CreatedAt = user.created_at,
                AvatarUrl = user.avatar_url,
                FullName = user.full_name,
                DateOfBirth = user.date_of_birth,
                PhoneNumber = user.phone_number,
                Gender = user.gender,
                NativeLanguage = user.native_language,
                EmergencyContactNumber = user.emergency_contact_number,
                EmergencyContactName = user.emergency_contact_name,
                HasStudentProfile = student != null,
                StudentCode = student?.student_code,
                StudentStatus = student?.student_status,
                EnrollmentDate = student?.enrollment_date,
                CurrentLevel = student?.current_level,
                LearningGoal = student?.learning_goal,
                StudentAvailableStatus = student?.available_status,
                IsStudent = isStudent,
                IsTeacher = isTeacher,
                IsAdministrator = isAdministrator,
                MyClasses = myClasses,
                Timetable = timetable,
                TimetableSlots = timetableSlots,
                StaffProfiles = user.Staff_Profile
                    .Where(s => s.available_status == "Available")
                    .Select(s => new StaffProfileViewModel
                    {
                        StaffId = s.staff_id,
                        UserId = s.user_id,
                        StaffCode = s.staff_code,
                        JobTitle = s.job_title,
                        EmploymentType = s.employment_type,
                        EmploymentStatus = s.employment_status,
                        Department = s.department,
                        Salary = s.salary,
                        HireDate = s.hire_date,
                        TerminationDate = s.termination_date,
                        CreatedAt = s.created_at,
                        UpdatedAt = s.updated_at,
                        AvailableStatus = s.available_status
                    }).ToList(),
                TeacherProfiles = user.Staff_Profile
                    .Where(s => s.available_status == "Available")
                    .SelectMany(s => s.Teacher_profile)
                    .Where(t => t.available_status == "Available")
                    .Select(t => new TeacherProfileViewModel
                    {
                        TeacherId = t.teacher_id,
                        StaffId = t.staff_id,
                        ProfessionalTitle = t.professional_title,
                        YearsOfExperience = t.years_of_experience,
                        TeachingBio = t.teaching_bio,
                        Specializations = t.specializations,
                        CreatedAt = t.created_at,
                        UpdatedAt = t.updated_at,
                        AvailableStatus = t.available_status
                    }).ToList(),
                Qualifications = user.Staff_Profile
                    .Where(s => s.available_status == "Available")
                    .SelectMany(s => s.Teacher_profile)
                    .Where(t => t.available_status == "Available")
                    .SelectMany(t => t.Teacher_qualification)
                    .Where(q => q.available_status == "Available")
                    .Select(q => new QualificationViewModel
                    {
                        QualificationId = q.qualification_id,
                        TeacherId = q.teacher_id,
                        QualificationName = q.qualification_name,
                        Institution = q.institution,
                        IssuedDate = q.issued_date,
                        ExpiryDate = q.expiry_date,
                        CreatedAt = q.created_at,
                        UpdatedAt = q.updated_at,
                        AvailableStatus = q.available_status
                    }).ToList()
            };
        }

        private static int WeekDayOrder(string weekDay)
        {
            switch (weekDay)
            {
                case "Monday": return 1;
                case "Tuesday": return 2;
                case "Wednesday": return 3;
                case "Thursday": return 4;
                case "Friday": return 5;
                case "Saturday": return 6;
                case "Sunday": return 7;
                default: return 8;
            }
        }

        private static int RolePriority(string role)
        {
            switch (role)
            {
                case "Administrator": return 3;
                case "Teacher": return 2;
                case "Student": return 1;
                default: return 0;
            }
        }

        private void FillReadOnlyFields(ProfileViewModel model, User user)
        {
            ProfileViewModel current = ToViewModel(user);
            model.UserId = current.UserId;
            model.Username = current.Username;
            model.Email = current.Email;
            model.AuthProvider = current.AuthProvider;
            model.ActiveStatus = current.ActiveStatus;
            model.Roles = current.Roles;
            model.HighestRole = current.HighestRole;
            model.CreatedAt = current.CreatedAt;
            model.AvatarUrl = current.AvatarUrl;
            model.StudentCode = current.StudentCode;
            model.StudentStatus = current.StudentStatus;
            model.EnrollmentDate = current.EnrollmentDate;
            model.StudentAvailableStatus = current.StudentAvailableStatus;
            model.HasStudentProfile = current.HasStudentProfile;
            model.IsStudent = current.IsStudent;
            model.IsTeacher = current.IsTeacher;
            model.IsAdministrator = current.IsAdministrator;
            model.StaffProfiles = current.StaffProfiles;
            model.TeacherProfiles = current.TeacherProfiles;
            model.Qualifications = current.Qualifications;
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
