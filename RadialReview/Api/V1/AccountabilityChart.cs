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
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Enums;

namespace RadialReview.Api.V1 {
	/// <summary>
	/// Seats are boxes on the accountability chart. A user can occupy more than one seat on the accountability chart.
	/// </summary>
	[RoutePrefix("api/v1")]
	public class SeatsController : BaseApiController {

		/// <summary>
		/// Add a user below a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		[Route("seats/{SEAT_ID}/directreport/{USER_ID}")]
		[HttpPost]
		public AngularAccountabilityNode AttachDirectReport(long SEAT_ID, long USER_ID) {
			return new AngularAccountabilityNode(AccountabilityAccessor.AppendNode(GetUser(), SEAT_ID, null, USER_ID));
		}

		/// <summary>
		/// Get a particular seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <returns></returns>
		// [GET/POST/(DELETE?)] /seats/{seatId}
		[Route("seats/{SEAT_ID}")]
		[HttpGet]
		public AngularAccountabilityNode GetSeat(long SEAT_ID) {
			return new AngularAccountabilityNode(AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID));
		}
		/// <summary>
		/// Delete a seat from the accountability chart
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID}")]
		[HttpDelete]
		public void RemoveSeat(long SEAT_ID) {
			AccountabilityAccessor.RemoveNode(GetUser(), SEAT_ID);
		}
		
		/// <summary>
		/// Get the position attached to a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <returns></returns>
		//[GET/PUT/DELETE] /seats/{SEAT_ID}/position
		[Route("seats/{SEAT_ID}/position")]
		[HttpGet]
		public AngularPosition GetPosition(long SEAT_ID) {
			var node = AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID);
			if (node.AccountabilityRolesGroup.Position != null) {
				return new AngularPosition(AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID).AccountabilityRolesGroup.NotNull(x => x.Position));
			} else {
				throw new HttpException(404, "Seat does not contain a position.");
			}
		}
		
		
		/// <summary>
		/// Set the position for a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <param name="POSITION_ID">Position ID</param>
		[Route("seats/{SEAT_ID}/position/{POSITION_ID}")]
		[HttpPost]
		public void AttachPosition(long SEAT_ID, long POSITION_ID) {
			AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, POSITION_ID);
		}

		/// <summary>
		/// Remove position for a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID}/position")]
		[HttpDelete]
		public void RemovePosition(long SEAT_ID) {
			AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, null);
		}

		/// <summary>
		/// Get the user for a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <returns></returns>
		[Route("seats/{SEAT_ID}/user")]
		[HttpGet]
		public AngularUser GetSeatUser(long SEAT_ID) // Angular
		{
			var getUser = AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID).User;
			return AngularUser.CreateUser(getUser);
		}

		/// <summary>
		/// Set a user for a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		/// <param name="USER_ID">User ID</param>
		[Route("seats/{SEAT_ID}/user/{USER_ID}")]
		[HttpPost]
		public void AttachUser(long SEAT_ID, long USER_ID) {
			AccountabilityAccessor.SetUser(GetUser(), SEAT_ID, USER_ID);
		}
		
		/// <summary>
		/// Remove user from a seat
		/// </summary>
		/// <param name="SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID}/user")]
		[HttpDelete]
		public void DetachUser(long SEAT_ID) {
			AccountabilityAccessor.SetUser(GetUser(), SEAT_ID, null); // null userId for detaching 
		}



		/// <summary>
		/// Get seats for a user
		/// </summary>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		[Route("seats/user/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularAccountabilityNode> GetSeatsForUser(long USER_ID) {
			return AccountabilityAccessor.GetNodesForUser(GetUser(), USER_ID).Select(x => new AngularAccountabilityNode(x));
		}

		/// <summary>
		/// Get your seats 
		/// </summary>
		/// <returns></returns>
		[Route("seats/user/mine")]
		[HttpGet]
		public IEnumerable<AngularAccountabilityNode> GetSeatsForMe() {
			return AccountabilityAccessor.GetNodesForUser(GetUser(), GetUser().Id).Select(x => new AngularAccountabilityNode(x));
		}

	}


	[RoutePrefix("api/v1")]
	public class RoleController : BaseApiController {

		/// <summary>
		/// Get a particular role
		/// </summary>
		/// <param name="ROLE_ID"></param>
		/// <returns>The specified role</returns>
		//[GET/POST/DELETE] /roles/{id}
		[Route("role/{ROLE_ID}")]
		[HttpGet]
		public AngularRole GetRoles(long ROLE_ID) // Angular
		{
			return new AngularRole(RoleAccessor.GetRoleById(GetUser(), ROLE_ID));
		}

		/// <summary>
		/// Update a role
		/// </summary>
		/// <param name="ROLE_ID"></param>
		/// <param name="title">Updated role</param>
		/// <returns></returns>
		[Route("role/{ROLE_ID}")]
		[HttpPut]
		public async Task UpdateRoles(long ROLE_ID, [FromBody]TitleModel body) {
			await RoleAccessor.EditRole(GetUser(), ROLE_ID, body.title);
		}


		/// <summary>
		/// Remove a role from a position
		/// </summary>
		/// <param name="ROLE_ID">Role ID</param>
		/// <returns></returns>
		[Route("role/{ROLE_ID}")]
		[HttpDelete]
		public async Task RemoveRoles(long ROLE_ID) {
			AccountabilityAccessor.RemoveRole(GetUser(), ROLE_ID);
		}


	}

	[RoutePrefix("api/v1")]
	public class PositionController : BaseApiController {
		/// <summary>
		/// List all your positions at the organization
		/// </summary>
		/// <returns>A list of your positions</returns>
		//[GET] /positions/mine
		[Route("positions/mine")]
		[HttpGet]
		public AngularPosition[] GetMinePosition() {
			return PositionAccessor.GetPositionModelForUser(GetUser(), GetUser().Id).Select(x => new AngularPosition(x)).ToArray();

		}
		/// <summary>
		/// Get a list of roles for a particular position
		/// </summary>
		/// <param name="POSITION_ID"></param>
		//[GET/PUT] /positions/{id}/roles/
		[Route("positions/{POSITION_ID}/roles")]
		[HttpGet]
		public IEnumerable<AngularRole> GetPositionRoles(long POSITION_ID) {
			// do it later.
			return PositionAccessor.GetPositionRoles(GetUser(), POSITION_ID).Select(x => new AngularRole(x));

		}
		/// <summary>
		/// Create a role for a position
		/// </summary>
		/// <param name="POSITION_ID">Position ID</param>
		/// <param name="model">Role title</param>
		/// <returns>The created role</returns>
		[Route("positions/{POSITION_ID}/roles")]
		[HttpPost]
		public async Task<AngularRole> AddPositionRoles(long POSITION_ID, [FromBody] TitleModel body) // Angular
		{
			return new AngularRole(await AccountabilityAccessor.AddRole(GetUser(), new Attach(AttachType.Position, POSITION_ID), body.title));
		}

		/// <summary>
		/// Create a new position
		/// </summary>
		/// <returns></returns>
		//[PUT] /positions/
		[Route("positions/create")]
		[HttpPost]
		public async Task<AngularPosition> CreatePosition([FromBody]TitleModel body) {
			//need to discuss?
			OrganizationAccessor _accessor = new OrganizationAccessor();
			var position = await _accessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, body.title);
			return new AngularPosition(position);
		}

		/// <summary>
		/// Get a particular position
		/// </summary>
		/// <param name="POSITION_ID">Position ID</param>
		/// <returns>The specified position</returns>
		//[GET/POST] /positions/{id}
		[Route("positions/{POSITION_ID}")]
		[HttpGet]
		public AngularPosition GetPositions(long POSITION_ID) {
			return new AngularPosition(new OrganizationAccessor().GetOrganizationPosition(GetUser(), POSITION_ID));
		}

		/// <summary>
		/// Update a position
		/// </summary>
		/// <param name="POSITION_ID">Position ID</param>
		/// <param name="body">Position name</param>
		/// <returns></returns>
		[Route("positions/{POSITION_ID}")]
		[HttpPut]
		public async Task UpdatePositions(long POSITION_ID, [FromBody]TitleModel body) {
			OrganizationAccessor _accessor = new OrganizationAccessor();
			var position = await _accessor.EditOrganizationPosition(GetUser(), POSITION_ID, GetUser().Organization.Id, body.title);
			new AngularPosition(position);
		}


	}
}
