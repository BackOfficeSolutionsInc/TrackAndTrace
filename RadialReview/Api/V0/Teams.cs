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
using RadialReview.Models.Askables;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class TeamsController : BaseApiController
    {

        //[GET/PUT] /teams
        [Route("teams")]
        [HttpPut]
        public OrganizationTeamModel AddTeam([FromBody]string name)
        {
            //in add case teamId=0
            return TeamAccessor.EditTeam(GetUser(), 0, name, false, true, GetUser().Id);
        }

        //[GET/POST] /teams/{teamId}
        [Route("teams/{teamId}")]
        [HttpGet]
        public OrganizationTeamModel GetTeams(long teamId)
        {
            return TeamAccessor.GetTeam(GetUser(), teamId);
        }


        [Route("teams/{teamId}")]
        [HttpPost]
        public OrganizationTeamModel UpdateTeam(long teamId, [FromBody]string name = null)
        {
            return TeamAccessor.EditTeam(GetUser(), teamId, name, null, null, null); // null while update
        }

        //[GET/PUT] /teams/{teamId}/members
        [Route("teams/{teamId}/members")]
        [HttpGet]
        public IEnumerable<AngularUser> GetTeamMember(long teamId)
        {
            return ResponsibilitiesAccessor.GetResponsibilityGroupMembers(GetUser(), teamId).Select(x => AngularUser.CreateUser(x));            
        }

        [Route("teams/{teamId}/members")]
        [HttpPut]
        public bool AddTeamMember(long teamId, [FromBody] long userId)
        {
            return TeamAccessor.AddMember(GetUser(), teamId, userId);
        }

        //[DELETE] /teams/{teamId}/members/{userId}
        [Route("teams/{teamId}/members/{userId}")]
        [HttpDelete]
        public bool RemoveTeamMember(long teamId, long userId)
        {
            return TeamAccessor.RemoveTeamMember(GetUser(), teamId, userId);
        }
    }
}