using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview.Models.L10;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace RadialReview.Models.Angular.Scorecard
{
	
	
	
	public class AngularMeasurable : BaseAngular
	{
        //public AngularMeasurable(L10Recurrence.L10Recurrence_Measurable measurable, bool skipUser = false) : this(measurable.Measurable,skipUser)
        //{
        //    RecurrenceMeasurableId = measurable.Id;
        //}
        public AngularMeasurable(long id) : base(id) {
        }

		public static AngularMeasurable Create(L10Recurrence.L10Recurrence_Measurable meetingMeasurable) {
			if (meetingMeasurable.IsDivider) {
				return CreateDivider(meetingMeasurable._Ordering, meetingMeasurable.Id);
			} else {
				return new AngularMeasurable(meetingMeasurable.Measurable) {
					Ordering = meetingMeasurable._Ordering
				};
			}
		}


		public AngularMeasurable(MeasurableModel measurable,bool skipUser=false):base(measurable.Id){

			Owner = AngularUser.CreateUser(skipUser?null : measurable.AccountableUser);
			Admin = AngularUser.CreateUser(skipUser ? null : measurable.AdminUser);
			Name = measurable.Title;
            Target = measurable.Goal;
            AltTarget = measurable.AlternateGoal;
            Direction = measurable.GoalDirection;
			Modifiers = measurable.UnitType;
			if (measurable.Id < 0)
				Disabled = true;
			Ordering = measurable._Ordering;
			IsDivider = false;
			ShowCumulative = measurable.ShowCumulative;
			CumulativeRange = measurable.CumulativeRange;
			Cumulative = measurable._Cumulative;
			if (measurable._Editable == false)
				Disabled = true;
		}

		public static AngularMeasurable CreateDivider(int ordering,long id)
		{
			return new AngularMeasurable(){
				Ordering = ordering,
				IsDivider = true,
				Id = -id
			};
		}

		public AngularMeasurable(){
			
		}
		[JsonProperty(Order = -10)]
		public string Name { get; set; }

		public AngularUser Owner { get; set; }
		public AngularUser Admin { get; set; }
        public decimal? Target { get; set; }
        public decimal? AltTarget { get; set; }
        public LessGreater? Direction { get; set; }

		public bool IsDivider { get; set; }

		public bool? ShowCumulative { get; set; }
		public decimal? Cumulative { get; set; }
		public DateTime? CumulativeRange { get; set; }

		
		public UnitType? Modifiers { get; set; }

		public int? Ordering { get; set; }
        public bool? Disabled { get; set; }
		
		[IgnoreDataMember]
		public long? RecurrenceId { get; set; }
		[IgnoreDataMember]
		public long? RecurrenceMeasurableId { get; set; }
		[IgnoreDataMember]
		public AngularMeasurableGroup Grouping { get; set; }
	}
}