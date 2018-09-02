using PdfSharp.Drawing;
using PdfSharp.Pdf;
using RadialReview.Accessors.PDF;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Models;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public partial class PdfAccessor {

		public class QuarterlyPdfOutput {
			public PdfDocument Document { get; set; }
			public string RecurrenceName { get; set; }
			public DateTime CreateTime { get; set; }
		}

		public static async Task<QuarterlyPdfOutput> QuarterlyPrintout(UserOrganizationModel caller, long recurrenceId, bool issues, bool todos, bool scorecard, bool rocks, bool headlines, bool vto, bool l10, bool acc, bool print, bool quarterly, bool pa, int? maxSec) {

			var recur = L10Accessor.GetL10Recurrence(caller, recurrenceId, LoadMeeting.False());

			var angRecur = await L10Accessor.GetOrGenerateAngularRecurrence(caller, recurrenceId, forceIncludeTodoCompletion: recur.IncludeAggregateTodoCompletionOnPrintout);
			var merger = new DocumentMerger();

			//
			var anyPages = false;
			AngularVTO vtoModel = VtoAccessor.GetAngularVTO(caller, angRecur.VtoId.Value);
			var settings = new PdfSettings(caller.Organization.Settings);

			if (rocks) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout6");
				PdfAccessor.AddRocks(caller, doc, settings, quarterly, angRecur, vtoModel, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (headlines && angRecur.Headlines.Any()) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout6");
				PdfAccessor.AddHeadLines(caller, doc, settings, quarterly, angRecur, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (vto && angRecur.VtoId.HasValue && angRecur.VtoId > 0) {
				//vtoModel 
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout1");

				var vtoSettings = new VtoPdfSettings(caller.GetOrganizationSettings()) {
					MaxSeconds = maxSec
				};
				try {
					await PdfAccessor.AddVTO(doc, vtoModel, caller.GetOrganizationSettings().GetDateFormat(), vtoSettings);
				} catch (LayoutTimeoutException e) {
					var sec = doc.AddSection();
					var p = sec.AddParagraph("Error: V/TO took too long to generate.");
					p.Format.Font.Color = new MigraDoc.DocumentObjectModel.Color(255, 0, 0);
				}
				anyPages = true;
				merger.AddDoc(doc);
			}
			if (acc) {
				try {
					var tree = AccountabilityAccessor.GetTree(caller, caller.Organization.AccountabilityChartId, expandAll: true);

					var nodes = new List<AngularAccountabilityNode>();
					var topNodes = tree.Root.GetDirectChildren();

					//Add nodes from the tree.
					tree.Dive(x => {
						if (x.User != null && angRecur.Attendees.Any(a => a.Id == x.User.Id))
							nodes.Add(x);
					});

					//Setup if has parents
					foreach (var n in nodes) {
						n._hasParent = topNodes.All(x => x.Id != n.Id);
					}
					var acSettings = new AccountabilityChartPDF.AccountabilityChartSettings(caller.Organization.Settings);
					var doc = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevelsMultiDocumentsPerPage(nodes, XUnit.FromInch(11), XUnit.FromInch(8.5), acSettings, true);


					merger.AddDoc(doc);
					anyPages = true;
				} catch (Exception e) {
					int a = 0;
				}
			}
			if (l10) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printou2t");
				PdfAccessor.AddL10(doc, angRecur, settings, L10Accessor.GetLastMeetingEndTime(caller, recurrenceId), addPageNumber: false);
				anyPages = true;
				merger.AddDoc(doc);
			}
			if (scorecard) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printou5t");
				var addedSC = PdfAccessor.AddScorecard(doc, angRecur, settings, addPageNumber: false);
				anyPages = anyPages || addedSC;
				if (addedSC) {
					merger.AddDoc(doc);
				}
			}
			if (todos) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout3");
				await PdfAccessor.AddTodos(caller, doc, angRecur, settings, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (issues) {
				var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout4");
				PdfAccessor.AddIssues(caller, doc, angRecur, settings, todos, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (pa) {
				try {
					var doc = PdfAccessor.CreateDoc(caller, "Quarterly Printout5");
					var peopleAnalyzer = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(caller, caller.Id, recurrenceId);
					var renderer = PeopleAnalyzerPdf.AppendPeopleAnalyzer(caller, doc, peopleAnalyzer, settings, DateTime.MaxValue);
					merger.AddDoc(renderer);
					anyPages = true;
				} catch (Exception e) {
					//eat it..
				}
			}
			var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
			if (!anyPages) {
				throw new Exception("No pages");
			}

			var doc1= merger.Flatten("Quarterly Printout", true, true, caller.Organization.Settings.GetDateFormat(), recur.Name);
			return new QuarterlyPdfOutput {
				CreateTime = DateTime.UtcNow,
				Document = doc1,
				RecurrenceName = recur.Name
			};
		}
	}
}