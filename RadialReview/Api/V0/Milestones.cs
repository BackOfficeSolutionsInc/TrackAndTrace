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
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Rocks;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class MilestonesController : BaseApiController
    {

        //[GET/POST/DELETE] /milestones/{id}
        [Route("milestones/{id}")]
        [HttpGet]
        public AngularMilestone GetMilestones(long id)
        {
            return new AngularMilestone(RockAccessor.GetMilestone(GetUser(), id));
        }

        [Route("milestones/{id}")]
        [HttpPost]
        public void UpdateMilestones(long id, [FromBody]string name = null, [FromBody]DateTime? dueDate = null, [FromBody]MilestoneStatus? status = null)
        {
            RockAccessor.EditMilestone(GetUser(), id, name, dueDate, null, status);
        }

        [Route("milestones/{id}")]
        [HttpDelete]
        public void RemoveMilestones(long id)
        {
            RockAccessor.DeleteMilestone(GetUser(), id);
        }


    }
}