﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors.PDF;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Permissions;
using System.IO;
using RadialReview.Accessors.PDF.JS;
using System.Threading.Tasks;

namespace TractionTools.UITests.PDF {
	[TestClass]
	public class AccountabilityChartPdfTest : BasePermissionsTest {
		[TestMethod]
		public async Task FullTreeDiagram() {
			var c = await Ctx.Build();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId);
			//Tree.Update(tree.Root, false);

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(tree.Root, 8.5, 10, false);

			pdf.Save(Path.Combine(GetPdfFolder(),"FullTreeDiagram.pdf"));
		}
	}
}
