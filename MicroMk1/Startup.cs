using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MicroMk1.Startup))]
namespace MicroMk1
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
