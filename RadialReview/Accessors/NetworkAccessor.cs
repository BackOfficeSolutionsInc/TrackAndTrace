using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace RadialReview.Accessors
{
	public class NetworkAccessor
	{

		public static string GetPublicIP()
		{
			var direction = "";
			var request = WebRequest.Create("http://checkip.dyndns.org/");
			using (var response = request.GetResponse())
			using (var stream = new StreamReader(response.GetResponseStream()))
			{
				direction = stream.ReadToEnd();
			}

			//Search for the ip in the html
			var first = direction.IndexOf("Address: ") + 9;
			var last = direction.LastIndexOf("</body>");
			direction = direction.Substring(first, last - first);

			return direction;
		}
	}
}