using System;
using System.Collections.Generic;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularAgendaItem : BaseAngular 
	{
		public AngularAgendaItem(long id,string name,string backendName) : base(id)
		{
			Duration = 5;
			Name = name;
			BackendName = backendName;
		}
		public string Name { get; set; }
		public string BackendName { get; set; }
		public decimal Duration { get; set; }
		public string TemplateUrl { get; set; }
		public decimal Ellapsed { get; set; }
		public DateTime PageStart { get; set; }
	}

	public class AngularAgendaItem_Segue : AngularAgendaItem
	{
		public AngularAgendaItem_Segue(long id,string name="Segue") : base(id,name,"segue") { }

	}
	public class AngularAgendaItem_Scorecard : AngularAgendaItem
	{
		public AngularAgendaItem_Scorecard(long id, string name = "Scorecard") : base(id, name, "scorecard") { }
		public List<AngularMeetingMeasurable> Measurables { get; set; } 
		public List<AngularScore> Scores { get; set; }
		public List<AngularWeek> Weeks { get; set; } 

	}	
	public class AngularAgendaItem_Rocks : AngularAgendaItem
	{
		public AngularAgendaItem_Rocks(long id, string name) : base(id,  name, "rocks") { }
		public List<AngularMeetingRock> Rocks { get; set; } 
	}
	public class AngularAgendaItem_Headlines : AngularAgendaItem
	{
		public AngularAgendaItem_Headlines(long id,  string name = "Headlines") : base(id, name, "headlines") { }
	}
	public class AngularAgendaItem_Todos : AngularAgendaItem
	{
		public AngularAgendaItem_Todos(long id,  string name = "Todo List") : base(id, name, "todo") { }

	}
	public class AngularAgendaItem_IDS : AngularAgendaItem
	{
		public AngularAgendaItem_IDS(long id, string name = "IDS") : base(id, name, "ids") { }

	}
	public class AngularAgendaItem_Conclusion : AngularAgendaItem
	{
		public AngularAgendaItem_Conclusion(long id, string name = "Conclusion") : base(id, name, "conclusion") { }

	}
}