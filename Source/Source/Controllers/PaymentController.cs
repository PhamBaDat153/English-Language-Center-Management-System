using Source.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        // GET: /Payment/Checkout/{id}
        // Trang hiển thị thông tin chuyển khoản / QR cho học viên
        public ActionResult Checkout(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var payment = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .FirstOrDefault(p => p.payment_id == id.Value);

            if (payment == null)
            {
                return HttpNotFound("Không tìm thấy thông tin hóa đơn thanh toán!");
            }

            Guid userId;
            var claim = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(claim, out userId) || payment.Enrollment?.Student_Profile?.user_id != userId)
            {
                return new HttpUnauthorizedResult();
            }

            return View(payment);
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
