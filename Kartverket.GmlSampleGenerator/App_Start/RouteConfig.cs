using System.Web.Mvc;
using System.Web.Routing;

namespace Kartverket.GmlSampleGenerator
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default",
                "{controller}/{action}/{id}",
                new {controller = "Generate", action = "Index", id = UrlParameter.Optional}
                );
        }
    }
}