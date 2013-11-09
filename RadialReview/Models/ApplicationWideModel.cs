
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ApplicationWideModel : ICustomQuestions
    {
        public virtual long Id { get; protected set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual OriginType QuestionOwner { get { return OriginType.Application; } }
    }

    public class ApplicationWideModelMap : ClassMap<ApplicationWideModel>
    {
        public ApplicationWideModelMap()
        {
            Id(x => x.Id);
            HasMany(x => x.CustomQuestions)
                .KeyColumn("ApplicationQuestion_Id")
                .Inverse();
        }
    }
}