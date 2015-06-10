using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Kartverket.GmlSampleGenerator.Startup))]
namespace Kartverket.GmlSampleGenerator
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
