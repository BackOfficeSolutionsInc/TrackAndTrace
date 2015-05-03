using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace RadialReview.Utilities
{
	public class ViewUtility
	{
		public class ViewRenderer
		{
			public ViewEngineResult ViewEngineResult { get; set; }
			public object Model {
				get { return ControllerContext.Controller.ViewData.Model; }
				set { ControllerContext.Controller.ViewData.Model=value; }
			}
			public ViewDataDictionary ViewData
			{
				get { return ControllerContext.Controller.ViewData; }
				set { ControllerContext.Controller.ViewData = value; }
			}
			public ControllerContext ControllerContext { get; set; }
			private bool Partial { get; set; }

			public ViewRenderer(ViewEngineResult viewEngineResult, ControllerContext context, bool partial)
			{
				ViewEngineResult = viewEngineResult;
				ControllerContext = context;
				Partial = partial;
			}

			public string Execute()
			{
				string result = null;
				using (var sw = new StringWriter()){
					var view = ViewEngineResult.View;
					var ctx = new ViewContext(ControllerContext, view, ControllerContext.Controller.ViewData, ControllerContext.Controller.TempData, sw);
					view.Render(ctx, sw);
					result = sw.ToString();
				}
				return result;
			}

			public override string ToString()
			{
				return Execute();
			}
		}

		public static ViewRenderer RenderPartial(string viewPath, object model = null){
			return Render(viewPath, model, true);
		}

		public static ViewRenderer RenderView(string viewPath, object model = null)
		{
			return Render(viewPath, model, false);
		}

		private static ViewRenderer Render(string viewPath, object model = null, bool partial = false)
		{
			var controller = CreateController<GenericController>();
			var context = controller.ControllerContext;
			ViewEngineResult viewEngineResult = null;
			viewEngineResult = partial ? ViewEngines.Engines.FindPartialView(context, viewPath) : ViewEngines.Engines.FindView(context, viewPath, null);

			if (viewEngineResult == null)
				throw new FileNotFoundException("View cannot be found.");
			context.Controller.ViewData.Model = model;
			return new ViewRenderer(viewEngineResult, controller.ControllerContext, false);
		}

		private static T CreateController<T>(RouteData routeData = null) where T : Controller, new()
		{
			// create a disconnected controller instance
			var controller = new T();

			// get context wrapper from HttpContext if available
			HttpContextBase wrapper;
			if (System.Web.HttpContext.Current != null)
				wrapper = new HttpContextWrapper(System.Web.HttpContext.Current);
			else
				throw new InvalidOperationException(
					"Can't create Controller Context if no " +
					"active HttpContext instance is available.");

			if (routeData == null)
				routeData = new RouteData();

			// add the controller routing if not existing
			if (!routeData.Values.ContainsKey("controller") &&
				!routeData.Values.ContainsKey("Controller"))
				routeData.Values.Add("controller",
									 controller.GetType()
											   .Name.ToLower().Replace("controller", ""));

			controller.ControllerContext = new ControllerContext(wrapper, routeData, controller);
			return controller;
		}
		
		// *any* controller class will do for the template
		private class GenericController : Controller
		{ }
	}

	
}