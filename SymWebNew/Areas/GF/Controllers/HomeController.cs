using SymRepository.Common;
using SymViewModel.Common;
using System;
using System.Web.Mvc;

namespace SymWebUI.Areas.GF.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /GF/Home/
        public ActionResult Index()
        {
            GfInfoDashboardVM model = new GfInfoDashboardVM();
            try
            {
                model = new HomePageInfoDashboardRepo().GetGfInfoDashboard();
            }
            catch (Exception ex)
            {
                ViewBag.DashboardError = ex.Message;
            }
            return View(model);
        }
    }
}
