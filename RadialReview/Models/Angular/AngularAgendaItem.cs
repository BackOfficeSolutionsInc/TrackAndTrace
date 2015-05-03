using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular
{
	public class AngularAgendaItem : Angular 
	{
		public AngularAgendaItem(long id) : base(id) { }
		public string Name {get; set;}
		public decimal Duration { get; set; }
	}

	public class AngularAgendaItem_Rocks : AngularAgendaItem
	{
		public AngularAgendaItem_Rocks(long id) : base(id){}

		public List<AngularRock> Rocks { get; set; } 
	}
}