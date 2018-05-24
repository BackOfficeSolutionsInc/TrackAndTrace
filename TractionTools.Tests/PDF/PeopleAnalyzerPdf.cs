using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using RadialReview;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Angular.Survey;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.Permissions;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class PeopleAnalyzerPDFTests : BasePermissionsTest {

		private AngularPeopleAnalyzer Generate(int values, int employees) {

			var valuesForModel = Enumerable.Range(0, values).Select(x => new AngularForModel() {
				ModelId = x,
				ModelType = "Value",
				PrettyString = "Value Value " + x
			}).ToList();


			var employeeForModel = Enumerable.Range(0, employees).Select(x => new AngularForModel() {
				ModelId = x,
				ModelType = "Employee",
				PrettyString = "Employee Employee " + x + " (function)"
			}).ToList();


			var pa = new AngularPeopleAnalyzer();
			pa.Rows = employeeForModel.Select(x => new AngularPeopleAnalyzerRow() {About = x}).ToList();
			pa.Values = valuesForModel.Select(x => new PeopleAnalyzerValue(x));

			pa.Responses = new List<AngularPeopleAnalyzerResponse>() {
				new AngularPeopleAnalyzerResponse() {
					AnswerFormatted = "Y",
					Answer = "yes",
					Source = valuesForModel[1],
					About = employeeForModel[1],
					IssueDate = DateTime.UtcNow,
				},
				new AngularPeopleAnalyzerResponse() {
					AnswerFormatted = "N",
					Answer = "no",
					Source = valuesForModel[0],
					About = employeeForModel[0],
					IssueDate = DateTime.UtcNow,
				},
				new AngularPeopleAnalyzerResponse() {
					AnswerFormatted = "-",
					Answer = "not-often",
					Source = valuesForModel[1],
					About = employeeForModel[0],
					IssueDate = DateTime.UtcNow,
				},
				new AngularPeopleAnalyzerResponse() {
					AnswerFormatted = "+",
					Answer = "often",
					Source = valuesForModel[0],
					About = employeeForModel[1],
					IssueDate = DateTime.UtcNow,
				},
				new AngularPeopleAnalyzerResponse() {
					AnswerFormatted = "+/-",
					Answer = "sometimes",
					Source = valuesForModel[2],
					About = employeeForModel[1],
					IssueDate = DateTime.UtcNow,
				}
			};

			pa.SurveyContainers = new List<AngularSurveyContainer>() {
				new AngularSurveyContainer() {
					IssueDate = DateTime.UtcNow,
					Name = "Survey container name"
				}
			};

			return pa;
		}

		[TestMethod]
		[TestCategory("PDF")]
		public async Task GeneratePeopleAnalyzerPdf_Landscape() {
			var doc = new Document();
			var pa = Generate(11, 20);
			var renderer = PeopleAnalyzerPdf.AppendPeopleAnalyzer(doc, "asdf", pa);
			renderer.Save(Path.Combine(GetCurrentPdfFolder(), "GeneratePeopleAnalyzerPdf_Landscape.pdf"));
		}

		[TestMethod]
		[TestCategory("PDF")]
		public async Task GeneratePeopleAnalyzerPdf_Portrait() {
			var doc = new Document();
			var pa = Generate(5, 20);
			var renderer = PeopleAnalyzerPdf.AppendPeopleAnalyzer(doc, "asdf", pa);
			renderer.Save(Path.Combine(GetCurrentPdfFolder(), "GeneratePeopleAnalyzerPdf_Portrait.pdf"));
		}


	}
}
