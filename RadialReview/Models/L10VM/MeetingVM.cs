using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Models.L10VM
{
	/*public class MeetingVM
	{
		public string Name { get; set; }
		public List<AgendaItem> AgendaItems;

		public Dictionary<UID, AgendaItem> Agenda
		{
			get { return (AgendaItems == null) ? null : AgendaItems.ToDictionary(x => x.UID, x => x); }
		}
		public long RecurrenceId { get; set; }
		public long MeetingId { get; set; }

	}*/

	
	/*
public abstract class AgendaItem
	{
		public UID UID { get; set; }
		public string Name { get; set; }
		public decimal Duration { get; set; }
		public abstract string Type { get; }
	}
	/*
	public class MeetingItem_ScorecardVM : MeetingItemVM
	{
		public class ScorecardRowVM
		{
			public string Measurable { get; set; }
			public LessGreater Direction { get; set; }
			public decimal Target { get; set; }

		}

		public class ScorecardScoreVM
		{
			public DateTime Week { get; set; }
			public decimal? Value { get; set; }
		}
	}*

	public class AgendaItem_Rocks : AgendaItem
	{
		public class Rock
		{
			public string Owner { get; set; }
			public RockState? Completion { get; set; }
			public DateTime DueDate { get; set; }
			public string Title { get; set; }
			public long Id { get; set; }
		}
		public List<Rock> Rocks { get; set; }


		public override string Type {get { return "Rock"; }}
	}
	*/
}