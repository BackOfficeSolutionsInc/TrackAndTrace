using System.Web;
using System.Web.Optimization;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview
{
	public class BundleConfig
	{
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundles/jquery")
				.Include("~/Scripts/jquery-{version}.js")
				.Include("~/Scripts/jquery.unobtrusive-ajax.js")
				);

			bundles.Add(new ScriptBundle("~/bundles/animations")
				.Include("~/Scripts/animations/*.js")
				.Include("~/Scripts/jquery/*.js")
				);

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
						"~/Scripts/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
						"~/Scripts/modernizr-*"));

			bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
					  "~/Scripts/components/posneg.js",
					  "~/Scripts/components/tristate.js",
					  "~/Scripts/components/fivestate.js",
					  "~/Scripts/components/checktree.js",
					  "~/Scripts/components/rockstate.js",
					  "~/Scripts/components/approvereject.js",
					  "~/Scripts/components/completeincomplete.js",
					  "~/Scripts/select2.min.js",
					  "~/Scripts/bootstrap.js",
					  "~/Scripts/respond.js",
					  "~/Scripts/bootstrap-slider.js",
					  "~/Scripts/bootstrap-datepicker.js"));

			bundles.Add(new StyleBundle("~/Content/css").Include(
					  "~/Content/components/posneg.css",
					  "~/Content/components/tristate.css",
					  "~/Content/components/fivestate.css",
					  "~/Content/components/table.css",
					  "~/Content/components/checktree.css",
					  "~/Content/components/rockstate.css",
					  "~/Content/components/approvereject.css",
					  "~/Content/components/CompleteIncomplete.css",
					  "~/Content/select2-bootstrap.css",
					  "~/Content/select2.css",
					  "~/Content/datepicker.css",
					  "~/Content/bootstrap.css",
					  "~/Content/slider.css",
					  "~/Content/site.css",
					  "~/Content/Fonts.css"));

			bundles.Add(new StyleBundle("~/Content/L10").Include(
					"~/Content/L10/L10.css",
					"~/Content/L10/L10Todo.css",
					"~/Content/L10/L10Stats.css",
					"~/Content/L10/L10Rocks.css",
					"~/Content/L10/L10IDS.v2.css",
					"~/Content/L10/L10Scorecard.css"
				));

			bundles.Add(new ScriptBundle("~/bundles/L10").Include(
					"~/Scripts/L10/L10.js",
					"~/Scripts/L10/rtL10.js",
					"~/Scripts/L10/L10Ids.js",
					"~/Scripts/L10/L10Todo.js",
					"~/Scripts/L10/L10Rocks.js",
					"~/Scripts/L10/L10Scorecard.js",
					"~/Scripts/L10/jquery.sortable.js"
				));

			bundles.Add(new ScriptBundle("~/bundles/main").Include(
					  "~/Scripts/Main/radial.js",
					//"~/Scripts/jquery.signalR-{version}.js",
					  "~/Scripts/jquery/jquery.qtip.js",
					  "~/Scripts/jquery/jquery.tablesorter.js",
					  "~/Scripts/Main/finally.js",
					  "~/Scripts/L10/jquery-ui.color.js"
			));
			BundleTable.EnableOptimizations = Config.OptimizationEnabled();
			/*
#if DEBUG 
			BundleTable.EnableOptimizations = false;
#else
			BundleTable.EnableOptimizations = true;
#endif
			 */
		}
	}
}
