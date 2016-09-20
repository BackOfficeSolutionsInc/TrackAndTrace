using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.L10;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Headlines {
	public class AngularHeadline : BaseAngular {
		public AngularHeadline(PeopleHeadline headline) : base(headline.Id)
		{
			Name = headline.Message;
			DetailsUrl = Config.NotesUrl() + "p/" + headline.HeadlinePadId + "?showControls=true&showChat=false";

			//Details = todo.Details;
			Owner = AngularUser.CreateUser(headline.Owner);
			if (headline.About != null)
				About = new AngularPicture(headline.About);
			else
				About = new AngularPicture(-headline.Id) {
					ImageUrl = ConstantStrings.ImageUserPlaceholder,
					Name = headline.AboutName
				};
			CloseTime = headline.CloseTime;
			CreateTime = headline.CreateTime;
			
			Link = "/L10/Timeline/" + headline.RecurrenceId + "#transcript-" + headline.Id;

		}

		public AngularHeadline() {

		}

		public string Name { get; set; }
		//public string Details { get; set; }
		public string DetailsUrl { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? CloseTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public AngularPicture About { get; set; }
		public string Link { get; set; }

	
	}
}