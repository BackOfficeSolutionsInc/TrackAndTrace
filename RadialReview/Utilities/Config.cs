using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.Enums;

namespace RadialReview.Utilities
{
	public class Config
	{

		public static string BaseUrl(OrganizationModel organization)
		{

			try{
				var strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
				return HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
			}catch (Exception){
				switch (GetEnv())
				{
					case Env.local_sqlite:
						return "http://localhost:2200/";
					case Env.local_mysql:
						return "http://localhost:2200/";
					case Env.production:
						if (organization==null)
							return "https://review.radialreview.com/";

						switch (organization.Settings.Branding)
						{
							case BrandingType.RadialReview:
								return "https://review.radialreview.com/";
							case BrandingType.RoundTable:
								return "https://radialroundtable.com/";
							default:
								throw new ArgumentOutOfRangeException();
						}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public static string ProductName(OrganizationModel organization = null)
		{
			if (organization != null)
			{
				switch (organization.Settings.Branding)
				{
					case BrandingType.RadialReview:
						return GetAppSetting("ProductName_Review", "Radial Review");
					case BrandingType.RoundTable:
						return GetAppSetting("ProductName_Roundtable", "Radial Review");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try
			{
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
					return GetAppSetting("ProductName_Review", "Radial Review");
				else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable"))
					return GetAppSetting("ProductName_Roundtable", "Radial Review");
			}
			catch (Exception)
			{
				//Fall back...
			}
			return GetAppSetting("ProductName_Review", "Radial Review");
		}
		public static string ReviewName(OrganizationModel organization = null)
		{
			if (organization != null)
			{
				switch (organization.Settings.Branding)
				{
					case BrandingType.RadialReview:
						return GetAppSetting("ReviewName_Review", "Review");
					case BrandingType.RoundTable:
						return GetAppSetting("ReviewName_Roundtable", "Round Table");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			try
			{
				if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
					return GetAppSetting("ReviewName_Review", "Review");
				else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable")) 
					return GetAppSetting("ReviewName_Roundtable", "Round Table");
			}
			catch (Exception)
			{
				//Fall back...
			}
			return GetAppSetting("ReviewName_Review", "Review");
		}

		public static bool IsLocal()
		{
			switch (GetEnv())
			{
				case Env.local_sqlite:	return true;
				case Env.local_mysql:	return true;
				case Env.production:	return false;
				default:throw new ArgumentOutOfRangeException();
			}
		}

		public static string GetAppSetting(string key,string deflt=null)
		{
			var config = System.Configuration.ConfigurationManager.AppSettings;
			return config[key] ?? deflt;
		}

		public static Env GetEnv()
		{
			Env result;
			if (Enum.TryParse(GetAppSetting("Env").ToLower(), out result))
			{
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
				case Env.local_sqlite: return false;
				case Env.local_mysql: return false;
				case Env.production: return true;
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
				switch (GetEnv())
				{
					case Env.local_mysql: goto case Env.local_sqlite;
					case Env.local_sqlite: return GetAppSetting("BasecampTestApp");
					case Env.production: return GetAppSetting("BasecampApp");
					default: throw new ArgumentOutOfRangeException();
				}
			}

			public static BCXAPI.Service GetService(OrganizationModel organization)
			{
				string key, secret, app;
				var redirect = BaseUrl(organization) + "Callback/Basecamp";
				switch (GetEnv())
				{
					case Env.local_mysql:
						goto case Env.local_sqlite;
					case Env.local_sqlite:
						{
							key = GetAppSetting("BasecampTestKey");
							secret = GetAppSetting("BasecampTestSecret");
							break;
						}
					case Env.production:
						{
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