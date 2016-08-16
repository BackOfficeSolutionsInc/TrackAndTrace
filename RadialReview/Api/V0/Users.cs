using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Api.V0
{
	[RoutePrefix("api/v0")]
	public class UsersController : BaseApiController
	{

		// GET: api/Scores/5
		[Route("users/{id:long}")]
		public UserOrganizationModel.DataContract Get(long id){
			return new UserAccessor().GetUserOrganization(GetUser(), id, false, false).GetUserDataContract();
		}
		[Route("users/{username}")]
		public UserOrganizationModel.DataContract Get(string username)
		{
			var self = GetUser();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					UserOrganizationModel found = null;
					try{
						found = new UserAccessor().GetUserOrganizations(s, username, "", false).FirstOrDefault(x => x.Organization.Id == self.Organization.Id);
					}catch (LoginException){
					}
					if (found==null)
						throw new HttpResponseException(HttpStatusCode.BadRequest);
					PermissionsUtility.Create(s, self).ViewUserOrganization(found.Id,false);
					return found.GetUserDataContract();
				}
			}
		}
		
		[Route("users/organization/{id?}")]
		public IEnumerable<UserOrganizationModel.DataContract> GetOrganizationUsers(long? id=null){
			return new OrganizationAccessor().GetOrganizationMembers(GetUser(), id??GetUser().Organization.Id, false, false).Select(x => x.GetUserDataContract());
		}
		
		[Route("users/managing")]
		public IEnumerable<UserOrganizationModel.DataContract> GetUsersManaged(){
			return DeepAccessor.Users.GetSubordinatesAndSelfModels(GetUser(), GetUser().Id).Select(x => x.GetUserDataContract());
		}


	}
}