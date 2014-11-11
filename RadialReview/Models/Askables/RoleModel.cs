using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables {
	public class RoleModel : Askable
	{
		public virtual long OrganizationId { get; set; }
		public virtual long ForUserId { get; set; }
		public virtual String Role { get; set; }

		public RoleModel()
		{
		}

		public class RMMap : SubclassMap<RoleModel>
		{
			public RMMap()
			{
				Map(x => x.OrganizationId);
				Map(x => x.ForUserId);
				Map(x => x.Role);
			}
		}

		public override QuestionType GetQuestionType(){
			return QuestionType.GWC;
		}

		public override string GetQuestion(){
			return Role;
		}
	}
}