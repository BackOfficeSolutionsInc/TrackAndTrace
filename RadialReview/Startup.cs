using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using RadialReview.Utilities;

[assembly: OwinStartupAttribute(typeof(RadialReview.Startup))]
namespace RadialReview {
	public partial class Startup {
		public void Configuration(IAppBuilder app) {
			ConfigureAuth(app);
			var redis = Config.RedisSignalR("Radial-SignalR");
			var redisConfig = new RedisScaleoutConfiguration(redis.Server, redis.Port, redis.Password, redis.ChannelName) {
			    MaxQueueLength = 500
			};
			GlobalHost.DependencyResolver.UseRedis(redisConfig/*redis.Server, redis.Port, redis.Password, redis.ChannelName,*/);
			app.MapSignalR();

		}
	}
}
