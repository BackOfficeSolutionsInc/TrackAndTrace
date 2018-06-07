using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using static RadialReview.Models.PermItem;

namespace RadialReview.Models.Accountability {

	public class AccountabilityChart : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual string Name { get; set; }
		public virtual long RootId { get; set; }


		public AccountabilityChart() {
			CreateTime = DateTime.UtcNow;
		}

		class Map : ClassMap<AccountabilityChart> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.RootId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
			}
		}

	}

	[DebuggerDisplay("Node: {User.EmailAtOrganization}")]
	public class AccountabilityNode : ILongIdentifiable, IHistorical, IForModel {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public virtual long? UserId { get; set; }
		public virtual long? ParentNodeId { get; set; }
		public virtual long AccountabilityRolesGroupId { get; set; }
		public virtual UserOrganizationModel User { get; set; }
		public virtual AccountabilityNode ParentNode { get; set; }
		public virtual AccountabilityRolesGroup AccountabilityRolesGroup { get; set; }
		public virtual List<AccountabilityNode> _Children { get; set; }


		public virtual string PositionName { get { return AccountabilityRolesGroup.NotNull(x => x.Position.GetName()); } }

		public virtual string _Name { get; set; }
		public virtual bool? _Editable { get; set; }
		public virtual int Ordering { get; set; }


		public virtual long ModelId { get { return Id; } }
		public virtual string ModelType { get { return ForModel.GetModelType<AccountabilityNode>(); } }
		public virtual bool Is<T>() {
#pragma warning disable CS0618 // Type or member is obsolete
			return ModelType == ForModel.GetModelType(typeof(T));
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public virtual string ToPrettyString() {
			return _Name ?? string.Join(" - ", new[] { User.NotNull(x => x.GetName()), AccountabilityRolesGroup.NotNull(y => y.Position.GetName()) }.Where(x => x != null));
		}
		public AccountabilityNode() {
			CreateTime = DateTime.UtcNow;
		}

		class Map : ClassMap<AccountabilityNode> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Ordering);
				Map(x => x.OrganizationId);
				Map(x => x.AccountabilityChartId);
				Map(x => x.UserId).Column("UserId").Index("idx__AccountabilityNode_UserId");
				References(x => x.User).Column("UserId").LazyLoad().ReadOnly();
				Map(x => x.AccountabilityRolesGroupId).Column("RolesGroupId");
				References(x => x.AccountabilityRolesGroup).Column("RolesGroupId").LazyLoad().ReadOnly();
				Map(x => x.ParentNodeId).Column("ParentNodeId").Index("idx__AccountabilityNode_ParentId");
				References(x => x.ParentNode).Column("ParentNodeId").LazyLoad().ReadOnly();
			}
		}
	}


	public class RoleGroupRole {
		public RoleGroupRole(RoleModel role, long? ordering) {
			Role = role;
			Ordering = ordering;
		}

		public RoleModel Role { get; set; }
		public long? Ordering { get; set; }

	}


	public class RoleGroup {
		public virtual long AttachId { get; set; }
		public virtual AttachType AttachType { get; set; }
		public virtual List<RoleGroupRole> Roles { get; set; }
		public virtual String AttachName { get; set; }

		public RoleGroup(List<RoleGroupRole> roles, long attachId, AttachType attachType, string attachName) {
			AttachId = attachId;
			AttachType = attachType;
			Roles = roles;
			AttachName = attachName;
		}

		public virtual Attach GetAttach() {
			return new Attach {
				Id = AttachId,
				Name = AttachName,
				Type = AttachType,
			};
		}
	}


	public class AccountabilityRolesGroup : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long? PositionId { get; set; }
		public virtual OrganizationPositionModel Position { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual List<RoleGroup> _Roles { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public virtual bool? _Editable { get; set; }

		public AccountabilityRolesGroup() {
			CreateTime = DateTime.UtcNow;
		}
		class Map : ClassMap<AccountabilityRolesGroup> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.AccountabilityChartId);
				Map(x => x.PositionId).Column("PositionId");
				References(x => x.Position).Column("PositionId").LazyLoad().ReadOnly();
			}
		}
	}



	public class AccountabilityNodeRoleMap : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long RoleId { get; set; }
		public virtual RoleModel Role { get; set; }
		public virtual long? PositionId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountabilityGroupId { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public AccountabilityNodeRoleMap() {
			CreateTime = DateTime.UtcNow;
		}
		class Map : ClassMap<AccountabilityNodeRoleMap> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.RoleId).Column("RoleId");
				References(x => x.Role).Column("RoleId").Not.LazyLoad().ReadOnly();
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.PositionId);
				Map(x => x.AccountabilityGroupId);
				Map(x => x.AccountabilityChartId);
			}
		}
	}
}