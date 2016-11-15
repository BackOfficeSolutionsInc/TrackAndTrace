using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.VideoConference {
	public class ZoomUserLink : AbstractVCProvider {

		public virtual string ZoomMeetingId{ get; set; }

		public override VideoConferenceType GetVideoConferenceType() {
			return VideoConferenceType.Zoom;
		}

		public override string GetUrl() {
			return "https://www.zoom.us/j/" + ZoomMeetingId;
		}

		public ZoomUserLink() {
		}

		public class Map : SubclassMap<ZoomUserLink> {
			public Map() {
				Map(x => x.ZoomMeetingId);
			}
		}
	}
}