using FluentNHibernate.Mapping;
using RadialReview.Models.AccountabilityGroupModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.AccountabilityGroupModels
{
    public class PositionModel : AccountabilityGroupModel
    {
        public virtual String Name { get; set; }

    }

    public class PositionModelMap : SubclassMap<PositionModel>
    {
        public PositionModelMap()
        {
            Map(x => x.Name);
        }
    }
}