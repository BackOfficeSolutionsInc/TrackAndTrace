using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Utilities
{
	public class Config
	{

		public static string BaseUrl()
		{
			switch(GetEnv()){
				case Env.local_sqlite:return "http://localhost:2200/";
				case Env.local_mysql:return "http://localhost:2200/";
				case Env.production:return "http://review.radialreview.com/";
				default:throw new ArgumentOutOfRangeException();
			}
		}

		public static bool IsLocal()
		{
			switch(GetEnv()){
				case Env.local_sqlite:
					return true;
				case Env.local_mysql:
					return true;
				case Env.production:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetAppSetting(string key)
		{
			var config = System.Configuration.ConfigurationManager.AppSettings;
			return config[key];
		}

		public static Env GetEnv()
		{
			Env result;
			if (Enum.TryParse(GetAppSetting("Env").ToLower(), out result)){
				return result;
			}
			throw new Exception("Invalid Environment");
		}

		public static string GetSecret()
		{
			return GetAppSetting("sha_secret");
		}
	}
}