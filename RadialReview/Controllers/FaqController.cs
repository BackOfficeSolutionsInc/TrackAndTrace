using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using RadialReview.Properties;

namespace RadialReview.Controllers {
	public class FaqController : BaseController {
		//
		// GET: /Faq/
		[Access(AccessLevel.Any)]
		public ActionResult Index() {
			return View();
		}


		private List<ApiSection> sections;
		private ApiSection AddSection(string name) {
			var a = ApiSection.Create(name);
			sections.Add(a);
			return a;
		}

		[Access(AccessLevel.Any)]
		public PartialViewResult GenerateTokenInstruction() {
			return PartialView();
		}

		[Access(AccessLevel.Any)]
		public ActionResult API() {

			sections = new List<ApiSection>();
			AddSection("Scorecard")
				.AddFunc("List of scorecard measurables", new List<string>() { "GET /api/v0/measurables/mine/", "GET /api/v0/measurables/organization/", "GET /api/v0/measurables/owner/USER_ID" },
					new HtmlString("This function gets a list of scorecard measurable."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/measurables/mine'"),
					@"[{Id:5,Title:""A measurable"",GoalDirection:""GreaterThan"",Goal:1,UnitType:""None"",AccountableUser:{Id:123,Name:""John Doe"",Username:""john.doe@organization.com""},AdminUser:{Id:345,Name:""Han Solo"",Username:""han.solo@organization.com""}},{Id:7,Title:""Anothermeasurable"",GoalDirection:""GreaterThan"",Goal:10,UnitType:""Dollars"",AccountableUser:{Id:123,Name:""JohnDoe"",Username:""john.doe@organization.com""},AdminUser:{Id:123,Name:""JohnDoe"",Username:""john.doe@organization.com""}},{...}]",
					new ApiSection.ApiParam("mine", "Get your scorecard measurables"), new ApiSection.ApiParam("organization", "Get scorecard measurables for your organization"), new ApiSection.ApiParam("USER_ID", "Get scorecard measurables for the specified user"))
				.AddFunc("Get a scorecard measurable", "GET /api/v0/measurables/MEASURABLE_ID",
					new HtmlString("This function gets a specific scorecard measurable."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/measurables/<b>5</b>'"),
					@"{Id:5,Title:""A measurable"",GoalDirection:""GreaterThan"",Goal:1,UnitType:""None"",AccountableUser:{Id:123,Name:""John Doe"",Username:""john.doe@organization.com""},AdminUser:{Id:345,Name:""Han Solo"",Username:""han.solo@organization.com""}}",
					new ApiSection.ApiParam("MEASURABLE_ID", "The scorecard measurable id"))
				.AddFunc("List scores", "GET /api/v0/measurables/MEASURABLE_ID/scores",
					new HtmlString("Gets scores for a specific scorecard measurable."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/measurables/5/scores'"),
					@"[{""Id"": 31,""MeasurableId"": 5,""ForWeekNumber"": 2352,""Value"": 1},{""Id"": 32,""MeasurableId"": 5,""ForWeekNumber"": 2353,""Value"": 1},{...}]",
					new ApiSection.ApiParam("MEASURABLE_ID", "The scorecard measurable id"))
				.AddFunc("Edit a score", new List<string>() { "PUT /api/v0/scores/MEASURABLE_ID/FOR_WEEK", "PUT /api/v0/scores/SCORE_ID/", },
					new HtmlString("This function sets a particular score. You must specify <b>Content-Type: application/json</b>"),
					new HtmlString(@"curl -i -X PUT -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" -H ""Content-Type:application/json"" -d '15.50' '" + ProductStrings.BaseUrl2 + "api/v0/scores/5/2352'"),
					@"202 - OK",
					new ApiSection.ApiParam("SCORE_ID", "Score id"),
					new ApiSection.ApiParam("MEASURABLE_ID", "The scorecard measurable id"),
					new ApiSection.ApiParam("FOR_WEEK", "Week id"),
					new ApiSection.ApiParam("body", "The new value as a decimal"))
				.AddFunc("Current Week", "GET /api/v0/week/current",
					new HtmlString("This function gets the current week id."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/week/current'"),
					@"{StartDate: ""2015-12-27T06:00:00Z"",EndDate: ""2016-01-03T06:00:00Z"",ForWeek: 2401}");

			AddSection("Users")
				.AddFunc("List users at organization", "GET /api/v0/users/organization/",
					new HtmlString("Gets a list of all the organization's users."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/users/organization/'"),
					@"[{Id:123,Name:""John Doe"",Username:""john.doe@organization.com""},{Id:345,Name:""Han Solo"",Username:""han.solo@organization.com""},{...}]")
				.AddFunc("List users you manage", "GET /api/v0/users/managing/",
					new HtmlString("Gets a list of all the users you manage."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/users/managing/'"),
					@"[{Id:123,Name:""John Doe"",Username:""john.doe@organization.com""},{Id:345,Name:""Han Solo"",Username:""han.solo@organization.com""},{...}]")
				.AddFunc("Get a user", "GET /api/v0/users/USER_ID/",
					new HtmlString("Get information about a user."),
					new HtmlString(@"curl -i -X GET -H ""Authorization:Bearer <b>YOUR_BEARER_TOKEN_HERE</b>"" '" + ProductStrings.BaseUrl2 + "api/v0/users/123/'"),
					@"{Id:123,Name:""John Doe"",Username:""john.doe@organization.com""}");


			return View(sections);
		}



		public class ApiSection {
			public static ApiSection Create(String name) {
				var found = new ApiSection() {
					Name = name,
					Functions = new List<ApiFunction>(),
					Anchor = name.Replace(" ", "-")
				};
				return found;
			}
			public ApiSection AddFunc(string name, string url, HtmlString details, HtmlString request, string response, params ApiParam[] parameters) {
				var func = new ApiFunction() {
					Name = name,
					Anchor = name.Replace(" ", "-"),
					URL = new List<string>() { url },
					Details = details,
					Parameters = parameters.ToList(),
					Request = request,
					Response = new HtmlString(response)
				};
				Functions.Add(func);
				return this;
			}
			public ApiSection AddFunc(string name, List<string> urls, HtmlString details, HtmlString request, string response, params ApiParam[] parameters) {
				var func = new ApiFunction() {
					Name = name,
					Anchor = name.Replace(" ", "-"),
					URL = urls,
					Details = details,
					Parameters = parameters.ToList(),
					Request = request,
					Response = new HtmlString(response)
				};
				Functions.Add(func);
				return this;
			}


			public string Name { get; set; }
			public string Anchor { get; set; }
			public List<ApiFunction> Functions { get; set; }

			public class ApiFunction {
				public string Name { get; set; }
				public string Anchor { get; set; }
				public List<string> URL { get; set; }
				public HtmlString Details { get; set; }

				public List<ApiParam> Parameters { get; set; }

				public HtmlString Request { get; set; }
				public HtmlString Response { get; set; }

			}

			public class ApiParam {
				public String Name { get; set; }
				public String Details { get; set; }

				public ApiParam(string name, string details) {
					Name = name;
					Details = details;
				}
			}

		}
	}
}