using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Scorecard
{
	public class AngularScore : BaseAngular
	{
		public AngularScore(ScoreModel score) : base(score.Id)
		{
			Week = DateTime.SpecifyKind(score.ForWeek,DateTimeKind.Utc);
			ForWeek = TimingUtility.GetWeekSinceEpoch(Week);
			if (Id == 0)
				Id = score.MeasurableId - ForWeek;


			Measurable = new AngularMeasurable(score.Measurable);
			Measured = score.Measured;
			DateEntered = score.DateEntered;
		}

		public AngularScore(){
		}
		public long ForWeek { get; set; }
		public DateTime Week { get; set; }
		public AngularMeasurable Measurable { get; set; } 
		public DateTime? DateEntered { get; set; }
		public decimal? Measured { get; set; }
		public bool? Disabled { get; set; }
	
	}
}