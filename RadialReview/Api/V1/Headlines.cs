using System;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Rocks;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using System.Threading.Tasks;
using RadialReview.Api.V0;

namespace RadialReview.Api.V1
{
    [RoutePrefix("api/v1")]
    public class HeadlinesController : BaseApiController
    {
		/// <summary>
		/// Get a specific people headline
		/// </summary>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <returns>The people headline</returns>
        //[GET/POST/DELETE] /headline/{id}
        [Route("headline/{HEADLINE_ID}")]
        [HttpGet]		
        public AngularHeadline GetHeadline(long HEADLINE_ID)
        {
            return new AngularHeadline(HeadlineAccessor.GetHeadline(GetUser(), HEADLINE_ID));
        }

		/// <summary>
		/// Update a People Headline
		/// </summary>
		/// <param name="HEADLINE_ID">People headline ID</param>
		/// <param name="title">Updated title</param>
		[Route("headline/{HEADLINE_ID}")]
        [HttpPut]
        public void UpdateHeadlines(long HEADLINE_ID, [FromBody]TitleModel body)
        {
            L10Accessor.UpdateHeadline(GetUser(), HEADLINE_ID, body.title);
        }

		/// <summary>
		/// Delete a people headline
		/// </summary>
		/// <param name="HEADLINE_ID"></param>
		/// <returns></returns>
		[Route("headline/{HEADLINE_ID}")]
        [HttpDelete]
        public async Task RemoveHeadlines(long HEADLINE_ID)
        {
			var nil_l10Id = 0; /*RecurrenceId is not needed*/
            await L10Accessor.Remove(GetUser(), new AngularHeadline() { Id = HEADLINE_ID }, nil_l10Id, null);
        }
    }
}