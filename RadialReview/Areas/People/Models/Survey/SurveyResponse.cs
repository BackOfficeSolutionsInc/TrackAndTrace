using FluentNHibernate.Mapping;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {


    public class SurveyResponse : ILongIdentifiable, IHistorical, IResponse {

        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        #region Do not use
        [Obsolete("Do not use. Not Saved.")]
        public virtual string Name { get; set; }
        [Obsolete("Do not use. Not Saved.")]
        public virtual string Help { get; set; }
        [Obsolete("Do not use. Not Saved.")]
        public virtual int Ordering { get; set; }
        #endregion  

        public virtual ForModel By { get; set; }
        public virtual ForModel About { get; set; }

        public virtual SurveyType SurveyType { get; set; }
        public virtual long OrgId { get; set; }
        public virtual long SurveyContainerId { get; set; }
        public virtual long SurveyId { get; set; }
        public virtual long SectionId { get; set; }

		public virtual string Answer { get; set; }
		public virtual DateTime? CompleteTime { get; set; }

		//public virtual SurveyItem Item { get; set; }

		public virtual long ItemId { get; set; }
        public virtual long ItemFormatId { get; set; }

        public virtual ISurveyContainer _SurveyContainer { get; set; }
        public virtual ISurvey _Survey { get; set; }
        public virtual ISection _Section { get; set; }
        public virtual IItem _Item { get; set; }
        public virtual IItemFormat _ItemFormat { get; set; }


#pragma warning disable CS0618 // Type or member is obsolete
		public SurveyResponse(IResponseInitializerCtx ctx,IItemFormat format,string defaultAnswer =null) :this() {
#pragma warning restore CS0618 // Type or member is obsolete
			OrgId = ctx.OrgId;
            By = ForModel.From(ctx.Survey.GetBy());
            About = ForModel.From(ctx.Survey.GetAbout());
            SurveyType = ctx.SurveyContainer.GetSurveyType();
            SurveyId = ctx.Survey.Id;
            SectionId = ctx.Section.Id;
            SurveyContainerId = ctx.SurveyContainer.Id;
            ItemId = ctx.Item.Id;
            Answer = defaultAnswer;
            ItemFormatId = format.Id;
			CreateTime = ctx.Now;

			_SurveyContainer = ctx.SurveyContainer;
            _Survey = ctx.Survey;
            _Section = ctx.Section;
            _Item = ctx.Item;
            _ItemFormat = format;
        }        

        [Obsolete("Use other constructor")]
        public SurveyResponse() {
            CreateTime = DateTime.UtcNow;
        }

        public virtual string ToPrettyString() {
            return "Response: " + (Answer??("(null)"));
        }

        public virtual long GetItemId() {
            return ItemId;
        }

        public virtual long GetItemFormatId() {
            return ItemFormatId;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public virtual string GetName() {
            return Name;
        }
        public virtual string GetHelp() {
			return Help;
		}
        public virtual int GetOrdering() {
            return Ordering;
        }
#pragma warning restore CS0618 // Type or member is obsolete

		public virtual string GetAnswer() {
			return Answer;
		}

		public virtual IByAbout GetByAbout() {
			return new ByAbout(By, About);
		}

		public class Map : ClassMap<SurveyResponse> {
            public Map() {
                Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.CompleteTime);
				Map(x => x.DeleteTime);
                Component(x => x.By).ColumnPrefix("By_");
                Component(x => x.About).ColumnPrefix("About_");
                Map(x => x.SurveyType);
                Map(x => x.SurveyContainerId);
                Map(x => x.SurveyId);
                Map(x => x.SectionId);
                Map(x => x.Answer).Length(8000);
                Map(x => x.ItemId);
                Map(x => x.ItemFormatId);
                Map(x => x.OrgId);

				//References(x => x.Item).Column("ItemId").LazyLoad().ReadOnly();
            }
        }
    }
}