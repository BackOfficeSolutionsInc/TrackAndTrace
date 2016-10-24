using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.VideoConference
{
	

	public class VideoConferenceVM
	{
		public String RoomId { get; set; }
		public VideoConferenceType VideoProvider { get; set; }
		public List<AbstractVCProvider> CurrentProviders { get; set; }
		public AbstractVCProvider Selected { get; set; }

		public VideoConferenceVM()		{
			RoomId = Guid.NewGuid().ToString();
			CurrentProviders = new List<AbstractVCProvider>();
		}
		
	}
}