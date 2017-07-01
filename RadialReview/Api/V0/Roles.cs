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
using RadialReview.Models.Angular.Roles;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class RoleController : BaseApiController
    {

        //[GET/POST/DELETE] /roles/{id}
        [Route("roles/{id}")]
        [HttpGet]
        public AngularRole GetRoles(long id) // Angular
        {
            return new AngularRole(RoleAccessor.GetRoleById(GetUser(), id));
        }

        [Route("roles/{id}")]
        [HttpPost]
        public async Task UpdateRoles([FromBody]string name, long id)
        {
            await RoleAccessor.EditRole(GetUser(), id, name);
        }

        [Route("roles/{id}")]
        [HttpDelete]
        public void RemoveRoles(long id)
        {
            AccountabilityAccessor.RemoveRole(GetUser(), id);
        }

    }
}
