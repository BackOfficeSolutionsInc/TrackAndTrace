using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Controllers;

namespace TractionTools.Tests.Upload {
    [TestClass]
    public class UploadScorecard : BaseTest {
        [TestMethod]
        public async Task ScorecardUploadOutOfRangeBelow() {
            var outOfRangeCsv =
@"name,meas,goal,217/01/01,217/02/01,217/03/01,217/04/01
john doe,meas2,15,5,10,15,20
clay upton,meas1,20,10,20,30,40";
            var org = await OrgUtil.CreateOrganization();

            var path = UploadAccessor.MockUpload(outOfRangeCsv);

            var ctrl = new UploadController();
            ctrl.MockUser(org.Manager);

            try {
                await ctrl.ProcessScorecardSelection(
                    new[] { 0, 1, 0, 2 },
                    new[] { 3, 0, 6, 0 },
                    new[] { 1, 1, 1, 2 },
                    new[] { 2, 1, 2, 2 },
                    -1,
                    path);
                Assert.Fail();
            } catch (Exception e) {
                Assert.IsTrue(e.Message.Contains("4 dates were invalid"));
            }
        }

        [TestMethod]
        public async Task ScorecardUploadOutOfRangeAbove() {
            var outOfRangeCsv =
@"name,meas,goal,2170/01/01,2170/02/01,2170/03/01,2170/04/01
john doe,meas2,15,5,10,15,20
clay upton,meas1,20,10,20,30,40";
            var org = await OrgUtil.CreateOrganization();

            var path = UploadAccessor.MockUpload(outOfRangeCsv);

            var ctrl = new UploadController();
            ctrl.MockUser(org.Manager);

            try {
                await ctrl.ProcessScorecardSelection(
                    new[] { 0, 1, 0, 2 },
                    new[] { 3, 0, 6, 0 },
                    new[] { 1, 1, 1, 2 },
                    new[] { 2, 1, 2, 2 },
                    -1,
                    path);
                Assert.Fail();
            } catch (Exception e) {
                Assert.IsTrue(e.Message.Contains("4 dates were invalid"));
            }
        }

        [TestMethod]
        public async Task ScorecardUploadInRange() {
            var outOfRangeCsv =
@"name,meas,goal,2017/01/01,2017/02/01,2017/03/01,2017/04/01
john doe,meas2,15,5,10,15,20
clay upton,meas1,20,10,20,30,40";
            var org = await OrgUtil.CreateOrganization();
            var l10 = await L10Utility.CreateRecurrence(org: org);
            var path = UploadAccessor.MockUpload(outOfRangeCsv);

            var ctrl = new UploadController();
            ctrl.MockUser(org.Manager);

            await ctrl.ProcessScorecardSelection(
                new[] { 0, 1, 0, 2 },
                new[] { 3, 0, 6, 0 },
                new[] { 1, 1, 1, 2 },
                new[] { 2, 1, 2, 2 },
                l10.Id,
                path);
        }
    }
}
