﻿using System;
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

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class PositionController : BaseApiController
    {

        //[GET] /positions/mine
        [Route("positions/mine")]
        [HttpGet]
        public IEnumerable<AngularPosition> GetMinePosition()
        {
            return PositionAccessor.GetPositionModelForUser(GetUser(), GetUser().Id).Select(x => new AngularPosition(x));

        }

        //[GET/PUT] /positions/{id}/roles/
        [Route("positions/{id}/roles")]
        [HttpGet]
        public void GetPositionRoles(long id)
        {
            // do it later.
        }

        [Route("positions/{id}/roles")]
        [HttpPut]
        public async Task<AngularRole> AddPositionRoles(long id, [FromBody]string name) // Angular
        {
            return new AngularRole(await AccountabilityAccessor.AddRole(GetUser(), new Models.Enums.Attach(Models.Enums.AttachType.Position, id), name));
        }

        //[PUT] /positions/
        [Route("positions/")]
        [HttpPut]
        public AngularPosition CreatePosition([FromBody] string name)
        {
            //need to discuss?
            OrganizationAccessor _accessor = new OrganizationAccessor();
            return new AngularPosition(_accessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, name));
        }

        //[GET/POST] /positions/{id}
        [Route("positions/{id}")]
        [HttpGet]
        public AngularPosition GetPositions(long id)
        {
            return new AngularPosition(new OrganizationAccessor().GetOrganizationPosition(GetUser(), id));
        }

        [Route("positions/{id}")]
        [HttpPost]
        public AngularPosition UpdatePositions([FromBody] string name, long id)
        {
            OrganizationAccessor _accessor = new OrganizationAccessor();
            return new AngularPosition(_accessor.EditOrganizationPosition(GetUser(), id, GetUser().Organization.Id, name));
        }


    }
}
