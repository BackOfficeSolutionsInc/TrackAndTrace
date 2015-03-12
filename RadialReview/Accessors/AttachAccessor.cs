using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Attach;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors
{
	public class AttachAccessor
	{
		public static AttachModel PopulateAttachUnsafe(ISession s,long attachId, AttachType type)
		{
			switch(type){
				case AttachType.Invalid: return new AttachModel();
				case AttachType.Position: return new AttachModel(){
					Name = s.Get<OrganizationPositionModel>(attachId).NotNull(x=>x.CustomName)
				};
				default:throw new ArgumentOutOfRangeException("type");
			}
		}

		public static void SetTemplateUnsafe(ISession s, long attachId, AttachType type,long? templateId)
		{
			switch (type)
			{
				case AttachType.Invalid:return;
				case AttachType.Position:{
					var p = s.Get<OrganizationPositionModel>(attachId);
					p.TemplateId = templateId;
					s.Update(p);
					return;
				}
				default: throw new ArgumentOutOfRangeException("type");
			}
		}
		public static List<long> GetMemberIdsUnsafe(ISession s, long attachId, AttachType type)
		{
			switch (type)
			{
				case AttachType.Invalid: return new List<long>();
				case AttachType.Position:
					{
						return s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.Position.Id == attachId)
							.Select(x => x.UserId)
							.List<long>().ToList();
					}
				default: throw new ArgumentOutOfRangeException("type");
			}
		}
		public static List<UserOrganizationModel> GetMembersUnsafe(ISession s, long attachId, AttachType type)
		{
			var p=GetMemberIdsUnsafe(s, attachId, type);
			return s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null)
								.WhereRestrictionOn(x => x.Id).IsIn(p)
								.List().ToList();
		}
	
	}
}