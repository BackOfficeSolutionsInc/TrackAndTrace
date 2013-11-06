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
        public virtual String Key { get; set; }
        public virtual String Value { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
       
    }

    public class QuestionKeyValuesMap : ClassMap<QuestionKeyValues>
    {
        public QuestionKeyValuesMap()
        {
            Id(x => x.Id);
            Map(x => x.Key);
            Map(x => x.Value);
            Map(x => x.DeleteTime);
        }
    }
}