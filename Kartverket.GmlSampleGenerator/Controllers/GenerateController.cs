using Kartverket.Generators;
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
        public FileContentResult GmlFromXsd(HttpPostedFileBase xsdfile)
        {
            if (xsdfile != null && xsdfile.ContentLength > 0)
            {
                var xsdFilename = Path.GetFileName(xsdfile.FileName);
                Stream xsdStream = xsdfile.InputStream;
                SampleGmlGenerator splGmlGen = new SampleGmlGenerator(xsdStream, xsdFilename);

                MemoryStream gmlStream = splGmlGen.GenerateGml();
                return File(gmlStream.ToArray(), "text/xml", "GeneratedFromXsd");
            }

            return null;
        }
    }
}
