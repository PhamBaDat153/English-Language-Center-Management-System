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
                int totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

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
                    activeStudentCount = db.Student_Profile.Count(student => student.student_status == "Active"),
                    courseCount = db.Courses.Count(course => course.available_status == "Available"),
                    classCount = db.Classes.Count(c => c.class_status == "Open" || c.class_status == "InProgress"),
                    activeTeacherCount = db.Teacher_profile.Count(teacher => teacher.available_status == "Available"),
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
                    Courses = new List<Course>()
                });
            }
        }

        // Cập nhật phương thức Courses hỗ trợ lọc theo Tên (searchName) và Trình độ (level)
        public ActionResult Courses(string searchName, string level, int page = 1)
        {
            try
            {
                const int pageSize = 6;
                page = Math.Max(page, 1);

                // 1. Lấy danh sách khóa học đang 'Available'
                var query = db.Courses.Where(c => c.available_status == "Available");

                // 2. Lọc theo từ khóa tên khóa học
                if (!string.IsNullOrWhiteSpace(searchName))
                {
                    searchName = searchName.Trim();
                    query = query.Where(c => c.course_name.Contains(searchName) || c.course_code.Contains(searchName));
                }

                // 3. Lọc theo trình độ khóa học
                if (!string.IsNullOrWhiteSpace(level))
                {
                    query = query.Where(c => c.level == level);
                }

                // Sắp xếp theo ngày tạo mới nhất
                var availableCourses = query.OrderByDescending(course => course.created_at);

                int totalCourses = availableCourses.Count();
                int totalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                var courses = availableCourses
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Lưu lại thông tin tìm kiếm ra ViewBag để giữ giá trị trên giao diện trang Courses
                ViewBag.CurrentSearchName = searchName;
                ViewBag.CurrentLevel = level;

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
                if (course == null)
                {
                    return HttpNotFound();
                }

                int totalStudents = await db.Enrollments
                    .Where(e => e.Class.course_id == id &&
                                (e.enrollment_status == "Confirmed" ||
                                 e.enrollment_status == "Studying" ||
                                 e.enrollment_status == "Completed"))
                    .Select(e => e.student_id)
                    .Distinct()
                    .CountAsync();

                // Chỉ đưa ra lớp đang mở, sắp khai giảng, có lịch cụ thể và phòng còn sử dụng được.
                var classes = await db.Classes
                    .Include(c => c.Room)
                    .Include(c => c.ClassSchedules.Select(s => s.TimeSlot))
                    .Where(c => c.course_id == id &&
                                c.class_status == "Open" &&
                                c.start_date >= DateTime.Today &&
                                c.end_date >= DateTime.Today &&
                                c.ClassSchedules.Any() &&
                                (c.Room == null || c.Room.available_status == "Available"))
                    .OrderBy(c => c.start_date)
                    .ThenBy(c => c.class_code)
                    .ToListAsync();

                var classIds = classes.Select(c => c.class_id).ToList();
                // Pending cũng giữ chỗ trong lúc thanh toán; chỉ trạng thái kết thúc mới nhả chỗ.
                var activeEnrollmentCounts = await db.Enrollments
                    .Where(e => classIds.Contains(e.class_id) &&
                                (e.enrollment_status == "Pending" ||
                                 e.enrollment_status == "Confirmed" ||
                                 e.enrollment_status == "Studying"))
                    .GroupBy(e => e.class_id)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToListAsync();
                var countByClassId = activeEnrollmentCounts.ToDictionary(x => x.ClassId, x => x.Count);

                var classOptions = classes.Select(classItem =>
                {
                    int enrolledCount = countByClassId.ContainsKey(classItem.class_id)
                        ? countByClassId[classItem.class_id]
                        : 0;
                    // Sĩ số thực tế không được vượt quá giới hạn lớp hoặc giới hạn phòng.
                    int capacity = classItem.Room == null
                        ? classItem.class_capacity
                        : Math.Min(classItem.class_capacity, classItem.Room.max_capacity);
                    string scheduleText = string.Join(", ", classItem.ClassSchedules
                        .OrderBy(s => s.week_day)
                        .ThenBy(s => s.TimeSlot.start_time)
                        .Select(s => TranslateWeekDay(s.week_day) + " " + s.TimeSlot.slot_name + " (" +
                                     s.TimeSlot.start_time.ToString(@"hh\:mm") + "–" +
                                     s.TimeSlot.end_time.ToString(@"hh\:mm") + ")"));

                    return new ClassOptionViewModel
                    {
                        ClassId = classItem.class_id,
                        ClassCode = classItem.class_code,
                        StartDate = classItem.start_date,
                        EndDate = classItem.end_date,
                        Capacity = capacity,
                        EnrolledCount = enrolledCount,
                        ScheduleText = string.IsNullOrWhiteSpace(scheduleText)
                            ? "Chưa xếp lịch học"
                            : scheduleText,
                        RoomName = classItem.Room == null ? "Chưa xếp phòng" : classItem.Room.room_name,
                        IsFull = enrolledCount >= capacity
                    };
                }).ToList();

                CourseDetailViewModel detailViewModel = new CourseDetailViewModel
                {
                    course_id = course.course_id,
                    course_code = course.course_code,
                    course_name = course.course_name,
                    description = course.description,
                    duration_weeks = course.duration_weeks,
                    level = course.level,
                    tuition_fee = course.tuition_fee,
                    TotalStudents = totalStudents,
                    AvailableClasses = classOptions,
                    RelatedCourses = await db.Courses
                                    .Where(c => c.level == course.level
                                             && c.course_id != course.course_id
                                             && c.available_status == "Available")
                                    .ToListAsync()
                };

                return View(detailViewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "HomeController.Details failed for course {0}: {1}",
                    id,
                    ex);
                return RedirectToAction("ServerError", "Error");
            }
        }

        private static string TranslateWeekDay(string weekDay)
        {
            switch (weekDay)
            {
                case "Monday": return "Thứ hai";
                case "Tuesday": return "Thứ ba";
                case "Wednesday": return "Thứ tư";
                case "Thursday": return "Thứ năm";
                case "Friday": return "Thứ sáu";
                case "Saturday": return "Thứ bảy";
                case "Sunday": return "Chủ nhật";
                default: return weekDay;
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            HomePageViewModel viewModel = new HomePageViewModel
            {
                activeStudentCount = db.Student_Profile.Count(student => student.student_status == "Active"),
                courseCount = db.Courses.Count(course => course.available_status == "Available"),
                classCount = db.Classes.Count(c => c.class_status == "Open" || c.class_status == "InProgress"),
                activeTeacherCount = db.Teacher_profile.Count(teacher => teacher.available_status == "Available")
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
