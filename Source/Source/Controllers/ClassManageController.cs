using Source.Models;
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

                // Giá trị mặc định khi mở form thêm lớp.
                var model = new Class
                {
                    class_id = Guid.NewGuid(),
                    start_date = DateTime.Today,
                    end_date = DateTime.Today,
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

                // Kiểm tra trạng thái thuộc enum ClassStatus.
                if (!ClassStatuses.Contains(model.class_status))
                {
                    ModelState.AddModelError(
                        "class_status",
                        "Trạng thái lớp học không hợp lệ.");
                }

                // Kiểm tra khóa học có tồn tại.
                bool courseExists = db.Set<Course>()
                    .Any(c => c.course_id == model.course_id);

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
                    bool roomExists = db.Set<Room>().Any(r =>
                        r.room_id == model.room_id.Value);

                    if (!roomExists)
                    {
                        ModelState.AddModelError(
                            "room_id",
                            "Phòng học được chọn không tồn tại.");
                    }
                }

                // Nếu có lỗi, nạp lại dropdown trước khi trả về form.
                if (!ModelState.IsValid)
                {
                    LoadDropdowns(
                        model.course_id,
                        model.teacher_id,
                        model.room_id);

                    return View(model);
                }

                // Không tin class_id/timestamps do form gửi lên.
                model.class_id = Guid.NewGuid();
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
        public ActionResult Details(Guid? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.BadRequest);
                }

                var classItem = db.Classes
                    .SingleOrDefault(c => c.class_id == id.Value);

                if (classItem == null)
                {
                    return HttpNotFound();
                }

                LoadDropdowns(
                    classItem.course_id,
                    classItem.teacher_id,
                    classItem.room_id);

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

                if (!ModelState.IsValid)
                {
                    LoadDropdowns(
                        model.course_id,
                        model.teacher_id,
                        model.room_id);

                    return View(model);
                }

                // Chỉ cập nhật các trường được phép chỉnh sửa.
                classItem.class_code = model.class_code;
                classItem.course_id = model.course_id;
                classItem.teacher_id = model.teacher_id;
                classItem.room_id = model.room_id;
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