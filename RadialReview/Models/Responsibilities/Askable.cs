using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public abstract class Askable : ILongIdentifiable
    {
        public virtual long Id { get; set; }

        public abstract QuestionType GetQuestionType();

        public abstract String GetQuestion();



    }



    public class AskableMap : ClassMap<Askable>
    {
        public AskableMap()
        {
            Id(x => x.Id);
        }
    }
}