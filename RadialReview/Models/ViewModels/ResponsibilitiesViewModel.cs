using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ResponsibilityTablesViewModel
    {
        public List<ResponsibilityTableViewModel> ResponsibilityTables { get; set; }

        //<ResponsibilityGroup,Editable>
        public ResponsibilityTablesViewModel(params ResponsibilityGroupModel[] responsibilities)
        {
            ResponsibilityTables=responsibilities.Select(x=>new ResponsibilityTableViewModel(false, x)).ToList();
        }
    }

    public class ResponsibilityTableViewModel
    {
        public long ResponsibilityGroupId { get; set; }
        public String GroupType { get; set; }
        public String Name { get; set; }
        public Boolean Activatable { get; set; }
        public bool Editable { get; set; }
        public int Weight { get; set; }
        public List<ResponsibilityRowViewModel> Responsibilities {get;set;}

        public ResponsibilityTableViewModel(Boolean activatable,ResponsibilityGroupModel rg)
        {
            Name = rg.GetName();
            ResponsibilityGroupId = rg.Id;
            GroupType = rg.GetGroupType();
            Activatable = activatable;
            Editable = rg.GetEditable();
            Responsibilities = rg.Responsibilities.Select(x => new ResponsibilityRowViewModel(x)).ToList();
        }

        public ResponsibilityTableViewModel()
        {

        }

    }

    public class ResponsibilityRowViewModel
    {
        public long Id {get;set;}
		public String Responsibility { get; set; }
		public String Category { get; set; }
		public String Type { get; set; }
        public Boolean Active {get;set;}
        public WeightType Weight { get; set; }
        public ResponsibilityRowViewModel()
        {

        }
        public ResponsibilityRowViewModel(ResponsibilityModel r)
        {
            Id = r.Id;
            Responsibility = r.Responsibility;
            Category = r.Category.Category.Translate();
            Active = r.DeleteTime == null;
            Weight = r.Weight;
	        Type = r.GetQuestionType()+"";
        }
        
    }
}