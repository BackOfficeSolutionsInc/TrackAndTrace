﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.L10;
using RadialReview.Models;
using System.Linq;
using RadialReview.Api.V0;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using static RadialReview.Controllers.L10Controller;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Areas.CoreProcess.Controllers;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Controllers.Api_V0;

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class ProcessDefApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestProcessStart()
        {
            var c = new Ctx();
            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            ProcessDef processController = new ProcessDef();
            processController.MockUser(c.E1);
            var getprocessDefList = processDefAccessor.GetList(c.E1, c.E1.Id);
            var getProcessInstanceList = processDefAccessor.GetProcessInstanceList(c.E1, getprocessDefList.FirstOrDefault().Id);
            var getResult = Task.Run(async () => await processController.StartProcess(getprocessDefList.FirstOrDefault().Id)).GetAwaiter().GetResult();
            //var getResult = processController.StartProcess(getprocessDefList.FirstOrDefault().Id);
            Assert.AreEqual(getprocessDefList.FirstOrDefault().Id, getResult.LocalId);
            //Assert.AreEqual(0,0);
        }

    }
}
