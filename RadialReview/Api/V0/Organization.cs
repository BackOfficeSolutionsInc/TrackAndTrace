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
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Users;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class Organization_Controller : BaseApiController
    {
        [Route("users/")]
        [HttpPut]
        public AngularUser CreateUser([FromBody]string firstName, [FromBody]string lastName, [FromBody]string email, [FromBody]long? managerNodeId = null, [FromBody]bool? SendEmail = null)
        {
            var outParam = new UserOrganizationModel();
            if (!SendEmail.HasValue)
            {
                SendEmail = GetUser().Organization.SendEmailImmediately;
            }
            JoinOrganizationAccessor.CreateUserUnderManager(GetUser(), new CreateUserOrganizationViewModel()
            { FirstName = firstName, LastName = lastName, Email = email, OrgId = GetUser().Organization.Id, SendEmail = SendEmail.Value }, out outParam);

            return AngularUser.CreateUser(outParam);
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
        public ResultObject DeleteUsers(long userId)
        {
            return new UserAccessor().RemoveUser(GetUser(), userId, DateTime.UtcNow);
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
        public IEnumerable<UserOrganizationModel> GetDirectReports(long userId) // wrap AngularUser
        {
            return new UserAccessor().GetDirectSubordinates(GetUser(), userId);
        }

        [Route("seats/{seatId}/directreport")]
        [HttpPut]
        public AngularAccountabilityNode AttachDirectReport(long seatId, [FromBody]long userId) // wrap AngularAccountabilityNode
        {
            return new Models.Angular.Accountability.AngularAccountabilityNode(AccountabilityAccessor.AppendNode(GetUser(), seatId, null, userId));
        }

        //[GET] /users/{userid}/supervisors/
        [Route("users/{userId}/supervisors")]
        [HttpGet]
        public IEnumerable<UserOrganizationModel> GetSupervisors(long userId)
        {
            return new UserAccessor().GetManagers(GetUser(), userId);
        }

        //[GET] /users/{userid}/seats/
        [Route("users/{userId}/seats")]
        [HttpGet]
        public IEnumerable<AccountabilityNode> GetSeats(long userId) // Angular
        {
            return AccountabilityAccessor.GetNodesForUser(GetUser(), userId);
        }

        // [GET/POST/(DELETE?)] /seats/{seatId}
        [Route("seats/{seatId}")]
        [HttpGet]
        public AccountabilityNode GetSeat(long seatId) // Angular
        {
            return AccountabilityAccessor.GetNodeById(GetUser(), seatId);
        }

        //[Route("seats/{seatId}")]
        //[HttpPost]
        //public void AttachUserToSeat(long seatId)
        //{
        //    AccountabilityAccessor.Update(GetUser(), new AngularAccountabilityNode() { Id = seatId }, null);
        //}

        [Route("seats/{seatId}")]
        [HttpDelete]
        public void RemoveSeat(long seatId)
        {
            AccountabilityAccessor.RemoveNode(GetUser(), seatId);
        }

        //[GET/PUT/DELETE] /seats/{seatId}/position
        [Route("seats/{seatId}/position")]
        [HttpGet]
        public OrganizationPositionModel GetPosition(long seatId) // Angular
        {
            return AccountabilityAccessor.GetNodeById(GetUser(), seatId).AccountabilityRolesGroup.NotNull(x => x.Position); // null check for AccountabilityRolesGroup
        }

        [Route("seats/{seatId}/position/{positionId}")]
        [HttpPost]
        public void AttachPosition(long seatId, long positionId)
        {
            AccountabilityAccessor.SetPosition(GetUser(), seatId, positionId);
        }

        [Route("seats/{seatId}/position")]
        [HttpDelete]
        public void RemovePosition(long seatId)
        {
            // positionId set to null while removing or detaching
            AccountabilityAccessor.SetPosition(GetUser(), seatId, null);
        }

        //[GET/PUT/DELETE] /seats/{seatId}/user

        [Route("seats/{seatId}/user")]
        [HttpGet]
        public UserOrganizationModel GetSeatUser(long seatId) // Angular
        {
            return AccountabilityAccessor.GetNodeById(GetUser(), seatId).User;
        }

        [Route("seats/{seatId}/user")]
        [HttpPost]
        public void AttachUser(long seatId, [FromBody]long? userId)
        {
            AccountabilityAccessor.SetUser(GetUser(), seatId, userId);
        }

        [Route("seats/{seatId}/user")]
        [HttpDelete]
        public void DetachUser(long seatId)
        {
            AccountabilityAccessor.SetUser(GetUser(), seatId, null); // null userId for detaching 
        }

        //[GET] /positions/mine
        [Route("positions/mine")]
        [HttpGet]
        public IEnumerable<OrganizationPositionModel> GetMinePosition()
        {
            return PositionAccessor.GetPositionModelForUser(GetUser(), GetUser().Id);
        }

        //[GET/PUT] /positions/{id}/roles/
        [Route("positions/{id}/roles")]
        [HttpGet]
        public void GetPositionRoles(long id, [FromBody]long seatId)
        {
            // do it later.
        }

        [Route("positions/{id}/roles")]
        [HttpPut]
        public RoleModel UpdatePositionRoles(long id, [FromBody]string name) // Angular
        {
            return AccountabilityAccessor.AddRole(GetUser(), new Models.Enums.Attach(Models.Enums.AttachType.Position, id), name);
        }

        //[PUT] /positions/
        [Route("positions/")]
        [HttpPut]
        public OrganizationPositionModel CreatePosition([FromBody] string name)
        {
            //need to discuss?
            OrganizationAccessor _accessor = new OrganizationAccessor();
            return _accessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, name);
        }

        //[GET/POST] /positions/{id}
        [Route("positions/{id}")]
        [HttpGet]
        public OrganizationPositionModel GetPositions(long id)
        {
            return new OrganizationAccessor().GetOrganizationPosition(GetUser(), id);
        }

        [Route("positions/{id}")]
        [HttpPost]
        public OrganizationPositionModel UpdatePositions([FromBody] string name, long id)
        {
            OrganizationAccessor _accessor = new OrganizationAccessor();
            return _accessor.EditOrganizationPosition(GetUser(), id, GetUser().Organization.Id, name);
        }

        //[GET/POST/DELETE] /roles/{id}
        [Route("roles/{id}")]
        [HttpGet]
        public RoleModel GetRoles(long id)
        {
            return RoleAccessor.GetRolesById(GetUser(), id);
        }

        [Route("roles/{id}")]
        [HttpPost]
        public void UpdateRoles([FromBody]string name, long id)
        {
            RoleAccessor.EditRole(GetUser(), id, name);
        }

        [Route("roles/{id}")]
        [HttpDelete]
        public void RemoveRoles(long id)
        {
            AccountabilityAccessor.RemoveRole(GetUser(), id);
        }

        // create separate files.

    }
}
