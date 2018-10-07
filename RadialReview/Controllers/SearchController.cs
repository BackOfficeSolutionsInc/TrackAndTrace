using RadialReview.Accessors;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class SearchController : BaseController
    {


        [Access(AccessLevel.Radial)]
        public JsonResult AdminAllUsers(string search)
        {
			//AllowAdminsWithoutAudit();
            return Json(SearchAccessor.AdminSearchAllUsers(GetUser(), search), JsonRequestBehavior.AllowGet);

            //using (var s = HibernateSession.GetCurrentSession()) {
            //	using (var tx = s.BeginTransaction()) {

            //		//OrganizationModel orgAlias = null;
            //		//UserLookup cacheAlias = null;
            //		LocalizedStringModel nameAlias = null;

            //		var searches = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //		var criteria = s.CreateCriteria<UserLookup>();
            //		foreach (var term in searches) {
            //			criteria = criteria.Add(Restrictions.Disjunction() // OR
            //				.Add(Restrictions.InsensitiveLike(Projections.Property<UserLookup>(x => x.Name), term, MatchMode.Anywhere))
            //				.Add(Restrictions.InsensitiveLike(Projections.Property<UserLookup>(x => x.Email), term, MatchMode.Anywhere))
            //				.Add(Restrictions.InsensitiveLike(Projections.Property<UserLookup>(x => x.Positions), term, MatchMode.Anywhere)));
            //		}

            //		var users = criteria.SetProjection(Projections.ProjectionList()
            //			.Add(Projections.Property<UserLookup>(x => x.Email))
            //			.Add(Projections.Property<UserLookup>(x => x.UserId))
            //			.Add(Projections.Property<UserLookup>(x => x.Positions))
            //			.Add(Projections.Property<UserLookup>(x => x.OrganizationId))
            //			.Add(Projections.Property<UserLookup>(x => x.Name))
            //		).List<object[]>();

            //		var orgs = s.QueryOver<OrganizationModel>()
            //			.WhereRestrictionOn(x => x.Id).IsIn(users.Select(x => (long)x[3]).Distinct().ToList())
            //			.JoinAlias(x => x.Name, () => nameAlias)
            //			.Select(x => x.Id, x => nameAlias.Standard)
            //			.List<object[]>()
            //			.ToDictionary(x => (long)x[0], x => "" + x[1]);

            //		//var users = s.QueryOver<UserOrganizationModel>()
            //		//                   .Where(x => x.DeleteTime == null)
            //		//                   .WhereRestrictionOn(c => c.EmailAtOrganization).IsLike("%" + search + "%")
            //		//	.JoinAlias(x => x.Organization, () => orgAlias)
            //		//	.JoinAlias(x => x.Cache, () => cacheAlias)
            //		//	.Select(x => x.EmailAtOrganization, x => x.Id,x=> cacheAlias.Positions, x=> nameAlias.Standard,x=>cacheAlias.Name)

            //		//                   .List<object[]>().ToList();
            //		return Json(new {
            //			results = users.Select(x => new {
            //				text = "" + x[0],
            //				value = "" + x[1],
            //				position = "" + x[2],
            //				organization = orgs.GetOrDefault((long)x[3], "<>"),
            //				name = "" + x[4]
            //			}).ToArray()
            //		}, JsonRequestBehavior.AllowGet);


        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult Users(string search, bool allfields = false)
        {
            return Json(SearchAccessor.SearchOrganizationUsers(GetUser(), GetUser().Organization.Id, search, !allfields), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult RGM(string search, bool includeTerm = false)
        {
            var res = SearchAccessor.SearchOrganizationRGM(GetUser(), GetUser().Organization.Id, search);

            if (includeTerm)
            {
                res.Add(new SearchResult()
                {
                    Description = "",
                    Id = -DateTime.UtcNow.ToJavascriptMilliseconds(),
                    ImageUrl = ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder,
                    Name = search.ToTitleCase(),
                    OrganizationId = -1,
                    ResultType = Models.Enums.RGMType.SearchResult,
                    Organization = "",
                    Email = ""
                });
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }
    }
}