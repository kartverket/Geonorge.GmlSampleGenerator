using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Kartverket.GmlSampleGenerator.Controllers
{
    public class GenerateController : Controller
    {
        // GET: Generate
        public ActionResult Index()
        {
            return RedirectToAction("GmlFromXsd");
        }

        [HttpGet]
        public ActionResult GmlFromXsd()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GmlFromXsd(HttpPostedFileBase xsdfile)
        {
            if (xsdfile != null && xsdfile.ContentLength > 0)
            {
                var xsdFilename = Path.GetFileName(xsdfile.FileName);
                // Process stream
            }

            return RedirectToAction("GmlFromXsd");
        }
    }
}
