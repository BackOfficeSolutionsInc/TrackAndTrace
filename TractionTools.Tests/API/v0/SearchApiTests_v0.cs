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
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class SearchApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestSearchUser()
        {
            var c = await Ctx.Build();
            RadialReview.Api.V0.SearchController searchController = new RadialReview.Api.V0.SearchController();
            searchController.MockUser(c.E1);

            string searchStr = "mana";
            var search = searchController.SearchUser(searchStr);
            CompareModelProperties(APIResult.SearchApiTests_v0_TestSearchUser, search);
            Assert.AreEqual(1, search.Count());
            Assert.AreEqual(true, search.FirstOrDefault().Name.StartsWith("manager"));
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestSearch()
        {
            var c = await Ctx.Build();
            RadialReview.Api.V0.SearchController searchController = new RadialReview.Api.V0.SearchController();
            searchController.MockUser(c.E1);

            string searchStr = "team";
            var search = searchController.Search(searchStr);
            CompareModelProperties(APIResult.SearchApiTests_v0_TestSearch, search);
            Assert.AreEqual(2, search.Count());
            Assert.AreEqual(true, search.Any(x => x.Name.Contains("interreviewing-team")));
            Assert.AreEqual(true, search.Any(x => x.Name.Contains("non-interreviewing-team")));
        }
    }
}
