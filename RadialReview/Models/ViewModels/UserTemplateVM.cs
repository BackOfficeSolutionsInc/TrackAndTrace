using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Controllers;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview;

namespace RadialReview.Models.ViewModels
{
	public class UserTemplateVM
	{
		public bool Edit { get; set; }
		public String Name { get; set; }

		public long TemplateId { get; set; }
		public string JobDescription { get; set; }
		public List<RockVM> Rocks { get; set; }
		public List<String> Roles { get; set; }
		public List<MeasurableVM> Measurables { get; set; }

		public UserTemplateVM(UserTemplate.UserTemplate template, bool editable)
			: this()
		{
			Name = template._Attach.NotNull(x => x.Name);
			TemplateId = template.Id;
			JobDescription = template.JobDescription;
			Rocks = GenRock(template._Rocks);
			Roles = template._Roles.NotNull(x => x.Select(y => y.Role).ToList()) ?? new List<string>();
			Measurables = template._Measurables.NotNull(x => x.Select(y => new MeasurableVM(y)).ToList()) ?? new List<MeasurableVM>();
			Edit = editable;
		}

		private List<RockVM> GenRock(List<UserTemplate.UserTemplate.UT_Rock> rocks)
		{
			if (rocks==null)
				return new List<RockVM>();
			return rocks.GroupBy(x => x.PeriodId).Select(x => new RockVM(){
				Period = x.First().Period.Name,
				Rocks = x.Select(y=>y.Rock).ToList()
			}).ToList();
		} 

		public UserTemplateVM()
		{
			Rocks = new List<RockVM>();
			Roles = new List<string>();
			Measurables = new List<MeasurableVM>();
		}

		public class RockVM
		{
			public string Period { get; set; }
			public List<String> Rocks { get; set; }
			
		}

		public class MeasurableVM
		{
			public string Measurable { get; set; }
			public string GoalDirection { get; set; }
			public decimal Goal { get; set; }

			public MeasurableVM(UserTemplate.UserTemplate.UT_Measurable m)
			{
				Measurable = m.Measurable;
                GoalDirection = m.GoalDirection.ToSymbol()+" ";

				Goal = m.Goal;
			}

		}
	}
}