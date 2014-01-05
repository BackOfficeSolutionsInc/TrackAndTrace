using FluentNHibernate.Mapping;
using Microsoft.AspNet.Identity.EntityFramework;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels
{

    public class UserRoleModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual String Role { get; set; }
        public virtual Boolean Deleted { get; set; }
    }

    public class UserRoleModelMap : ClassMap<UserRoleModel>
    {
        public UserRoleModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Role);
            Map(x => x.Deleted);
        }
    }
}