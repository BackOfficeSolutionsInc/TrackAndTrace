
using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class QuestionModel : Askable, IDeletable
    {
        public virtual DateTime DateCreated { get; set; }
        public virtual LocalizedStringModel Question { get; set; }
        public virtual long CreatedById { get; set; }
        //public virtual IList<QuestionKeyValues> KeyValues { get; set; }
        public virtual IList<LongModel> DisabledFor { get; set; }
        /*public virtual ApplicationWideModel ForApplication { get; set; }
        public virtual OrganizationModel ForOrganization { get; set; }
        public virtual UserOrganizationModel ForUser { get; set; }
        public virtual IndustryModel ForIndustry { get; set; }
        public virtual GroupModel ForGroup { get; set; }*/
        public virtual QuestionType QuestionType { get;set;}
		

        public virtual OriginType OriginType { get; set; }
        public virtual long OriginId { get; set; }

        public QuestionModel() :base()
        {
            DateCreated = DateTime.UtcNow;
            DisabledFor = new List<LongModel>();
            Question = new LocalizedStringModel();
        }
        /*
        public static QuestionModel CreateFeedbackQuestion(UserOrganizationModel caller, LocalizedStringModel question, QuestionCategoryModel category)
        {
            return new QuestionModel()
            {
                Category=category,
                CreatedById=caller.Id,
                OriginId = caller.Organization.Id,
                OriginType = OriginType.Organization,
                Question=question,
                QuestionType=QuestionType.Feedback,
                Weight=WeightType.No,                
            };
        }*/


        public override QuestionType GetQuestionType()
        {
            return QuestionType;
        }

        public override string GetQuestion()
        {
            return Question.Translate();
        }
    }

    public class QuestionModelMap : SubclassMap<QuestionModel>
    {
        public QuestionModelMap()
        {
            Map(x => x.DateCreated);
            Map(x => x.QuestionType);
            Map(x => x.OriginId);
			Map(x => x.OriginType);


			References(x => x.Question)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

            /*
            References(x => x.ForOrganization)
                .Column("OrganizationQuestion_Id")
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            References(x => x.ForApplication)
                .Column("ApplicationQuestion_Id")
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            References(x => x.ForGroup)
                .Column("GroupQuestion_Id")
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            References(x => x.ForUser)
                .Column("UserQuestion_Id")
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            References(x => x.ForIndustry)
                .Column("IndustryQuestion_Id")
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
             */

            Map(x => x.CreatedById);
               // .Cascade.SaveUpdate()
               // .Column("CreatedQuestionsId");
            References(x => x.Category)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();


            HasMany(x => x.DisabledFor)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            /*HasMany(x => x.KeyValues)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();*/

        }
    }
}