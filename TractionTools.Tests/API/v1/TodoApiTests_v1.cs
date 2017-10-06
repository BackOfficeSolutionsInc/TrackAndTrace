using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using TractionTools.Tests.TestUtils;
using System.Linq;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Api.V1;

namespace TractionTools.Tests.API.v0 {
	[TestClass]
    public class TodoApiTests_v1 : BaseApiTest {
		public TodoApiTests_v1() : base(VERSION_1) { }


		[TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetMineTodos() {
            var c = await Ctx.Build();

            {
				// -2 for personal TODO
				var todoC = TodoCreation.CreatePersonalTodo("Todo from Test Method", null, c.E1.Id);
				var todo = await TodoAccessor.CreateTodo(c.E1, todoC);

                TodosController cnt = new TodosController();
                cnt.MockUser(c.E1);

                await c.Org.RegisterUser(c.E1);

                var _model = cnt.GetMineTodos();
                CompareModelProperties(/*APIResult.TodoApiTests_v0_TestGetMineTodos*/ _model, false);
                Assert.AreEqual(1, _model.Count());
                Assert.AreEqual(todo.Message, _model.FirstOrDefault().Name);
            }

            {
                //// attach meeting to TODO                
                var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);

               // todo.ForRecurrenceId = _recurrence.Id;
				var todoC = TodoCreation.CreateL10Todo("GetMineTodo from Test Method", null, c.E1.Id,null,_recurrence.Id);
				var todo = await TodoAccessor.CreateTodo(c.E1, todoC);

                TodosController cnt = new TodosController();
                cnt.MockUser(c.E1);

                //await c.Org.RegisterUser(c.E1);

                var _model = cnt.GetMineTodos();
                CompareModelProperties(/*APIResult.TodoApiTests_v0_TestGetMineTodos_list*/ _model);
                Assert.AreEqual(2, _model.Count());
                Assert.IsTrue(_model.Any(x => x.Name == todo.Message));

            }
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestCreateTodo() {
            var c = await Ctx.Build();

            var todo = new TodoModel() {
                AccountableUser = c.E1,
                Message = "Create Todo from Test Method",
                TodoType = TodoType.Personal
            };

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);
            var result = await cnt.CreateTodo( new TodosController.CreateTodoModel { title = todo.Message, dueDate = DateTime.UtcNow });

            Assert.IsTrue(result.Id !=null);
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetTodo() {
            var c = await Ctx.Build();

            //var todo = new TodoModel() {
            //    AccountableUser = c.E1,
            //    Message = "Todo from Test Method",
            //    TodoType = TodoType.Personal
            //};

			var todoC = TodoCreation.CreatePersonalTodo("Todo from Test Method", null, c.E1.Id);
			var todo = await TodoAccessor.CreateTodo(c.E1, todoC);

			//bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _todo = cnt.Get(todo.Id);
            CompareModelProperties(/*APIResult.TodoApiTests_v0_TestGetTodo*/ _todo);
            Assert.IsNotNull(_todo);
            Assert.AreEqual(todo.Message, _todo.Name);
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetUserTodos() {
            var c = await Ctx.Build();
			//var todo = new TodoModel() {
			//    AccountableUser = c.E1,
			//    Message = "GetUserTodo from Test Method",
			//    TodoType = TodoType.Personal
			//};
			var todoC = TodoCreation.CreatePersonalTodo("GetUserTodo from Test Method", null, c.E1.Id);
			var todo = await TodoAccessor.CreateTodo(c.E1, todoC);
			//bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);
            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _model = cnt.GetUserTodos(c.E1.Id);
            CompareModelProperties(/*APIResult.TodoApiTests_v0_TestGetUserTodos*/ _model);
            Assert.AreEqual(1, _model.Count());

        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestMarkComplete() {
            var c = await Ctx.Build();
            var now = DateTime.UtcNow;
			//var todo = new TodoModel() {
			//    AccountableUser = c.E1,
			//    Message = "GetUserTodo from Test Method",
			//    TodoType = TodoType.Personal
			//};
			var todoC = TodoCreation.CreatePersonalTodo("GetUserTodo from Test Method", null, c.E1.Id);
			var todo = await TodoAccessor.CreateTodo(c.E1, todoC);
			//bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);
            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var _model = await cnt.MarkComplete(todo.Id);
//            CompareModelProperties(_model);
			Assert.IsTrue(_model);
            //Assert.AreEqual(_model.Id, todo.Id);
            //Assert.IsNotNull(_model.CompleteTime);
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestEditTodo() {
            var c = await Ctx.Build();
            var now = DateTime.UtcNow;
            //var todo = new TodoModel() {
            //    AccountableUser = c.E1,
            //    Message = "Todo message for Test Method.",
            //    DueDate = new DateTime(2017, 04, 03),
            //    TodoType = TodoType.Personal
            //};

			var todoC = TodoCreation.CreatePersonalTodo("Todo message for Test Method.", null, c.E1.Id, new DateTime(2017, 04, 03));
			var todo = await TodoAccessor.CreateTodo(c.E1, todoC);
			//bool result = await TodoAccessor.CreateTodo(c.E1, -2, todo);

            TodosController cnt = new TodosController();
            cnt.MockUser(c.E1);

            var newMessage = "New Todo message for Test Method.";
            var newDueDate = new DateTime(2017, 04, 04);

            await cnt.EditTodo(todo.Id,new TodosController.UpdateTodoModel { title = newMessage, dueDate = newDueDate });

            var _todo = cnt.Get(todo.Id);

            Assert.AreEqual(newMessage, _todo.Name);

            Assert.AreNotEqual(todo.Message, _todo.Name);

            Assert.AreNotEqual(todo.DueDate, _todo.DueDate);

        }
    }
}
