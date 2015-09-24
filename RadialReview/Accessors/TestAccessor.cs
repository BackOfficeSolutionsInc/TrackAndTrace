using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.UI.WebControls;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using RadialReview.Controllers;
using RadialReview.Models.Tests;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class TestAccessor
	{

		public void AddUrlTest(string url, int expectedCode, params long[] forUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				if (forUsers.Length == 0)
					throw new Exception("At least one user required.");

				using (var tx = s.BeginTransaction())
				{
					foreach (var id in forUsers)
						s.Save(new TestUrl()
						{
							Active = true,
							AsUserId = id,
							ExpectedCode = expectedCode,
							Url = url,
						});
					tx.Commit();
					s.Flush();
				}
			}
		}


		public class MvcProcessor : MvcHandler
		{
			public MvcProcessor(RequestContext ctx):base(ctx){}

			public void Process(HttpContext ctx)
			{
				ProcessRequest(ctx);
			}
		}

		public class MvcIdentity : IIdentity,IUser{
			public string Name { get; set; }
			public string AuthenticationType { get; set; }
			public bool IsAuthenticated { get; set; }
			public string Id { get; set; }
			public string UserName { get; set; }
		}

		public async Task<TestUrlResult> RunTest(string userId,TestUrlBatch batch,string server, TestUrl url)
		{
			var result = new TestUrlResult()
			{
				Batch = batch,
				TestUrl = url,
			};

			var statusCode=-1;
			try
			{
				await Task.Delay(50);
				//var webClient = new WebClient();
				var fullUrl = (server.TrimEnd('/') + "/" + url.Url.TrimStart('/'));
				//var str = await webClient.DownloadStringTaskAsync(new Uri(fullUrl, UriKind.Absolute));

				var uri = new Uri(fullUrl, UriKind.Absolute);
				var httpRequest = new HttpRequest(string.Empty, uri.ToString(), uri.Query.TrimStart('?'));
				var stringWriter = new StringWriter();
				var httpResponse = new HttpResponse(stringWriter);
				var httpContext = new HttpContext(httpRequest, httpResponse);

				var identity= new GenericIdentity("<Test User>");
				identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
				httpContext.User = new ClaimsPrincipal(identity);


				var context = new HttpContextWrapper(httpContext);

				var handler = new MvcProcessor(
					new RequestContext(context,
					RouteTable.Routes.GetRouteData(context))
				);

				handler.Process(httpContext);

				statusCode = handler.RequestContext.HttpContext.Response.StatusCode;
				result.HttpCode = statusCode;


			}
			catch (WebException ex)
			{
				statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
				result.HttpCode = statusCode;
				result.Error = ex.Message;

			}
			catch (Exception e)
			{
				result.Passed = false;
				result.HttpCode = -1;
				result.Error = e.Message;
			}
			result.EndTime = DateTime.UtcNow;
			result.DurationMs = (result.EndTime.Value - result.StartTime).TotalMilliseconds;
			result.Passed = (result.HttpCode == url.ExpectedCode);

			return result;
		}


		public async Task<TestResults> RunAllUrlTests(string userId)
		{
			List<TestUrl> urls = null;
			TestUrlBatch batch = null;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					urls = s.QueryOver<TestUrl>().Where(x => x.DeleteTime == null && x.Active).List().ToList();
					batch = new TestUrlBatch();
					s.Save(batch);
					tx.Commit();
					s.Flush();
				}
			}
			var server = Config.BaseUrl(null);
			var results = new List<TestUrlResult>();
			foreach (var url in urls)
			{
				var result = await RunTest(userId,batch, server, url);
				results.Add(result);
			}

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var res in results)
					{
						s.Save(res);
					}
					var batchResolved = s.Get<TestUrlBatch>(batch.Id);
					batchResolved.Passed = results.Count(x => x.Passed);
					batchResolved.Failed = results.Count(x => !x.Passed);
					batchResolved.CompleteTime = DateTime.UtcNow;
					s.Update(batchResolved);

					batch = batchResolved;

					var history =s.QueryOver<TestUrlResult>().Where(x => x.Batch.Id > batchResolved.Id - 10).List().ToList();
					results.ForEach(x => {
						x._History = history.Where(y => y.TestUrl.Id == x.TestUrl.Id).OrderBy(y => y.Batch.CreateTime).ToList();
					});


					

					tx.Commit();
					s.Flush();
				}
			}


			return new TestResults(){
				Results = results,
				Batch = batch,
			};
		}
	}
}