using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Models.Scheduler;

namespace RadialReview.Utilities
{


	public class Scheduler
	{



		private RecurrenceModel Model { get; set; }


		protected Scheduler(DateTime start,DateTime end,long organizationId)
		{
			Model = new RecurrenceModel(){
				StartDate = start,
				EndDate = end,
			};
		}
		 
		public static Scheduler Create(long organizationId,DateTime startTime,DateTime? endTime = null)
		{
			return new Scheduler(startTime, endTime ?? DateTime.MaxValue, organizationId);
		}


		public static RecurrenceModel RepeatEveryDay(long organizationId,string name,string description, TimeSpan startTime, TimeSpan endTime, DateTime startDate, DateTime? endtDate = null)
		{
			throw new Exception("Incomplete");
			//return new RecurrenceModel(){
			//	OrganizationId = organizationId,
			//	StartDate = startDate,
			//	EndDate = endtDate ?? DateTime.MaxValue,
			//	StartTime = startTime,
			//	EndTime = endTime,
			//	Name = name,
			//	Description = description,
				
			//};


		}
		
	}
}