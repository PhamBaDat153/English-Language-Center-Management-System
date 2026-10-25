using Source.Models;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;

namespace Source.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private NewGen_english_center_dbEntities db = new NewGen_english_center_dbEntities();

        // POST: /Enrollment/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DangKy(Guid courseId, Guid? classId)
        {
            try
            {
                // =========================================================
                // 1. LẤY TÀI KHOẢN ĐĂNG NHẬP & SỬA LỖI ĐÁ VỀ TRANG CHỦ
                // =========================================================
                User currentUser = null;
                Guid currentUserId = Guid.Empty;

                // 1.1 Lấy ID từ Session
                if (Session["UserId"] != null)
                {
                    currentUserId = (Guid)Session["UserId"];
                }
                else if (Session["StudentId"] != null)
                {
                    currentUserId = (Guid)Session["StudentId"];
                }
                else if (Session["User"] != null)
                {
                    var userObj = Session["User"] as User;
                    if (userObj != null) currentUserId = userObj.user_id;
                }

                if (currentUserId != Guid.Empty)
                {
                    currentUser = await db.Users.FirstOrDefaultAsync(u => u.user_id == currentUserId);
                }

                // 1.2 KHÔI PHỤC SESSION TỪ COOKIE (Ngăn chặn việc bị đá về Trang chủ)
                if (currentUser == null && User.Identity.IsAuthenticated)
                {
                    string username = User.Identity.Name;
                    currentUser = await db.Users.FirstOrDefaultAsync(u => u.username == username || u.email == username);

                    if (currentUser != null)
                    {
                        Session["UserId"] = currentUser.user_id;
                        Session["User"] = currentUser;
                    }
                }

                // 1.3 Nếu người dùng chưa đăng nhập thật sự
                if (currentUser == null)
                {
                    Session.Clear();
                    FormsAuthentication.SignOut(); // Xóa Cookie để không bị AuthenticationController đá về Trang chủ
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập trước khi đăng ký khóa học!";
                    return RedirectToAction("Login", "Authentication");
                }

                // Tìm hồ sơ học viên; chỉ tạo hồ sơ sau khi xác nhận lớp còn chỗ.
                var studentProfile = await db.Student_Profile
                    .FirstOrDefaultAsync(s => s.user_id == currentUser.user_id);

                var course = await db.Courses.FirstOrDefaultAsync(c =>
                    c.course_id == courseId && c.available_status == "Available");
                if (course == null)
                {
                    return HttpNotFound("Không tìm thấy khóa học hoặc khóa học đã đóng.");
                }

                if (!classId.HasValue || classId.Value == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn lớp học trước khi đăng ký.";
                    return RedirectToAction("Details", "Home", new { id = courseId });
                }

                using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    // Giữ khóa trên dòng lớp trong suốt giao dịch để hai lượt đăng ký
                    // đồng thời không thể cùng nhận chỗ cuối cùng.
                    Guid lockedClassId = await db.Database.SqlQuery<Guid>(
                        "SELECT class_id FROM dbo.[Class] WITH (UPDLOCK, HOLDLOCK) WHERE class_id = @p0",
                        classId.Value).SingleOrDefaultAsync();

                    if (lockedClassId == Guid.Empty)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy lớp đã chọn.";
                        return RedirectToAction("Details", "Home", new { id = courseId });
                    }

                    var targetClass = await db.Classes
                        .Include(c => c.Room)
                        .FirstOrDefaultAsync(c =>
                            c.class_id == lockedClassId &&
                            c.course_id == courseId);

                    if (targetClass == null)
                    {
                        TempData["ErrorMessage"] = "Lớp đã chọn không thuộc khóa học này.";
                        return RedirectToAction("Details", "Home", new { id = courseId });
                    }

                    if (targetClass.class_status != "Open" ||
                        targetClass.start_date.Date < DateTime.Today ||
                        targetClass.end_date.Date < DateTime.Today ||
                        !await db.ClassSchedules.AnyAsync(s => s.class_id == targetClass.class_id) ||
                        (targetClass.Room != null && targetClass.Room.available_status != "Available"))
                    {
                        TempData["ErrorMessage"] = "Lớp đã chọn hiện không mở đăng ký.";
                        return RedirectToAction("Details", "Home", new { id = courseId });
                    }

                    // Room có thể giới hạn thêm sĩ số; khi không có phòng, dùng giới hạn riêng của lớp.
                    int capacity = targetClass.Room == null
                        ? targetClass.class_capacity
                        : Math.Min(targetClass.class_capacity, targetClass.Room.max_capacity);

                    var existingEnrollment = studentProfile == null
                        ? null
                        : await db.Enrollments.FirstOrDefaultAsync(e =>
                            e.student_id == studentProfile.student_id &&
                            e.class_id == targetClass.class_id);

                    if (existingEnrollment != null)
                    {
                        if (existingEnrollment.enrollment_status == "Pending")
                        {
                            var existingPayment = await db.Payments.FirstOrDefaultAsync(p =>
                                p.enrollment_id == existingEnrollment.enrollment_id);
                            if (existingPayment != null)
                            {
                                return RedirectToAction("Checkout", "Payment", new { id = existingPayment.payment_id });
                            }
                        }

                        TempData["ErrorMessage"] = "Bạn đã có đăng ký cho lớp này.";
                        return RedirectToAction("Details", "Home", new { id = courseId });
                    }

                    // Đếm trong giao dịch đang khóa dòng lớp, để không thể vượt sức chứa do đăng ký đồng thời.
                    int activeEnrollmentCount = await db.Enrollments.CountAsync(e =>
                        e.class_id == targetClass.class_id &&
                        (e.enrollment_status == "Pending" ||
                         e.enrollment_status == "Confirmed" ||
                         e.enrollment_status == "Studying"));

                    if (activeEnrollmentCount >= capacity)
                    {
                        TempData["ErrorMessage"] = "Lớp vừa đủ sĩ số. Vui lòng chọn lớp khác.";
                        return RedirectToAction("Details", "Home", new { id = courseId });
                    }

                    if (studentProfile == null)
                    {
                        studentProfile = new Student_Profile
                        {
                            student_id = Guid.NewGuid(),
                            user_id = currentUser.user_id,
                            student_code = "STD-" + DateTime.Now.ToString("yyMMddHHmmss"),
                            student_status = "Active",
                            available_status = "Available",
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        db.Student_Profile.Add(studentProfile);
                    }

                    var enrollment = new Enrollment
                    {
                        enrollment_id = Guid.NewGuid(),
                        class_id = targetClass.class_id,
                        student_id = studentProfile.student_id,
                        enrollment_date = DateTime.Now,
                        tuition_amount = course.tuition_fee,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now,
                        enrollment_status = "Pending"
                    };
                    db.Enrollments.Add(enrollment);

                    string uniqueReceipt = "REC-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + new Random().Next(100, 999);

                    var payment = new Payment
                    {
                        payment_id = Guid.NewGuid(),
                        enrollment_id = enrollment.enrollment_id,
                        recorded_by = currentUser.user_id,
                        amount = course.tuition_fee,
                        receipt_number = uniqueReceipt,
                        payment_date = DateTime.Now,
                        created_at = DateTime.Now,
                        payment_status = "Pending",
                        payment_method = "BankTransfer"
                    };
                    db.Payments.Add(payment);

                    // Lưu enrollment và payment cùng nhau; nếu một bước lỗi, giao dịch sẽ rollback.
                    await db.SaveChangesAsync();
                    transaction.Commit();

                    return RedirectToAction("Checkout", "Payment", new { id = payment.payment_id });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Enrollment.DangKy failed: {0}", ex);
                TempData["ErrorMessage"] = "Không thể hoàn tất đăng ký. Vui lòng thử lại.";
                return RedirectToAction("Details", "Home", new { id = courseId });
            }
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
