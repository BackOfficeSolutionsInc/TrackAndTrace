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

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class TodoTests : BaseTest
    {        
        [TestMethod]
        public async Task TestGetMineTodos()
        {
            //var userId = 2;

            var c = new Ctx();
            var todo = new TodoModel()
            {
                 AccountableUser = c.E1                  
            };

            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            RadialReview.Controllers.TodoController cnt = new RadialReview.Controllers.TodoController();
            cnt.MockUser(c.E1);

            //cnt.GetMineTodos();




        }
        
    }
}
