var currentTodoDetailsId;

$(function () {

	$("body").on("click", ".todo-list>.todo-row>.message", function () {
		var todoRow = $(this).closest(".todo-row");
		$(".todo-row.selected").removeClass("selected");
		$(todoRow).addClass("selected");
		currentTodoDetailsId = $(todoRow).data("todo");
		var createtime = $(todoRow).data("createtime");
		var accountable = $(todoRow).data("accountable");
		var owner = $(todoRow).data("name");
		var message = $(todoRow).data("message");
		var details = $(todoRow).data("details");
		//var issueId = $(todoRow).data("issue");
		var todo = $(todoRow).data("todo");
		//var recurrence_issue = $(todoRow).data("recurrence_issue");
		var checked = $(todoRow).find(".issue-checkbox").prop("checked");

		$("#todoDetails").html("");
		$("#todoDetails").append("<span class='expandContract btn-group pull-right'></span>");
		$("#todoDetails").append("<div class='createTime'>" + new Date(createtime).toLocaleDateString() + "</div>");

		$("#todoDetails").append("<div class='heading'><h4 class='message-holder clickable' data-todo='" + todo + "'><span data-todo='" + todo + "' class='message'>" + message + "</span></h4></div>");
		$("#todoDetails").append("<textarea id='todoDetailsField' class='details todo-details' data-todo='" + todo + "'>" + details + "</textarea>");
		$("#todoDetails").append("<div class='button-bar'>" +
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton'><input data-todo='" + todo + "' class='todo-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Done</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='issuefromtodo' data-todo='" + todo + "' data-recurrence='" + recurrenceId + "' data-meeting='" + meetingId + "'><span class='glyphicon glyphicon-pushpin'></span> New Issue</span>" +
			//"<span class='btn btn-default btn-xs createTodoButton todoModal'><span class='glyphicon glyphicon-unchecked todoButton'></span> Todo</span>"+
			"</span>" +
			"<span class='clearfix'></span>" +
			"<span class='gray'>Assigned to:</span><span style='width:250px;padding-left:10px;' class='assignee' data-accountable='"+accountable+"' data-todo='"+todo+"' ><span data-todo='"+todo+"' class='btn btn-link'>"+owner+"</span></span>" +
			"</div>");
	});
		
	$("body").on("click", ".todoDetails .doneButton", function () { $(this).find(">input").trigger("click"); });
	
	$("body").on("click", ".todoDetails .message-holder .message", function() {
		var input = $("<input value='" + $(this).html() + "' data-old='" +escapeString($(this).html()) + "' onblur='sendTodoMessage(this," + $(this).parent().data("todo") + ")'/>");
		$(this).parent().html(input);
		input.focusTextToEnd();
	});

	$("body").on("click", ".todoDetails .assignee .btn", function() {
		var that = $(this).parent();
		$.ajax({
			method:"POST",
			url: "/L10/Members/" + recurrenceId,
			success: function(data) {
				if (showJsonAlert(data)) {

					var input = $("<select data-todo='"+$(that).data("todo")+"'/>");

					for (var i = 0; i < data.Object.length; i++) {
						var d = data.Object[i];
						debugger;
						var selected = $(that).attr("data-accountable")==d.id?"selected":"";
						$(input).append("<option "+selected+" data-img='"+d.imageUrl+"' value='"+d.id+"'>"+d.name+"</option>");
					}

					$(input).on('change', function() {
						debugger;
						sendNewAccountable(this, $(this).data("todo"));
					});

					$(that).html(input);

					var item = $(input).select2({
						templateResult: imageListFormat,
						templateSelection: imageListFormat
					});
					$(item).css("width", "250px");
					$(input).select2("open");
				}
			}
		});
		
	});

	$("body").on("blur", ".todoDetails .todo-details", function() {
		sendTodoDetails(this,$(this).data("todo"));
	});

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

function sendNewAccountable(self, id) {
	var val = $(self).val();
	var data = {
		accountableUser: val
	};
	var found =$(".todo .assignee[data-todo=" + id + "]");
	found.html("<span class='btn btn-link' data-todo='"+id+"'></span>");
	found.attr("data-accountable", val);
	$.ajax({
		method:"POST",
		data:data,
		url: "/L10/UpdateTodo/" + id,
		success:function(data) {
			if (showJsonAlert(data, false, true)) {
			}
		}
	});
}

function sendTodoDetails(self,id) {
	var val = $(self).val();
	var data = {
		details: val
	};
	$(".todo .todo-details[data-todo=" + id + "]").prop("disabled", true);
	$.ajax({
		method:"POST",
		data:data,
		url: "/L10/UpdateTodo/" + id,
		success:function(data) {
			if (showJsonAlert(data, false, true)) {
				$(".todo .todo-details[data-todo=" + id + "]").prop("disabled", false);
			}
		}
	});
}

function sendTodoMessage(self,id) {
	var val = $(self).val();
	$(".todoDetails .message-holder[data-todo=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};
	$.ajax({
		method:"POST",
		data:data,
		url: "/L10/UpdateTodo/" + id,
		success:function(data) {
			if (showJsonAlert(data, false, true)) {
				$(".todoDetails .message-holder[data-todo="+id+"]").html("<span data-todo='"+id+"' class='message'>"+val+"</span>");
			}
		}
	});
}

function refreshCurrentTodoDetails() {
	$(".todo-row[data-todo=" + currentTodoDetailsId + "]")
		.closest(".todo-list>.todo-row").find(">.message")
		.trigger("click");
	$(".todo-row[data-todo=" + currentTodoDetailsId + "]").addClass("selected");
}

function sortTodoByUser(recurrenceId,todoList) {
	$(todoList).find("li").sort(function(a, b) {
		if ($(a).attr("data-name") < $(b).attr("data-name"))
			return true;
		return $(a).attr("data-message") < $(b).attr("data-message") ;
	}).each(function() {
		$(todoList).prepend(this);
	});
	updateTodoList(recurrenceId, todoList);
}

function constructTodoRow(todo) {
	debugger;
	//Accountable user name populated?
	return '<li class="todo-row dd-item"' +
			'data-createtime="' + todo.createtime + '"' +
			'data-checked="' + todo.checked + '" ' +
			'data-imageurl="' + todo.imageurl + '" ' +
			'data-name="' + todo.accountableUser + '" ' +
			'data-accountable="' + todo.accountableUserId + '" ' +
			'data-todo="' + todo.todo + '" ' +
			'data-message="' + todo.message + '" ' +
			'data-details="' + todo.details + '">' +
			'	<input data-todo="' + todo.todo + '" class="todo-checkbox" type="checkbox" ' + (todo.checked ? "checked" : "") + '/>' +
			'	<div class="move-icon noselect dd-handle">' +
			'	<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>' +
			'	<span class="inner icon fontastic-icon-primitive-square"></span>' +
			'	</div>' +
			'	<span class="profile-image"><span class="profile-picture">' +
			'	<span class="picture-container" title="'+todo.accountableUser+'">' +
			'	<span class="picture" style="background: url('+todo.imageurl+') no-repeat center center;"></span>' +
			'	</span>' +
			'	</span></span>' +
			'	<div class="message" data-todo='+todo.todo+'>' + todo.message + '</div>' +
			'	<div class="todo-details-container"><div class="todo-details" data-todo='+todo.todo+'>' + todo.details + '</div></div>' +
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

function updateTodoMessage(id, message) {
	$(".todo .message[data-todo=" + id + "]").val(message);
	$(".todo .todo-row[data-todo=" + id + "]").data("message",escapeString(message));
}
function updateTodoDetails(id, details) {
	$(".todo .todo-details[data-todo=" + id + "]").html(details);
	$(".todo textarea.todo-details[data-todo=" + id + "]").val(details);
	$(".todo .todo-row[data-todo=" + id + "]").data("details",escapeString(details));
}

function updateTodoAccountableUser(id, userId,name, image) {
	$(".todo [data-todo=" + id + "] .picture-container").prop("title",name);
	$(".todo [data-todo=" + id + "] .picture").css("background","url("+image+") no-repeat center center");
	$(".todo .assignee .btn[data-todo=" + id + "]").html(name);
	var row = $(".todo .todo-row[data-todo=" + id + "]");
	$(row).data("name", name);
	$(row).data("accountable", userId);
	$(row).data("imageurl", image);
}