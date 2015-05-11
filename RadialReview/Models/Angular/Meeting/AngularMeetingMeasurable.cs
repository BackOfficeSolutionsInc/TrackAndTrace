using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.Angular.Scorecard
{
	public class AngularMeetingMeasurable : BaseAngular
	{
		public AngularMeetingMeasurable(L10Meeting.L10Meeting_Measurable measurable): base(measurable.Id)
		{
			Measurable = new AngularMeasurable(measurable.Measurable);

		}
		public AngularMeasurable Measurable { get; set; }

	}
}