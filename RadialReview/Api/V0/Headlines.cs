using System;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Rocks;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using System.Threading.Tasks;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class HeadlinesController : BaseApiController
    {
        // Put method is in L10 for Adding headline

        //[GET/POST/DELETE] /headline/{id}
        [Route("headline/{id}")]
        [HttpGet]
        public AngularHeadline GetHeadlines(long id)
        {
            return new AngularHeadline(HeadlineAccessor.GetHeadline(GetUser(), id));
        }

        [Route("headline/{id}")]
        [HttpPost]
        public void UpdateHeadlines(long id, [FromBody]string message)
        {
            L10Accessor.UpdateHeadline(GetUser(), id, message);
        }       

        [Route("headline/{id}")]
        [HttpDelete]
        public async Task RemoveHeadlines(long id)
        {
            await L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = id }, 0, null);
        }
    }
}