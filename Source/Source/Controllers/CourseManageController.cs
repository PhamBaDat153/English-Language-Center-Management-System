using Source.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class CourseManageController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();
        // GET: CourseManage
        public ActionResult Index(string courseCode, string courseName,   string level,   string status,   int page = 1)
        {
            var query = db.Courses.AsQueryable();
            // Lọc theo mã khóa học
            if (!string.IsNullOrWhiteSpace(courseCode))
            {
                query = query.Where(c =>
                    c.course_code != null &&
                    c.course_code.Contains(courseCode));
            }

            // Lọc theo tên khóa học
            if (!string.IsNullOrWhiteSpace(courseName))
            {
                query = query.Where(c =>
                    c.course_name != null &&
                    c.course_name.Contains(courseName));
            }

            // Lọc theo trình độ
            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(c => c.level == level);
            }

            // Lọc theo trạng thái
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


        // GET: CourseManage/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CourseManage/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Course course,
            IEnumerable<HttpPostedFileBase> pdfFiles)
        {
            // Bo qua cac file khong duoc chon
            var files = pdfFiles?
                .Where(f => f != null && f.ContentLength > 0)
                .ToList();

            // Kiem tra duoi file
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
                            "Chi duoc upload file PDF.");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            try
            {
                // Upload PDF len Cloudinary truoc
                var uploadedMaterials =
                    new List<SyllabusMaterial>();

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

                // Luu khoa hoc
                course.course_id = Guid.NewGuid();
                course.created_at = DateTime.Now;
                course.updated_at = DateTime.Now;
                course.available_status = "Available";

                db.Courses.Add(course);
                db.SaveChanges();

                // Lien ket tai lieu voi khoa hoc
                foreach (var material in uploadedMaterials)
                {
                    material.course_id = course.course_id;
                    db.SyllabusMaterials.Add(material);
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.PropertyName + ": " + e.ErrorMessage)
                    .ToList();

                ModelState.AddModelError(
                    "",
                    "Loi du lieu: " + string.Join(" | ", errors)
                );

                return View(course);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Chi tiet loi: " + ex.GetBaseException().Message
                );

                return View(course);
            }
        }


        //GET details
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest
                );
            }

            // Tim khoa hoc theo ID
            Course course = db.Courses
                .FirstOrDefault(c => c.course_id == id.Value);

            if (course == null)
            {
                return HttpNotFound();
            }

            // Lay cac tai lieu PDF cua khoa hoc
            var materials = db.SyllabusMaterials
                .Where(s => s.course_id == course.course_id)
                .ToList();

            ViewBag.SyllabusMaterials = materials;

            return View(course);
        }


        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(Course model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.SyllabusMaterials = db.SyllabusMaterials
                    .Where(s => s.course_id == model.course_id)
                    .ToList();

                return View(model);
            }

            var course = db.Courses.FirstOrDefault(
                c => c.course_id == model.course_id
            );

            if (course == null)
            {
                return HttpNotFound();
            }

            // Cap nhat thong tin khoa hoc
            course.course_code = model.course_code;
            course.course_name = model.course_name;
            course.level = model.level;
            course.duration_weeks = model.duration_weeks;
            course.tuition_fee = model.tuition_fee;
            course.description = model.description;
            course.available_status = model.available_status;
            course.updated_at = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật khóa học thành công.";

            return RedirectToAction(
                "Details",
                new { id = course.course_id }
            );
        }

        //UPLOAD PDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterials(
            Guid courseId,
            IEnumerable<HttpPostedFileBase> pdfFiles)
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
                TempData["PdfError"] = "Vui lòng chọn ít nhất một file PDF.";
                return RedirectToAction("Details", new { id = courseId });
            }

            if (files.Any(f => !string.Equals(
                System.IO.Path.GetExtension(f.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase)))
            {
                TempData["PdfError"] = "Chỉ được tải lên file PDF.";
                return RedirectToAction("Details", new { id = courseId });
            }

            try
            {
                var cloudinaryService = new Source.Services.CloudinaryService();

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

                TempData["PdfSuccess"] = "Đã thêm tài liệu PDF thành công.";
            }
            catch (Exception ex)
            {
                TempData["PdfError"] =
                    "Không thể thêm PDF: " + ex.GetBaseException().Message;
            }

            return RedirectToAction("Details", new { id = courseId });
        }



        //XÓa PDF 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMaterial(Guid id)
        {
            var material = db.SyllabusMaterials
                .FirstOrDefault(s => s.syllabus_id == id);

            if (material == null)
            {
                return HttpNotFound("Không tìm thấy tài liệu PDF.");
            }

            Guid courseId = material.course_id;

            db.SyllabusMaterials.Remove(material);
            db.SaveChanges();

            TempData["PdfSuccess"] = "Đã xóa tài liệu PDF thành công.";

            return RedirectToAction("Details", new { id = courseId });
        }



        [HttpGet]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);

            var course = db.Courses
                .FirstOrDefault(c => c.course_id == id.Value);

            if (course == null)
                return HttpNotFound();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            var course = db.Courses
                .FirstOrDefault(c => c.course_id == id);

            if (course == null)
                return HttpNotFound();

            var materials = db.SyllabusMaterials
                .Where(s => s.course_id == id)
                .ToList();

            db.SyllabusMaterials.RemoveRange(materials);
            db.Courses.Remove(course);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã xóa khóa học thành công.";

            return RedirectToAction("Index");
        }


    }
}