using Source.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class NotificationManageController : Controller
    {
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();

        // 1. GET: /NotificationManage
        // Hiển thị danh sách thông báo đã phát ra hệ thống
        public ActionResult Index(string search, int page = 1)
        {
            try
            {
                var query = db.Notifications
                    .Include(n => n.User)
                    .AsQueryable();

                // Lọc theo từ khóa tìm kiếm (Tiêu đề, Nội dung, Tên người nhận, Email)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string keyword = search.Trim();
                    query = query.Where(n =>
                        n.title.Contains(keyword) ||
                        n.message.Contains(keyword) ||
                        (n.User != null &&
                         (n.User.full_name.Contains(keyword) ||
                          n.User.email.Contains(keyword)))
                    );
                }

                int filteredCount = query.Count();
                int pageSize = 10;
                int totalPages = (int)Math.Ceiling(
                    (double)filteredCount / pageSize);

                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages)
                    page = totalPages;

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;
                ViewBag.FilteredCount = filteredCount;

                var list = query
                    .OrderByDescending(n => n.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View(list);
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // 2. GET: /NotificationManage/Create
        // Hiển thị màn hình Soạn / Tạo thông báo
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

        // 3. POST: /NotificationManage/Create
        // Xử lý gửi và lưu thông báo vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string sendTarget,
            List<Guid> selectedUserIds,
            string title,
            string message)
        {
            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng nhập đầy đủ Tiêu đề và Nội dung thông báo!";
                return View();
            }

            try
            {
                List<Guid> targetUserIds = new List<Guid>();

                if (sendTarget == "All")
                {
                    targetUserIds = db.Users
                        .Select(u => u.user_id)
                        .ToList();
                }
                else if (sendTarget == "Specific")
                {
                    if (selectedUserIds == null || !selectedUserIds.Any())
                    {
                        TempData["ErrorMessage"] =
                            "Vui lòng chọn ít nhất 1 người dùng từ danh sách để gửi!";
                        return View();
                    }

                    targetUserIds = selectedUserIds;
                }

                if (!targetUserIds.Any())
                {
                    TempData["ErrorMessage"] =
                        "Không tìm thấy người dùng phù hợp trong hệ thống để gửi thông báo!";
                    return View();
                }

                DateTime now = DateTime.Now;
                List<Notification> notificationsToAdd =
                    new List<Notification>();

                foreach (var userId in targetUserIds)
                {
                    notificationsToAdd.Add(new Notification
                    {
                        notification_id = Guid.NewGuid(),
                        user_id = userId,
                        title = title.Trim(),
                        message = message.Trim(),
                        notification_type = "General",
                        created_at = now,
                        updated_at = now,
                        read_status = "Unread"
                    });
                }

                db.Notifications.AddRange(notificationsToAdd);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    $"Đã gửi thông báo thành công cho {notificationsToAdd.Count} người dùng!";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // 4. GET: /NotificationManage/Details/{id}
        // Xem chi tiết thông báo
        public ActionResult Details(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }

                var notification = db.Notifications
                    .Include(n => n.User)
                    .FirstOrDefault(n => n.notification_id == id);

                if (notification == null)
                {
                    TempData["ErrorMessage"] =
                        "Không tìm thấy thông báo.";
                    return RedirectToAction("Index");
                }

                return View(notification);
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }
        }

        // 5. POST: /NotificationManage/Resend
        // Xử lý gửi lại thông báo từ trang Details và chuyển về Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Resend(
            Guid notification_id,
            string title,
            string message)
        {
            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng nhập đầy đủ Tiêu đề và Nội dung thông báo!";

                return RedirectToAction(
                    "Details",
                    new { id = notification_id });
            }

            try
            {
                var notification = db.Notifications.FirstOrDefault(
                    n => n.notification_id == notification_id);

                if (notification != null)
                {
                    DateTime now = DateTime.Now;

                    notification.title = title.Trim();
                    notification.message = message.Trim();
                    notification.created_at = now;
                    notification.updated_at = now;
                    notification.read_status = "Unread";

                    db.SaveChanges();

                    TempData["SuccessMessage"] =
                        "Đã gửi lại thông báo thành công!";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Không tìm thấy thông báo cần gửi lại.";
                }
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }

            return RedirectToAction("Index");
        }

        // 6. POST: /NotificationManage/Delete/{id}
        // Xóa thông báo khỏi CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id)
        {
            try
            {
                var notification = db.Notifications.FirstOrDefault(
                    n => n.notification_id == id);

                if (notification != null)
                {
                    db.Notifications.Remove(notification);
                    db.SaveChanges();

                    TempData["SuccessMessage"] =
                        "Đã xóa vĩnh viễn thông báo thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Không tìm thấy thông báo cần xóa.";
                }
            }
            catch (Exception)
            {
                return RedirectToAction("ServerError", "Error");
            }

            return RedirectToAction("Index");
        }

        // 7. AJAX API: Tìm kiếm người dùng khi gửi chọn lọc
        [HttpGet]
        public JsonResult SearchUsers(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    var defaultUsers = db.Users
                        .OrderBy(u => u.full_name)
                        .Take(20)
                        .Select(u => new
                        {
                            id = u.user_id,
                            name = u.full_name,
                            email = u.email
                        })
                        .ToList();

                    return Json(
                        defaultUsers,
                        JsonRequestBehavior.AllowGet);
                }

                string keyword = term.Trim().ToLower();

                var users = db.Users
                    .Where(u =>
                        (u.full_name != null &&
                         u.full_name.ToLower().Contains(keyword)) ||
                        (u.email != null &&
                         u.email.ToLower().Contains(keyword)))
                    .OrderBy(u => u.full_name)
                    .Take(30)
                    .Select(u => new
                    {
                        id = u.user_id,
                        name = u.full_name,
                        email = u.email
                    })
                    .ToList();

                return Json(
                    users,
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(
                    new { error = true },
                    JsonRequestBehavior.AllowGet);
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