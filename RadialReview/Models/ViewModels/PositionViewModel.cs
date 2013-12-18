using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class PositionViewModel
    {
        public long Id {get;set;}
        public List<PositionModel> Positions { get; set; }

        public long? Position { get; set; }

        public String PositionName { get;set;}
    }
}