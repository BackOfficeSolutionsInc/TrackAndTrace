using System.Diagnostics;
using System.Threading.Tasks;
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

namespace RadialReview.Models {
	[DebuggerDisplay("{FirstName} {LastName}")]
	public class UserModel : IdentityUser, IDeletable, IStringIdentifiable {
		//[Key]
		//public new virtual string Id {get; protected set;}
		//public new virtual string UserName { get; set; }
		//public virtual long Id { get; protected set; }
		//[Index("IX_IdMapping", unique: true)]
		//public virtual String IdMapping { get; set; }
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
		public virtual string Email { get { return UserName; } }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
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
		public virtual int UserOrganizationCount { get; set; }

		public virtual String Name() {
			return ((FirstName ?? "").Trim() + " " + (LastName ?? "").Trim()).Trim();
		}

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
		public virtual IList<UserRoleModel> Roles { get; set; }

		public virtual async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<UserModel> manager, string authenticationType) {
			// Note the authenticationType must match the one defined in 
			// CookieAuthenticationOptions.AuthenticationType
			var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
			// Add custom user claims here
			return userIdentity;
		}
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword


		public UserModel() {
			UserOrganization = new List<UserOrganizationModel>();
			Hints = true;
			SendTodoTime = -1;
			Roles = new List<UserRoleModel>();
			ConsoleLog = false;
			CreateTime = DateTime.UtcNow;
		}

		public virtual long? GetCurrentRole() {
			if (IsRadialAdmin)
				return CurrentRole;

			if (UserOrganizationIds != null && UserOrganizationIds.Any(x => x == CurrentRole))
				return CurrentRole;
			return null;
		}


		public virtual DateTime? DeleteTime { get; set; }

		public virtual bool ReverseScorecard { get; set; }

		public virtual bool IsRadialAdmin { get; set; }
		public virtual long[] UserOrganizationIds {
			get { return _UserOrganizationIds == null ? null : _UserOrganizationIds.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLong()).ToArray(); }
			set { _UserOrganizationIds = String.Join("~", value); }
		}
		public virtual UserStyleSettings _StylesSettings { get; set; }

		public class UserModelMap : ClassMap<UserModel> {
			public UserModelMap() {
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);
				Map(x => x.UserName).Index("UserName_IDX").Length(400);
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
				Map(x => x.CreateTime);
				Map(x => x.UserOrganizationCount);
				Map(x => x.ReverseScorecard);
				Map(x => x.DisableTips);
				Map(x => x.ConsoleLog);
				HasMany(x => x.UserOrganization).LazyLoad().Cascade.SaveUpdate();
				HasMany(x => x.Logins).Cascade.SaveUpdate();
				HasMany(x => x.Roles).Cascade.SaveUpdate();
				HasMany(x => x.Claims).Cascade.SaveUpdate();
			}
		}



		public virtual DateTime CreateTime { get; set; }
		public virtual bool DisableTips { get; set; }
	}



	public class IdentityUserLoginMap : ClassMap<IdentityUserLogin> {
		public IdentityUserLoginMap() {
			Id(x => x.UserId).Length(36);
			Map(x => x.LoginProvider);
			Map(x => x.ProviderKey);
		}
	}
	public class IdentityUserClaimMap : ClassMap<IdentityUserClaim> {
		public IdentityUserClaimMap() {
			Id(x => x.Id);
		}
	}
}
