using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors.PDF;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Permissions;
using System.IO;
using RadialReview.Accessors.PDF.JS;
using System.Threading.Tasks;
using PdfSharp.Drawing;

namespace TractionTools.UITests.PDF {
	[TestClass]
	public class AccountabilityChartPdfTest : BasePermissionsTest {
		[TestMethod]
		public async Task FullTreeDiagram1() {
			var c = await Ctx.Build();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId);
			//Tree.Update(tree.Root, false);

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(tree.Root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), false).Document;

			pdf.Save(Path.Combine(GetPdfFolder(),"FullTreeDiagram1.pdf"));
		}
	}
}
