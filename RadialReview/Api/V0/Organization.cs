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
using RadialReview.Models.Accountability;
using RadialReview.Models.ViewModels;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class OrganizationController : BaseApiController
    {
        [Route("users/")]
        [HttpPut]
        public void CreateUsers([FromBody]string firstName)
        {
            //            CreateUserOrganizationViewModel
            //FirstName
            //LastName
            //Email
            //OrgId

            //Optional Params
            //ManagerNodeId = null
            //SendEmail = null(default: GetUser().Organization.SendEmailImmediately)

            var outParam = new UserOrganizationModel();
            var model = new Models.ViewModels.CreateUserOrganizationViewModel();

            JoinOrganizationAccessor.CreateUserUnderManager(GetUser(), model, out outParam);
            //need to discuss?
        }


        //[GET/DELETE] /users/{userId}
        [Route("users/{userId}")]
        [HttpGet]
        public UserOrganizationModel GetUsers(long userId)
        {
            return new UserAccessor().GetUserOrganization(GetUser(), userId, false, false);
        }

        [Route("users/{userId}")]
        [HttpDelete]
        public ResultObject DeleteUsers(long userId)
        {
            return new UserAccessor().RemoveUser(GetUser(), userId, DateTime.UtcNow);
        }

        //[GET] /users/{userid}/roles/
        [Route("users/{userId}/roles")]
        [HttpGet]
        public IEnumerable<Models.Askables.RoleModel> GetRoles(long userId)
        {
            RoleAccessor obj = new Accessors.RoleAccessor();
            return obj.GetRoles(GetUser(), userId);
        }

        // [GET] /users/{userid}/positions/
        [Route("users/{userId}/positions")]
        [HttpGet]
        public IEnumerable<Models.Angular.Positions.AngularPosition> GetPositions(long userId)
        {
            return PositionAccessor.GetPositionModelForUser(GetUser(), userId).Select(x => new Models.Angular.Positions.AngularPosition(x));
        }

        //[GET/PUT] /users/{userid}/directreports/
        [Route("users/{userId}/directreports")]
        [HttpGet]
        public IEnumerable<UserOrganizationModel> GetDirectReports(long userId)
        {
            return new UserAccessor().GetDirectSubordinates(GetUser(), userId);
        }

        [Route("seats/{seatId}/directreport")]
        [HttpPut]
        public AccountabilityNode AttachDirectReport(long seatId, [FromBody]long userId)
        {
            return AccountabilityAccessor.AppendNode(GetUser(), seatId, null, userId);
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