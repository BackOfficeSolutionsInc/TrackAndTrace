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
				.Include("~/Scripts/jquery/jquery.qtip.js")
				//.Include("~/Scripts/jquery/jquery.attrchange.js")
				);

			bundles.Add(new ScriptBundle("~/bundles/animations")
				.Include("~/Scripts/animations/*.js")
				.Include("~/Scripts/jquery/*.js")
				);

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
						"~/Scripts/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			//bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
			//			"~/Scripts/modernizr-*"));

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
					"~/Content/bootstrap/bootstrap.css",
					//"~/Content/Bootstrap-tabs.css",
					"~/Content/bootstrap.vertical-tabs.css",
					"~/Content/slider.css",
					"~/Content/site.css",
					"~/Content/Fonts.css",
					"~/Content/jquery.qtip.css"));

			//customStyles.Transforms.Add(new LessTransform());

			/*
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
					  //"~/Content/Bootstrap-tabs.css",
					  "~/Content/bootstrap.vertical-tabs.css",
					  "~/Content/slider.css",
					  "~/Content/site.css",
					  "~/Content/Fonts.css",
					  "~/Content/jquery.qtip.css"));
			*/

			bundles.Add(new StyleBundle("~/Content/ClientDetails").Include(
					"~/Content/Chart/Scatter.v2.css",
					"~/Content/ReportBuilder/evaluation.css",
					"~/Content/Chart/Scatter.v2.css",
					"~/Content/ReportBuilder/evaluation.css",
					"~/Content/Chart/Chart.css",
					"~/Content/ReportBuilder/ClientDetails.css",
					"~/Content/chart.css"
				));
			bundles.Add(new ScriptBundle("~/bundles/ClientDetails").Include(
					"~/Scripts/d3/d3.v3.min.js",
					"~/scripts/d3/d3.csv.js",
					"~/scripts/d3/Plot.js",
					"~/Scripts/d3/Scatter.v2.js",
					"~/Scripts/review/translateSlider.js"
				));
			
			bundles.Add(new StyleBundle("~/styles/L10").Include(
					"~/Content/L10/L10.css",
					"~/Content/L10/L10Todo.css",
					"~/Content/L10/L10Stats.css",
					"~/Content/L10/L10Rocks.css",
					"~/Content/L10/L10IDS.v2.css",
					"~/Content/L10/L10Scorecard.css",
					"~/Content/L10/L10Notes.css",
					"~/Content/L10/L10Transcribe.css",
					"~/Content/L10/L10ChatLog.css"
				));

			bundles.Add(new ScriptBundle("~/bundles/L10").Include(
					"~/Scripts/L10/resize-columns.js",
					"~/Scripts/jquery/jquery.ui.sortable.js",
					"~/Scripts/L10/charts/sparklines.min.js",
					"~/Scripts/home/resizeable-tables.js",
					"~/Scripts/L10/L10.js",
					"~/Scripts/L10/L10Ids.js",
					"~/Scripts/L10/L10Todo.js",
					"~/Scripts/L10/L10Rocks.js",
					"~/Scripts/L10/L10Scorecard.js",
					"~/Scripts/L10/L10Notes.js",
					"~/Scripts/L10/sortable.js",
					"~/Scripts/speechrecog.js",
					"~/Scripts/L10/L10Transcribe.js",
					"~/Scripts/L10/L10ChatLog.js",
					"~/Scripts/L10/rtL10.js"//Ensure last
				));


			var angularHelpers_Scripts = new[]{
				"~/Scripts/Main/moment.min.js",
				"~/Scripts/Angular/Helpers/Libraries/angular-filter.min.js", 
				"~/Scripts/Angular/Helpers/Libraries/angular-daterangepicker.js", 
				"~/Scripts/Angular/Helpers/transform.js", 
				"~/Scripts/Angular/Helpers/signalR.js",
				"~/Scripts/Angular/Helpers/Directives/AnywhereButHere.js", 
				"~/Scripts/Angular/Helpers/Directives/bindUnsafeHtml.js",
				"~/Scripts/Angular/Helpers/Directives/ImageTemplates.js",
				"~/Scripts/Angular/Helpers/Directives/fcsaNumber.js",
				"~/Scripts/Angular/Helpers/angular-timer.min.js",
				"~/Scripts/Angular/helpers.js", 
			};
			var angularHelpers_Styles = new[]{
				"~/Content/components/daterangepicker-bs3.css"
			};



			bundles.Add(new ScriptBundle("~/bundles/meeting")
				.Include(angularHelpers_Scripts)
				.Include(
					"~/Scripts/Angular/Meetings/RockState.js",
					"~/Scripts/Angular/Meetings/ButtonBar.js",
					"~/Scripts/Angular/Meetings/L10App.js",
					"~/Scripts/Angular/Meetings/L10Controller.js"
				));
			bundles.Add(new ScriptBundle("~/bundles/vto")
				.Include(angularHelpers_Scripts)
				.Include(
					"~/Scripts/Angular/VTO/VtoApp.js",
					"~/Scripts/Angular/VTO/VtoController.js"
				));
			bundles.Add(new StyleBundle("~/styles/meeting")
				.Include(angularHelpers_Styles));


			bundles.Add(new ScriptBundle("~/bundles/main").Include(
					  "~/Scripts/Main/radial.js",
					//"~/Scripts/jquery.signalR-{version}.js",
					  "~/Scripts/jquery/jquery.tablesorter.js",
					  "~/Scripts/Main/finally.js",
					  "~/Scripts/Main/intercom.min.js",
					  "~/Scripts/L10/jquery-ui.color.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/DashboardGrid").Include(
				"~/Scripts/Grid/fixtures.js",
				"~/Scripts/Grid/src/gridList.js",
				"~/Scripts/Grid/src/jquery.gridList.js",
				"~/Scripts/Grid/loadTiles.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/Video").Include(
				"~/Scripts/video/RTCMultiConnection.js",
				"~/Scripts/video/FileBufferReader.js",
				"~/Scripts/video/socket.io.js",
				"~/Scripts/video/hark.js"

				
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
