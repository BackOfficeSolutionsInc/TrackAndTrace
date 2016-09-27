using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace RadialReview.Accessors {
	public class SearchResult {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public long OrganizationId { get; set; }
		public string Organization { get; set; }
		public string Email { get; set; }
		public string ImageUrl { get; set; }
		public RGMType ResultType { get; set; }
	}
	public class SearchAccessor : BaseAccessor {
		public class SearchSelectors<T> {
			public SearchSelectors(RGMType resultType, bool forceLookupOrganizationName = false/*, bool or = false*/) {
				ResultType = resultType;
				LookupOrganizationName = forceLookupOrganizationName;
				ImageUrlTransform = (x) => x.ImageUrl;
				DescriptionTransform = (x) => x.Description;
				//Or = or;
			}

			public Expression<Func<T, long>> Id { get; set; }
			public Expression<Func<T, string>> Name { get; set; }
			public Expression<Func<T, long>> OrganizationId { get; set; }
			public Expression<Func<T, string>> Description { get; set; }
			public Expression<Func<T, string>> Organization { get; set; }
			public Expression<Func<T, string>> Email { get; set; }
			public Expression<Func<T, string>> ImageUrl { get; set; }
			public Func<SearchResult, string> ImageUrlTransform { get; set; }
			public Func<SearchResult, string> DescriptionTransform { get; set; }
			public RGMType ResultType { get; set; }
			public bool LookupOrganizationName { get; set; }
			public bool Or { get; set; }

			public ProjectionList ToProjectionList() {
				var list = Projections.ProjectionList();
				foreach (var e in GetExpressions()) {
					list.Add(Projections.Property(e));
				}
				return list;
			}

			private List<Expression<Func<T, object>>> GetExpressions() {
				var list = new List<Expression<Func<T, object>>>();
				if (Id != null)
					list.Add(Id.AddBox());
				if (Name != null)
					list.Add(Name.AddBox());
				if (OrganizationId != null)
					list.Add(OrganizationId.AddBox());
				if (Description != null)
					list.Add(Description.AddBox());
				if (Organization != null)
					list.Add(Organization.AddBox());
				if (Email != null)
					list.Add(Email.AddBox());
				if (ImageUrl != null)
					list.Add(ImageUrl.AddBox());
				return list;
			}

			private int GetIndex(Expression<Func<T, object>> exp) {
				var idx = 0;
				if (exp == null)
					return -1;
				foreach (var e in GetExpressions()) {
					if (e == exp)
						return idx;
					idx++;
				}
				return -1;
			}

			public List<U> GetAllFromExpression<U>(List<object[]> results, Expression<Func<T, U>> exp) {
				var idx = GetIndex(exp.AddBox());
				if (idx == -1)
					return null;
				return results.Select(x => {
					if (x[idx] == null)
						return default(U);
					return (U)x[idx];
				}).ToList();
			}

			private Dictionary<string, int> IndexLookup = null;

			private U GetField<U>(object[] obj, Expression<Func<T, U>> exp) {
				if (IndexLookup == null)
					IndexLookup = GetExpressions().Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1.ToString(), x => x.Item2);
				if (exp != null) {
					var boxed = exp.AddBox().ToString();
					if (IndexLookup.ContainsKey(boxed)) {
						return (U)obj[IndexLookup[boxed]];
					}
				}
				return default(U);
			}

			public List<SearchResult> ToSearchResults(List<object[]> results) {
				IndexLookup = null;
				return results.Select(x => {
					var res = new SearchResult() {
						Email = GetField(x, Email),
						Id = GetField(x, Id),
						Name = GetField(x, Name),
						Organization = GetField(x, Organization),
						OrganizationId = GetField(x, OrganizationId),
						Description = GetField(x, Description),
						ImageUrl = GetField(x, ImageUrl),
						ResultType = ResultType
					};
					var newDes = DescriptionTransform(res);
					var newImg = ImageUrlTransform(res);
					res.Description = newDes;
					res.ImageUrl = newImg;
					return res;
				}).ToList();
			}
		}

		public static List<SearchResult> SearchOrganizationUsers(UserOrganizationModel caller, long orgId, string search, bool nameOnly = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(orgId);

					var lookAt = new List<Expression<Func<UserLookup, object>>>();
					if (nameOnly)
						lookAt.Add(x => x.Name);

					return SearchUsersUnsafe(s, search, x =>
						  x.Add(Restrictions.IsNull(Projections.Property<UserLookup>(y => y.DeleteTime)))
						   .Add(Restrictions.Eq(Projections.Property<UserLookup>(y => y.OrganizationId), orgId)),
						  lookAt: lookAt.ToArray()
					);
				}
			}
		}

		public static List<SearchResult> SearchOrganizationRGM(UserOrganizationModel caller, long orgId, string search, bool or = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
					var users = SearchUsersUnsafe(s, search, x =>
							x.Add(Restrictions.IsNull(Projections.Property<UserLookup>(y => y.DeleteTime)))
							 .Add(Restrictions.Eq(Projections.Property<UserLookup>(y => y.OrganizationId), orgId)),
							lookAt: x => x.Name
					);
					var teams = SearchTeamsUnsafe(s, search, x =>
						   x.Add(Restrictions.IsNull(Projections.Property<OrganizationTeamModel>(y => y.DeleteTime)))
							.Add(Restrictions.Eq(Projections.Property<OrganizationTeamModel>(y => y.Organization.Id), orgId))
					);
					var positions = SearchPositionsUnsafe(s, search, x =>
						   x.Add(Restrictions.IsNull(Projections.Property<OrganizationTeamModel>(y => y.DeleteTime)))
							.Add(Restrictions.Eq(Projections.Property<OrganizationTeamModel>(y => y.Organization.Id), orgId))
					);
					var all = new List<SearchResult>();
					all.AddRange(users);
					all.AddRange(teams);
					all.AddRange(positions);
					return all;
				}
			}
		}

		public static List<SearchResult> AdminSearchAllUsers(UserOrganizationModel caller, string search) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RadialAdmin();
					return SearchUsersUnsafe(s, search, includeOrganizationName: true);
				}
			}
		}

		//public List<string> skipShortWords = new List<string>() {
		//	"the", 
		//	"and", 
		//	"for", 
		//	"are", 
		//	"but", 
		//	"not", 
		//	"you", 
		//	"all", 
		//	"any", 
		//	"can", 
		//	"her", 
		//	"was", 
		//	"one", 
		//	"our", 
		//	"out", 
		//	"day", 
		//	"get", 
		//	"has", 
		//	"him", 
		//	"his", 
		//	"how", 
		//	"man", 
		//	"new", 
		//	"now", 
		//	"old", 
		//	"see", 
		//	"two", 
		//	"way", 
		//	"who", 
		//	"boy", 
		//	"did", 
		//	"its", 
		//	"let", 
		//	"put", 
		//	"say", 
		//	"she", 
		//	"too", 
		//	"use", 
		//	"dad", 
		//	"mom",
		//};


		private static List<SearchResult> SearchAbstract<T>(ISession s, string search, Func<ICriteria, ICriteria> filter, SearchSelectors<T> selectors, params Expression<Func<T, object>>[] lookAt) where T : class {
			if (!lookAt.Any())
				throw new Exception("Must look at at least one field");


			var searches = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var criteria = s.CreateCriteria<T>();
			if (filter != null) {
				criteria = filter(criteria);
			}

			//if (selectors.Or) {
			//	//OR  search terms
				
			//	var disjunction = Restrictions.Disjunction();    // OR
			//	foreach (var term in searches) {
			//		if (term.Length >= 3) {
			//			foreach (var la in lookAt) {
			//				disjunction = (Disjunction)disjunction.Add(Restrictions.InsensitiveLike(Projections.Property<T>(la), term, MatchMode.Anywhere));
			//			}
			//		}
			//	}
			//	criteria = criteria.Add(disjunction);
			//} else {
				//AND search terms
				foreach (var term in searches) {
					var disjunction = Restrictions.Disjunction();    // OR
					foreach (var la in lookAt) {
						disjunction = (Disjunction)disjunction.Add(Restrictions.InsensitiveLike(Projections.Property<T>(la), term, MatchMode.Anywhere));
					}
					criteria = criteria.Add(disjunction);
				}
			//}
			var results = criteria.SetProjection(selectors.ToProjectionList()).List<object[]>().ToList();

			Dictionary<long, string> orgs = new Dictionary<long, string>();
			var output = selectors.ToSearchResults(results);
			if (selectors.LookupOrganizationName && selectors.OrganizationId != null && selectors.Organization == null) {
				var orgIds = selectors.GetAllFromExpression(results, selectors.OrganizationId);
				LocalizedStringModel nameAlias = null;
				orgs = s.QueryOver<OrganizationModel>()
					.WhereRestrictionOn(x => x.Id).IsIn(orgIds.Distinct().ToList())
					.JoinAlias(x => x.Name, () => nameAlias)
					.Select(x => x.Id, x => nameAlias.Standard)
					.List<object[]>()
					.ToDictionary(x => (long)x[0], x => "" + x[1]);
				output.ForEach(x => {
					x.Organization = orgs.GetOrDefault(x.OrganizationId, "");
				});
			}
			return output;
		}

		private static List<SearchResult> SearchTeamsUnsafe(ISession s, string search, Func<ICriteria, ICriteria> filter = null, bool includeOrganizationName = false) {
			var selectors = new SearchSelectors<OrganizationTeamModel>(RGMType.Team, includeOrganizationName) {
				Id = x => x.Id,
				OrganizationId = x => x.Organization.Id,
				Name = x => x.Name,
				ImageUrlTransform = x => ConstantStrings.AmazonS3Location + ConstantStrings.ImageGroupPlaceholder,
				DescriptionTransform = x => {
					var o = "Team";
					if (includeOrganizationName && !string.IsNullOrWhiteSpace(x.Organization))
						o += " at " + x.Organization;
					return o;
				}
			};
			return SearchAbstract(s, search, filter, selectors, x => x.Name);
		}

		private static List<SearchResult> SearchPositionsUnsafe(ISession s, string search, Func<ICriteria, ICriteria> filter = null, bool includeOrganizationName = false) {
			var selectors = new SearchSelectors<OrganizationPositionModel>(RGMType.Position, includeOrganizationName) {
				Id = x => x.Id,
				OrganizationId = x => x.Organization.Id,
				Name = x => x.CustomName,
				ImageUrlTransform = x => ConstantStrings.AmazonS3Location + ConstantStrings.ImagePositionPlaceholder,
				DescriptionTransform = x => {
					var o = "Position";
					if (includeOrganizationName && !string.IsNullOrWhiteSpace(x.Organization))
						o += " at " + x.Organization;
					return o;
				}
			};
			return SearchAbstract(s, search, filter, selectors, x => x.CustomName);
		}

		private static List<SearchResult> SearchUsersUnsafe(ISession s, string search, Func<ICriteria, ICriteria> filter = null, bool includeOrganizationName = false,/* bool or = false,*/ params Expression<Func<UserLookup, object>>[] lookAt) {
			var selectors = new SearchSelectors<UserLookup>(RGMType.User, includeOrganizationName) {
				Email = x => x.Email,
				Id = x => x.UserId,
				Description = x => x.Positions,
				OrganizationId = x => x.OrganizationId,
				Name = x => x.Name,
				ImageUrl = x => x._ImageUrlSuffix,
				ImageUrlTransform = x => UserLookup.TransformImageSuffix(x.ImageUrl),
				DescriptionTransform = x => {
					var o = x.Description;
					if (includeOrganizationName && !string.IsNullOrWhiteSpace(x.Organization))
						o += " at " + x.Organization;
					return o;
				}
			};
			if (!lookAt.Any())
				lookAt = new Expression<Func<UserLookup, object>>[] { x => x.Name, x => x.Email, x => x.Positions };
			return SearchAbstract<UserLookup>(s, search, filter, selectors, lookAt);
		}

	}
}