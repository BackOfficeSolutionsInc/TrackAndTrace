using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularMeetingRock : BaseAngular
	{
		public AngularMeetingRock(L10Meeting.L10Meeting_Rock meetingRock):base(meetingRock.Id){
			Rock = new AngularRock(meetingRock);
		}
		public AngularRock Rock { get; set; }
	}
}