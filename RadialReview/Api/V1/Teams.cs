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
using RadialReview.Models.Angular.Team;

namespace RadialReview.Api.V1
{
    [RoutePrefix("api/v1")]
    public class TeamsController : BaseApiController
    {
		/// <summary>
		/// Create a new team
		/// </summary>
		/// <param name="body"></param>
		/// <returns></returns>
        //[GET/PUT] /teams
        [Route("teams/create")]
        [HttpPost]
        public AngularTeam AddTeam([FromBody]TitleModel body)
        {
            //in add case teamId=0
            return new AngularTeam(TeamAccessor.EditTeam(GetUser(), 0, body.title, false, true, GetUser().Id));
        }

		/// <summary>
		/// Get a particular team
		/// </summary>
		/// <param name="TEAM_ID">Team ID</param>
		/// <returns></returns>
		//[GET/POST] /teams/{TEAM_ID}
		[Route("teams/{TEAM_ID}")]
        [HttpGet]
        public AngularTeam GetTeams(long TEAM_ID)
        {
            return new AngularTeam(TeamAccessor.GetTeam(GetUser(), TEAM_ID));
        }

		/// <summary>
		/// Update a team
		/// </summary>
		/// <param name="TEAM_ID">Team ID</param>
		/// <param name="body"></param>
		/// <returns></returns>
		[Route("teams/{TEAM_ID}")]
        [HttpPut]
        public AngularTeam UpdateTeam(long TEAM_ID, [FromBody]TitleModel body){
            return new AngularTeam(TeamAccessor.EditTeam(GetUser(), TEAM_ID, body.title, null, null, null)); // null while update
		}
		///// <summary>
		///// Delete a particular team
		///// </summary>
		///// <param name="TEAM_ID">Team ID</param>
		///// <returns></returns>
		////[GET/POST] /teams/{TEAM_ID}
		//[Route("teams/{TEAM_ID}")]
		//[HttpDelete]
		//public AngularTeam DeleteTeams(long TEAM_ID) {
		//	TeamAccessor.de
		//	return new AngularTeam(TeamAccessor.GetTeam(GetUser(), TEAM_ID));
		//}

		/// <summary>
		/// Get team members
		/// </summary>
		/// <param name="TEAM_ID"></param>
		/// <returns></returns>
		//[GET/PUT] /teams/{TEAM_ID}/members
		[Route("teams/{TEAM_ID}/members")]
        [HttpGet]
        public IEnumerable<AngularUser> GetTeamMembers(long TEAM_ID)
        {
            return ResponsibilitiesAccessor.GetResponsibilityGroupMembers(GetUser(), TEAM_ID).Select(x => AngularUser.CreateUser(x));            
        }

		/// <summary>
		/// Add a user to a team
		/// </summary>
		/// <param name="TEAM_ID">Team ID</param>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
        [Route("teams/{TEAM_ID}/member/{USER_ID}")]
        [HttpPost]
        public bool AddTeamMember(long TEAM_ID, long USER_ID)
        {
            return TeamAccessor.AddMember(GetUser(), TEAM_ID, USER_ID);
        }

		/// <summary>
		/// Remove a team member
		/// </summary>
		/// <param name="TEAM_ID">Team ID</param>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		//[DELETE] /teams/{teamId}/members/{userId}
		[Route("teams/{TEAM_ID}/member/{USER_ID}")]
        [HttpDelete]
        public bool RemoveTeamMember(long TEAM_ID, long USER_ID)
        {
            return TeamAccessor.RemoveTeamMember(GetUser(), TEAM_ID, USER_ID);
        }
    }
}