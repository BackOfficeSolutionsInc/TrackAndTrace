using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.VideoConference
{
	public class VideoConferenceVM
	{
		public String RoomId { get; set; }

		public VideoConferenceVM()
		{
			RoomId = Guid.NewGuid().ToString();
		}
		
	}
}