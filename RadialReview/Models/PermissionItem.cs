using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using static RadialReview.Models.PermItem;

namespace RadialReview.Models {
	public class PermissionsHeading {
		public string ViewName { get; set; }
		public string EditName { get; set; }
		public string AdminName { get; set; }
		public PermissionsHeading() {
			ViewName  = "View";
			EditName  = "Edit";
			AdminName = "Admin";

		}

		public static PermissionsHeading GetHeading(ResourceType type) {
			switch (type) {
				case ResourceType.UpgradeUsersForOrganization:
					return new PermissionsHeading() {
						ViewName = null,
						EditName = "Edit",
						AdminName = "Admin"
					};
				default:
					return new PermissionsHeading();
			};
		}
	}

	public class PermItem : ILongIdentifiable, IHistorical
	{


		public enum AccessType
		{
			Invalid = 0,
			Creator = 100,
            RGM     = 200,
            Members = 300,
            Admins  = 400,
            Email   = 500,
		}
        public enum ResourceType {
            Invalid = 0,
            L10Recurrence = 1,
            InvoiceForOrganization = 2,
            VTO = 3,
            AccountabilityHierarchy= 4,
			UpgradeUsersForOrganization = 5,
			CoreProcess = 6,
            SurveyContainer = 7,
            Survey = 8,
            //Survey = 7,
		}
		[Flags]
		public enum AccessLevel
		{
			Invalid = 0,
			View = 1,
			Edit = 2,
			Admin = 4,
		}
		public virtual long Id { get; set; }
		public virtual bool IsArchtype { get; set; }

		public virtual long CreatorId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }


		public virtual bool CanView { get; set; }
		public virtual bool CanEdit { get; set; }
		public virtual bool CanAdmin { get; set; }

		public virtual String _DisplayText { get; set; }
		public virtual string _ImageUrl { get; set; }
		public virtual string _DisplayInitials { get; set; }
        public virtual int _Color { get; set; }
		
		public virtual bool HasFlags(AccessLevel level)
		{
			if (level.HasFlag(AccessLevel.View) && !CanView)
				return false;
			if (level.HasFlag(AccessLevel.Edit) && !CanEdit)
				return false;
			if (level.HasFlag(AccessLevel.Admin) && !CanAdmin)
				return false;
			return true;
		}


		public virtual AccessType AccessorType { get; set; }
		public virtual long AccessorId { get; set; }

		public virtual ResourceType ResType { get; set; }
		public virtual long ResId { get; set; }

		public PermItem()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<PermItem>
		{
			public Map()
			{
				Id(x => x.Id);
				Map(x => x.CanView);
				Map(x => x.CanEdit);
				Map(x => x.CanAdmin);
				Map(x => x.IsArchtype);
				Map(x => x.AccessorId);
				Map(x => x.AccessorType);
				Map(x => x.ResId);
				Map(x => x.ResType);

				Map(x => x.CreatorId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
			}
		}

    }
}

public class EmailPermItem : IHistorical{
    public virtual long Id { get; set; }
    public virtual string Email { get; set; }
    public virtual long CreatorId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }

    public EmailPermItem()
    {
        CreateTime = DateTime.UtcNow;
    }

    public class Map : ClassMap<EmailPermItem> {
        public Map()
        {
            Id(x => x.Id);
            Map(x => x.Email);
            Map(x => x.CreatorId);
            Map(x => x.CreateTime);
            Map(x => x.DeleteTime);
        }
    }
}
