using Source.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Source.Controllers
{
    public class NotificationController : Controller
    {
        private readonly NewGen_english_center_dbEntities db = new NewGen_english_center_dbEntities();

        // Hàm hỗ trợ lấy ID người dùng chuẩn xác
        private Guid? GetCurrentUserId()
        {
            // 1. Lấy từ Session nếu có
            if (Session["UserId"] != null && Guid.TryParse(Session["UserId"].ToString(), out Guid sessionUserId))
            {
                return sessionUserId;
            }

            // 2. Lấy theo tên tài khoản đăng nhập (Identity.Name)
            if (User.Identity.IsAuthenticated && !string.IsNullOrEmpty(User.Identity.Name))
            {
                string identifier = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.username == identifier || u.email == identifier);
                if (user != null)
                {
                    Session["UserId"] = user.user_id;
                    return user.user_id;
                }
            }

            // 3. Dự phòng cho mục đích test: Lấy ID người dùng đầu tiên trong CSDL
            return db.Users.FirstOrDefault()?.user_id;
        }

        // 1. GET: /Notification
        // Màn hình danh sách thông báo cá nhân của người dùng
        public ActionResult Index(string filter = "all", int page = 1)
        {
            Guid? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            var query = db.Notifications
                .Where(n => n.user_id == userId)
                .AsQueryable();

            if (filter == "unread")
            {
                query = query.Where(n => n.read_status == "Unread");
            }

            int totalItems = query.Count();
            int pageSize = 10;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var list = query
                .OrderByDescending(n => n.created_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Filter = filter;
            ViewBag.UnreadCount = db.Notifications.Count(n => n.user_id == userId && n.read_status == "Unread");

            return View(list);
        }

        // 2. AJAX GET: /Notification/GetHeaderNotifications
        // Trả về số lượng chưa đọc để hiển thị Badge trên Header Menu
        [HttpGet]
        public JsonResult GetHeaderNotifications()
        {
            Guid? userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, unreadCount = 0 }, JsonRequestBehavior.AllowGet);
            }

            int unreadCount = db.Notifications.Count(n => n.user_id == userId && n.read_status == "Unread");

            return Json(new { success = true, unreadCount = unreadCount }, JsonRequestBehavior.AllowGet);
        }

        // 3. AJAX POST: /Notification/MarkAsRead
        // Đánh dấu 1 thông báo là "Đã đọc"
        [HttpPost]
        public JsonResult MarkAsRead(Guid id)
        {
            Guid? userId = GetCurrentUserId();
            var notification = db.Notifications.FirstOrDefault(n => n.notification_id == id && n.user_id == userId);

            if (notification != null && notification.read_status != "Read")
            {
                notification.read_status = "Read";
                notification.updated_at = DateTime.Now;
                db.SaveChanges();
            }

            int remainingUnread = db.Notifications.Count(n => n.user_id == userId && n.read_status == "Unread");
            return Json(new { success = true, unreadCount = remainingUnread });
        }

        // 4. AJAX POST: /Notification/MarkAllAsRead
        // Đánh dấu tất cả thông báo của người dùng là "Đã đọc"
        [HttpPost]
        public JsonResult MarkAllAsRead()
        {
            Guid? userId = GetCurrentUserId();
            if (userId != null)
            {
                var unreadList = db.Notifications.Where(n => n.user_id == userId && n.read_status == "Unread").ToList();
                DateTime now = DateTime.Now;
                foreach (var item in unreadList)
                {
                    item.read_status = "Read";
                    item.updated_at = now;
                }
                db.SaveChanges();
            }

            return Json(new { success = true, unreadCount = 0 });
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