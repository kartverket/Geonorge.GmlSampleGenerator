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
                var httpRequest = (HttpWebRequest)WebRequest.Create(urlToXsd);
                
                var response = (HttpWebResponse)httpRequest.GetResponse();
                Stream xsdStream = response.GetResponseStream();

                string xsdFilename = urlToXsd.Split('/').Last();

                SampleGmlGenerator splGmlGen = new SampleGmlGenerator(xsdStream, xsdFilename);

                MemoryStream gmlStream = splGmlGen.GenerateGml();
                return File(gmlStream.ToArray(), "text/xml", "GeneratedFromXsd");
            }

            return null;
        }

        [HttpPost]
        public FileContentResult GmlFromXsdFile(HttpPostedFileBase xsdfile)
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
