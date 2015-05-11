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

		public static bool OptimizationEnabled()
		{
			switch (GetEnv())
			{
				case Env.local_sqlite:	return false;
				case Env.local_mysql:	return false;
				case Env.production:	return true;
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetTrelloKey()
		{
			return GetAppSetting("TrelloKey");
		}

		public static class Basecamp
		{
			/*public static string GetUrl()
			{
				return GetAppSetting("BasecampUrl");
			}*/

			public static string GetUserAgent()
			{
				switch (GetEnv()){
					case Env.local_mysql:	goto case Env.local_sqlite;
					case Env.local_sqlite:	return GetAppSetting("BasecampTestApp");
					case Env.production:	return GetAppSetting("BasecampApp");
					default:				throw new ArgumentOutOfRangeException();
				}
			}

			public static BCXAPI.Service GetService()
			{
				string key, secret, app;
				var redirect = BaseUrl() + "Callback/Basecamp";
				switch(GetEnv()){
					case Env.local_mysql:
						goto case Env.local_sqlite;
					case Env.local_sqlite:{
						key = GetAppSetting("BasecampTestKey");
						secret = GetAppSetting("BasecampTestSecret");
						break;
					}
					case Env.production:{
						key = GetAppSetting("BasecampKey");
						secret = GetAppSetting("BasecampSecret");
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
				return new BCXAPI.Service(key, secret, redirect, GetUserAgent());
			}
		}
	}
}