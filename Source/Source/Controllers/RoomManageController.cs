using Source.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{

    public class RoomManageController : Controller
    {
        // Khởi tạo DbContext theo cấu trúc UserManageController.
        private readonly NewGen_english_center_dbEntities db =
            new NewGen_english_center_dbEntities();
        // GET: RoomManage
        // Hiển thị danh sách phòng, tìm kiếm, lọc trạng thái và phân trang
        public ActionResult Index(
            string roomCode,
            string roomName,
            string status,
            int page = 1)
        {
            try
            {
                int pageSize = 15;

                // Không cho phép số trang nhỏ hơn 1
                page = Math.Max(1, page);

                // Lấy dữ liệu phòng học
                var allRooms = db.Set<Room>().AsQueryable();

                // Thống kê toàn bộ phòng, không phụ thuộc bộ lọc tìm kiếm
                ViewBag.TotalAvailableRooms = allRooms.Count(
                    r => r.available_status == "Available");

                ViewBag.TotalUnavailableRooms = allRooms.Count(
                    r => r.available_status == "Unavailable");

                // Tạo truy vấn danh sách theo bộ lọc
                var query = allRooms;

                // Tìm kiếm theo mã phòng
                if (!string.IsNullOrWhiteSpace(roomCode))
                {
                    roomCode = roomCode.Trim();

                    query = query.Where(r =>
                        r.room_code.Contains(roomCode));
                }

                // Tìm kiếm theo tên phòng
                if (!string.IsNullOrWhiteSpace(roomName))
                {
                    roomName = roomName.Trim();

                    query = query.Where(r =>
                        r.room_name.Contains(roomName));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(r =>
                        r.available_status == status);
                }

                // Tổng số phòng sau khi áp dụng bộ lọc
                int totalItems = query.Count();

                // Tổng số trang
                int totalPages = (int)Math.Ceiling(
                    (double)totalItems / pageSize);

                // Giới hạn page để tránh vượt quá tổng số trang
                if (totalPages > 0)
                {
                    page = Math.Min(page, totalPages);
                }
                else
                {
                    page = 1;
                }

                // Lấy dữ liệu của trang hiện tại
                var rooms = query
                    .OrderBy(r => r.room_code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Dữ liệu phân trang cho View
                ViewBag.TotalItems = totalItems;
                ViewBag.FilteredRoomCount = totalItems;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalAvailableRooms = allRooms.Count(r => r.available_status == "Available");
                ViewBag.TotalUnavailableRooms = allRooms.Count(r => r.available_status == "Unavailable");

                // Giữ lại bộ lọc trên giao diện
                ViewBag.RoomCode = roomCode;
                ViewBag.RoomName = roomName;
                ViewBag.Status = status;

                return View(rooms);
            }
            catch (Exception ex)
            {
                Trace.TraceError("RoomManage Index error: " + ex);

                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        // GET: RoomManage/Create
        // Hiển thị form thêm phòng
        public ActionResult Create()
        {
            try
            {
                var room = new Room
                {
                    room_id = Guid.NewGuid(),
                    available_status = "Available",
                    max_capacity = 20
                };

                return View(room);
            }
            catch (Exception ex)
            {
                Trace.TraceError("RoomManage Create GET error: " + ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        // POST: RoomManage/Create
        // Nhận dữ liệu form và lưu phòng mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Room room)
        {
            try
            {
                // Kiểm tra mã phòng đã tồn tại chưa
                bool codeExists = db.Set<Room>().Any(r =>
                    r.room_code == room.room_code);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        "room_code", "Mã phòng đã tồn tại.");
                }

                // Kiểm tra sức chứa
                if (room.max_capacity <= 0)
                {
                    ModelState.AddModelError(
                        "max_capacity",
                        "Sức chứa phải lớn hơn 0.");
                }

                // Kiểm tra trạng thái hợp lệ
                if (room.available_status != "Available" &&
                    room.available_status != "Unavailable")
                {
                    ModelState.AddModelError(
                        "available_status",
                        "Trạng thái phòng không hợp lệ.");
                }

                if (!ModelState.IsValid)
                {
                    return View(room);
                }

                // Tạo ID và thời gian hệ thống
                room.room_id = Guid.NewGuid();
                room.created_at = DateTime.Now;
                room.updated_at = DateTime.Now;

                db.Set<Room>().Add(room);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm phòng thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError("RoomManage Create POST error: " + ex);

                ModelState.AddModelError(
                    "",
                    "Không thể thêm phòng. Vui lòng kiểm tra dữ liệu.");

                return View(room);
            }
        }

        // GET: RoomManage/Details/{id}
        // Hiển thị thông tin phòng để chỉnh sửa
        public ActionResult Details(Guid? id)
        {
            try
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var room = db.Set<Room>()
                    .FirstOrDefault(r => r.room_id == id.Value);

                if (room == null)
                {
                    return HttpNotFound();
                }

                return View(room);
            }
            catch (Exception ex)
            {
                Trace.TraceError("RoomManage Details GET error: " + ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }

        // POST: RoomManage/Details
        // Cập nhật thông tin phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(Room room)
        {
            try
            {
                var existingRoom = db.Set<Room>()
                    .FirstOrDefault(r => r.room_id == room.room_id);

                if (existingRoom == null)
                {
                    return HttpNotFound();
                }

                // Không cho phép trùng mã phòng với phòng khác
                bool codeExists = db.Set<Room>().Any(r =>
                    r.room_code == room.room_code &&
                    r.room_id != room.room_id);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        "room_code", "Mã phòng đã tồn tại.");
                }

                if (room.max_capacity <= 0)
                {
                    ModelState.AddModelError(
                        "max_capacity",
                        "Sức chứa phải lớn hơn 0.");
                }

                // Không để thay đổi sức chứa phòng làm lớp đang dùng phòng vượt giới hạn.
                if (db.Classes.Any(c =>
                    c.room_id == existingRoom.room_id &&
                    c.class_capacity > room.max_capacity))
                {
                    ModelState.AddModelError(
                        "max_capacity",
                        "Sức chứa phòng không thể nhỏ hơn sĩ số tối đa của lớp đang sử dụng phòng này.");
                }

                if (room.available_status != "Available" &&
                    room.available_status != "Unavailable")
                {
                    ModelState.AddModelError(
                        "available_status",
                        "Trạng thái phòng không hợp lệ.");
                }

                if (!ModelState.IsValid)
                {
                    // Giữ nguyên thời gian tạo để hiển thị lại
                    room.created_at = existingRoom.created_at;
                    room.updated_at = existingRoom.updated_at;
                    return View(room);
                }

                // Cập nhật các trường được phép thay đổi
                existingRoom.room_code = room.room_code;
                existingRoom.room_name = room.room_name;
                existingRoom.max_capacity = room.max_capacity;
                existingRoom.location = room.location;
                existingRoom.available_status = room.available_status;
                existingRoom.updated_at = DateTime.Now;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật phòng thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError("RoomManage Details POST error: " + ex);

                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật phòng. Vui lòng thử lại.");

                return View(room);
            }
        }

        // POST: RoomManage/Delete/{id}
        // Xóa phòng trực tiếp từ danh sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id)
        {
            try
            {
                var room = db.Set<Room>()
                    .FirstOrDefault(r => r.room_id == id);

                if (room == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phòng cần xóa.";
                    return RedirectToAction("Index");
                }

                db.Set<Room>().Remove(room);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Xóa phòng thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Có thể phòng đang được lớp học sử dụng
                Trace.TraceError("RoomManage Delete POST error: " + ex);

                TempData["ErrorMessage"] =
                    "Không thể xóa phòng. Có thể phòng đang được lớp học sử dụng.";

                return RedirectToAction("Index");
            }
        }

        // Giải phóng tài nguyên database khi Controller kết thúc
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
