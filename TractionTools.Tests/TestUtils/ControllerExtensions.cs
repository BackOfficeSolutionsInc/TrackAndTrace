using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;
using RadialReview;
using RadialReview.Api.V0;
using RadialReview.Controllers;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace TractionTools.Tests.TestUtils {
	public class ControllerCtx : IDisposable {
		private BaseController Controller { get; set; }
		private UserOrganizationModel Caller { get; set; }
		public ControllerCtx(UserOrganizationModel caller) {
			Caller = caller;
		}
		public T Get<T>() where T : BaseController, new() {
			if (Controller != null) {
				if (Controller is T) {
					return (T)Controller;
				} else {
					throw new ArgumentException("Controller type incorrect", "T");
				}
			} else {
				Controller = new T();
				Controller.MockUser(Caller);
				return (T)Controller;
			}
		}
		public void Dispose() {
			Controller.MockUser(null);
		}
	}
	public class ControllerCtx<CTRL> : IDisposable where CTRL : BaseController, new() {

		private BaseController Controller { get; set; }
		private UserOrganizationModel Caller { get; set; }
		public ControllerCtx(UserOrganizationModel caller) {
			Caller = caller;
		}

		public bool TransformAngular = false;
		protected CTRL Get() {
			if (Controller != null) {
				if (Controller is CTRL) {
					return (CTRL)Controller;
				} else {
					throw new ArgumentException("Controller type incorrect", "T");
				}
			} else {
				Controller = new CTRL();
				Controller.MockUser(Caller);
				Controller.SetValue("TransformAngular", TransformAngular);
				return (CTRL)Controller;
			}
		}
		public async Task<AR> Get<AR>(Func<CTRL, Task<AR>> method) where AR : ActionResult {
			var ctrl = Get();
			if (HttpContext.Current != null && HttpContext.Current.Items != null) {
				HttpContext.Current.Items["NHibernateSession"] = null;
			}
			var result = await method(ctrl);
			if (HttpContext.Current != null && HttpContext.Current.Items != null && HttpContext.Current.Items["NHibernateSession"] != null) {
				HibernateSession.CloseCurrentSession();
			}
			if (result == null)
				throw new ArgumentNullException("Output was null");
			if (!(result is AR))
				throw new TypeLoadException("Output was not of type " + typeof(AR).Name);
			return result;
		}

		public AR Get<AR>(Func<CTRL, AR> method) where AR : ActionResult {
			var ctrl = Get();
			if (HttpContext.Current != null && HttpContext.Current.Items != null) {
				HttpContext.Current.Items["NHibernateSession"] = null;
			}
			var result = method(ctrl);
			if (HttpContext.Current != null && HttpContext.Current.Items != null && HttpContext.Current.Items["NHibernateSession"] != null) {
				HibernateSession.CloseCurrentSession();
			}
			if (result == null)
				throw new ArgumentNullException("Output was null");
			if (!(result is AR))
				throw new TypeLoadException("Output was not of type " + typeof(AR).Name);
			return result;
		}


		public ViewResult GetView<AR>(Func<CTRL, AR> method) where AR : ActionResult {
			return Get(x => method(x) as ViewResult);
		}
		public JsonResult GetJson<AR>(Func<CTRL, AR> method) where AR : ActionResult {
			return Get(x => method(x) as JsonResult);
		}
		public PartialViewResult GetPartial<AR>(Func<CTRL, AR> method) where AR : ActionResult {
			return Get(x => method(x) as PartialViewResult);
		}
		public RedirectToRouteResult GetRedirect<AR>(Func<CTRL, AR> method) where AR : ActionResult {
			return Get(x => method(x) as RedirectToRouteResult);
		}

		public async Task<ViewResult> GetView<AR>(Func<CTRL, Task<AR>> method) where AR : ActionResult {
			return await Get(async x => (await method(x)) as ViewResult);
		}
		public async Task<JsonResult> GetJson<AR>(Func<CTRL, Task<AR>> method) where AR : ActionResult {
			return await Get(async x => (await method(x)) as JsonResult);
		}
		public async Task<PartialViewResult> GetPartial<AR>(Func<CTRL, Task<AR>> method) where AR : ActionResult {
			return await Get(async x => (await method(x)) as PartialViewResult);
		}
		public async Task<RedirectToRouteResult> GetRedirect<AR>(Func<CTRL, Task<AR>> method) where AR : ActionResult {
			return await Get(async x => (await method(x)) as RedirectToRouteResult);
		}



		public void Dispose() {
			Controller.MockUser(null);
		}
	}

	public static class ActionResultExtensions {
		public static T GetModel<T>(this ActionResult view) {
			if (view is ViewResultBase) {
				return GetModel<T>((ViewResultBase)view);
			} else if (view is JsonResult) {
				return GetModel<T>((JsonResult)view);
			}
			throw new ArgumentOutOfRangeException("Unknown model type");
		}
		public static void AssertModelType<T>(this ActionResult view) {
			if (view is ViewResultBase) {
				AssertModelType<T>((ViewResultBase)view);
			} else if (view is JsonResult) {
				AssertModelType<T>((JsonResult)view);
			} else {
				throw new ArgumentOutOfRangeException("Unknown model type");
			}
		}
		public static T GetModel<T>(this ViewResultBase view) {
			AssertModelType<T>(view);
			return (T)view.Model;
		}
		public static T GetModel<T>(this JsonResult view) {
			AssertModelType<T>(view);
			if (view.Data is ResultObject)
				return (T)((ResultObject)view.Data).Object;
			if (view.Data is IAngular)
				return (T)view.Data;
			throw new ArgumentOutOfRangeException("Unknown model type");
		}

		public static void AssertModelType<T>(this ViewResultBase view) {
			Assert.IsNotNull(view, "View was null");
			Assert.IsNotNull(view.Model, "Model was null");
			Assert.IsInstanceOfType(view.Model, typeof(T));
		}
		public static void AssertModelType<T>(this JsonResult view) {
			Assert.IsNotNull(view, "Json was null");
			Assert.IsNotNull(view.Data, "Model was null");
			if (view.Data is ResultObject) {
				Assert.IsInstanceOfType(view.Data, typeof(ResultObject));
				Assert.IsInstanceOfType(((ResultObject)view.Data).Object, typeof(T));
			} else if (view.Data is IAngular) {
				Assert.IsInstanceOfType(view.Data, typeof(T));
			} else {
				throw new ArgumentOutOfRangeException("Unknown model type");
			}
		}
	}


	public static class ControllerExtensions {

		public static void MockUser(this BaseApiController controller, UserOrganizationModel user) {
			user._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			controller.SetValue("MockUser", user);
		}

		public static void MockUser(this BaseController controller, UserOrganizationModel user) {
			controller.SetValue("MockUser", user);
		}

		public static void SetupRequest(this BaseController controller, UserOrganizationModel user) {
			controller.MockUser(user);
			var moqRequest = new Mock<HttpRequestBase>();
			var request = new Mock<HttpRequestBase>();
			// Not working - IsAjaxRequest() is static extension method and cannot be mocked
			// request.Setup(x => x.IsAjaxRequest()).Returns(true /* or false */);
			// use this
			//request.SetupGet(x => x.Headers).Returns(
			//    new System.Net.WebHeaderCollection {
			//        {"X-Requested-With", "XMLHttpRequest"}
			//    });

			var context = new Mock<HttpContextBase>();
			context.SetupGet(x => x.Request).Returns(request.Object);

			controller.ControllerContext = new ControllerContext(context.Object, new RouteData(), controller);
		}
	}

}
