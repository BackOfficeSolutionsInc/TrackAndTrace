﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.Angular.Scorecard
{
	
	
	
	public class AngularMeasurable : BaseAngular
	{
		public AngularMeasurable(MeasurableModel measurable,bool skipUser=false):base(measurable.Id)
		{

			Owner = AngularUser.CreateUser(skipUser?null : measurable.AccountableUser);
			Admin = AngularUser.CreateUser(skipUser ? null : measurable.AdminUser);
			Name = measurable.Title;
			Target = measurable.Goal;
			Direction = measurable.GoalDirection;
			Modifiers = measurable.UnitType;
			if (measurable.Id < 0)
				Disabled = true;
			Ordering = measurable._Ordering;
			IsDivider = false;
		}

		public static AngularMeasurable CreateDivider(int ordering,long id)
		{
			return new AngularMeasurable(){
				Ordering = ordering,
				IsDivider = true,
				Id = -id
			};
		}

		public AngularMeasurable()
		{
			
		}

		public bool IsDivider { get; set; }

		public AngularUser Owner { get; set; }
		public AngularUser Admin { get; set; }
		public string Name { get; set; }
		public decimal? Target { get; set; }
		public LessGreater? Direction { get; set; }
		
		public UnitType? Modifiers { get; set; }

		public int? Ordering { get; set; }
		public bool? Disabled { get; set; }
		public long RecurrenceId { get; set; }
	}
}