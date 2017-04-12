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
    public class SeatsController : BaseApiController
    {

        [Route("seats/{seatId}/directreport")]
        [HttpPut]
        public AngularAccountabilityNode AttachDirectReport(long seatId, [FromBody]long userId) // wrap AngularAccountabilityNode
        {
            return new Models.Angular.Accountability.AngularAccountabilityNode(AccountabilityAccessor.AppendNode(GetUser(), seatId, null, userId));
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

    }
}
