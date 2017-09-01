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
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.API.v0 {
    [TestClass]
    public class TodoApiTests_v0 : BaseTest {
        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetMineTodos() {
            var c = await Ctx.Build();

            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "Todo from Test Method",
                TodoType = TodoType.Personal
            };
            // -3 for personal TODO
            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            await c.Org.RegisterUser(c.E1);

            var _model = cnt.GetMineTodos();
            CompareModelProperties(APIResult.TodoApiTests_v0_TestGetMineTodos, _model);
            Assert.AreEqual(1, _model.Count());

            Assert.AreEqual(todo.Message, _model.FirstOrDefault().Name);

        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateTodo() {
            var c = await Ctx.Build();

            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "Create Todo from Test Method",
                TodoType = TodoType.Personal
            };

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);
            bool result = await cnt.CreateTodo(todo.Message, DateTime.Now);

            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetTodo() {
            var c = await Ctx.Build();

            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "Todo from Test Method",
                TodoType = TodoType.Personal
            };

            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _todo = cnt.Get(todo.Id);
            CompareModelProperties(APIResult.TodoApiTests_v0_TestGetTodo, _todo);
            Assert.IsNotNull(_todo);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetUserTodos() {
            var c = await Ctx.Build();
            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "GetUserTodo from Test Method",
                TodoType = TodoType.Personal
            };
            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);
            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _model = cnt.GetUserTodos(c.E1.Id);
            CompareModelProperties(APIResult.TodoApiTests_v0_TestGetUserTodos, _model);
            Assert.AreEqual(1, _model.Count());

        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetRecurrenceTodos() {
            var c = await Ctx.Build();
            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "GetUserTodo from Test Method",
                TodoType = TodoType.Recurrence
            };

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);

            todo.ForRecurrenceId = _recurrence.Id;
            bool result = await TodoAccessor.CreateTodo(c.E1, _recurrence.Id, todo);

            var _model = cnt.GetRecurrenceTodos(_recurrence.Id);
            CompareModelProperties(APIResult.TodoApiTests_v0_TestGetRecurrenceTodos, _model);
            Assert.AreEqual(1, _model.Count());
            Assert.AreEqual(_model.FirstOrDefault().Owner.Id, c.E1.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestMarkComplete() {
            var c = await Ctx.Build();
            var now = DateTime.UtcNow;
            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "GetUserTodo from Test Method",
                TodoType = TodoType.Personal
            };
            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);
            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _model = cnt.MarkComplete(todo.Id, now);
            CompareModelProperties(APIResult.TodoApiTests_v0_TestMarkComplete, _model);
            Assert.AreEqual(_model.Id, todo.Id);
            Assert.IsNotNull(_model.CompleteTime);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestEditTodo() {
            var c = await Ctx.Build();
            var now = DateTime.UtcNow;
            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "Todo message for Test Method.",
                DueDate = new DateTime(2017, 04, 03),
                TodoType = TodoType.Personal
            };

            bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var newMessage = "New Todo message for Test Method.";
            var newDueDate = new DateTime(2017, 04, 04);

            await cnt.EditTodo(todo.Id, newMessage, newDueDate);

            var _todo = cnt.Get(todo.Id);

            Assert.AreEqual(newMessage, _todo.Name);

            Assert.AreNotEqual(todo.Message, _todo.Name);

            Assert.AreNotEqual(todo.DueDate, _todo.DueDate);

        }
    }
}
