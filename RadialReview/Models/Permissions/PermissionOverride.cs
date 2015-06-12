using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Permissions
{
	public enum PermissionType
	{
		[Display(Name="<Select a permission...>")]
		Invalid = 0,

		[Display(Name = "Edit Employee Details")]
		EditEmployeeDetails = 1,
		
		/*[Display(Name = "Manage Employee")]
		ManageEmployees =2*/
		[Display(Name = "Delete Employees")]
		DeleteEmployees =3

	}

	public class PermissionOverride : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual UserOrganizationModel ForUser { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual PermissionType Permissions { get; set; }
		public virtual UserOrganizationModel AsUser { get; set; }

		public PermissionOverride()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class PermissionOverrideMap : ClassMap<PermissionOverride>
		{
			public PermissionOverrideMap()
			{
				Id(x => x.Id);
				Map(x => x.DeleteTime);
				Map(x => x.CreateTime);
				References(x => x.ForUser).Column("ForUserId");
				References(x => x.AsUser).Column("AsUserId");
				References(x => x.Organization).Column("OrganizationId");
				Map(x => x.Permissions).CustomType<PermissionType>();

			}
		}
	}
}