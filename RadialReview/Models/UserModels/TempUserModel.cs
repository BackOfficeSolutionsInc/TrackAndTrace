using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.UserModels
{
    public class TempUserModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual String FirstName { get; set; }
        public virtual String LastName { get; set; }
        public virtual String Email { get; set; }
        public virtual DateTime Created { get; set; }

        public virtual string Name()
        {
            var possible=((FirstName ?? "").Trim() + " " + (LastName ?? "").Trim()).Trim();
            if (String.IsNullOrWhiteSpace(possible))
            {
                if (String.IsNullOrWhiteSpace(Email))
                    return "TempUserId[" + Id + "]";
                return Email;
            }
            return possible;
        }
    }

    public class TempUserModelMap : ClassMap<TempUserModel>
    {
        public TempUserModelMap()
        {
            Id(x => x.Id);
            Map(x => x.FirstName);
            Map(x => x.LastName);
            Map(x => x.Email);
            Map(x => x.Created);
        }
    }
}
