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
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Rocks;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class RocksController : BaseApiController
    {

        //[GET/PUT] /rocks/{id}/milestones
        [Route("rocks/{id}/milestones")]
        [HttpGet]
        public IEnumerable<AngularMilestone> GetRocksMilestones(long id)
        {
            return RockAccessor.GetMilestonesForRock(GetUser(), id).Select(x => new AngularMilestone(x));
        }

        [Route("rocks/{id}/milestones")]
        [HttpPut]
        public AngularMilestone AddRocksMilestones(long id, [FromBody]string milestone, [FromBody]DateTime dueDate)
        {
            return new Models.Angular.Rocks.AngularMilestone(RockAccessor.AddMilestone(GetUser(), id, milestone, dueDate));
        }
    }
}