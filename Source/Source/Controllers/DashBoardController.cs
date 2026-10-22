using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Source.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Mở bản giao diện trang chủ giáo viên; dữ liệu hiện là nội dung mẫu tĩnh.
        /// </summary>
        [HttpGet]
        public ActionResult TeacherHome()
        {
            try
            {
                return View("~/Views/DashBoard/TeacherHome.cshtml");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "DashboardController.TeacherHome failed: {0}",
                    ex);
                return View("~/Views/Shared/Error_500.cshtml");
            }
        }
    }
}
