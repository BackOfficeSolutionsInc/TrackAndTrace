using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.L10;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Issues;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using static RadialReview.Accessors.DeepAccessor;
using RadialReview.Models.Json;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class OrganizationController : BaseApiController
    {
        [Route("users/")]
        [HttpPut]
        public void CreateUsers([FromBody]string name)
        {
            //need to discuss?
        }


        //[GET/DELETE] /users/{userId}
        [Route("users/{userId}")]
        [HttpGet]
        public UserModel GetUsers(string userId)
        {
            return new UserAccessor().GetUserById(userId);
        }

        [Route("users/{userId}")]
        [HttpDelete]
        public ResultObject DeleteUsers(long userId)
        {
            return new UserAccessor().RemoveUser(GetUser(), userId, DateTime.Now);

        }


        //[GET] /users/{userid}/roles/
        [Route("users/{userId}/roles")]
        [HttpGet]
        public IEnumerable<UserRoleModel> GetRoles(string userId)
        {
            return new UserAccessor().GetUserById(userId).Roles.ToList();
        }


        // [GET] /users/{userid}/positions/
        [Route("users/{userId}/positions")]
        [HttpGet]
        public void GetPositions(string userId)
        {
            //need to discuss?
        }

        //[GET/PUT] /users/{userid}/directreports/
        [Route("users/{userId}/directreports")]
        [HttpGet]
        public IEnumerable<UserOrganizationModel> GetDirectReports(long userId)
        {
            return new UserAccessor().GetDirectSubordinates(GetUser(), userId);
        }

        [Route("users/{organizationId}/directreports")]
        [HttpPut]
        public int CreateDirectReports(long organizationId)
        {
            return new UserAccessor().CreateDeepSubordinateTree(GetUser(), organizationId, DateTime.Now);
        }

        //[GET] /users/{userid}/supervisors/
        [Route("users/{userId}/supervisors")]
        [HttpGet]
        public IEnumerable<UserOrganizationModel> GetSupervisors(long userId)
        {
            return new UserAccessor().GetManagers(GetUser(), userId);
        }

    }
}