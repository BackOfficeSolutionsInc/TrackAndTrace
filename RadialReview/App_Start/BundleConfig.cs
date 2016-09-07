﻿using System.Web;
using System.Web.Optimization;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview {
	public class BundleConfig {
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles) {
			var angularHelpers_Scripts = new[]{
				"~/Scripts/Main/moment.min.js",
				"~/Scripts/Angular/Helpers/Libraries/angular-filter.min.js",
				"~/Scripts/Angular/Helpers/Libraries/angular-daterangepicker.js",
				"~/Scripts/Angular/Helpers/transform.js",
				"~/Scripts/Angular/Helpers/updater.js",
				"~/Scripts/Angular/Helpers/signalR.js",
				"~/Scripts/Angular/Helpers/radialModule.js",
				"~/Scripts/Angular/Helpers/Directives/AnywhereButHere.js",
				"~/Scripts/Angular/Helpers/Directives/LiveSearch.js",
				"~/Scripts/Angular/Helpers/Directives/TableSort.js",
				"~/Scripts/Angular/Helpers/Directives/RightClick.js",
				"~/Scripts/Angular/Helpers/Directives/ElemReady.js",
				"~/Scripts/Angular/Helpers/Directives/bindUnsafeHtml.js",
				"~/Scripts/Angular/Helpers/Directives/ImageTemplates.js",
				"~/Scripts/Angular/Helpers/Directives/ScoreTemplates.js",
				"~/Scripts/Angular/Helpers/Directives/FastWidth.js",
				"~/Scripts/Angular/Helpers/Directives/PriorityTemplates.js",
				"~/Scripts/Angular/Helpers/Directives/fcsaNumber.js",
				"~/Scripts/Angular/Helpers/Directives/vsRepeat.js",
				"~/Scripts/Angular/Helpers/angular-timer.min.js",
				"~/Scripts/Angular/Helpers/angular-elastic-input.js",
                     
                //"~/bower_components/angular-animate/angular-animate.js",           
               // "~/bower_components/angular-aria/angular-aria.js",
               // "~/bower_components/angular-material/angular-material.js",

				"~/Scripts/Angular/helpers.js",
			};
			var angularHelpers_Styles = new[]{
				"~/Content/components/daterangepicker-bs3.css",
				"~/Content/Angular/tablesort.css",
				"~/Content/Angular/xeditable.min.css",
                
                //"~/bower_components/angular-material/angular-material.css"
			};


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
					"~/Content/bootstrap/custom/Site.css",
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

			bundles.Add(new StyleBundle("~/Content/MdStepper").Include("~/Content/MaterialDesign/Libraries/md-stepper.css"));
			bundles.Add(new StyleBundle("~/Content/MdFile").Include("~/Content/MaterialDesign/Libraries/lf-ng-md-file-input.css"));
			bundles.Add(new ScriptBundle("~/bundles/MdStepper").Include("~/Scripts/Angular/MaterialDesign/md-stepper.js"));
			bundles.Add(new ScriptBundle("~/bundles/MdFile").Include("~/Scripts/Angular/MaterialDesign/lf-ng-md-file-input.min.js"));
			bundles.Add(new ScriptBundle("~/bundles/filtertable").Include("~/Scripts/jquery/jquery.filtertable.min.js"));

			bundles.Add(new ScriptBundle("~/bundles/TakeReview").Include(
				"~/Scripts/review/review.js",
				"~/Scripts/review/translateSlider.js",
				"~/Scripts/jquery/jquery-simple-slider.js"
			));
			bundles.Add(new StyleBundle("~/Content/TakeReview").Include(
				"~/Content/simple-slider.css",
				"~/Content/Review.css"
			));


			bundles.Add(new StyleBundle("~/Content/ReviewDetails").Include(
				"~/Content/Chart/Scatter.v2.css",
				"~/Content/jquery.nouislider.css",
				"~/Content/Chart/Chart.css",
				"~/Content/toggle.css",
				"~/Content/bootstrap-switch.css",
				"~/Content/ReportBuilder/ReportBuilder.css",
				"~/Content/ReportBuilder/evaluation.css"
		   ));
			bundles.Add(new ScriptBundle("~/bundles/ReviewDetails").Include(
				"~/Scripts/Angular/MaterialDesign/md-stepper.js",
				"~/Scripts/jquery/jquery.nouislider.js",
				"~/Scripts/d3/d3.v3.js",
				"~/scripts/d3/d3.csv.js",
				"~/Scripts/d3/Scatter.v2.js",
				"~/Scripts/bootstrap-switch.js",
				"~/Scripts/review/translateSlider.js",
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/d3/Plot.js",
				"~/Scripts/moment.min.js",
				"~/Scripts/report/reportbuilder.js"
			));


			bundles.Add(new ScriptBundle("~/bundles/VideoChat1").Include(
				"~/Scripts/video/hark.js",
				"~/Scripts/video/video.js"
			));
			bundles.Add(new ScriptBundle("~/bundles/VideoChat2").Include(
				"~/Scripts/L10/AV/adapter.js",
				"~/Scripts/L10/AV/connectionManager.js",
				"~/Scripts/L10/AV/app.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/AccountabilityChart")
				.Include(angularHelpers_Scripts)
			.Include(
				"~/Scripts/d3/d3.js",
				"~/Scripts/Angular/Helpers/d3/panzoom.js",
				"~/Scripts/Angular/Helpers/d3/tree.js",
				"~/Scripts/AccountabilityChart/accountabilityChart.js",
				"~/Scripts/Angular/AccountabilityChart/ACController.js",
				"~/Scripts/undo.js"
			));
			bundles.Add(new StyleBundle("~/Content/AccChart")
				.Include("~/Content/AccountabilityChart/AccountabilityChart.css"));

			bundles.Add(new ScriptBundle("~/bundles/d3").Include("~/Scripts/d3/d3.min.js"));
			bundles.Add(new ScriptBundle("~/bundles/d3v3").Include("~/Scripts/d3/d3.v3.js", "~/Scripts/d3/line.v1.js"));
			bundles.Add(new ScriptBundle("~/bundles/linechart").Include("~/Scripts/d3/line.v1.js"));
			bundles.Add(new StyleBundle("~/Content/Charts").Include("~/Content/Chart/Chart.css"));
			bundles.Add(new ScriptBundle("~/bundles/TranslateSliders").Include("~/Scripts/review/translateSlider.js"));
			bundles.Add(new ScriptBundle("~/bundles/Reorganize").Include("~/Scripts/d3/orgchart.js"));

			bundles.Add(new ScriptBundle("~/bundles/MeasurableList").Include("~/Scripts/jquery/jquery.ui.sortable.js", "~/Scripts/L10/L10Scorecard.js"));

			bundles.Add(new ScriptBundle("~/bundles/GetStarted").Include("~/Scripts/Angular/GettingStartd/GSController.js", "~/Scripts/jquery/jquery.redirect.js", "~/Scripts/jquery/jquery.vide.js"));
			bundles.Add(new StyleBundle("~/Content/GetStarted").Include("~/Content/GettingStarted/GettingStarted.css"));


			bundles.Add(new StyleBundle("~/Content/ClientDetails").Include(
					"~/Content/Chart/Scatter.v2.css",
					//"~/Content/Chart/Scatter.v2.css",
					"~/Content/ReportBuilder/evaluation.css",
					//"~/Content/ReportBuilder/evaluation.css",
					"~/Content/Chart/Chart.css",
					"~/Content/ReportBuilder/ClientDetails.css",
					"~/Content/chart.css"
				));
			bundles.Add(new ScriptBundle("~/bundles/ClientDetails").Include(
					"~/Scripts/d3/d3.v3.js",
					"~/scripts/d3/d3.csv.js",
					"~/scripts/d3/Plot.js",
					"~/Scripts/d3/Scatter.v2.js",
					"~/Scripts/review/translateSlider.js"
				));

			bundles.Add(new StyleBundle("~/styles/L10").Include(
					"~/Content/L10/fireworks.css",
					"~/Content/L10/L10.css",
					"~/Content/L10/L10Todo.css",
					"~/Content/L10/L10Stats.css",
					"~/Content/L10/L10Rocks.css",
					"~/Content/L10/L10IDS.v2.css",
					"~/Content/L10/L10Scorecard.css",
					"~/Content/L10/L10Notes.css",
					"~/Content/L10/L10Transcribe.css",
					"~/Content/L10/L10ChatLog.css",
					"~/Content/bootstrap-switch.css"
				));

			bundles.Add(new ScriptBundle("~/bundles/L10").Include(
					"~/Scripts/L10/resize-columns.js",
					"~/Scripts/jquery/jquery.ui.sortable.js",
					"~/Scripts/L10/charts/sparklines.min.js",
					"~/Scripts/home/resizeable-tables.js",
					"~/Scripts/undo.js",
					"~/Scripts/L10/fireworks.js",
					"~/Scripts/L10/L10.js",
					"~/Scripts/L10/L10Ids.js",
					"~/Scripts/L10/L10Todo.js",
					"~/Scripts/L10/L10Rocks.js",
					"~/Scripts/L10/L10Scorecard.js",
					"~/Scripts/L10/L10Headlines.js",
					"~/Scripts/L10/L10Notes.js",
					"~/Scripts/L10/sortable.js",
					"~/Scripts/speechrecog.js",
					"~/Scripts/L10/L10Transcribe.js",
					"~/Scripts/L10/L10ChatLog.js",
					"~/Scripts/components/rockstate.js",
					"~/Scripts/L10/rtL10.js"//Ensure last
				));

			bundles.Add(new ScriptBundle("~/bundles/L10Wizard").Include("~/Scripts/Wizard/wizard.js", "~/Scripts/L10/L10Wizard.js", "~/Scripts/L10/L10.js"));
			bundles.Add(new StyleBundle("~/Content/L10Wizard").Include(
				"~/Content/L10/L10Wizard.css",
				"~/Content/Angular/xeditable.min.css",
				"~/Content/Angular/tablesort.css",
				"~/Content/Angular/livesearch.css"
			));
			bundles.Add(new ScriptBundle("~/bundles/SetCard").Include("~/Scripts/jquery/jquery.redirect.js"));

			//_L10App.cshtml
			bundles.Add(new ScriptBundle("~/bundles/L10js").Include(
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js"));

			bundles.Add(new ScriptBundle("~/bundles/meeting")
				.Include(angularHelpers_Scripts)
				.Include(
					"~/Scripts/Angular/Meetings/RockState.js",
					"~/Scripts/Angular/Meetings/ButtonBar.js",
					"~/Scripts/Angular/Helpers/Libraries/angular-xeditable.js",
					"~/Scripts/Angular/Meetings/L10App.js",
					"~/Scripts/Angular/Meetings/L10Controller.js"
				));
			bundles.Add(new ScriptBundle("~/bundles/vto")
				.Include(angularHelpers_Scripts)
				.Include(
					"~/Scripts/jquery/jquery.autoresize.js",
					"~/Scripts/Angular/VTO/VtoApp.js",
					"~/Scripts/Angular/VTO/VtoController.js",
					"~/Scripts/VTO/vto.js"
				));
			bundles.Add(new StyleBundle("~/styles/meeting").Include(angularHelpers_Styles));
			bundles.Add(new StyleBundle("~/styles/archive").Include("~/Content/L10/Archive/Archive.css"));

			bundles.Add(new ScriptBundle("~/bundles/main").Include(
					  "~/Scripts/Main/radial.js",
					  "~/Scripts/Main/log-helper.js",
					  /*"~/Scripts/jquery.signalR-{version}.js",Was deleted*/
					  "~/Scripts/jquery/jquery.tablesorter.js",
					  "~/Scripts/Main/finally.js",
					  "~/Scripts/Main/intercom.min.js",
					  "~/Scripts/L10/jquery-ui.color.js"/*,
					  "~/Scripts/Main/realtime.js"*/
			));

			bundles.Add(new StyleBundle("~/styles/Dashboard").Include(
				"~/Content/bootstrap/custom/dashboard.css",
				"~/Scripts/Grid/style.css",
				"~/Content/Angular/LineChart.css"
			));

			///I dont think this is used anywhere...
			bundles.Add(new ScriptBundle("~/bundles/Dashboard").Include(
				"~/Scripts/Dashboard/dashboard.js",
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js",
				"~/Scripts/L10/L10Scorecard.js",
				"~/Scripts/d3/d3.js"
			));
			//^^^^^

			bundles.Add(new ScriptBundle("~/bundles/DashboardGrid").Include(
				"~/Scripts/Dashboard/dashboard.js",
				"~/Scripts/Grid/fixtures.js",
				"~/Scripts/Grid/src/gridList.js",
				"~/Scripts/Grid/src/jquery.gridList.js",
				"~/Scripts/Grid/loadTiles.js"
			//....
			));

			bundles.Add(new ScriptBundle("~/bundles/DashboardPostAngular").Include(
				"~/Scripts/d3/d3.js",
				"~/Scripts/Angular/Helpers/d3/LineChart.js",
				"~/Scripts/Angular/Tiles/L10StatsTile.js"
			));



			bundles.Add(new ScriptBundle("~/bundles/ScorecardDetails").Include(
				"~/Scripts/jquery/jquery.ui.sortable.js",
				"~/Scripts/L10/L10Scorecard.js"
			));
			bundles.Add(new ScriptBundle("~/bundles/MeetingDetails").Include(
				"~/Scripts/L10/charts/sparklines.min.js",
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/MeetingEdit").Include("~/Scripts/components/jquery.bootstrap-duallistbox.js"));
			bundles.Add(new StyleBundle("~/Content/MeetingEdit").Include("~/Content/bootstrap-duallistbox.css"));

			bundles.Add(new ScriptBundle("~/bundles/MeetingPage").Include(
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/jquery/jquery.scrollTo.js",
				"~/Scripts/bootstrap-switch.js"
			));


			bundles.Add(new ScriptBundle("~/bundles/Video").Include(
				"~/Scripts/video/RTCMultiConnection.js",
				"~/Scripts/video/FileBufferReader.js",
				"~/Scripts/video/socket.io.js",
				"~/Scripts/video/hark.js"
			));

			bundles.Add(new ScriptBundle("~/bundles/Manage").Include(
				"~/Scripts/jquery/jquery.tablesorter.js",
				"~/Scripts/jquery/jquery.filtertable.min.js"
			));

			bundles.Add(new StyleBundle("~/Content/ManageCSS")
				.Include("~/Content/Manage/Manage.css"));


			bundles.Add(new ScriptBundle("~/bundles/Angular").Include(
			//"~/bower_components/angular-material-data-table/dist/md-data-table.min.js"
			));

			bundles.Add(new StyleBundle("~/styles/Angular").Include(
			 //"~/bower_components/angular-material-data-table/dist/md-data-table.min.css"
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
