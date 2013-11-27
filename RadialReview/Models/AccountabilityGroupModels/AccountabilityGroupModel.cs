using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.AccountabilityGroupModels
{
    public abstract class AccountabilityGroupModel
    {
        public virtual long Id { get; set; }
    }

    public class AccountabilityGroupMap : ClassMap<AccountabilityGroupModel>
    {
        public AccountabilityGroupMap()
        {
            Id(x => x.Id);
        }
    }
}