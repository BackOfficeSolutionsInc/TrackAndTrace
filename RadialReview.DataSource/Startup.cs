using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(RadialReview.DataSource.Startup))]
namespace RadialReview.DataSource
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
