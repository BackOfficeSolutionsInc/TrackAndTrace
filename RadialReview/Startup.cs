using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(RadialReview.Startup))]
namespace RadialReview
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
