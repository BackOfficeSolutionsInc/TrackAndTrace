using System.Web;
using System.Web.Optimization;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System.Collections.Generic;

namespace RadialReview {
	public class BundleConfig {

		private static Bundle UpdateMinification(Bundle scripts) {
			if (Config.DisableMinification()) {
				scripts.Transforms.Clear();
			}
			return scripts;
		}


		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles) {
			string[] angularHelpers_Scripts, angularHelpers_Styles;// angularPeople_Scripts, angularPeople_Styles;
			AngularHelpers(out angularHelpers_Scripts, out angularHelpers_Styles/*, out angularPeople_Scripts*/);

			JQuery(bundles);
			Bootstrap(bundles);
			MaterialDesign(bundles);
			OldReview(bundles);
			VideoChat(bundles);
			AccoutabilityChart(bundles, angularHelpers_Scripts);
			D3(bundles);
			GettingStarted(bundles);
			L10(bundles);
			L10Wizard(bundles, angularHelpers_Scripts, angularHelpers_Styles);
			SetCard(bundles);
			Compatability(bundles);

			Main(bundles);

			VTO(bundles, angularHelpers_Scripts);
			Dashboard(bundles);
			DashboardWidgets(bundles);
			MeetingEdit(bundles);
			Manage(bundles);
			Angular(bundles);
			AngularMaterial(bundles);
			People(bundles, angularHelpers_Styles, angularHelpers_Scripts);

            TagInput(bundles);
            SnackBar(bundles);

			BundleTable.EnableOptimizations = Config.OptimizationEnabled();



		}

        private static void SnackBar(BundleCollection bundles){
            bundles.Add(new StyleBundle("~/styles/snackbar").Include("~/Content/SnackbarAlerts.css"));
        }

