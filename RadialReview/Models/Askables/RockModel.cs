using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Periods;
using RadialReview.Models.Angular.Base;
using Newtonsoft.Json;

namespace RadialReview.Models.Askables {
	public class RockModel :Askable/*, IAngularizer<RockModel>*/ {

		public virtual String Rock { get; set; }

		[JsonIgnore]
		public virtual string Name { get { return Rock; }  set { Rock = value; } }

		public virtual long? FromTemplateItemId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long ForUserId { get; set; }
		///RE-ADD to the map
		[Obsolete("Do not use. Instead use L10Recurrent.Rock")]
		public virtual bool CompanyRock { get; set; }
		public virtual bool _CompanyRock { get; set; }
		public virtual DateTime? DueDate { get; set; }
        public virtual RockState Completion { get; set; }
        public virtual bool _AddedToVTO { get; set; }
        public virtual bool _AddedToL10 { get; set; }

		public override QuestionType GetQuestionType(){
			return QuestionType.Rock;
		}

		public virtual long? PeriodId { get; set; }
		public virtual PeriodModel Period { get; set; }
		//public new virtual DateTime CreateTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }

        public virtual String PadId { get; set; }
        public virtual bool Archived { get; set; }

		public RockModel()
		{
			CreateTime = DateTime.UtcNow;
			OnlyAsk = AboutType.Self;
			Completion = RockState.OnTrack;
            PadId = "-" + Guid.NewGuid().ToString();
		}

		public override string GetQuestion()
		{
			return Rock;
		}

		public virtual string ToFriendlyString()
		{
			var b = Rock;
			if (AccountableUser != null)
				b += " (Owner: " + AccountableUser.GetName() + ")";

			var p = "";
            //if (Period != null)
            //    p = Period.Name+" ";

			//if (CompanyRock)
			//	b += "[" + p + "Company Rock]";
			/*else*/ if (!string.IsNullOrWhiteSpace(p))
				b += "[" + p.Trim() + "]";

			return b ;
		}

		public class RockModelMap : SubclassMap<RockModel> {
			public RockModelMap()
            {
                Map(x => x.Rock);
                Map(x => x.Archived);
                Map(x => x.PadId);
				Map(x => x.Completion);
				Map(x => x.DueDate);
				Map(x => x.OrganizationId);
				Map(x => x.FromTemplateItemId);
			    Map(x => x.CompanyRock);
			    //Map(x => x.CreateTime);
				Map(x => x.CompleteTime);
				Map(x => x.PeriodId).Column("PeriodId");
				References(x => x.Period).Column("PeriodId").Not.LazyLoad().ReadOnly();
				Map(x => x.ForUserId).Column("ForUserId");
				References(x => x.AccountableUser).Column("ForUserId").Not.LazyLoad().ReadOnly();
			}
		}
		public virtual void Angularize(Angularizer<RockModel> angularizer)
		{
			angularizer.Add("Name", x => x.Rock);
			angularizer.Add("Owner", x => x.AccountableUser);
			angularizer.Add("DueDate", x => x.DueDate);
			angularizer.Add("Complete", x => x.CompleteTime != null);
			angularizer.Add("Completion", x => x.Completion);
		}

    }
}