using Source.Models;
using Source.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class HomeController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        public ActionResult Index(int page = 1)
        {
            try
            {
                const int pageSize = 6;

                page = Math.Max(page, 1);

                var availableCourses = db.Courses
                    .Where(course => course.available_status == "Available")
                    .OrderByDescending(course => course.created_at);

                int totalCourses = availableCourses.Count();

                int totalPages = (int)Math.Ceiling(
                    (double)totalCourses / pageSize);

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                var courses = availableCourses
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                HomePageViewModel viewModel = new HomePageViewModel
                {
                    activeStudentCount = db.Student_Profile.Count(
                        student => student.student_status == "Active"),

                    courseCount = db.Courses.Count(
                        course => course.available_status == "Available"),

                    classCount = db.Classes.Count(
                        c => c.class_status == "Open" ||
                             c.class_status == "InProgress"),

                    activeTeacherCount = db.Teacher_profile.Count(
                        teacher => teacher.available_status == "Available"),

                    Courses = courses,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                return View(new HomePageViewModel
                {
                    Courses = new System.Collections.Generic.List<Course>()
                });
            }
        }

        public ActionResult Courses(int page = 1)
        {
            try
            {
                const int pageSize = 6;

                page = Math.Max(page, 1);

                var availableCourses = db.Courses
                    .Where(course => course.available_status == "Available")
                    .OrderByDescending(course => course.created_at);

                int totalCourses = availableCourses.Count();

                int totalPages = (int)Math.Ceiling(
                    (double)totalCourses / pageSize);

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                var courses = availableCourses
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                HomePageViewModel viewModel = new HomePageViewModel
                {
                    Courses = courses,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                return View(new HomePageViewModel
                {
                    Courses = new List<Course>(),
                    CurrentPage = 1,
                    TotalPages = 0
                });
            }
        }

        public async Task<ActionResult> Details(Guid id)
        {
            try
            {
                Course course = await db.Courses.FirstOrDefaultAsync(c => c.course_id == id && c.available_status == "Available");

                CourseDetailViewModel detailViewModel = new CourseDetailViewModel
                {
                    course_code = course.course_code,
                    course_name = course.course_name,
                    description = course.description,
                    duration_weeks = course.duration_weeks,
                    level = course.level,
                    tuition_fee = course.tuition_fee,
                    RelatedCourses = await db.Courses
                                    .Where(c => c.level == course.level
                                             && c.course_id != course.course_id
                                             && c.available_status == "Available")
                                    .ToListAsync()
                };

                return View(detailViewModel);

            }
            catch
            {
                // Có lỗi khi truy vấn hoặc xử lý dữ liệu thì hiển thị trang lỗi 500.
                return RedirectToAction("ServerError", "Error"); ;
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

          

            HomePageViewModel viewModel = new HomePageViewModel
            {
                activeStudentCount = db.Student_Profile.Count(
                    student => student.student_status == "Active"),

                courseCount = db.Courses.Count(
                    course => course.available_status == "Available"),

                classCount = db.Classes.Count(
                    c => c.class_status == "Open" ||
                         c.class_status == "InProgress"),

                activeTeacherCount = db.Teacher_profile.Count(
                    teacher => teacher.available_status == "Available")
            };

            return View(viewModel);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}