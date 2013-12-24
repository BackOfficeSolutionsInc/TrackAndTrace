using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class QuestionKeyValues : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual String QuestionKey { get; set; }
        public virtual String QuestionValue { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
       
    }

    public class QuestionKeyValuesMap : ClassMap<QuestionKeyValues>
    {
        public QuestionKeyValuesMap()
        {
            Id(x => x.Id);
            Map(x => x.QuestionKey);
            Map(x => x.QuestionValue);
            Map(x => x.DeleteTime);
        }
    }
}