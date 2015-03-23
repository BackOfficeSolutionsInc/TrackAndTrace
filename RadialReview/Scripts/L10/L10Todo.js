var currentTodoDetailsId;

$(function () {

	$("body").on("click", ".todo-list>.todo-row>.message", function () {
		debugger;
		var todoRow = $(this).closest(".todo-row");
		$(".todo-row.selected").removeClass("selected");
		$(todoRow).addClass("selected");
		currentTodoDetailsId = $(todoRow).data("todo");
		var createtime = $(todoRow).data("createtime");
		var message = $(todoRow).data("message");
		var details = $(todoRow).data("details");
		//var issueId = $(todoRow).data("issue");
		var todo = $(todoRow).data("todo");
		//var recurrence_issue = $(todoRow).data("recurrence_issue");
		var checked = $(todoRow).find(".issue-checkbox").prop("checked");

		$("#todoDetails").html("");
		$("#todoDetails").append("<span class='expandContract btn-group pull-right'></span>");
		$("#todoDetails").append("<div class='createTime'>" + new Date(createtime).toLocaleDateString() + "</div>");

		$("#todoDetails").append("<div class='heading'><h4>" + message + "</h2></div>");
		$("#todoDetails").append("<textarea id='todoDetailsField' class='details rt'>" + details + "</textarea>");
		$("#todoDetails").append("<div class='button-bar'>" +
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton'><input data-todo='" + todo + "' class='todo-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Done</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='issuefromtodo' data-todo='" + todo + "' data-recurrence='" + recurrenceId + "' data-meeting='" + meetingId + "'><span class='glyphicon glyphicon-pushpin'></span> New Issue</span>" +
			//"<span class='btn btn-default btn-xs createTodoButton todoModal'><span class='glyphicon glyphicon-unchecked todoButton'></span> Todo</span>"+
			"</span>" +
			"<span class='clearfix'></span>" +
			"</div>");
	});
		
	$("body").on("click", ".todoDetails .doneButton", function () { $(this).find(">input").trigger("click"); });



	$("body").on("change", ".todo-checkbox", function () {
		var todoId = $(this).data("todo");
		var checked = $(this).prop("checked");
		var selector = ".todo-checkbox[data-todo='" + todoId + "']";
		var selector2 = ".todo-row[data-todo='" + todoId + "']";
		var that = this;
		$(selector).prop("disabled", true);
		$(selector).prop("checked", checked);
		$(selector2).data("checked", checked);
		$.ajax({
			url: "/l10/UpdateTodoCompletion/" + recurrenceId,
			method: "post",
			data: { todoId: todoId, checked: checked, connectionId: $.connection.hub.id },
			success: function (data) {
				showJsonAlert(data, false, true);
				$(selector).prop("checked", (!data.Error ? data.Object : !checked));
				$(selector2).data("checked", (!data.Error ? data.Object : !checked));
			},
			error: function () {
				$(selector).prop("checked", !checked);
				$(selector2).data("checked", !checked);
			},
			complete: function () {
				$(selector).prop("disabled", false);
			}
		});
	});

});

function refreshCurrentTodoDetails() {
	$(".todo-row[data-todo=" + currentTodoDetailsId + "]")
		.closest(".todo-list>.todo-row").find(">.message")
		.trigger("click");
	$(".todo-row[data-todo=" + currentTodoDetailsId + "]").addClass("selected");
}

function constructTodoRow(todo) {
	return '<li class="todo-row dd-item"' +
			'data-createtime="' + todo.createtime + '"' +
			'data-checked="' + todo.checked + '" ' +
			'data-imageurl="' + todo.imageurl + '" ' +
			'data-name="' + todo.name + '" ' +
			'data-todo="' + todo.todo + '" ' +
			'data-message="' + todo.message + '" ' +
			'data-details="' + todo.details + '">' +
			'	<input data-todo="' + todo.todo + '" class="todo-checkbox" type="checkbox" ' + (todo.checked ? "checked" : "") + '/>' +
			'	<div class="move-icon noselect dd-handle">' +
			'	<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>' +
			'	<span class="inner icon fontastic-icon-primitive-square"></span>' +
			'	</div>' +
			'	<span class="profile-image"><span class="profile-picture">' +
			'	<span class="picture-container" title="'+todo.name+'">' +
			'	<span class="picture" style="background: url('+todo.imageurl+') no-repeat center center;"></span>' +
			'	</span>' +
			'	</span></span>' +
			'	<div class="message">' + todo.message + '</div>' +
			'	<div class="todo-details-container"><div class="todo-details">' + todo.details + '</div></div>' +
			'</li>';
}

function updateTodoList(recurrenceId, todoRow) {
	var d = { todos: $(todoRow).sortable('serialize').toArray(), connectionId: $.connection.hub.id };
	console.log(d);
	var that = todoRow;
	$.ajax({
		url: "/l10/UpdateTodos/" + recurrenceId,
		data: JSON.stringify(d),
		contentType: "application/json; charset=utf-8",
		method: "POST",
		success: function (d) {
			if (!d.Error) {
				oldTodoList = $(".todo-list").clone(true);
			} else {
				showJsonAlert(d, false, true);
				$(that).html("");
				setTimeout(function () {
					$('.todo-container').html(oldTodoList);
					oldTodoList = $(".todo-list").clone(true);
					refreshCurrentTodoDetails();
				}, 1);
			}
		},
		error: function (a, b) {
			clearAlerts();
			showAlert(a.statusText || b);
			$('.dd').html("");
			setTimeout(function () {
				$('.dd').html(oldTodoList);
				oldTodoList = $(".todo-list").clone(true);
				refreshCurrentTodoDetails();
			}, 1);
		}
	});
}

function deserializeTodos(selector, todoList) {
	var sub = "";
	for (var i = 0; i < todoList.todos.length; i++) {
		sub += constructTodoRow(todoList.todos[i]);
	}
	$(selector).html(sub);
	refreshCurrentTodoDetails();
}

function appendTodo(selector, issue) {
	var li = $(constructTodoRow(issue));
	$(selector).append(li);
	$(li).flash();
	refreshCurrentTodoDetails();
}

function updateTodoCompletion(todoId, complete) {
	var selector = ".todo-checkbox[data-todo='" + todoId + "']";
	$(selector).prop("checked", complete);
}