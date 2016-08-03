using Kartverket.Generators;
using System.IO;
using System.Linq;
using System.Net;
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
        public FileContentResult GmlFromXsdUrl(string urlToXsd)
        {
            if (!string.IsNullOrEmpty(urlToXsd)) // TODO: Validate url
            {
                Stream xsdStream = WebRequest.Create(urlToXsd).GetResponse().GetResponseStream();
                string xsdFilename = urlToXsd.Split('/').Last();

                return GmlFileFromXsdStream(xsdStream, xsdFilename);
            }

            return null;
        }

        [HttpPost]
        public FileContentResult GmlFromXsdFile(HttpPostedFileBase xsdfile)
        {
            if (xsdfile != null && xsdfile.ContentLength > 0)
            {
                Stream xsdStream = xsdfile.InputStream;
                string xsdFileName = Path.GetFileName(xsdfile.FileName);

                return GmlFileFromXsdStream(xsdStream, xsdFileName);
            }

            return null;
        }

        private FileContentResult GmlFileFromXsdStream(Stream xsdStream, string xsdFilename)
        {
            MemoryStream gmlStream = new SampleGmlGenerator(xsdStream, xsdFilename).GenerateGml();
            string gmlFileName = "generatedFromXsd_" + Path.GetFileNameWithoutExtension(xsdFilename) + ".gml";

            return File(gmlStream.ToArray(), "text/xml", gmlFileName);
        }
    }
}