        private static void People(BundleCollection bundles, string[] ngStyles, string[] ngScripts) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/people").Include(ngScripts)
				.Include(
					"~/Scripts/Angular/People/init.js",
					"~/Scripts/Angular/People/Survey/SurveyComponents.js",
					"~/Scripts/Angular/People/PeopleAnalyzer/PeopleAnalyzer.js",
					"~/Scripts/People/*.js"
				)));
			bundles.Add(new StyleBundle("~/styles/people").Include(ngStyles).Include(
				"~/Content/SnackbarAlerts.css", "~/Content/People/*.css"));
		}

        private static void TagInput(BundleCollection bundles) {
            bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/taginput").Include(
                      "~/Scripts/Inputs/underscore.min.js",
                      "~/Scripts/Inputs/underscore.string.min.js",
                      "~/Scripts/Inputs/backbone.min.js",
                      "~/Scripts/Inputs/backbone.subviews.js",
                      "~/Scripts/Inputs/liquidmetal.js",
                      "~/Scripts/Inputs/require.js",
                      "~/Scripts/Inputs/token-editor.js"
            )));

            bundles.Add(new StyleBundle("~/styles/taginput").Include(
             "~/Content/Inputs/TagInput.css"
             ));
        }

		private static void SetCard(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/SetCard").Include("~/Scripts/jquery/jquery.redirect.js")));
		}

		private static void Compatability(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/compatability").Include(
					  "~/Scripts/Main/iefixes.js"
			)));
		}

		private static void Main(BundleCollection bundles) {

			var list = new List<string>() {
				"~/Scripts/Main/time.js",
				"~/Scripts/Main/linq.js",
				"~/Scripts/Main/radial.js",
				"~/Scripts/Main/modals.js",
				"~/Scripts/Main/datepickers.js",
				"~/Scripts/Main/support.js",
				"~/Scripts/Main/backwardcompatability.js",
				"~/Scripts/Main/ajaxintercepters.js",
				"~/Scripts/Main/datatable.js",
				"~/Scripts/Main/tours.js",
				"~/Scripts/Main/alerts.js",
				"~/Scripts/Main/clickableclass.js",
				"~/Scripts/Main/profilepicture.js",
                "~/Scripts/Main/libraries.js",
                "~/Scripts/Main/chart.js"
            };


			//Only intercept logs if not local...
			if (Config.GetEnv() != Env.local_mysql)
				list.Add("~/Scripts/Main/log-helper.js");

			list.AddRange(new[] {
                /*"~/Scripts/jquery.signalR-{version}.js",Was deleted*/
                "~/Scripts/jquery/jquery.tablesorter.js",
				"~/Scripts/Main/finally.js",
				"~/Scripts/Main/intercom.min.js",
				"~/Scripts/L10/jquery-ui.color.js",
				"~/Scripts/jquery/jquery.tabbable.js",
				"~/Scripts/components/milestones.js",
				"~/Scripts/Main/keyboard.js",
				"~/Scripts/Main/tooltips.js",
				"~/Scripts/Main/beta.js"
			});

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/main").Include(list.ToArray())));	

		}

		private static void AngularMaterial(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/AngularMaterial").Include(
				//"~/bower_components/angular-material-data-table/dist/md-data-table.min.js"
				//S:\repos\Radial\RadialReview\RadialReview\Scripts\Angular\Helpers\Libraries\angular-material-custom.js
				"~/Scripts/Angular/Helpers/Libraries/angular-material-custom.js"
			)));
			bundles.Add(UpdateMinification(new StyleBundle("~/styles/AngularMaterial").Include(
				"~/Content/Angular/angular-material.css"
			)));
		}

		private static void Angular(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/Angular").Include(
				//"~/bower_components/angular-material-data-table/dist/md-data-table.min.js"
				"~/Scripts/Angular/MaterialDesign/md-data-table.js"
			)));

			bundles.Add(new StyleBundle("~/styles/Angular").Include(
			 //"~/bower_components/angular-material-data-table/dist/md-data-table.min.css"
			 ));
		}

		private static void VTO(BundleCollection bundles, string[] angularHelpers_Scripts) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/vto")
							.Include(angularHelpers_Scripts)
							.Include(
								"~/Scripts/jquery/jquery.autoresize.js",
								"~/Scripts/Angular/VTO/VtoApp.js",
								"~/Scripts/Angular/VTO/VtoController.js",
								"~/Scripts/VTO/vto.js"
							)));
		}

		private static void Dashboard(BundleCollection bundles) {
			bundles.Add(new StyleBundle("~/styles/Dashboard").Include(
				"~/Content/bootstrap/custom/dashboard.css",
				"~/Scripts/Grid/style.css",
				"~/Content/Angular/LineChart.css"
			));

			///I dont think this is used anywhere...
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/Dashboard").Include(
				"~/Scripts/Dashboard/dashboard.js",
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js",
				"~/Scripts/L10/L10Scorecard.js",
				"~/Scripts/d3/d3.js"
			)));
			//^^^^^

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/DashboardGrid").Include(
				"~/Scripts/Dashboard/dashboard.js",
				"~/Scripts/Grid/fixtures.js",
				"~/Scripts/Grid/src/gridList.js",
				"~/Scripts/Grid/src/jquery.gridList.js",
				"~/Scripts/Grid/loadTiles.js",
				"~/Scripts/CoreProcess/coreProcessRT.js"
			//....
			)));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/DashboardPostAngular").Include(
				"~/Scripts/d3/d3.js",
				"~/Scripts/Angular/Helpers/d3/LineChart.js",
				"~/Scripts/Angular/Tiles/L10StatsTile.js"
			)));




		}

		private static void Manage(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/Manage").Include(
				"~/Scripts/jquery/jquery.tablesorter.js",
				"~/Scripts/jquery/jquery.filtertable.min.js"
			)));

			bundles.Add(new StyleBundle("~/Content/ManageCSS")
				.Include("~/Content/Manage/Manage.css"));
		}

		private static void MeetingEdit(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MeetingEdit").Include("~/Scripts/components/jquery.bootstrap-duallistbox.js")));
			bundles.Add(new StyleBundle("~/Content/MeetingEdit").Include("~/Content/bootstrap-duallistbox.css"));
		}

		private static void DashboardWidgets(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MeasurableList").Include("~/Scripts/jquery/jquery.ui.sortable.js", "~/Scripts/L10/L10Scorecard.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/ScorecardDetails").Include(
				"~/Scripts/jquery/jquery.ui.sortable.js",
				"~/Scripts/L10/L10Scorecard.js"
			)));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MeetingDetails").Include(
				"~/Scripts/L10/charts/sparklines.min.js",
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js"
			)));
		}

		private static void L10Wizard(BundleCollection bundles, string[] angularHelpers_Scripts, string[] angularHelpers_Styles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/L10Wizard").Include("~/Scripts/Wizard/wizard.js", "~/Scripts/L10/L10Wizard.js", "~/Scripts/L10/L10.js")));
			bundles.Add(new StyleBundle("~/Content/L10Wizard").Include(
				"~/Content/L10/L10Wizard.css",
				"~/Content/Angular/xeditable.min.css",
				"~/Content/Angular/tablesort.css",
				"~/Content/Angular/livesearch.css"
			));

			//_L10App.cshtml
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/L10js").Include(
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/L10/L10.js")));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/meeting")
				.Include(angularHelpers_Scripts)
				.Include(
					"~/Scripts/Angular/Meetings/RockState.js",
					"~/Scripts/Angular/Meetings/ButtonBar.js",
					"~/Scripts/Angular/Helpers/Libraries/angular-xeditable.js",
					"~/Scripts/Angular/Meetings/L10App.js",
					"~/Scripts/Angular/Meetings/L10Controller.js"
				)));
			bundles.Add(new StyleBundle("~/styles/meeting").Include(angularHelpers_Styles));
			bundles.Add(new StyleBundle("~/styles/archive").Include("~/Content/L10/Archive/Archive.css"));


		}

		private static void L10(BundleCollection bundles) {
			bundles.Add(new StyleBundle("~/styles/L10").Include(
				"~/Content/L10/fireworks.css",
				"~/Content/L10/L10.css",
				"~/Content/L10/L10Todo.css",
				"~/Content/L10/L10Stats.css",
				"~/Content/L10/L10Rocks.css",
				"~/Content/L10/L10Headlines.css",
				"~/Content/L10/L10IDS.v2.css",
				"~/Content/L10/L10Scorecard.css",
				"~/Content/L10/L10Notes.css",
				"~/Content/L10/L10Transcribe.css",
				"~/Content/L10/L10ChatLog.css",
				"~/Content/bootstrap-switch.css"
			));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/L10").Include(
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
					"~/Scripts/L10/L10ChatLog.js",
					"~/Scripts/L10/sortable.js",
					"~/Scripts/speechrecog.js",
					"~/Scripts/L10/L10Transcribe.js",
					"~/Scripts/L10/L10ChatLog.js",
					"~/Scripts/components/rockstate.js",
					"~/Scripts/L10/rtL10.js"//Ensure last
				)));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MeetingPage").Include(
				"~/Scripts/jquery/jquery.ba-throttle-debounce.js",
				"~/Scripts/jquery/jquery.scrollTo.js",
				"~/Scripts/bootstrap-switch.js"
			)));

		}

		private static void GettingStarted(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/GetStarted").Include("~/Scripts/Angular/GettingStartd/GSController.js", "~/Scripts/jquery/jquery.redirect.js", "~/Scripts/jquery/jquery.vide.js")));
			bundles.Add(new StyleBundle("~/Content/GetStarted").Include("~/Content/GettingStarted/GettingStarted.css"));

		}

		private static void D3(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/d3").Include("~/Scripts/d3/d3.min.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/d3v3").Include("~/Scripts/d3/d3.v3.js", "~/Scripts/d3/line.v1.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/linechart").Include("~/Scripts/d3/line.v1.js")));
			bundles.Add(new StyleBundle("~/Content/Charts").Include("~/Content/Chart/Chart.css"));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/TranslateSliders").Include("~/Scripts/review/translateSlider.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/Reorganize").Include("~/Scripts/d3/orgchart.js")));

		}

		private static void AccoutabilityChart(BundleCollection bundles, string[] angularHelpers_Scripts) {

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/AccountabilityChart")
				.Include(angularHelpers_Scripts)
			.Include(
				"~/Scripts/d3/d3.js",
				"~/Scripts/Angular/Helpers/d3/panzoom.js",
				"~/Scripts/Angular/Helpers/d3/tree.js",
				"~/Scripts/home/html2canvas.js",
				"~/Scripts/home/jspdf.js",
				"~/Scripts/AccountabilityChart/accountabilityChart.js",
				"~/Scripts/Angular/AccountabilityChart/ACController.js",
				"~/Scripts/d3/radial.d3.tree.js",
				"~/Scripts/undo.js"
			)));
			bundles.Add(new StyleBundle("~/Content/AccChart")
				.Include("~/Content/AccountabilityChart/AccountabilityChart.css"));

		}

		private static void VideoChat(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/VideoChat1").Include(
	"~/Scripts/video/hark.js",
	"~/Scripts/video/video.js"
)));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/VideoChat2").Include(
				"~/Scripts/L10/AV/adapter.js",
				"~/Scripts/L10/AV/connectionManager.js",
				"~/Scripts/L10/AV/app.js"
			)));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/Video").Include(
				"~/Scripts/video/RTCMultiConnection.js",
				"~/Scripts/video/FileBufferReader.js",
				"~/Scripts/video/socket.io.js",
				"~/Scripts/video/hark.js"
			)));


		}

		private static void MaterialDesign(BundleCollection bundles) {
			bundles.Add(new StyleBundle("~/Content/MdStepper").Include("~/Content/MaterialDesign/Libraries/md-stepper.css"));
			bundles.Add(new StyleBundle("~/Content/MdFile").Include("~/Content/MaterialDesign/Libraries/lf-ng-md-file-input.css"));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MdStepper").Include("~/Scripts/Angular/MaterialDesign/md-stepper.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/MdFile").Include("~/Scripts/Angular/MaterialDesign/lf-ng-md-file-input.min.js")));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/filtertable").Include("~/Scripts/jquery/jquery.filtertable.min.js")));

		}

		private static void OldReview(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/TakeReview").Include(
	"~/Scripts/review/review.js",
	"~/Scripts/review/translateSlider.js",
	"~/Scripts/jquery/jquery-simple-slider.js"
)));
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
				"~/Content/ReportBuilder/evaluation.css",
				"~/Content/Reports/ReportDetails.css"
		   ));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/ReviewDetails").Include(
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
			)));
			bundles.Add(new StyleBundle("~/Content/ClientDetails").Include(
					"~/Content/Chart/Scatter.v2.css",
					//"~/Content/Chart/Scatter.v2.css",
					"~/Content/ReportBuilder/evaluation.css",
					//"~/Content/ReportBuilder/evaluation.css",
					"~/Content/Chart/Chart.css",
					"~/Content/ReportBuilder/ClientDetails.css",
					"~/Content/chart.css",
					"~/Content/Reports/ClientDetails.css"
				));
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/ClientDetails").Include(
					"~/Scripts/d3/d3.v3.js",
					"~/scripts/d3/d3.csv.js",
					"~/scripts/d3/Plot.js",
					"~/Scripts/d3/Scatter.v2.js",
					"~/Scripts/review/translateSlider.js"
				)));
			bundles.Add(new StyleBundle("~/Content/ReportStyles").Include("~/Content/Reports/Reports.css"));

		}

		private static void Bootstrap(BundleCollection bundles) {
			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			//bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
			//			"~/Scripts/modernizr-*"));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/bootstrap").Include(
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
					  "~/Scripts/bootstrap-datepicker.js")));

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
		}

		private static void JQuery(BundleCollection bundles) {
			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/jquery")
							.Include("~/Scripts/jquery-{version}.js")
							.Include("~/Scripts/jquery.unobtrusive-ajax.js")
							.Include("~/Scripts/jquery/jquery.qtip.js")
							//.Include("~/Scripts/jquery/jquery.attrchange.js")
							));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/animations")
				.Include("~/Scripts/animations/*.js")
				.Include("~/Scripts/jquery/*.js")
				));

			bundles.Add(UpdateMinification(new ScriptBundle("~/bundles/jqueryval").Include(
						"~/Scripts/jquery.validate*")));
		}

		private static void AngularHelpers(out string[] angularHelpers_Scripts, out string[] angularHelpers_Styles/*, out string[] angularPeople_Scripts*/) {
			angularHelpers_Scripts = new[]{
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
				"~/Scripts/Angular/Helpers/Libraries/ngsortable.js",
               // "~/Scripts/Angular/Helpers/Libraries/ngfittext.js",
                     
                //"~/bower_components/angular-animate/angular-animate.js",           
               // "~/bower_components/angular-aria/angular-aria.js",
               // "~/bower_components/angular-material/angular-material.js",

				"~/Scripts/Angular/helpers.js",
			};
			angularHelpers_Styles = new[]{
				"~/Content/components/daterangepicker-bs3.css",
				"~/Content/Angular/tablesort.css",
				"~/Content/Angular/xeditable.min.css",
				"~/Content/Angular/ngsortable.css",
			};
			//angularPeople_Scripts = new[] {
			//    "~/Scripts/Angular/Helpers/updater.js",
			//    "~/Scripts/Angular/Helpers/signalR.js",
			//    "~/Scripts/Angular/Helpers/radialModule.js",

			//    "~/Scripts/Angular/People/Survey/SurveyComponents.js",

			//    "~/Scripts/Angular/helpers.js",
			//};
		}
	}
}
