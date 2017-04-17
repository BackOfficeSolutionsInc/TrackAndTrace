using System;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Rocks;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using System.Collections.Generic;

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class SearchController : BaseApiController
    {

        //[GET] /search?term={term}
        [Route("search/all")]
        [HttpGet]
        public IEnumerable<SearchResult> Search(string term)
        {
            return SearchAccessor.SearchOrganizationRGM(GetUser(), GetUser().Organization.Id, term);
        }

        //[GET] /search/user? term = { term }
        [Route("search/user")]
        [HttpGet]
        public IEnumerable<SearchResult> SearchUser(string term)
        {
            return SearchAccessor.SearchOrganizationUsers(GetUser(), GetUser().Organization.Id, term, true);
        }
    }
}