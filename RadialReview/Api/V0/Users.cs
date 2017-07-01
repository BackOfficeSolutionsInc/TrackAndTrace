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
using RadialReview.Models.Angular.Users;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Json;
using RadialReview.Models.Accountability;
using RadialReview.Models.UserModels;
using System.Threading.Tasks;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class UsersController : BaseApiController
    {

        // GET: api/Scores/5
        [Route("users/{id:long}")]
        public UserOrganizationModel.DataContract Get(long id)
        {
            return new UserAccessor().GetUserOrganization(GetUser(), id, false, false).GetUserDataContract();
        }
        [Route("users/{username}")]
        public UserOrganizationModel.DataContract Get(string username)
        {
            var self = GetUser();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    UserOrganizationModel found = null;
                    try
                    {
                        found = new UserAccessor().GetUserOrganizations(s, username, "", false).FirstOrDefault(x => x.Organization.Id == self.Organization.Id);
                    }
                    catch (LoginException)
                    {
                    }
                    if (found == null)
                        throw new HttpResponseException(HttpStatusCode.BadRequest);
                    PermissionsUtility.Create(s, self).ViewUserOrganization(found.Id, false);
                    return found.GetUserDataContract();
                }
            }
        }

        [Route("users/organization/{id?}")]
        public IEnumerable<UserOrganizationModel.DataContract> GetOrganizationUsers(long? id = null)
        {
            return new OrganizationAccessor().GetOrganizationMembers(GetUser(), id ?? GetUser().Organization.Id, false, false).Select(x => x.GetUserDataContract());
        }

        [Route("users/managing")]
        public IEnumerable<UserOrganizationModel.DataContract> GetUsersManaged()
        {
            return DeepAccessor.Users.GetSubordinatesAndSelfModels(GetUser(), GetUser().Id).Select(x => x.GetUserDataContract());
        }

        //--
        [Route("users/")]
        [HttpPut]
        public async Task<AngularUser> CreateUser([FromBody]string firstName, [FromBody]string lastName, [FromBody]string email, [FromBody]long? managerNodeId = null, [FromBody]bool? SendEmail = null)
        {
            //var outParam = new UserOrganizationModel();
            if (!SendEmail.HasValue)
            {
                SendEmail = GetUser().Organization.SendEmailImmediately;
            }
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, Email = email, OrgId = GetUser().Organization.Id, SendEmail = SendEmail.Value };
            var result = await JoinOrganizationAccessor.CreateUserUnderManager(GetUser(), model);

            return AngularUser.CreateUser(result.User);
        }


        //[GET/DELETE] /users/{userId}
        [Route("users/{userId}")]
        [HttpGet]
        public AngularUser GetUser(long userId)
        {
            var user = new UserAccessor().GetUserOrganization(GetUser(), userId, false, false);
            return AngularUser.CreateUser(user);
        }

        [Route("users/{userId}")]
        [HttpDelete]
        public async Task<ResultObject> DeleteUsers(long userId)
        {
            return await new UserAccessor().RemoveUser(GetUser(), userId, DateTime.UtcNow);
        }

        //[GET] /users/{userid}/roles/
        [Route("users/{userId}/roles")]
        [HttpGet]
        public IEnumerable<Models.Askables.RoleModel> GetUserRoles(long userId)
        {
            RoleAccessor obj = new Accessors.RoleAccessor();
            return obj.GetRoles(GetUser(), userId);
        }

        // [GET] /users/{userid}/positions/
        [Route("users/{userId}/positions")]
        [HttpGet]
        public IEnumerable<Models.Angular.Positions.AngularPosition> GetUserPositions(long userId)
        {
            return PositionAccessor.GetPositionModelForUser(GetUser(), userId).Select(x => new Models.Angular.Positions.AngularPosition(x));
        }

        //[GET/PUT] /users/{userid}/directreports/
        [Route("users/{userId}/directreports")]
        [HttpGet]
        public IEnumerable<AngularUser> GetDirectReports(long userId) // wrap AngularUser
        {
            return new UserAccessor().GetDirectSubordinates(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
        }

        //[GET] /users/{userid}/supervisors/
        [Route("users/{userId}/supervisors")]
        [HttpGet]
        public IEnumerable<AngularUser> GetSupervisors(long userId)
        {
            return new UserAccessor().GetManagers(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
        }

        //[GET] /users/{userid}/seats/
        [Route("users/{userId}/seats")]
        [HttpGet]
        public IEnumerable<Models.Angular.Accountability.AngularAccountabilityNode> GetSeats(long userId) // Angular
        {
            return AccountabilityAccessor.GetNodesForUser(GetUser(), userId).Select(x => new Models.Angular.Accountability.AngularAccountabilityNode(x));
        }

        //[GET] /user/mine/teams
        [Route("users/mine/teams")]
        [HttpGet]
        public IEnumerable<TeamDurationModel> GetMineTeam()
        {
            return TeamAccessor.GetUsersTeams(GetUser(), GetUser().Id);
        }


        //[GET] /user/{userId}/teams
        [Route("users/{userId}/teams")]
        [HttpGet]
        public IEnumerable<TeamDurationModel> GetUserTeams(long userId)
        {
            return TeamAccessor.GetAllTeammembersAssociatedWithUser(GetUser(), userId);
        }

    }
}