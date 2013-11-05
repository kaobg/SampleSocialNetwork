using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SampleSocialNetwork.Web.Startup))]
namespace SampleSocialNetwork.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
