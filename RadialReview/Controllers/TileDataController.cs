using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Todos;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class TileDataController : BaseController
    {
        // GET: TileData

		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none",Location=OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserTodo2()
		{
			return PartialView("UserTodo");
		}

		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserScorecard2()
		{
			return PartialView("UserScorecard");
		}
		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserRock2()
		{
			return PartialView("UserRock");
		}
		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserManage2()
		{
			return PartialView("UserManage");
		}

		[Access(AccessLevel.User)]
		public PartialViewResult UserProfile2()
		{
			return PartialView("UserProfile",GetUser().User);
		}
		
		[Access(AccessLevel.User)]
		public PartialViewResult UserButtons()
		{
			return PartialView("UserButtons");
		}
		[Access(AccessLevel.User)]
		public PartialViewResult SoftwareUpdates(int days=14)
		{
			var daysAgo = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(days));
			var daysStr = daysAgo.ToString("yyyyMMdd");
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"SoftwareUpdates\");
			
			var files = Directory.GetFiles(path).Where(x=>x.CompareTo(daysStr)>=0).ToList();

			var groups = new List<SoftwareUpdateGroup>();

			foreach (var f in files){
				var date = DateTime.MinValue;
				var html = "<i>Could not read update</i>";
				var title = "";
				try{
					var text = string.Join("\r\n", FileUtilities.WriteSafeReadAllLines(f));
					var file = f.Substring(f.LastIndexOf("\\")+1);
					html = CommonMark.CommonMarkConverter.Convert(text);
					date = new DateTime(file.Substring(0, 4).ToInt(), file.Substring(4, 2).ToInt(), file.Substring(6, 2).ToInt());
					title = file.Substring(8).Replace(".txt", "");
				}
				catch (Exception e){
					
				}
				groups.Add(new SoftwareUpdateGroup(){
					Date = date,
					Markup = new HtmlString(html),
					Title = title
				});
			}

			return PartialView("SoftwareUpdates", groups);
		}

	    public class SoftwareUpdateGroup
	    {
			public DateTime Date { get; set; }
			public HtmlString Markup { get; set; }
			public string Title { get; set; }
		}
    }
}