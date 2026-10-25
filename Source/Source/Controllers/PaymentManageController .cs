using Source.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class PaymentManageController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        // GET: PaymentManage
        public ActionResult Index(string search, string statusFilter, int page = 1)
        {
            var query = db.Payments.AsQueryable();

            // Lọc theo từ khóa (Tên học viên, Email, Mã hóa đơn, Tên khóa học)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.Enrollment != null &&
                     p.Enrollment.Student_Profile != null &&
                     p.Enrollment.Student_Profile.User != null &&
                     p.Enrollment.Student_Profile.User.full_name != null &&
                     p.Enrollment.Student_Profile.User.full_name.ToLower().Contains(searchLower)) ||
                    (p.Enrollment != null &&
                     p.Enrollment.Student_Profile != null &&
                     p.Enrollment.Student_Profile.User != null &&
                     p.Enrollment.Student_Profile.User.email != null &&
                     p.Enrollment.Student_Profile.User.email.ToLower().Contains(searchLower)) ||
                    (p.Enrollment != null &&
                     p.Enrollment.Class != null &&
                     p.Enrollment.Class.Course != null &&
                     p.Enrollment.Class.Course.course_name != null &&
                     p.Enrollment.Class.Course.course_name.ToLower().Contains(searchLower)) ||
                    p.payment_id.ToString().ToLower().Contains(searchLower));
            }

            // Lọc theo trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(p => p.payment_status == statusFilter);
            }

            // Thống kê toàn bộ hóa đơn
            ViewBag.TotalPayments = db.Payments.Count();
            ViewBag.PendingPayments = db.Payments.Count(p => p.payment_status == "Pending");
            ViewBag.CompletedPayments = db.Payments.Count(p => p.payment_status == "Completed");
            ViewBag.CancelledPayments = db.Payments.Count(p => p.payment_status == "Cancelled");

            // Giữ lại điều kiện tìm kiếm trên giao diện
            ViewBag.Search = search;
            ViewBag.StatusFilter = statusFilter;

            // Phân trang
            int pageSize = 10;
            int totalPayments = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalPayments / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.FilteredCount = totalPayments;
            ViewBag.PageSize = pageSize;

            var payments = query
                .OrderByDescending(p => p.created_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(payments);
        }

        // GET: PaymentManage/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var payment = db.Payments.FirstOrDefault(p => p.payment_id == id.Value);

            if (payment == null)
            {
                return HttpNotFound("Không tìm thấy thông tin hóa đơn thanh toán.");
            }

            return View(payment);
        }

        // POST: PaymentManage/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(Guid paymentId)
        {
            try
            {
                var payment = db.Payments.FirstOrDefault(p => p.payment_id == paymentId);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hóa đơn cần duyệt.";
                    return RedirectToAction("Index");
                }

                // Cập nhật trạng thái Payment -> Completed
                payment.payment_status = "Completed";

                // Cập nhật trạng thái Enrollment -> Confirmed
                if (payment.Enrollment != null)
                {
                    payment.Enrollment.enrollment_status = "Confirmed";
                    payment.Enrollment.confirmed_at = DateTime.Now;
                    payment.Enrollment.updated_at = DateTime.Now;
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xác nhận thanh toán thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi duyệt thanh toán: " + ex.GetBaseException().Message;
            }

            return RedirectToAction("Index");
        }

        // POST: PaymentManage/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid paymentId)
        {
            var payment = db.Payments.FirstOrDefault(p => p.payment_id == paymentId);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn cần xóa.";
                return RedirectToAction("Index");
            }

            try
            {
                var enrollment = payment.Enrollment;

                // Xóa bản ghi Payment
                db.Payments.Remove(payment);

                // Xóa bản ghi Enrollment tương ứng (nếu có)
                if (enrollment != null)
                {
                    db.Enrollments.Remove(enrollment);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa hoàn toàn hóa đơn khỏi hệ thống.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa hóa đơn: " + ex.GetBaseException().Message;
            }

            return RedirectToAction("Index");
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
