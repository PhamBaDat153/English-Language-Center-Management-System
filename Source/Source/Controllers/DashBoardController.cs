using Source.Models;
using Source.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

namespace Source.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        private static readonly string[] ActiveEnrollmentStatuses =
        {
            "Pending", "Confirmed", "Studying"
        };

        private static readonly string[] GradeEnrollmentStatuses =
        {
            "Pending", "Confirmed", "Studying", "Completed"
        };

        private static readonly string[] TeachingClassStatuses =
        {
            "Planned", "Open", "InProgress"
        };

        // GET: Dashboard
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            try
            {
                var activeStatuses = ActiveEnrollmentStatuses;
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var nextMonth = monthStart.AddMonths(1);
                var chartStart = monthStart.AddMonths(-5);

                var model = new AdminDashboardViewModel
                {
                    TotalUsers = db.Users.Count(),
                    ActiveUsers = db.Users.Count(u => u.active_status == "Active"),
                    ActiveStudents = db.Student_Profile.Count(s => s.student_status == "Active"),
                    AvailableTeachers = db.Teacher_profile.Count(t => t.available_status == "Available"),
                    AvailableCourses = db.Courses.Count(c => c.available_status == "Available"),
                    OpenClasses = db.Classes.Count(c => c.class_status == "Open"),
                    InProgressClasses = db.Classes.Count(c => c.class_status == "InProgress"),
                    ActiveEnrollments = db.Enrollments.Count(e => activeStatuses.Contains(e.enrollment_status)),
                    PendingPayments = db.Payments.Count(p => p.payment_status == "Pending"),
                    MonthlyRevenue = db.Payments
                        .Where(p => p.payment_status == "Completed" &&
                                    p.payment_date >= monthStart && p.payment_date < nextMonth)
                        .Select(p => (decimal?)p.amount)
                        .Sum() ?? 0m
                };

                model.RevenueByMonth = Enumerable.Range(0, 6)
                    .Select(i => chartStart.AddMonths(i))
                    .Select(month =>
                    {
                        var monthEnd = month.AddMonths(1);
                        return new DashboardChartPointViewModel
                        {
                            Label = month.ToString("MM/yyyy"),
                            Value = db.Payments
                                .Where(p => p.payment_status == "Completed" &&
                                            p.payment_date >= month &&
                                            p.payment_date < monthEnd)
                                .Select(p => (decimal?)p.amount)
                                .Sum() ?? 0m
                        };
                    })
                    .ToList();

                model.EnrollmentByMonth = Enumerable.Range(0, 6)
                    .Select(i => chartStart.AddMonths(i))
                    .Select(month =>
                    {
                        var monthEnd = month.AddMonths(1);
                        return new DashboardChartPointViewModel
                        {
                            Label = month.ToString("MM/yyyy"),
                            Value = db.Enrollments.Count(e =>
                                e.enrollment_date >= month &&
                                e.enrollment_date < monthEnd)
                        };
                    })
                    .ToList();

                model.ClassStatusSummary = new[] { "Planned", "Open", "InProgress", "Completed", "Cancelled" }
                    .Select(status => new DashboardChartPointViewModel
                    {
                        Label = status,
                        Value = db.Classes.Count(c => c.class_status == status)
                    })
                    .ToList();

                model.StudentLevelSummary = new[] { "Beginner", "Elementary", "Intermediate", "UpperIntermediate", "Advanced", "IELTS", "TOEIC" }
                    .Select(level => new DashboardChartPointViewModel
                    {
                        Label = level,
                        Value = db.Student_Profile.Count(s => s.current_level == level && s.student_status == "Active")
                    })
                    .ToList();

                model.PendingPaymentList = db.Payments
                    .AsNoTracking()
                    .Include(p => p.Enrollment.Student_Profile.User)
                    .Include(p => p.Enrollment.Class.Course)
                    .Where(p => p.payment_status == "Pending")
                    .OrderByDescending(p => p.created_at)
                    .Take(8)
                    .ToList()
                    .Select(p => new PendingPaymentViewModel
                    {
                        PaymentId = p.payment_id,
                        StudentName = p.Enrollment?.Student_Profile?.User?.full_name ?? "-",
                        CourseName = p.Enrollment?.Class?.Course?.course_name ?? "-",
                        Amount = p.amount,
                        PaymentDate = p.payment_date
                    })
                    .ToList();

                var classRows = db.Classes
                    .AsNoTracking()
                    .Include(c => c.Course)
                    .Include(c => c.Room)
                    .Include(c => c.Teacher_profile.Staff_Profile.User)
                    .Where(c => c.class_status == "Open" && c.start_date >= today)
                    .OrderBy(c => c.start_date)
                    .Take(12)
                    .ToList();

                var classIds = classRows.Select(c => c.class_id).ToList();
                var enrollmentCounts = db.Enrollments
                    .Where(e => classIds.Contains(e.class_id) && activeStatuses.Contains(e.enrollment_status))
                    .GroupBy(e => e.class_id)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.ClassId, x => x.Count);

                model.UpcomingClasses = classRows
                    .Take(8)
                    .Select(c =>
                    {
                        var enrolled = enrollmentCounts.ContainsKey(c.class_id) ? enrollmentCounts[c.class_id] : 0;
                        var capacity = c.Room == null ? c.class_capacity : Math.Min(c.class_capacity, c.Room.max_capacity);
                        return new UpcomingClassViewModel
                        {
                            ClassId = c.class_id,
                            ClassCode = c.class_code,
                            CourseName = c.Course?.course_name ?? "-",
                            TeacherName = c.Teacher_profile?.Staff_Profile?.User?.full_name ?? "Chưa phân công",
                            RoomName = c.Room?.room_name ?? "Chưa xếp phòng",
                            StartDate = c.start_date,
                            EnrolledCount = enrolled,
                            Capacity = capacity,
                            RemainingSlots = Math.Max(0, capacity - enrolled)
                        };
                    })
                    .ToList();

                model.NearlyFullClasses = classRows
                    .Select(c =>
                    {
                        var enrolled = enrollmentCounts.ContainsKey(c.class_id) ? enrollmentCounts[c.class_id] : 0;
                        var capacity = c.Room == null ? c.class_capacity : Math.Min(c.class_capacity, c.Room.max_capacity);
                        var occupancy = capacity <= 0 ? 0m : enrolled * 100m / capacity;
                        return new NearlyFullClassViewModel
                        {
                            ClassId = c.class_id,
                            ClassCode = c.class_code,
                            CourseName = c.Course?.course_name ?? "-",
                            EnrolledCount = enrolled,
                            Capacity = capacity,
                            OccupancyRate = occupancy
                        };
                    })
                    .Where(c => c.OccupancyRate >= 80m)
                    .OrderByDescending(c => c.OccupancyRate)
                    .Take(8)
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.Index failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }
        //----------------------------------------------------------// EXCEL



        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public ActionResult ExportExcel()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var chartStart = monthStart.AddMonths(-5);

            try
            {
                using (var package = new ExcelPackage())
                {
                    // 1. Tổng quan Dashboard
                    var summaryRows = new List<object[]>
            {
                new object[] { "Tổng số người dùng", db.Users.Count() },
                new object[] { "Người dùng đang hoạt động",
                    db.Users.Count(u => u.active_status == "Active") },
                new object[] { "Học viên đang hoạt động",
                    db.Student_Profile.Count(s => s.student_status == "Active") },
                new object[] { "Giáo viên sẵn sàng",
                    db.Teacher_profile.Count(t => t.available_status == "Available") },
                new object[] { "Khóa học khả dụng",
                    db.Courses.Count(c => c.available_status == "Available") },
                new object[] { "Lớp đang mở",
                    db.Classes.Count(c => c.class_status == "Open") },
                new object[] { "Lớp đang học",
                    db.Classes.Count(c => c.class_status == "InProgress") },
                new object[] { "Đăng ký đang hoạt động",
                    db.Enrollments.Count(e =>
                        ActiveEnrollmentStatuses.Contains(e.enrollment_status)) },
                new object[] { "Thanh toán đang chờ",
                    db.Payments.Count(p => p.payment_status == "Pending") }
            };

                    var nextMonth = monthStart.AddMonths(1);
                    var monthlyRevenue = db.Payments
                        .Where(p => p.payment_status == "Completed"
                            && p.payment_date >= monthStart
                            && p.payment_date < nextMonth)
                        .Select(p => (decimal?)p.amount)
                        .Sum() ?? 0m;

                    summaryRows.Add(new object[]
                        { "Doanh thu tháng hiện tại", monthlyRevenue });

                    AddExcelSheet(package, "TongQuan",
                        new[] { "Chỉ số", "Giá trị" }, summaryRows);

                    // 2. Doanh thu theo tháng
                    var revenueRows = new List<object[]>();

                    for (int i = 0; i < 6; i++)
                    {
                        var month = chartStart.AddMonths(i);
                        var monthEnd = month.AddMonths(1);

                        var revenue = db.Payments
                            .Where(p => p.payment_status == "Completed"
                                && p.payment_date >= month
                                && p.payment_date < monthEnd)
                            .Select(p => (decimal?)p.amount)
                            .Sum() ?? 0m;

                        revenueRows.Add(new object[]
                            { month.ToString("MM/yyyy"), revenue });
                    }

                    AddExcelSheet(package, "DoanhThuTheoThang",
                        new[] { "Tháng", "Doanh thu" }, revenueRows);

                    // 3. Đăng ký theo tháng
                    var enrollmentRows = new List<object[]>();

                    for (int i = 0; i < 6; i++)
                    {
                        var month = chartStart.AddMonths(i);
                        var monthEnd = month.AddMonths(1);

                        var count = db.Enrollments.Count(e =>
                            e.enrollment_date >= month
                            && e.enrollment_date < monthEnd);

                        enrollmentRows.Add(new object[]
                            { month.ToString("MM/yyyy"), count });
                    }

                    AddExcelSheet(package, "DangKyTheoThang",
                        new[] { "Tháng", "Số lượt đăng ký" }, enrollmentRows);

                    // 4. Trạng thái lớp
                    var statuses = new[]
                        { "Planned", "Open", "InProgress", "Completed", "Cancelled" };

                    var statusRows = statuses.Select(status => new object[]
                    {
                status,
                db.Classes.Count(c => c.class_status == status)
                    }).ToList();

                    AddExcelSheet(package, "TrangThaiLop",
                        new[] { "Trạng thái", "Số lớp" }, statusRows);

                    // 5. Trình độ học viên đang hoạt động
                    var levels = new[]
                    {
                "Beginner", "Elementary", "Intermediate",
                "UpperIntermediate", "Advanced", "IELTS", "TOEIC"
            };

                    var levelRows = levels.Select(level => new object[]
                    {
                level,
                db.Student_Profile.Count(s =>
                    s.current_level == level
                    && s.student_status == "Active")
                    }).ToList();

                    AddExcelSheet(package, "TrinhDoHocVien",
                        new[] { "Trình độ", "Số học viên" }, levelRows);

                    // 6. Danh sách thanh toán đang chờ
                    var pendingPayments = db.Payments
                        .AsNoTracking()
                        .Include(p => p.Enrollment.Student_Profile.User)
                        .Include(p => p.Enrollment.Class.Course)
                        .Where(p => p.payment_status == "Pending")
                        .OrderByDescending(p => p.created_at)
                        .Take(8)
                        .ToList();

                    var paymentRows = pendingPayments.Select(p => new object[]
                    {
                p.payment_id,
                p.Enrollment?.Student_Profile?.User?.full_name ?? "-",
                p.Enrollment?.Class?.Course?.course_name ?? "-",
                p.amount,
                p.payment_date
                    }).ToList();

                    AddExcelSheet(package, "ThanhToanCho",
                        new[] { "Mã thanh toán", "Học viên", "Khóa học",
                    "Số tiền", "Ngày thanh toán" }, paymentRows);

                    // Dữ liệu lớp dùng cho 2 sheet tiếp theo
                    var classRows = db.Classes
                        .AsNoTracking()
                        .Include(c => c.Course)
                        .Include(c => c.Room)
                        .Include(c => c.Teacher_profile.Staff_Profile.User)
                        .Where(c => c.class_status == "Open" && c.start_date >= today)
                        .OrderBy(c => c.start_date)
                        .Take(12)
                        .ToList();

                    var classIds = classRows.Select(c => c.class_id).ToList();

                    var enrollmentCounts = db.Enrollments
                        .Where(e => classIds.Contains(e.class_id)
                            && ActiveEnrollmentStatuses.Contains(e.enrollment_status))
                        .GroupBy(e => e.class_id)
                        .Select(g => new { ClassId = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.ClassId, x => x.Count);

                    // 7. Lớp sắp diễn ra
                    var upcomingRows = classRows
                        .Take(8)
                        .Select(c =>
                        {
                            var enrolled = enrollmentCounts.ContainsKey(c.class_id)
                                ? enrollmentCounts[c.class_id] : 0;

                            var capacity = c.Room == null
                                ? c.class_capacity
                                : Math.Min(c.class_capacity, c.Room.max_capacity);

                            return new object[]
                            {
                        c.class_id,
                        c.class_code,
                        c.Course?.course_name ?? "-",
                        c.Teacher_profile?.Staff_Profile?.User?.full_name
                            ?? "Chưa phân công",
                        c.Room?.room_name ?? "Chưa xếp phòng",
                        c.start_date,
                        enrolled,
                        capacity,
                        Math.Max(0, capacity - enrolled)
                            };
                        }).ToList();

                    AddExcelSheet(package, "LopSapDienRa",
                        new[] { "Mã lớp", "Tên/mã lớp", "Khóa học", "Giáo viên",
                    "Phòng", "Ngày bắt đầu", "Số học viên", "Sức chứa",
                    "Chỗ còn lại" }, upcomingRows);

                    // 8. Lớp gần đầy (tỷ lệ từ 80%)
                    var nearlyFullRows = classRows
                        .Select(c =>
                        {
                            var enrolled = enrollmentCounts.ContainsKey(c.class_id)
                                ? enrollmentCounts[c.class_id] : 0;

                            var capacity = c.Room == null
                                ? c.class_capacity
                                : Math.Min(c.class_capacity, c.Room.max_capacity);

                            var occupancy = capacity <= 0
                                ? 0m : enrolled * 100m / capacity;

                            return new
                            {
                                c.class_id,
                                c.class_code,
                                CourseName = c.Course?.course_name ?? "-",
                                Enrolled = enrolled,
                                Capacity = capacity,
                                Occupancy = occupancy
                            };
                        })
                        .Where(c => c.Occupancy >= 80m)
                        .OrderByDescending(c => c.Occupancy)
                        .Take(8)
                        .Select(c => new object[]
                        {
                    c.class_id,
                    c.class_code,
                    c.CourseName,
                    c.Enrolled,
                    c.Capacity,
                    Math.Round(c.Occupancy, 2)
                        }).ToList();

                    AddExcelSheet(package, "LopGanDay",
                        new[] { "Mã lớp", "Tên/mã lớp", "Khóa học",
                    "Số học viên", "Sức chứa", "Tỷ lệ lấp đầy (%)" },
                        nearlyFullRows);

                    var fileBytes = package.GetAsByteArray();

                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Dashboard_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx"
                    );
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.ExportExcel failed: {0}", ex);
                return Content("Xuất Excel thất bại: " + ex.Message);
            }
        }

        private static void AddExcelSheet(
            ExcelPackage package,
            string sheetName,
            string[] headers,
            IEnumerable<object[]> rows)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            worksheet.Cells[1, 1].LoadFromArrays(
                new[] { headers.Cast<object>().ToArray() }.Concat(rows)
            );

            using (var header = worksheet.Cells[1, 1, 1, headers.Length])
            {
                header.Style.Font.Bold = true;
                header.Style.Font.Color.SetColor(Color.White);
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(Color.DarkGreen);
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.View.FreezePanes(2, 1);
        }






























        //-------------------------------------------------------------------//

        /// <summary>
        /// Hiển thị trang chủ với số liệu và lịch thật của giáo viên đang đăng nhập.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult TeacherHome()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var classItems = GetTeacherClasses(teacherId.Value, true);
                var weekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
                var weekEnd = weekStart.AddDays(7);
                var sessions = GetTeacherSessions(teacherId.Value, DateTime.Today, null);
                var thisWeekSessions = GetTeacherSessions(teacherId.Value, weekStart, weekEnd);
                var weeklySchedules = GetTeacherWeeklySchedules(teacherId.Value);
                var latestAssessments = GetTeacherAssessments(teacherId.Value);
                var activeStudentCount = classItems.Sum(c => c.StudentCount);
                var pendingAttendance = thisWeekSessions.Count(s =>
                    s.SessionDate <= DateTime.Now && s.AttendanceCount < s.StudentCount);

                var model = new TeacherDashboardViewModel
                {
                    TeacherName = ViewBag.TeacherName as string ?? "Giáo viên",
                    ClassCount = classItems.Count,
                    StudentCount = activeStudentCount,
                    // Khi chưa sinh các buổi cụ thể, dùng số lịch lặp như số buổi dự kiến trong tuần.
                    SessionsThisWeek = thisWeekSessions.Count > 0
                        ? thisWeekSessions.Count : weeklySchedules.Count,
                    AttendanceToComplete = pendingAttendance,
                    NextSession = sessions.FirstOrDefault(s => s.SessionDate >= DateTime.Today),
                    Classes = classItems,
                    UpcomingSessions = sessions.Where(s => s.SessionDate >= DateTime.Today).Take(4).ToList(),
                    WeeklySchedules = weeklySchedules.Take(4).ToList(),
                    LatestAssessments = latestAssessments.Take(4).ToList()
                };

                return View("~/Views/DashBoard/TeacherHome.cshtml", model);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.TeacherHome failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Danh sách lớp đang được phân công cho giáo viên hiện tại.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult Classes(string search, int page = 1)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var classes = GetTeacherClasses(teacherId.Value, true)
                    .Where(c => Matches(search, c.ClassCode, c.CourseName, c.CourseCode, c.RoomName)).ToList();
                SetPagination(classes.Count, page);
                return View("~/Views/DashBoard/TeacherClasses.cshtml", new TeacherClassesViewModel
                {
                    Classes = PageItems(classes, page), Search = search
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.Classes failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Chi tiết lớp và roster chỉ được trả về nếu lớp thuộc giáo viên đăng nhập.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult ClassDetails(Guid? id)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();
                if (!id.HasValue) return new HttpStatusCodeResult(400);

                var classItem = db.Classes
                    .Include(c => c.Course)
                    .Include(c => c.Room)
                    .SingleOrDefault(c => c.class_id == id.Value && c.teacher_id == teacherId.Value);
                if (classItem == null) return HttpNotFound();

                var enrollments = db.Enrollments
                    .Include(e => e.Student_Profile.User)
                    .Where(e => e.class_id == classItem.class_id && ActiveEnrollmentStatuses.Contains(e.enrollment_status))
                    .OrderBy(e => e.Student_Profile.User.full_name)
                    .ToList();

                var model = new TeacherClassDetailsViewModel
                {
                    Class = ToClassItem(classItem, enrollments.Count),
                    Students = enrollments.Select(e => new TeacherStudentItemViewModel
                    {
                        EnrollmentId = e.enrollment_id,
                        StudentCode = e.Student_Profile.student_code,
                        StudentName = e.Student_Profile.User.full_name,
                        Email = e.Student_Profile.User.email,
                        EnrollmentStatus = e.enrollment_status
                    }).ToList(),
                    Assessments = GetTeacherAssessments(teacherId.Value)
                        .Where(a => a.ClassId == classItem.class_id).ToList()
                };

                return View("~/Views/DashBoard/TeacherClassDetails.cshtml", model);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.ClassDetails failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Lịch buổi học đã tạo và lịch lặp hàng tuần của các lớp được giao.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult Schedule(string search, int page = 1)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var sessions = GetTeacherSessions(teacherId.Value, null, null)
                    .Where(s => Matches(search, s.ClassCode, s.CourseName, s.RoomName, s.Topic, s.TimeSlotName)).ToList();
                var schedules = GetTeacherWeeklySchedules(teacherId.Value)
                    .Where(s => Matches(search, s.ClassCode, s.CourseName, s.RoomName, s.WeekDay, s.TimeSlotName)).ToList();
                SetPagination(sessions.Count, page);
                return View("~/Views/DashBoard/TeacherSchedule.cshtml", new TeacherScheduleViewModel
                {
                    Sessions = PageItems(sessions, page),
                    WeeklySchedules = schedules, Search = search
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.Schedule failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Xem điểm đã nhập và mở bảng điểm để giáo viên chỉnh sửa.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult Results(Guid? assessmentId, string search, int page = 1)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var assessments = GetTeacherAssessments(teacherId.Value)
                    .Where(a => Matches(search, a.ClassCode, a.CourseName, a.AssessmentName, a.AssessmentType)).ToList();
                SetPagination(assessments.Count, page);
                var model = new TeacherResultsViewModel
                {
                    Assessments = PageItems(assessments, page), Search = search
                };

                if (assessmentId.HasValue)
                {
                    var assessment = db.Assessments
                        .Include(a => a.Class)
                        .SingleOrDefault(a => a.assessment_id == assessmentId.Value &&
                                              a.Class.teacher_id == teacherId.Value);
                    if (assessment == null) return HttpNotFound();
                    model.SelectedAssessment = BuildGradeViewModel(assessment);
                }

                return View("~/Views/DashBoard/TeacherResults.cshtml", model);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.Results failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Lưu điểm sau khi kiểm tra quyền sở hữu lớp, học viên và thang điểm.</summary>
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveResults(AssessmentGradeViewModel model, string search, int page = 1)
        {
            try
            {
                if (model == null) return new HttpStatusCodeResult(400);
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var assessment = db.Assessments
                    .Include(a => a.Class)
                    .SingleOrDefault(a => a.assessment_id == model.assessment_id &&
                                          a.class_id == model.class_id &&
                                          a.Class.teacher_id == teacherId.Value);
                if (assessment == null) return HttpNotFound();

                var userId = GetCurrentUserId();
                if (!userId.HasValue) return new HttpUnauthorizedResult();

                var submittedStudents = model.Students ?? new List<AssessmentGradeStudentViewModel>();
                var submittedIds = submittedStudents.Select(s => s.enrollment_id).ToList();
                var activeIds = db.Enrollments
                    .Where(e => e.class_id == assessment.class_id &&
                                GradeEnrollmentStatuses.Contains(e.enrollment_status))
                    .Select(e => e.enrollment_id).ToList();

                // Không tin ID/điểm gửi từ trình duyệt: chỉ chấp nhận học viên trong lớp và điểm trong thang.
                if (submittedIds.Count != submittedIds.Distinct().Count() ||
                    !activeIds.OrderBy(id => id).SequenceEqual(submittedIds.OrderBy(id => id)))
                    ModelState.AddModelError("", "Danh sách học viên đã thay đổi. Vui lòng tải lại bảng điểm.");
                foreach (var student in submittedStudents)
                {
                    if (!activeIds.Contains(student.enrollment_id))
                    {
                        ModelState.AddModelError("", "Danh sách học viên không còn hợp lệ. Vui lòng tải lại trang.");
                        break;
                    }
                    if (student.score.HasValue &&
                        (student.score.Value < 0 || student.score.Value > assessment.max_score))
                        ModelState.AddModelError("", "Điểm phải nằm trong khoảng 0 đến " + assessment.max_score + ".");
                    if (!string.IsNullOrEmpty(student.feedback) && student.feedback.Length > 2000)
                        ModelState.AddModelError("", "Nhận xét không được vượt quá 2000 ký tự.");
                }

                if (!ModelState.IsValid)
                {
                    var selectedAssessment = BuildGradeViewModel(assessment);
                    var submittedById = submittedStudents
                        .GroupBy(s => s.enrollment_id)
                        .ToDictionary(g => g.Key, g => g.First());
                    foreach (var student in selectedAssessment.Students)
                    {
                        if (!submittedById.ContainsKey(student.enrollment_id)) continue;
                        student.score = submittedById[student.enrollment_id].score;
                        student.feedback = submittedById[student.enrollment_id].feedback;
                    }

                    var filteredAssessments = GetTeacherAssessments(teacherId.Value)
                        .Where(a => Matches(search, a.ClassCode, a.CourseName, a.AssessmentName, a.AssessmentType)).ToList();
                    SetPagination(filteredAssessments.Count, page);
                    var resultsPage = new TeacherResultsViewModel
                    {
                        Assessments = PageItems(filteredAssessments, page),
                        SelectedAssessment = selectedAssessment, Search = search
                    };
                    return View("~/Views/DashBoard/TeacherResults.cshtml", resultsPage);
                }

                var scoreRecords = db.AssessmentScores
                    .Where(s => s.assessment_id == assessment.assessment_id && activeIds.Contains(s.enrollment_id))
                    .ToList();
                foreach (var student in submittedStudents)
                {
                    // Empty score leaves the current record unchanged; a score can be edited repeatedly.
                    if (!student.score.HasValue) continue;

                    var score = scoreRecords.SingleOrDefault(s => s.enrollment_id == student.enrollment_id);
                    if (score == null)
                    {
                        db.AssessmentScores.Add(new AssessmentScore
                        {
                            assessment_score_id = Guid.NewGuid(),
                            assessment_id = assessment.assessment_id,
                            enrollment_id = student.enrollment_id,
                            entered_by = userId.Value,
                            score = student.score.Value,
                            feedback = student.feedback,
                            created_at = DateTime.Now
                        });
                    }
                    else
                    {
                        score.score = student.score.Value;
                        score.feedback = student.feedback;
                        score.entered_by = userId.Value;
                    }
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã lưu kết quả học tập.";
                return RedirectToAction("Results", new { assessmentId = assessment.assessment_id, search, page });
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.SaveResults failed: {0}", ex);
                TempData["ErrorMessage"] = "Không thể lưu điểm lúc này. Vui lòng thử lại.";
                return RedirectToAction("Results", new { assessmentId = model == null ? (Guid?)null : model.assessment_id, search, page });
            }
        }

        /// <summary>Danh sách các buổi của lớp được giao để giáo viên chọn điểm danh.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult Attendance()
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();
                return View("~/Views/DashBoard/TeacherAttendance.cshtml", GetTeacherSessions(teacherId.Value, null, null));
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.Attendance failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Mở bảng điểm danh của một buổi học do chính giáo viên phụ trách.</summary>
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public ActionResult TakeAttendance(Guid? sessionId)
        {
            try
            {
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();
                if (!sessionId.HasValue) return new HttpStatusCodeResult(400);

                var session = db.ClassSessions
                    .Include(s => s.Class)
                    .Include(s => s.TimeSlot)
                    .SingleOrDefault(s => s.session_id == sessionId.Value &&
                                          s.Class.teacher_id == teacherId.Value);
                if (session == null) return HttpNotFound();
                return View("~/Views/DashBoard/TeacherTakeAttendance.cshtml", BuildAttendanceViewModel(session));
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.TakeAttendance GET failed: {0}", ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>Lưu điểm danh có kiểm tra lại quyền lớp và toàn bộ danh sách học viên.</summary>
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public ActionResult TakeAttendance(ClassAttendanceViewModel model)
        {
            try
            {
                if (model == null) return new HttpStatusCodeResult(400);
                var teacherId = GetCurrentTeacherId();
                if (!teacherId.HasValue) return new HttpUnauthorizedResult();

                var session = db.ClassSessions
                    .Include(s => s.Class)
                    .Include(s => s.TimeSlot)
                    .SingleOrDefault(s => s.session_id == model.session_id &&
                                          s.Class.teacher_id == teacherId.Value);
                if (session == null) return HttpNotFound();

                var students = model.Students ?? new List<ClassAttendanceStudentViewModel>();
                var activeEnrollments = db.Enrollments
                    .Where(e => e.class_id == session.class_id && ActiveEnrollmentStatuses.Contains(e.enrollment_status))
                    .Select(e => e.enrollment_id).ToList();
                var submittedIds = students.Select(s => s.enrollment_id).ToList();
                var validStatuses = new[] { "Present", "Absent", "Excused" };

                // Yêu cầu đúng một bản ghi cho mỗi học viên đang học để không bỏ sót hoặc sửa người ngoài lớp.
                if (submittedIds.Count != submittedIds.Distinct().Count() ||
                    !activeEnrollments.OrderBy(id => id).SequenceEqual(submittedIds.OrderBy(id => id)))
                    ModelState.AddModelError("", "Danh sách học viên đã thay đổi. Vui lòng tải lại bảng điểm danh.");

                foreach (var student in students)
                {
                    if (!validStatuses.Contains(student.attendance_status))
                        ModelState.AddModelError("", "Vui lòng chọn trạng thái cho từng học viên.");
                    if (!string.IsNullOrEmpty(student.remarks) && student.remarks.Length > 1000)
                        ModelState.AddModelError("", "Ghi chú không được vượt quá 1000 ký tự.");
                }

                if (!ModelState.IsValid)
                    return View("~/Views/DashBoard/TeacherTakeAttendance.cshtml", BuildAttendanceViewModel(session, students));

                var userId = GetCurrentUserId();
                if (!userId.HasValue) return new HttpUnauthorizedResult();
                var records = db.AttendanceRecords
                    .Where(a => a.session_id == session.session_id && activeEnrollments.Contains(a.enrollment_id))
                    .ToList();

                foreach (var student in students)
                {
                    var record = records.SingleOrDefault(a => a.enrollment_id == student.enrollment_id);
                    if (record == null)
                    {
                        db.AttendanceRecords.Add(new AttendanceRecord
                        {
                            attendance_id = Guid.NewGuid(),
                            enrollment_id = student.enrollment_id,
                            session_id = session.session_id,
                            recorded_by = userId.Value,
                            attendance_status = student.attendance_status,
                            remarks = student.remarks,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        });
                    }
                    else
                    {
                        record.attendance_status = student.attendance_status;
                        record.remarks = student.remarks;
                        record.recorded_by = userId.Value;
                        record.updated_at = DateTime.Now;
                    }
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã lưu điểm danh.";
                return RedirectToAction("Attendance");
            }
            catch (Exception ex)
            {
                Trace.TraceError("DashboardController.TakeAttendance POST failed: {0}", ex);
                TempData["ErrorMessage"] = "Không thể lưu điểm danh lúc này. Vui lòng thử lại.";
                return RedirectToAction("Attendance");
            }
        }

        private Guid? GetCurrentUserId()
        {
            var identity = User.Identity as ClaimsIdentity;
            Guid userId;
            return identity != null && Guid.TryParse(
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId)
                ? (Guid?)userId : null;
        }

        private Guid? GetCurrentTeacherId()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return null;

            var teacher = db.Teacher_profile
                .Where(t => t.Staff_Profile.user_id == userId.Value)
                .Select(t => new { Id = t.teacher_id, Name = t.Staff_Profile.User.full_name })
                .FirstOrDefault();
            if (teacher == null || teacher.Id == Guid.Empty) return null;

            // Dùng tên hiện tại trong navigation chung mà không cần truy vấn lại ở từng action.
            ViewBag.TeacherName = teacher.Name;
            return teacher.Id;
        }

        private List<TeacherClassItemViewModel> GetTeacherClasses(Guid teacherId, bool onlyTeachingStatuses)
        {
            var query = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Room)
                .Where(c => c.teacher_id == teacherId);
            if (onlyTeachingStatuses)
                query = query.Where(c => TeachingClassStatuses.Contains(c.class_status));

            return query.OrderBy(c => c.start_date).ThenBy(c => c.class_code)
                .ToList()
                .Select(c => ToClassItem(c, c.Enrollments.Count(e => ActiveEnrollmentStatuses.Contains(e.enrollment_status))))
                .ToList();
        }

        private static TeacherClassItemViewModel ToClassItem(Class classItem, int studentCount)
        {
            return new TeacherClassItemViewModel
            {
                ClassId = classItem.class_id,
                ClassCode = classItem.class_code,
                CourseName = classItem.Course == null ? "-" : classItem.Course.course_name,
                CourseCode = classItem.Course == null ? "-" : classItem.Course.course_code,
                RoomName = classItem.Room == null ? "Chưa xếp phòng" : classItem.Room.room_name,
                ClassStatus = classItem.class_status,
                ClassStatusLabel = ClassStatusLabel(classItem.class_status),
                StartDate = classItem.start_date,
                EndDate = classItem.end_date,
                StudentCount = studentCount
            };
        }

        private static string ClassStatusLabel(string status)
        {
            switch (status)
            {
                case "Planned": return "Dự kiến";
                case "Open": return "Đang mở đăng ký";
                case "InProgress": return "Đang học";
                case "Completed": return "Đã hoàn thành";
                case "Cancelled": return "Đã hủy";
                default: return status ?? "Chưa xác định";
            }
        }

        // Dùng chung cách lọc/phân trang để các trang giáo viên giữ cùng hành vi.
        private static bool Matches(string search, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            return values.Any(value => !string.IsNullOrEmpty(value) &&
                value.IndexOf(search.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private IList<T> PageItems<T>(IList<T> items, int page)
        {
            const int pageSize = 10;
            var safePage = Math.Max(1, Math.Min(page, Math.Max(1, (int)Math.Ceiling(items.Count / (double)pageSize))));
            return items.Skip((safePage - 1) * pageSize).Take(pageSize).ToList();
        }

        private void SetPagination(int totalItems, int page)
        {
            const int pageSize = 10;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            ViewBag.CurrentPage = Math.Max(1, Math.Min(page, totalPages));
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
        }

        private List<TeacherRecurringScheduleItemViewModel> GetTeacherWeeklySchedules(Guid teacherId)
        {
            var classIds = db.Classes.Where(c => c.teacher_id == teacherId)
                .Select(c => c.class_id).ToList();
            return db.ClassSchedules
                .Include(s => s.Class.Course)
                .Include(s => s.Class.Room)
                .Include(s => s.TimeSlot)
                .Where(s => classIds.Contains(s.class_id) &&
                            TeachingClassStatuses.Contains(s.Class.class_status))
                .ToList()
                .Select(s => new TeacherRecurringScheduleItemViewModel
                {
                    ClassId = s.class_id,
                    ClassCode = s.Class.class_code,
                    CourseName = s.Class.Course == null ? "-" : s.Class.Course.course_name,
                    RoomName = string.IsNullOrWhiteSpace(s.room_name)
                        ? (s.Class.Room == null ? "Chưa xếp phòng" : s.Class.Room.room_name)
                        : s.room_name,
                    WeekDay = WeekDayLabel(s.week_day),
                    TimeSlotName = s.TimeSlot == null ? "-" : s.TimeSlot.slot_name,
                    StartTime = s.TimeSlot == null ? TimeSpan.Zero : s.TimeSlot.start_time,
                    EndTime = s.TimeSlot == null ? TimeSpan.Zero : s.TimeSlot.end_time
                })
                .OrderBy(s => WeekDayOrder(s.WeekDay))
                .ThenBy(s => s.StartTime)
                .ToList();
        }

        private List<TeacherSessionItemViewModel> GetTeacherSessions(Guid teacherId, DateTime? from, DateTime? to)
        {
            var query = db.ClassSessions
                .Include(s => s.Class.Course)
                .Include(s => s.Class.Room)
                .Include(s => s.TimeSlot)
                .Where(s => s.Class.teacher_id == teacherId);
            if (from.HasValue) query = query.Where(s => s.session_date >= from.Value);
            if (to.HasValue) query = query.Where(s => s.session_date < to.Value);

            var sessions = query.OrderBy(s => s.session_date)
                .ThenBy(s => s.TimeSlot.start_time).ToList();
            var sessionIds = sessions.Select(s => s.session_id).ToList();
            var classIds = sessions.Select(s => s.class_id).Distinct().ToList();
            var activeCounts = db.Enrollments
                .Where(e => classIds.Contains(e.class_id) && ActiveEnrollmentStatuses.Contains(e.enrollment_status))
                .GroupBy(e => e.class_id)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.ClassId, g => g.Count);
            var attendanceCounts = db.AttendanceRecords
                .Where(a => sessionIds.Contains(a.session_id) &&
                            ActiveEnrollmentStatuses.Contains(a.Enrollment.enrollment_status))
                .GroupBy(a => a.session_id)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.SessionId, g => g.Count);

            return sessions.Select(s => new TeacherSessionItemViewModel
            {
                SessionId = s.session_id,
                ClassId = s.class_id,
                ClassCode = s.Class.class_code,
                CourseName = s.Class.Course == null ? "-" : s.Class.Course.course_name,
                RoomName = s.Class.Room == null ? "Chưa xếp phòng" : s.Class.Room.room_name,
                SessionDate = s.session_date,
                TimeSlotName = s.TimeSlot == null ? "-" : s.TimeSlot.slot_name,
                StartTime = s.TimeSlot == null ? TimeSpan.Zero : s.TimeSlot.start_time,
                EndTime = s.TimeSlot == null ? TimeSpan.Zero : s.TimeSlot.end_time,
                Topic = s.topic,
                StudentCount = activeCounts.ContainsKey(s.class_id) ? activeCounts[s.class_id] : 0,
                AttendanceCount = attendanceCounts.ContainsKey(s.session_id) ? attendanceCounts[s.session_id] : 0,
                CanTakeAttendance = true
            }).ToList();
        }

        private List<TeacherAssessmentItemViewModel> GetTeacherAssessments(Guid teacherId)
        {
            return db.Assessments
                .Include(a => a.Class.Course)
                .Include(a => a.Class.Enrollments)
                .Include(a => a.AssessmentScores)
                .Where(a => a.Class.teacher_id == teacherId && a.Class.class_status != "Cancelled")
                .OrderByDescending(a => a.assessment_date)
                .ThenBy(a => a.assessment_name)
                .ToList()
                .Select(a =>
                {
                    var activeIds = a.Class.Enrollments
                        .Where(e => GradeEnrollmentStatuses.Contains(e.enrollment_status))
                        .Select(e => e.enrollment_id).ToList();
                    return new TeacherAssessmentItemViewModel
                    {
                        AssessmentId = a.assessment_id,
                        ClassId = a.class_id,
                        ClassCode = a.Class.class_code,
                        CourseName = a.Class.Course == null ? "-" : a.Class.Course.course_name,
                        AssessmentName = a.assessment_name,
                        AssessmentType = a.assessment_type,
                        MaxScore = a.max_score,
                        AssessmentDate = a.assessment_date,
                        StudentCount = activeIds.Count,
                        ScoredCount = a.AssessmentScores.Count(s => activeIds.Contains(s.enrollment_id))
                    };
                }).ToList();
        }

        private AssessmentGradeViewModel BuildGradeViewModel(Assessment assessment)
        {
            var enrollments = db.Enrollments
                .Include(e => e.Student_Profile.User)
                .Where(e => e.class_id == assessment.class_id && GradeEnrollmentStatuses.Contains(e.enrollment_status))
                .OrderBy(e => e.Student_Profile.User.full_name)
                .ToList();
            var scoreByEnrollment = db.AssessmentScores
                .Where(s => s.assessment_id == assessment.assessment_id)
                .ToList()
                .GroupBy(s => s.enrollment_id)
                .ToDictionary(g => g.Key, g => g.First());

            return new AssessmentGradeViewModel
            {
                assessment_id = assessment.assessment_id,
                class_id = assessment.class_id,
                class_code = assessment.Class.class_code,
                assessment_name = assessment.assessment_name,
                assessment_type = assessment.assessment_type,
                max_score = assessment.max_score,
                assessment_date = assessment.assessment_date,
                Students = enrollments.Select(e => new AssessmentGradeStudentViewModel
                {
                    enrollment_id = e.enrollment_id,
                    student_code = e.Student_Profile.student_code,
                    student_name = e.Student_Profile.User.full_name,
                    score = scoreByEnrollment.ContainsKey(e.enrollment_id)
                        ? (decimal?)scoreByEnrollment[e.enrollment_id].score : null,
                    feedback = scoreByEnrollment.ContainsKey(e.enrollment_id)
                        ? scoreByEnrollment[e.enrollment_id].feedback : null
                }).ToList()
            };
        }

        private ClassAttendanceViewModel BuildAttendanceViewModel(
            ClassSession session,
            IList<ClassAttendanceStudentViewModel> submitted = null)
        {
            var enrollments = db.Enrollments
                .Include(e => e.Student_Profile.User)
                .Where(e => e.class_id == session.class_id && ActiveEnrollmentStatuses.Contains(e.enrollment_status))
                .OrderBy(e => e.Student_Profile.User.full_name)
                .ToList();
            var records = db.AttendanceRecords
                .Where(a => a.session_id == session.session_id)
                .ToList()
                .GroupBy(a => a.enrollment_id)
                .ToDictionary(g => g.Key, g => g.First());
            var submittedById = (submitted ?? new List<ClassAttendanceStudentViewModel>())
                .GroupBy(s => s.enrollment_id).ToDictionary(g => g.Key, g => g.First());

            return new ClassAttendanceViewModel
            {
                session_id = session.session_id,
                class_id = session.class_id,
                class_code = session.Class.class_code,
                session_date = session.session_date,
                time_slot_name = session.TimeSlot == null ? null : session.TimeSlot.slot_name,
                topic = session.topic,
                Students = enrollments.Select(e => new ClassAttendanceStudentViewModel
                {
                    enrollment_id = e.enrollment_id,
                    student_code = e.Student_Profile.student_code,
                    student_name = e.Student_Profile.User.full_name,
                    attendance_status = submittedById.ContainsKey(e.enrollment_id)
                        ? submittedById[e.enrollment_id].attendance_status
                        : records.ContainsKey(e.enrollment_id) ? records[e.enrollment_id].attendance_status : null,
                    remarks = submittedById.ContainsKey(e.enrollment_id)
                        ? submittedById[e.enrollment_id].remarks
                        : records.ContainsKey(e.enrollment_id) ? records[e.enrollment_id].remarks : null
                }).ToList()
            };
        }

        private static int WeekDayOrder(string weekDay)
        {
            switch (weekDay)
            {
                case "Monday":
                case "Thứ Hai": return 1;
                case "Tuesday":
                case "Thứ Ba": return 2;
                case "Wednesday":
                case "Thứ Tư": return 3;
                case "Thursday":
                case "Thứ Năm": return 4;
                case "Friday":
                case "Thứ Sáu": return 5;
                case "Saturday":
                case "Thứ Bảy": return 6;
                case "Sunday":
                case "Chủ Nhật": return 7;
                default: return 8;
            }
        }

        private static string WeekDayLabel(string weekDay)
        {
            switch (weekDay)
            {
                case "Monday": return "Thứ Hai";
                case "Tuesday": return "Thứ Ba";
                case "Wednesday": return "Thứ Tư";
                case "Thursday": return "Thứ Năm";
                case "Friday": return "Thứ Sáu";
                case "Saturday": return "Thứ Bảy";
                case "Sunday": return "Chủ Nhật";
                default: return weekDay ?? "Chưa xác định";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
