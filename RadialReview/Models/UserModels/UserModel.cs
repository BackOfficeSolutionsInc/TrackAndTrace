using System.Diagnostics;
using Antlr.Runtime.Tree;
using FluentNHibernate.Mapping;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace RadialReview.Models
{
	[DebuggerDisplay("{FirstName} {LastName}")]
    public class UserModel : IdentityUser, IDeletable, IStringIdentifiable
    {
        //[Key]
        //public new virtual string Id {get; protected set;}
        //public new virtual string UserName { get; set; }
        //public virtual long Id { get; protected set; }
        //[Index("IX_IdMapping", unique: true)]
        //public virtual String IdMapping { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string Email { get { return UserName; } }
        public virtual bool Hints { get; set; }
        public virtual bool ConsoleLog { get; set; }
        public virtual long CurrentRole { get; set; }
        public virtual string ImageGuid { get; set; }
        public virtual GenderType? Gender { get; set; }
		protected virtual string _UserOrganizationIds { get; set; }
        /*
        public ICollection<UserLogin> Logins { get; set; }
        */
		public virtual int? SendTodoTime { get; set; }

        //[NotMapped]
        private string _ImageUrl { get; set; }


        public virtual IList<UserOrganizationModel> UserOrganization { get; set; }
		public virtual int UserOrganizationCount{get;set;}

        public virtual String Name()
        {
            return ((FirstName ?? "").Trim() + " " + (LastName ?? "").Trim()).Trim();
        }

        public virtual IList<UserRoleModel> Roles { get; set; }
        
        public UserModel()
        {
            UserOrganization = new List<UserOrganizationModel>();
            Hints = true;
	        SendTodoTime = 10;
			Roles = new List<UserRoleModel>();
            ConsoleLog = false;
        }

        public virtual long? GetCurrentRole()
        {
            if (UserOrganizationIds!=null && UserOrganizationIds.Any(x => x == CurrentRole))
                return CurrentRole;
            return null;
        }


        public virtual DateTime? DeleteTime { get; set; }


        public virtual bool IsRadialAdmin { get; set; }
		public virtual long[] UserOrganizationIds
		{
		    get { return _UserOrganizationIds == null ? null : _UserOrganizationIds.Split(new[]{'~'}, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLong()).ToArray(); }
		    set { _UserOrganizationIds = String.Join("~", value); }
	    }

	    public class UserModelMap : ClassMap<UserModel>
		{
			public UserModelMap()
			{
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);
				Map(x => x.UserName).Index("UserName_IDX");
				Map(x => x.FirstName).Not.LazyLoad();
				Map(x => x.LastName).Not.LazyLoad();
				Map(x => x.SendTodoTime);
				Map(x => x.PasswordHash);
				Map(x => x.Hints);
				Map(x => x.CurrentRole);
				Map(x => x._UserOrganizationIds).Length(10000);
				Map(x => x.SecurityStamp);
				Map(x => x.IsRadialAdmin);
				Map(x => x.DeleteTime);
				Map(x => x.ImageGuid);
                Map(x => x.Gender);
                Map(x => x.UserOrganizationCount);
                Map(x => x.ConsoleLog);
				HasMany(x => x.UserOrganization).LazyLoad().Cascade.SaveUpdate();
				HasMany(x => x.Logins).Cascade.SaveUpdate();
				HasMany(x => x.Roles).Cascade.SaveUpdate();
				HasMany(x => x.Claims).Cascade.SaveUpdate();
			}
		}

    }

    

    public class IdentityUserLoginMap : ClassMap<IdentityUserLogin>
    {
        public IdentityUserLoginMap()
        {
            Id(x => x.UserId);
            Map(x => x.LoginProvider);
            Map(x => x.ProviderKey);
        }
    }
    public class IdentityUserClaimMap : ClassMap<IdentityUserClaim>
    {
        public IdentityUserClaimMap()
        {
            Id(x => x.Id);
        }
    }
}
