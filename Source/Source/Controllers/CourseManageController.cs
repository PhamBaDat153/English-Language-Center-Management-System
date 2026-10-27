using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class CourseManageController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        // GET: CourseManage
        public ActionResult Index(
            string courseCode,
            string courseName,
            string level,
            string status,
            int page = 1)
        {
            try
            {
                var query = db.Courses.AsQueryable();

                if (!string.IsNullOrWhiteSpace(courseCode))
                {
                    query = query.Where(c =>
                        c.course_code != null &&
                        c.course_code.Contains(courseCode));
                }

                if (!string.IsNullOrWhiteSpace(courseName))
                {
                    query = query.Where(c =>
                        c.course_name != null &&
                        c.course_name.Contains(courseName));
                }

                if (!string.IsNullOrWhiteSpace(level))
                {
                    query = query.Where(c => c.level == level);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(c => c.available_status == status);
                }

                // Thống kê toàn bộ khóa học
                ViewBag.TotalCourses = db.Courses.Count();

                ViewBag.TotalActiveCourses = db.Courses.Count(
                    c => c.available_status == "Available");

                ViewBag.TotalInactiveCourses = db.Courses.Count(
                    c => c.available_status == "Unavailable");

                // Giữ lại điều kiện tìm kiếm trên giao diện
                ViewBag.CourseCode = courseCode;
                ViewBag.CourseName = courseName;
                ViewBag.Level = level;
                ViewBag.Status = status;

                // Phân trang
                int pageSize = 10;
                int totalCourses = query.Count();

                int totalPages = (int)Math.Ceiling(
                    (double)totalCourses / pageSize);

                if (page < 1)
                {
                    page = 1;
                }

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.FilteredCourseCount = totalCourses;
                ViewBag.PageSize = pageSize;

                var courses = query
                    .OrderBy(c => c.course_name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View(courses);
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // GET: CourseManage/Create
        public ActionResult Create()
        {
            try
            {
                return View();
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // POST: CourseManage/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Course course,
            IEnumerable<HttpPostedFileBase> pdfFiles)
        {
            try
            {
                // Bỏ qua các file không được chọn
                var files = pdfFiles?
                    .Where(f => f != null && f.ContentLength > 0)
                    .ToList();

                // Kiểm tra đuôi file
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (!string.Equals(
                            System.IO.Path.GetExtension(file.FileName),
                            ".pdf",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(
                                "pdfFiles",
                                "Chỉ được upload file PDF.");
                            break;
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(course);
                }

                // Upload PDF lên Cloudinary trước
                var uploadedMaterials = new List<SyllabusMaterial>();

                if (files != null && files.Count > 0)
                {
                    var cloudinaryService =
                        new Source.Services.CloudinaryService();

                    foreach (var file in files)
                    {
                        string fileUrl =
                            cloudinaryService.UploadPdf(file);

                        uploadedMaterials.Add(new SyllabusMaterial
                        {
                            syllabus_id = Guid.NewGuid(),
                            title = System.IO.Path.GetFileNameWithoutExtension(
                                file.FileName),
                            description = "",
                            file_url = fileUrl,
                            available_status = "Available"
                        });
                    }
                }

                // Lưu khóa học
                course.course_id = Guid.NewGuid();
                course.created_at = DateTime.Now;
                course.updated_at = DateTime.Now;
                course.available_status = "Available";

                db.Courses.Add(course);
                db.SaveChanges();

                // Liên kết tài liệu với khóa học
                foreach (var material in uploadedMaterials)
                {
                    material.course_id = course.course_id;
                    db.SyllabusMaterials.Add(material);
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // GET: CourseManage/Details
        public ActionResult Details(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.BadRequest);
                }

                // Tìm khóa học theo ID
                Course course = db.Courses
                    .FirstOrDefault(c => c.course_id == id.Value);

                if (course == null)
                {
                    return HttpNotFound();
                }

                // Lấy các tài liệu PDF của khóa học
                var materials = db.SyllabusMaterials
                    .Where(s => s.course_id == course.course_id)
                    .ToList();

                ViewBag.SyllabusMaterials = materials;

                return View(course);
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // POST: CourseManage/Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(Course model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.SyllabusMaterials = db.SyllabusMaterials
                        .Where(s => s.course_id == model.course_id)
                        .ToList();

                    return View(model);
                }

                var course = db.Courses.FirstOrDefault(
                    c => c.course_id == model.course_id);

                if (course == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật thông tin khóa học
                course.course_code = model.course_code;
                course.course_name = model.course_name;
                course.level = model.level;
                course.duration_weeks = model.duration_weeks;
                course.tuition_fee = model.tuition_fee;
                course.description = model.description;
                course.available_status = model.available_status;
                course.updated_at = DateTime.Now;

                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Cập nhật khóa học thành công.";

                return RedirectToAction(
                    "Details",
                    new { id = course.course_id });
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // UPLOAD PDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterials(
            Guid courseId,
            IEnumerable<HttpPostedFileBase> pdfFiles)
        {
            try
            {
                var course = db.Courses.FirstOrDefault(
                    c => c.course_id == courseId);

                if (course == null)
                {
                    return HttpNotFound();
                }

                var files = pdfFiles?
                    .Where(f => f != null && f.ContentLength > 0)
                    .ToList();

                if (files == null || files.Count == 0)
                {
                    TempData["PdfError"] =
                        "Vui lòng chọn ít nhất một file PDF.";

                    return RedirectToAction(
                        "Details",
                        new { id = courseId });
                }

                if (files.Any(f => !string.Equals(
                    System.IO.Path.GetExtension(f.FileName),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["PdfError"] = "Chỉ được tải lên file PDF.";

                    return RedirectToAction(
                        "Details",
                        new { id = courseId });
                }

                var cloudinaryService =
                    new Source.Services.CloudinaryService();

                foreach (var file in files)
                {
                    string fileUrl = cloudinaryService.UploadPdf(file);

                    db.SyllabusMaterials.Add(new SyllabusMaterial
                    {
                        syllabus_id = Guid.NewGuid(),
                        course_id = courseId,
                        title = System.IO.Path.GetFileNameWithoutExtension(
                            file.FileName),
                        description = "",
                        file_url = fileUrl,
                        available_status = "Available"
                    });
                }

                db.SaveChanges();

                TempData["PdfSuccess"] =
                    "Đã thêm tài liệu PDF thành công.";

                return RedirectToAction(
                    "Details",
                    new { id = courseId });
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // XÓA PDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMaterial(Guid id)
        {
            try
            {
                var material = db.SyllabusMaterials
                    .FirstOrDefault(s => s.syllabus_id == id);

                if (material == null)
                {
                    return HttpNotFound(
                        "Không tìm thấy tài liệu PDF.");
                }

                Guid courseId = material.course_id;

                db.SyllabusMaterials.Remove(material);
                db.SaveChanges();

                TempData["PdfSuccess"] =
                    "Đã xóa tài liệu PDF thành công.";

                return RedirectToAction(
                    "Details",
                    new { id = courseId });
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // GET: CourseManage/Delete
        [HttpGet]
        public ActionResult Delete(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(
                        System.Net.HttpStatusCode.BadRequest);
                }

                var course = db.Courses
                    .FirstOrDefault(c => c.course_id == id.Value);

                if (course == null)
                {
                    return HttpNotFound();
                }

                return View(course);
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // POST: CourseManage/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            try
            {
                var course = db.Courses
                    .FirstOrDefault(c => c.course_id == id);

                if (course == null)
                {
                    return HttpNotFound();
                }

                var materials = db.SyllabusMaterials
                    .Where(s => s.course_id == id)
                    .ToList();

                db.SyllabusMaterials.RemoveRange(materials);
                db.Courses.Remove(course);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã xóa khóa học thành công.";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }
    }
}