using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using RadialReview.Controllers;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests.Accessors {
    [TestClass]
    public class PdfAccessor_RunAll : BaseTest {
        [TestMethod]
        public void VTO()
        {

            MockHttpContext();
            var r = L10Utility.CreateRecurrence("VTO");
            var d = PdfAccessor.CreateDoc(r.Creator, "d");

            var avto = VtoAccessor.GetAngularVTO(r.Creator, r.Recur.VtoId);

            PdfAccessor.AddVTO(d, avto, "MM-dd-yyyy");
        }
        [TestMethod]
        public void QuarterlyPrintout()
        {
            var r = L10Utility.CreateRecurrence("QuarterlyPrintout");
            var d = PdfAccessor.CreateDoc(r.Creator, "d");

			//var ctrl = new QuarterlyController();

			//Assert.Inconclusive("Fix me");

			//MockController<QuarterlyController>(r.Creator, ctrl => {
			//	ctrl.Printout(r.Id, true, true, true, true, true, true, true);
			//});


        }
    }
}
