using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels {
    public class UserStyleSettings {
        public virtual string Id { get; set; }
        public virtual bool ShowScorecardColors { get; set; }

        public UserStyleSettings()
        {
            ShowScorecardColors = true;
        }

        public class Map : ClassMap<UserStyleSettings> {
            public Map()
            {
                Id(x => x.Id).GeneratedBy.Assigned();
                Map(x => x.ShowScorecardColors);
            }
        }
    }
}