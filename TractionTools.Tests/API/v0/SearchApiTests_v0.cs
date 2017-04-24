using System;
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
using RadialReview.Models.ViewModels;

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class SearchApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestSearchUser()
        {
            var c = new Ctx();
            RadialReview.Api.V0.SearchController searchController = new RadialReview.Api.V0.SearchController();
            searchController.MockUser(c.E1);

            string searchStr = "mana";
            var search = searchController.SearchUser(searchStr);

            Assert.AreEqual(1, search.Count());
            Assert.AreEqual(true, search.FirstOrDefault().Name.StartsWith("manager"));
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestSearch()
        {
            var c = new Ctx();
            RadialReview.Api.V0.SearchController searchController = new RadialReview.Api.V0.SearchController();
            searchController.MockUser(c.E1);

            string searchStr = "team";
            var search = searchController.Search(searchStr);

            Assert.AreEqual(2, search.Count());
            Assert.AreEqual(true, search.Any(x => x.Name.Contains("interreviewing-team")));
            Assert.AreEqual(true, search.Any(x => x.Name.Contains("non-interreviewing-team")));
        }
    }
}
