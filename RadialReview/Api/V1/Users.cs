using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using RadialReview.Models.Angular.Users;

namespace RadialReview.Api.V1 {
    [RoutePrefix("api/v1")]
    public class Users_Controller :BaseApiController {
        [Route("users/{USER_ID:long}")]
        [HttpGet]
        public AngularUser GetUser(long USER_ID) {
            return AngularUser.CreateUser(UserAccessor.GetUserOrganization(GetUser(), USER_ID, false, false));
        }
        [Route("users/mine")]
        [HttpGet]
        public AngularUser GetMineUser() {
            return GetUser(GetUser().Id);
        }
    }
}