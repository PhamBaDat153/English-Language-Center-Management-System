using Source.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;

namespace Source.Controllers
{
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

                // =========================================================
                // 2. KẾT NỐI / TỰ ĐỘNG TẠO HỒ SƠ HỌC VIÊN (STUDENT_PROFILE)
                // =========================================================
                var studentProfile = await db.Student_Profile
                    .FirstOrDefaultAsync(s => s.user_id == currentUser.user_id);

                if (studentProfile == null)
                {
                    studentProfile = new Student_Profile
                    {
                        student_id = Guid.NewGuid(),
                        user_id = currentUser.user_id,
                        student_code = "STD-" + DateTime.Now.ToString("yyMMddHHmmss"),
                        student_status = "Active",       // Khớp CK_Students_Status ('Active')
                        available_status = "Available",  // Khớp CK_Students_Available ('Available')
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.Student_Profile.Add(studentProfile);
                    await db.SaveChangesAsync();
                }

                // =========================================================
                // 3. TÌM KHÓA HỌC & LỚP HỌC (CHUẨN CHECK CONSTRAINT 'Open')
                // =========================================================
                var course = await db.Courses.Include(c => c.Classes).FirstOrDefaultAsync(c => c.course_id == courseId);
                if (course == null)
                {
                    return HttpNotFound("Không tìm thấy thông tin khóa học!");
                }

                Guid targetClassId;
                if (classId.HasValue)
                {
                    targetClassId = classId.Value;
                }
                else
                {
                    var firstClass = course.Classes.FirstOrDefault();
                    if (firstClass == null)
                    {
                        var newClass = new Class
                        {
                            class_id = Guid.NewGuid(),
                            course_id = course.course_id,
                            class_code = course.course_code + "-C" + DateTime.Now.ToString("yyMMddHHmmss"),
                            start_date = DateTime.Now.AddDays(7),
                            end_date = DateTime.Now.AddMonths(3),
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now,
                            class_status = "Open" // Khớp CK_Classes_Status ('Open')
                        };
                        db.Classes.Add(newClass);
                        await db.SaveChangesAsync();
                        targetClassId = newClass.class_id;
                    }
                    else
                    {
                        targetClassId = firstClass.class_id;
                    }
                }

                // Nếu đã từng đăng ký trước đó -> Chuyển thẳng tới đơn thanh toán cũ
                var existingEnrollment = await db.Enrollments
                    .FirstOrDefaultAsync(e => e.student_id == studentProfile.student_id && e.class_id == targetClassId);

                if (existingEnrollment != null)
                {
                    var existingPayment = await db.Payments
                        .FirstOrDefaultAsync(p => p.enrollment_id == existingEnrollment.enrollment_id);

                    if (existingPayment != null)
                    {
                        return RedirectToAction("Checkout", "Payment", new { id = existingPayment.payment_id });
                    }
                }

                // =========================================================
                // 4. TẠO BẢN GHI ENROLLMENT VÀ PAYMENT
                // =========================================================
                var enrollment = new Enrollment
                {
                    enrollment_id = Guid.NewGuid(),
                    class_id = targetClassId,
                    student_id = studentProfile.student_id,
                    enrollment_date = DateTime.Now,
                    tuition_amount = course.tuition_fee,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    enrollment_status = "Pending" // Khớp CK_Enrollments_Status ('Pending')
                };
                db.Enrollments.Add(enrollment);

                // Tạo mã biên lai tự động không trùng lặp (Khắc phục lỗi UQ_Payments_Receipt)
                string uniqueReceipt = "REC-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + new Random().Next(100, 999);

                var payment = new Payment
                {
                    payment_id = Guid.NewGuid(),
                    enrollment_id = enrollment.enrollment_id,
                    recorded_by = currentUser.user_id,
                    amount = course.tuition_fee,
                    receipt_number = uniqueReceipt,   // Gán giá trị biên lai duy nhất
                    payment_date = DateTime.Now,
                    created_at = DateTime.Now,
                    payment_status = "Pending",       // Khớp CK_Payments_Status ('Pending')
                    payment_method = "BankTransfer"  // Khớp CK_Payments_Method ('BankTransfer')
                };
                db.Payments.Add(payment);

                // Lưu dữ liệu vào SQL Server
                await db.SaveChangesAsync();

                // =========================================================
                // 5. CHUYỂN HƯỚNG TỚI TRANG THANH TOÁN (PAYMENT/CHECKOUT)
                // =========================================================
                return RedirectToAction("Checkout", "Payment", new { id = payment.payment_id });
            }
            catch (Exception ex)
            {
                string sqlError = ex.GetBaseException().Message;
                return Content("<div style='color:red; padding: 30px; font-family: sans-serif;'>" +
                               "<h2>Lỗi SQL Server:</h2>" +
                               "<p><b>Chi tiết:</b> " + sqlError + "</p></div>");
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