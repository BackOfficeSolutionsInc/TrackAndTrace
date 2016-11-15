using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Accessors {
	public class AttachAccessor {
		public static Attach PopulateAttachUnsafe(ISession s, Attach attach) {
			return PopulateAttachUnsafe(s, attach.Id, attach.Type);
		}
		public static Attach PopulateAttachUnsafe(ISession s, long attachId, AttachType type) {
			var a = new Attach() {
				Id = attachId,
				Type = type,
			};

			switch (type) {

				case AttachType.Invalid:
					return a;
				case AttachType.Position:
					a.Name = s.Get<OrganizationPositionModel>(attachId).NotNull(x => x.CustomName);
					return a;
				case AttachType.Team:
					a.Name = s.Get<OrganizationTeamModel>(attachId).NotNull(x => x.Name);
					return a;
				case AttachType.User:
					a.Name = s.Get<UserOrganizationModel>(attachId).NotNull(x => x.GetName());
					return a;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static void SetTemplateUnsafe(ISession s, Attach attach, long? templateId) {
			var attachId = attach.Id;
			var type = attach.Type;
			switch (type) {
				case AttachType.Invalid:
					return;
				case AttachType.Position: {
						var p = s.Get<OrganizationPositionModel>(attachId);
						p.TemplateId = templateId;
						s.Update(p);
						return;
					}
				case AttachType.Team: {
						var p = s.Get<OrganizationTeamModel>(attachId);
						p.TemplateId = templateId;
						s.Update(p);
						return;
					}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static List<long> GetMemberIdsUnsafe(ISession s, Attach attach) {
			var attachId = attach.Id;
			var type = attach.Type;
			switch (type) {
				case AttachType.Invalid:
					return new List<long>();
				case AttachType.Position: {
						return s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.Position.Id == attachId)
							.Select(x => x.UserId)
							.List<long>().ToList();
					}
				case AttachType.Team: {
						return s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null && x.TeamId == attachId)
							.Select(x => x.UserId)
							.List<long>().ToList();
					}
				case AttachType.User: {
						return attachId.AsList();
					}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static List<TinyUser> GetTinyMembersUnsafe(ISession s, Attach attach) {
			var p = GetMemberIdsUnsafe(s, attach);
			return TinyUserAccessor.GetUsers_Unsafe(s, p).ToList();
		}

		public static List<UserOrganizationModel> GetMembersUnsafe(ISession s, Attach attach) {
			var p = GetMemberIdsUnsafe(s, attach);
			return s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null)
								.WhereRestrictionOn(x => x.Id).IsIn(p)
								.List().ToList();
		}
		
		public static long GetOrganizationId(ISession s, Attach attach) {
			var attachId = attach.Id;
			var type = attach.Type;

			switch (type) {
				case AttachType.Invalid:
					throw new ArgumentOutOfRangeException("type");
				case AttachType.Position: {
						var p = s.Get<OrganizationPositionModel>(attachId);
						return p.Organization.Id;
				}
				case AttachType.Team: {
						var p = s.Get<OrganizationTeamModel>(attachId);
						return p.Organization.Id;
				}
				case AttachType.User: {
						var p = s.Get<UserOrganizationModel>(attachId);
						return p.Organization.Id;
				}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

	}
}