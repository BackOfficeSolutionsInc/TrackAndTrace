using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Models;
using RadialReview.Controllers;
using System.Web.Mvc;
using RadialReview.Models.L10.VM;
using RadialReview.Accessors;
using RadialReview.Models.L10;
using TractionTools.Tests.Utilities;
using RadialReview.Models.Json;
using RadialReview.Models.Askables;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Meeting;

namespace TractionTools.Tests.Accessors {
    [TestClass]
    public class L10Accessor_AngularTests  : BaseTest {
       

        [TestMethod]
        public void RemoveRockAlsoArchives()
        {
            var r=L10Utility.CreateRecurrence();
            var rocks = L10Accessor.GetRocksForRecurrence(r.Creator, r.Id);
            Assert.AreEqual(0, rocks.Count);
            var ctrl = new L10Controller();
            //controller.SetValue("SkipValidation", true);
            ctrl.MockUser(r.Creator);
            MockHttpContext();
            var result =ctrl.AddAngularRock(r.Id).Data as ResultObject;
            Assert.IsNotNull(result);
            var rock = result.Object as RockModel;
            Assert.IsNotNull(rock);
            
            rocks = L10Accessor.GetRocksForRecurrence(r.Creator, r.Id);
            Assert.AreEqual(1, rocks.Count);
            Assert.AreEqual(null, rocks[0].ForRock.Rock);


            var r2 = L10Utility.CreateRecurrence(r);

            Assert.AreNotEqual(r.Id, r2.Id);
            DbCommit(s=>{
                rock._AddedToL10 = false;
                rock._AddedToVTO = false;
                var sameRock=s.Get<RockModel>(rock.Id);
                L10Accessor.AddRock(s, PermissionsUtility.Create(s, r2.Creator), r2.Id, sameRock);
            });
            var rocks2 = L10Accessor.GetRocksForRecurrence(r2.Creator, r2.Id);
            Assert.AreEqual(1, rocks2.Count);
            Assert.AreEqual(null, rocks2[0].ForRock.Rock);

            Assert.AreNotEqual(rocks2[0].Id, rocks[0].Id);//Are not the same meetingRocks
            Assert.AreEqual(rocks2[0].ForRock.Id, rocks[0].ForRock.Id); //in fact the same rock


            var ra = new RockAccessor();
            var myRocks = ra.GetRocks(r.Creator, r.Creator.Id);

            Assert.AreEqual(1, myRocks.Count);
            Assert.AreEqual(rocks2[0].ForRock.Id, myRocks[0].Id);

            ctrl.RemoveAngularRock(r.Id, new AngularRock(rocks[0])); //Remove it from one meeting
            myRocks = ra.GetRocks(r.Creator, r.Creator.Id);
            Assert.AreEqual(1, myRocks.Count);
            Assert.AreEqual(rocks2[0].ForRock.Id, myRocks[0].Id);

            ctrl.RemoveAngularRock(r2.Id, new AngularRock(rocks[0]));
            myRocks = ra.GetRocks(r.Creator, r.Creator.Id);
            Assert.AreEqual(0, myRocks.Count);

        }

     
    }
}
