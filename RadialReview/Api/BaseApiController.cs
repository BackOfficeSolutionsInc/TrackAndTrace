using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.Controllers;
using System.Web;
using RadialReview.Utilities.Synchronize;

namespace RadialReview.Api
{
	[Authorize]
	public class BaseApiController : ApiController
	{
	
		private UserOrganizationModel _CurrentUser = null;
		private UserOrganizationModel MockUser = null;
		//private string _CurrentUserOrganizationId = null;
		

		protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
		
		protected override void Initialize(HttpControllerContext controllerContext) {
			HttpContext.Current.Items[SyncUtil.NO_SYNC_EXCEPTION] = true;
			base.Initialize(controllerContext);
		}

		private UserOrganizationModel ForceGetUser(ISession s, string userId)
		{
			var user = s.Get<UserModel>(userId);
			if (user.IsRadialAdmin)
				_CurrentUser = s.Get<UserOrganizationModel>(user.CurrentRole);
			else
			{
				if (user.CurrentRole == 0)
				{
					if (user.UserOrganizationIds != null && user.UserOrganizationIds.Count() == 1)
					{
						user.CurrentRole = user.UserOrganizationIds[0];
						s.Update(user);
					}
					else
					{
						throw new OrganizationIdException();
					}
				}

				var found = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (found.DeleteTime != null || found.User.Id == userId)
				{
					//Expensive
					var avail = user.UserOrganization.ToListAlive();
					_CurrentUser = avail.FirstOrDefault(x => x.Id == user.CurrentRole);
					if (_CurrentUser == null)
						_CurrentUser = avail.FirstOrDefault();
					if (_CurrentUser == null)
						throw new NoUserOrganizationException("No user exists.");
				}
				else
				{
					_CurrentUser = found;
				}


			}
			return _CurrentUser;
		}

		protected UserOrganizationModel GetUser()//long? organizationId, Boolean full = false)
		{
			if (MockUser != null)
				return MockUser;
			if (_CurrentUser != null)
				return _CurrentUser;
			var userId = User.Identity.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.");

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return ForceGetUser(s, userId);
				}
			}
		}
		
		protected UserOrganizationModel GetUser(ISession s)//long? organizationId, Boolean full = false)
		{
			if (MockUser != null)
				return MockUser;
			if (_CurrentUser != null)
				return _CurrentUser;

			var userId = User.Identity.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.");

			return ForceGetUser(s, userId);
		}
	}

	[SwaggerName(Name="Title")]
	public class TitleModel {
		/// <summary>
		/// Title
		/// </summary>
		[Required]
		public string title { get; set; }
	}
}