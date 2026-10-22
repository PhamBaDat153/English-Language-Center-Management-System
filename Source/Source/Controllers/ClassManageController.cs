using Source.Models;
using Source.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace Source.Controllers
{
    public class ClassManageController : Controller
    {
        // Khởi tạo DbContext theo cấu trúc UserManageController.
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        // Các trạng thái hợp lệ, phải khớp với CHECK constraint của database.
        private static readonly string[] ClassStatuses =
        {
            "Planned",
            "Open",
            "InProgress",
            "Completed",
            "Cancelled"
        };

        /// <summary>
        /// Chuyển exception chưa được xử lý sang trang lỗi 500 dùng chung.
        /// </summary>
        protected override void OnException(ExceptionContext filterContext)
        {
            // Ghi lại exception để tiện kiểm tra lỗi khi chạy ứng dụng.
            Trace.TraceError(
                "ClassManageController: {0}",
                filterContext.Exception);

            filterContext.ExceptionHandled = true;

            // Dùng chung trang lỗi với UserManageController.
            filterContext.Result =
                View("~/Views/Shared/Error_500.cshtml");
        }

        /// <summary>
        /// Hiển thị danh sách lớp học, tìm kiếm và phân trang.
        /// </summary>
        [HttpGet]
        public ActionResult Index(
    string classCode,
    string status,
    Guid? courseId,
    Guid? teacherId,
    int page = 1)
        {
            try
            {
                int pageSize = 15;

                // Lấy lớp học cùng thông tin giáo viên, khóa học và phòng học
                var query = db.Classes
                    .Include(c => c.Teacher_profile)
                    .Include(c => c.Course)
                    .Include(c => c.Room)
                    .AsQueryable();

                // Lọc theo mã lớp
                if (!string.IsNullOrWhiteSpace(classCode))
                {
                    query = query.Where(c =>
                        c.class_code.Contains(classCode));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(c =>
                        c.class_status == status);
                }

                // Lọc theo khóa học
                if (courseId.HasValue)
                {
                    query = query.Where(c =>
                        c.course_id == courseId.Value);
                }

                // Lọc theo giáo viên
                if (teacherId.HasValue)
                {
                    query = query.Where(c =>
                        c.teacher_id == teacherId.Value);
                }

                int totalItems = query.Count();

                var classes = query
                    .OrderBy(c => c.class_code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Tính sĩ số trên các trạng thái đang giữ chỗ/đang học để danh sách phản ánh sức chứa thực.
                var classIds = classes.Select(c => c.class_id).ToList();
                ViewBag.ActiveEnrollmentCounts = db.Enrollments
                    .Where(e => classIds.Contains(e.class_id) &&
                                (e.enrollment_status == "Pending" ||
                                 e.enrollment_status == "Confirmed" ||
                                 e.enrollment_status == "Studying"))
                    .GroupBy(e => e.class_id)
                    .ToDictionary(g => g.Key, g => g.Count());

                ViewBag.TotalItems = totalItems;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages =
                    (int)Math.Ceiling((double)totalItems / pageSize);
                ViewBag.PageSize = pageSize;
                ViewBag.ClassCode = classCode;
                ViewBag.Status = status;
                ViewBag.CourseId = courseId;
                ViewBag.TeacherId = teacherId;

                LoadDropdowns(courseId, teacherId);

                return View(classes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "ClassManage Index error: " + ex);

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>
        /// Hiển thị form thêm lớp học.
        /// </summary>
        [HttpGet]
        public ActionResult Create()
        {
            try
            {
                LoadDropdowns();
                ViewBag.CourseDurations = db.Set<Course>()
                    .ToDictionary(c => c.course_id.ToString(), c => c.duration_weeks);

                // Giá trị mặc định khi mở form thêm lớp.
                var model = new Class
                {
                    class_id = Guid.NewGuid(),
                    start_date = DateTime.Today,
                    end_date = DateTime.Today,
                    class_capacity = 20,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    class_status = "Planned"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ClassManageController.Create GET: {0}",
                    ex);

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>
        /// Nhận dữ liệu form và tạo lớp học mới.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Class model)
        {
            try
            {
                var selectedCourse = db.Set<Course>()
                    .SingleOrDefault(c => c.course_id == model.course_id);

                if (selectedCourse != null && model.start_date != default(DateTime))
                {
                    model.end_date = model.start_date.Date
                        .AddDays(selectedCourse.duration_weeks * 7 - 1);
                }

                // Kiểm tra mã lớp bắt buộc và không được trùng.
                if (string.IsNullOrWhiteSpace(model.class_code))
                {
                    ModelState.AddModelError(
                        "class_code",
                        "Mã lớp không được để trống.");
                }
                else
                {
                    model.class_code = model.class_code.Trim();

                    if (db.Classes.Any(c =>
                        c.class_code == model.class_code))
                    {
                        ModelState.AddModelError(
                            "class_code",
                            "Mã lớp đã tồn tại.");
                    }
                }

                // Kiểm tra ngày bắt đầu và kết thúc.
                if (model.end_date.Date < model.start_date.Date)
                {
                    ModelState.AddModelError(
                        "end_date",
                        "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                }

                // Kiểm tra sức chứa lớp trước khi ghi, đồng thời bảo đảm lớp vừa được sức chứa phòng.
                if (model.class_capacity <= 0)
                {
                    ModelState.AddModelError(
                        "class_capacity",
                        "Sĩ số tối đa phải lớn hơn 0.");
                }

                // Kiểm tra trạng thái thuộc enum ClassStatus.
                if (!ClassStatuses.Contains(model.class_status))
                {
                    ModelState.AddModelError(
                        "class_status",
                        "Trạng thái lớp học không hợp lệ.");
                }

                // Kiểm tra khóa học có tồn tại.
                bool courseExists = selectedCourse != null;

                if (!courseExists)
                {
                    ModelState.AddModelError(
                        "course_id",
                        "Vui lòng chọn khóa học hợp lệ.");
                }

                // Giáo viên là tùy chọn, nhưng nếu chọn thì phải tồn tại.
                if (model.teacher_id.HasValue)
                {
                    bool teacherExists = db.Teacher_profile.Any(t =>
                        t.teacher_id == model.teacher_id.Value);

                    if (!teacherExists)
                    {
                        ModelState.AddModelError(
                            "teacher_id",
                            "Giáo viên được chọn không tồn tại.");
                    }
                }

                // Phòng học cũng là tùy chọn.
                if (model.room_id.HasValue)
                {
                    var selectedRoom = db.Set<Room>().SingleOrDefault(r =>
                        r.room_id == model.room_id.Value);

                    if (selectedRoom == null)
                    {
                        ModelState.AddModelError(
                            "room_id",
                            "Phòng học được chọn không tồn tại.");
                    }
                    else if (model.class_capacity > selectedRoom.max_capacity)
                    {
                        ModelState.AddModelError(
                            "class_capacity",
                            "Sĩ số lớp không được vượt quá sức chứa của phòng.");
                    }
                }

                // Nếu có lỗi, nạp lại dropdown trước khi trả về form.
                if (!ModelState.IsValid)
                {
                    LoadDropdowns(
                        model.course_id,
                        model.teacher_id,
                        model.room_id);
                    ViewBag.CourseDurations = db.Set<Course>()
                        .ToDictionary(c => c.course_id.ToString(), c => c.duration_weeks);

                    return View(model);
                }

                // Không tin class_id/timestamps do form gửi lên.
                model.class_id = Guid.NewGuid();
                model.start_date = model.start_date.Date;
                model.end_date = model.end_date.Date;
                model.created_at = DateTime.Now;
                model.updated_at = DateTime.Now;

                // Thêm entity vào DbContext và lưu xuống database.
                db.Classes.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã thêm lớp học thành công.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ClassManageController.Create POST: {0}",
                    ex);

                ModelState.AddModelError(
                    "",
                    "Không thể thêm lớp học. Vui lòng kiểm tra dữ liệu.");

                try
                {
                    LoadDropdowns(
                        model.course_id,
                        model.teacher_id,
                        model.room_id);
                }
                catch (Exception dropdownEx)
                {
                    Trace.TraceError(
                        "ClassManageController.Create dropdown: {0}",
                        dropdownEx);
                }

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>
        /// Hiển thị thông tin chi tiết của lớp học.
        /// </summary>
        [HttpGet]
        public ActionResult Details(
            Guid? id,
            string studentName,
            string studentEmail,
            string studentStatus)
        {
            try
            {
                if (!id.HasValue)
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.BadRequest);
                }

                var classItem = db.Classes
                    .Include(c => c.ClassSchedules.Select(s => s.TimeSlot))
                    .Include(c => c.ClassSessions.Select(s => s.TimeSlot))
                    .Include(c => c.Room)
                    .SingleOrDefault(c => c.class_id == id.Value);

                if (classItem == null)
                {
                    return HttpNotFound();
                }

                ViewBag.ActiveEnrollmentCount = db.Enrollments.Count(e =>
                    e.class_id == classItem.class_id &&
                    (e.enrollment_status == "Pending" ||
                     e.enrollment_status == "Confirmed" ||
                     e.enrollment_status == "Studying"));

                var studentsQuery = db.Enrollments
                    .Include(e => e.Student_Profile.User)
                    .Where(e => e.class_id == classItem.class_id);

                if (!string.IsNullOrWhiteSpace(studentName))
                {
                    studentName = studentName.Trim();
                    studentsQuery = studentsQuery.Where(e =>
                        e.Student_Profile.User.full_name.Contains(studentName));
                }

                if (!string.IsNullOrWhiteSpace(studentEmail))
                {
                    studentEmail = studentEmail.Trim();
                    studentsQuery = studentsQuery.Where(e =>
                        e.Student_Profile.User.email.Contains(studentEmail));
                }

                if (!string.IsNullOrWhiteSpace(studentStatus))
                {
                    studentsQuery = studentsQuery.Where(e =>
                        e.enrollment_status == studentStatus);
                }

                ViewBag.ClassStudents = studentsQuery
                    .OrderBy(e => e.Student_Profile.User.full_name)
                    .ToList();
                ViewBag.StudentName = studentName;
                ViewBag.StudentEmail = studentEmail;
                ViewBag.StudentStatus = studentStatus;

                LoadDropdowns(
                    classItem.course_id,
                    classItem.teacher_id,
                    classItem.room_id);

                ViewBag.ScheduleTimeSlots = new SelectList(
                    db.TimeSlots
                        .Where(t => t.available_status == "Available")
                        .OrderBy(t => t.start_time)
                        .ToList()
                        .Select(t => new
                        {
                            Id = t.time_slot_id,
                            Name = t.slot_name + " (" +
                                t.start_time.ToString(@"hh\:mm") + " - " +
                                t.end_time.ToString(@"hh\:mm") + ")"
                        }),
                    "Id",
                    "Name");

                return View(classItem);
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ClassManageController.Details GET: {0}",
                    ex);

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        /// <summary>
        /// Cập nhật thông tin lớp học từ form Details.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(Class model)
        {
            try
            {
                // Tìm entity đang tồn tại để tránh cập nhật trực tiếp dữ liệu gửi lên.
                var classItem = db.Classes.SingleOrDefault(c =>
                    c.class_id == model.class_id);

                if (classItem == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra mã lớp không trùng với lớp khác.
                if (string.IsNullOrWhiteSpace(model.class_code))
                {
                    ModelState.AddModelError(
                        "class_code",
                        "Mã lớp không được để trống.");
                }
                else
                {
                    model.class_code = model.class_code.Trim();

                    if (db.Classes.Any(c =>
                        c.class_code == model.class_code &&
                        c.class_id != model.class_id))
                    {
                        ModelState.AddModelError(
                            "class_code",
                            "Mã lớp đã tồn tại.");
                    }
                }

                if (model.end_date.Date < model.start_date.Date)
                {
                    ModelState.AddModelError(
                        "end_date",
                        "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                }

                // Không cho giảm sĩ số xuống thấp hơn lượng chỗ đã giữ hoặc học viên đang học.
                int activeEnrollmentCount = db.Enrollments.Count(e =>
                    e.class_id == classItem.class_id &&
                    (e.enrollment_status == "Pending" ||
                     e.enrollment_status == "Confirmed" ||
                     e.enrollment_status == "Studying"));

                if (model.class_capacity <= 0)
                {
                    ModelState.AddModelError(
                        "class_capacity",
                        "Sĩ số tối đa phải lớn hơn 0.");
                }
                else if (model.class_capacity < activeEnrollmentCount)
                {
                    ModelState.AddModelError(
                        "class_capacity",
                        "Sĩ số tối đa không thể nhỏ hơn số học viên đang giữ chỗ hoặc đang học.");
                }

                if (model.course_id != classItem.course_id &&
                    db.Enrollments.Any(e => e.class_id == classItem.class_id))
                {
                    ModelState.AddModelError(
                        "course_id",
                        "Không thể đổi khóa học của lớp đã có học viên đăng ký.");
                }

                if (!ClassStatuses.Contains(model.class_status))
                {
                    ModelState.AddModelError(
                        "class_status",
                        "Trạng thái lớp học không hợp lệ.");
                }

                if (!db.Set<Course>().Any(c =>
                    c.course_id == model.course_id))
                {
                    ModelState.AddModelError(
                        "course_id",
                        "Vui lòng chọn khóa học hợp lệ.");
                }

                if (model.teacher_id.HasValue &&
                    !db.Teacher_profile.Any(t =>
                        t.teacher_id == model.teacher_id.Value))
                {
                    ModelState.AddModelError(
                        "teacher_id",
                        "Giáo viên được chọn không tồn tại.");
                }

                if (model.room_id.HasValue &&
                    !db.Set<Room>().Any(r =>
                        r.room_id == model.room_id.Value))
                {
                    ModelState.AddModelError(
                        "room_id",
                        "Phòng học được chọn không tồn tại.");
                }
                else if (model.room_id.HasValue)
                {
                    var selectedRoom = db.Set<Room>().Single(r =>
                        r.room_id == model.room_id.Value);
                    if (model.class_capacity > selectedRoom.max_capacity)
                    {
                        ModelState.AddModelError(
                            "class_capacity",
                            "Sĩ số lớp không được vượt quá sức chứa của phòng.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    LoadDropdowns(
                        model.course_id,
                        model.teacher_id,
                        model.room_id);
                    ViewBag.ActiveEnrollmentCount = activeEnrollmentCount;

                    return View(model);
                }

                // Chỉ cập nhật các trường được phép chỉnh sửa.
                classItem.class_code = model.class_code;
                classItem.course_id = model.course_id;
                classItem.teacher_id = model.teacher_id;
                classItem.room_id = model.room_id;
                classItem.class_capacity = model.class_capacity;
                classItem.start_date = model.start_date;
                classItem.end_date = model.end_date;
                classItem.class_status = model.class_status;

                // Giữ nguyên class_id và created_at; chỉ cập nhật updated_at.
                classItem.updated_at = DateTime.Now;

                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã cập nhật lớp học thành công.";

                return RedirectToAction(
                    "Details",
                    new { id = classItem.class_id });
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ClassManageController.Details POST: {0}",
                    ex);

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveStudent(Guid enrollmentId, Guid classId)
        {
            var enrollment = db.Enrollments.SingleOrDefault(e =>
                e.enrollment_id == enrollmentId && e.class_id == classId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy học sinh trong lớp.";
                return RedirectToAction("Details", new { id = classId });
            }

            db.Enrollments.Remove(enrollment);

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa học sinh khỏi lớp.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.RemoveStudent: {0}", ex);
                TempData["ErrorMessage"] = "Không thể xóa enrollment. Dữ liệu liên quan vẫn đang sử dụng enrollment này.";
            }

            return RedirectToAction("Details", new { id = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WithdrawStudent(Guid enrollmentId, Guid classId)
        {
            var enrollment = db.Enrollments.SingleOrDefault(e =>
                e.enrollment_id == enrollmentId && e.class_id == classId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy học sinh trong lớp.";
                return RedirectToAction("Details", new { id = classId });
            }

            enrollment.enrollment_status = "Withdrawn";
            enrollment.withdrawn_at = DateTime.Now;
            enrollment.updated_at = DateTime.Now;

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa học sinh trong lớp.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.WithdrawStudent: {0}", ex);
                TempData["ErrorMessage"] = "Không thể vô hiệu hóa học sinh.";
            }

            return RedirectToAction("Details", new { id = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReactivateStudent(Guid enrollmentId, Guid classId)
        {
            var enrollment = db.Enrollments.SingleOrDefault(e =>
                e.enrollment_id == enrollmentId && e.class_id == classId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy học sinh trong lớp.";
                return RedirectToAction("Details", new { id = classId });
            }

            enrollment.enrollment_status = "Confirmed";
            enrollment.withdrawn_at = null;
            enrollment.updated_at = DateTime.Now;

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã kích hoạt lại học sinh trong lớp.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.ReactivateStudent: {0}", ex);
                TempData["ErrorMessage"] = "Không thể kích hoạt lại học sinh.";
            }

            return RedirectToAction("Details", new { id = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateSessions(Guid classId)
        {
            var classItem = db.Classes.Include(c => c.ClassSchedules)
                .SingleOrDefault(c => c.class_id == classId);
            if (classItem == null) return HttpNotFound();

            var schedules = classItem.ClassSchedules.ToList();
            if (!schedules.Any())
            {
                TempData["ErrorMessage"] = "Lớp chưa có lịch học hàng tuần.";
                return RedirectToAction("Details", new { id = classId });
            }

            if (classItem.start_date.Date > classItem.end_date.Date)
            {
                TempData["ErrorMessage"] = "Ngày bắt đầu của lớp phải nhỏ hơn hoặc bằng ngày kết thúc.";
                return RedirectToAction("Details", new { id = classId });
            }

            var existing = db.ClassSessions.Where(s => s.class_id == classId)
                .Select(s => new { s.session_date, s.time_slot_id }).ToList();
            var added = 0;
            for (var date = classItem.start_date.Date; date <= classItem.end_date.Date; date = date.AddDays(1))
            {
                var weekDay = GetScheduleWeekDay(date.DayOfWeek);
                foreach (var schedule in schedules.Where(s =>
                    string.Equals(s.week_day, weekDay, StringComparison.OrdinalIgnoreCase)))
                {
                    if (existing.Any(s => s.session_date == date && s.time_slot_id == schedule.time_slot_id)) continue;
                    db.ClassSessions.Add(new ClassSession
                    {
                        session_id = Guid.NewGuid(), class_id = classId,
                        time_slot_id = schedule.time_slot_id, session_date = date
                    });
                    existing.Add(new { session_date = date, time_slot_id = schedule.time_slot_id });
                    added++;
                }
            }
            db.SaveChanges();
            TempData["SuccessMessage"] = "Đã tạo thêm " + added + " buổi học từ lịch tuần.";
            return RedirectToAction("Details", new { id = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSessionTopic(Guid sessionId, Guid classId, string topic)
        {
            var session = db.ClassSessions.SingleOrDefault(s =>
                s.session_id == sessionId && s.class_id == classId);
            if (session == null) return HttpNotFound();

            session.topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
            if (session.topic != null && session.topic.Length > 300)
            {
                TempData["ErrorMessage"] = "Chủ đề không được vượt quá 300 ký tự.";
                return RedirectToAction("Details", new { id = classId });
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Đã cập nhật chủ đề buổi học.";
            return RedirectToAction("Details", new { id = classId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSession(Guid sessionId, Guid classId)
        {
            var session = db.ClassSessions.SingleOrDefault(s =>
                s.session_id == sessionId && s.class_id == classId);
            if (session == null) return HttpNotFound();

            if (db.AttendanceRecords.Any(a => a.session_id == sessionId))
            {
                TempData["ErrorMessage"] = "Không thể xóa buổi học đã có dữ liệu điểm danh.";
                return RedirectToAction("Details", new { id = classId });
            }

            db.ClassSessions.Remove(session);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa buổi học.";
            return RedirectToAction("Details", new { id = classId });
        }

        private static string GetScheduleWeekDay(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "Monday";
                case DayOfWeek.Tuesday: return "Tuesday";
                case DayOfWeek.Wednesday: return "Wednesday";
                case DayOfWeek.Thursday: return "Thursday";
                case DayOfWeek.Friday: return "Friday";
                case DayOfWeek.Saturday: return "Saturday";
                default: return "Sunday";
            }
        }

        private static readonly string[] ScheduleWeekDays =
        {
            "Monday", "Tuesday", "Wednesday", "Thursday",
            "Friday", "Saturday", "Sunday"
        };

        [HttpGet]
        public ActionResult AddSchedule(Guid? classId)
        {
            if (!classId.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            if (!db.Classes.Any(c => c.class_id == classId.Value))
                return HttpNotFound();

            var model = new ClassScheduleViewModel { class_id = classId.Value };
            LoadScheduleDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddSchedule(ClassScheduleViewModel model)
        {
            var classItem = db.Classes
                .Include(c => c.Room)
                .SingleOrDefault(c => c.class_id == model.class_id);

            if (classItem == null)
                return HttpNotFound();

            // The ID is generated server-side; it must not be required from the form.
            ModelState.Remove("class_schedule_id");
            model.class_schedule_id = Guid.NewGuid();
            model.room_name = classItem.Room == null ? null : classItem.Room.room_name;
            ValidateSchedule(model);
            if (!ModelState.IsValid)
            {
                LoadScheduleDropdowns(model);
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, errors = ModelStateErrors() });
                return View(model);
            }

            var now = DateTime.Now;
            db.ClassSchedules.Add(new ClassSchedule
            {
                class_schedule_id = Guid.NewGuid(),
                class_id = model.class_id,
                time_slot_id = model.time_slot_id,
                week_day = model.week_day,
                room_name = classItem.Room == null ? null : classItem.Room.room_name,
                created_at = now,
                updated_at = now
            });

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.AddSchedule: {0}", ex);
                ModelState.AddModelError("", "Không thể thêm lịch học. Vui lòng thử lại.");
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, errors = ModelStateErrors() });
                LoadScheduleDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Đã thêm lịch học thành công.";
            if (Request.IsAjaxRequest())
                return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = model.class_id }) });
            return RedirectToAction("Details", new { id = model.class_id });
        }

        [HttpGet]
        public ActionResult EditSchedule(Guid? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var schedule = db.ClassSchedules.Find(id.Value);
            if (schedule == null)
                return HttpNotFound();

            var model = new ClassScheduleViewModel
            {
                class_schedule_id = schedule.class_schedule_id,
                class_id = schedule.class_id,
                time_slot_id = schedule.time_slot_id,
                week_day = schedule.week_day,
                room_name = schedule.room_name
            };
            LoadScheduleDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSchedule(ClassScheduleViewModel model)
        {
            var schedule = db.ClassSchedules.Find(model.class_schedule_id);
            if (schedule == null)
                return HttpNotFound();

            if (schedule.class_id != model.class_id)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var classItem = db.Classes
                .Include(c => c.Room)
                .SingleOrDefault(c => c.class_id == schedule.class_id);
            model.room_name = classItem == null || classItem.Room == null
                ? null : classItem.Room.room_name;

            ValidateSchedule(model);
            if (!ModelState.IsValid)
            {
                LoadScheduleDropdowns(model);
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, errors = ModelStateErrors() });
                return View(model);
            }

            schedule.time_slot_id = model.time_slot_id;
            schedule.week_day = model.week_day;
            schedule.room_name = model.room_name;
            schedule.updated_at = DateTime.Now;

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.EditSchedule: {0}", ex);
                ModelState.AddModelError("", "Không thể cập nhật lịch học. Vui lòng thử lại.");
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, errors = ModelStateErrors() });
                LoadScheduleDropdowns(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Đã cập nhật lịch học thành công.";
            if (Request.IsAjaxRequest())
                return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = schedule.class_id }) });
            return RedirectToAction("Details", new { id = schedule.class_id });
        }

        private string[] ModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "Dữ liệu không hợp lệ."
                    : e.ErrorMessage)
                .ToArray();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSchedule(Guid id)
        {
            var schedule = db.ClassSchedules.Find(id);
            if (schedule == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Không tìm thấy lịch học." });
                return HttpNotFound();
            }

            var classId = schedule.class_id;
            db.ClassSchedules.Remove(schedule);
            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa lịch học thành công.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClassManageController.DeleteSchedule: {0}", ex);
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Không thể xóa lịch học. Vui lòng thử lại." });
                TempData["ErrorMessage"] = "Không thể xóa lịch học. Vui lòng thử lại.";
            }

            if (Request.IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Details", new { id = classId })
                });

            return RedirectToAction("Details", new { id = classId });
        }

        private void ValidateSchedule(ClassScheduleViewModel model)
        {
            if (!ScheduleWeekDays.Contains(model.week_day))
                ModelState.AddModelError("week_day", "Ngày học không hợp lệ.");

            if (!db.TimeSlots.Any(t => t.time_slot_id == model.time_slot_id &&
                t.available_status == "Available"))
                ModelState.AddModelError("time_slot_id", "Vui lòng chọn khung giờ đang khả dụng.");

            if (db.ClassSchedules.Any(s => s.class_id == model.class_id &&
                s.week_day == model.week_day && s.time_slot_id == model.time_slot_id &&
                s.class_schedule_id != model.class_schedule_id))
                ModelState.AddModelError("", "Lớp đã có lịch học vào ngày và khung giờ này.");

            var classItem = db.Classes
                .SingleOrDefault(c => c.class_id == model.class_id);

            if (classItem != null && classItem.teacher_id.HasValue &&
                db.ClassSchedules.Any(s =>
                    s.class_id != model.class_id &&
                    s.week_day == model.week_day &&
                    s.time_slot_id == model.time_slot_id &&
                    db.Classes.Any(c =>
                        c.class_id == s.class_id &&
                        c.teacher_id == classItem.teacher_id)))
            {
                ModelState.AddModelError(
                    "",
                    "Giáo viên đã có lịch dạy vào ngày và khung giờ này.");
            }

            if (classItem != null && classItem.room_id.HasValue &&
                db.ClassSchedules.Any(s =>
                    s.class_id != model.class_id &&
                    s.week_day == model.week_day &&
                    s.time_slot_id == model.time_slot_id &&
                    db.Classes.Any(c =>
                        c.class_id == s.class_id &&
                        c.room_id == classItem.room_id)))
            {
                ModelState.AddModelError(
                    "",
                    "Phòng học đã được sử dụng vào ngày và khung giờ này.");
            }

            model.room_name = string.IsNullOrWhiteSpace(model.room_name)
                ? null : model.room_name.Trim();
        }

        private void LoadScheduleDropdowns(ClassScheduleViewModel model)
        {
            var slots = db.TimeSlots
                .Where(t => t.available_status == "Available")
                .OrderBy(t => t.start_time)
                .ToList()
                .Select(t => new
                {
                    Id = t.time_slot_id,
                    Name = t.slot_name + " (" + t.start_time.ToString(@"hh\:mm") +
                        " - " + t.end_time.ToString(@"hh\:mm") + ")"
                });

            model.TimeSlots = new SelectList(slots, "Id", "Name", model.time_slot_id);
            model.WeekDays = new SelectList(ScheduleWeekDays, model.week_day);
        }

        // POST: ClassManage/Delete/{id}
        // Xóa lớp học trực tiếp từ danh sách.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            try
            {
                var classItem = db.Classes
                    .SingleOrDefault(c => c.class_id == id);

                if (classItem == null)
                {
                    TempData["ErrorMessage"] =
                        "Không tìm thấy lớp học cần xóa.";

                    return RedirectToAction("Index");
                }

                // Không tự xóa dữ liệu liên quan.
                // Nếu lớp đang được bảng khác tham chiếu,
                // SQL Server sẽ báo lỗi khóa ngoại.
                db.Classes.Remove(classItem);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã xóa lớp học thành công.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "ClassManageController.Delete POST: {0}",
                    ex);

                TempData["ErrorMessage"] =
                    "Không thể xóa lớp học. Lớp có thể đang được dữ liệu khác sử dụng.";

                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Nạp danh sách khóa học, giáo viên, phòng học và trạng thái cho dropdown.
        /// </summary>
        private void LoadDropdowns(
    Guid? selectedCourseId = null,
    Guid? selectedTeacherId = null,
    Guid? selectedRoomId = null)
        {
            // Value là ID, Text là tên khóa học
            var courses = db.Set<Course>()
                .ToList()
                .Select(c => new
                {
                    Id = c.course_id,
                    Name = c.course_name
                })
                .ToList();

            // Lấy danh sách giáo viên kèm họ tên đầy đủ từ bảng User.
            var teachers = (
                from t in db.Teacher_profile
                join s in db.Staff_Profile
                    on t.staff_id equals s.staff_id
                join u in db.Users
                    on s.user_id equals u.user_id
                where t.available_status == "Available"
                orderby u.full_name
                select new
                {
                    Id = t.teacher_id,
                    Name = u.full_name
                }
            ).ToList();

            // Value là ID, Text là tên phòng học
            var rooms = db.Set<Room>()
                .ToList()
                .Select(r => new
                {
                    Id = r.room_id,
                    Name = r.room_name
                })
                .ToList();

            ViewBag.Courses = new SelectList(
                courses, "Id", "Name", selectedCourseId);

            ViewBag.Teachers = new SelectList(
                teachers, "Id", "Name", selectedTeacherId);

            ViewBag.Rooms = new SelectList(
                rooms, "Id", "Name", selectedRoomId);

            ViewBag.ClassStatuses = new SelectList(
                new[] { "Planned", "Open", "InProgress", "Completed", "Cancelled" });
        }

        /// <summary>
        /// Giải phóng tài nguyên của DbContext khi controller kết thúc.
        /// </summary>
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
