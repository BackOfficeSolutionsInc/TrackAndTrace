﻿var currentTodoDetailsId;

$(function () {
    $("body").on("click", ".todo-list>.todo-row", function (evt) {
        if ($(evt.target).hasClass("todo-checkbox"))
            return;

		var todoRow = $(this);
		$(".todo-row.selected").removeClass("selected");
		$(todoRow).addClass("selected");
		currentTodoDetailsId = $(todoRow).data("todo");
		var createtime = $(todoRow).data("createtime");
		var duedate = +$(todoRow).attr("data-duedate");
		var accountable = $(todoRow).data("accountable");
		var owner = $(todoRow).data("name");
		var message = $(todoRow).data("message");
		var details = $(todoRow).data("details");
		var padId = $(todoRow).data("padid");
		//var issueId = $(todoRow).data("issue");
		var todo = $(todoRow).data("todo");

		var due =  new Date(new Date(duedate).toUTCString().substr(0,16));
		//var recurrence_issue = $(todoRow).data("recurrence_issue");
		var checked = $(todoRow).find(".issue-checkbox").prop("checked");

		$("#todoDetails").html("");
		$("#todoDetails").append("<span class='expandContract btn-group pull-right'></span>");
		$("#todoDetails").append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");

		$("#todoDetails").append("<div class='heading'><h4 class='message-holder clickable' data-todo='" + todo + "'><span data-todo='" + todo + "' class='message editable-text'>" + message + "</span></h4></div>");
		//$("#todoDetails").append("<textarea id='todoDetailsField' class='details todo-details' data-todo='" + todo + "'>" + details + "</textarea>");
		$("#todoDetails").append("<iframe class='details todo-details' name='embed_readwrite' src='https://notes.traction.tools/p/"+padId+"?showControls=true&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName="+encodeURI(UserName)+"' width='100%' height='100%'></iframe>");

		$("#todoDetails").append(
			"<div class='button-bar'>" +
				"<div style='height:28px'>"+
				"<span class='btn-group pull-right'>" +
					"<span class='btn btn-default btn-xs doneButton'><input data-todo='" + todo + "' class='todo-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Complete</span>" +
				"</span>" +
				"<span class='expandContract btn-group'>" +
				"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='issuefromtodo' data-todo='" + todo + "' data-recurrence='" + recurrenceId + "' data-meeting='" + meetingId + "'><span class='icon fontastic-icon-pinboard'></span> New Issue</span>" +
				//"<span class='btn btn-default btn-xs createTodoButton todoModal'><span class='glyphicon glyphicon-unchecked todoButton'></span> Todo</span>"+
				"</span>" +
				"</div>"+
				"<span class='clearfix'></span>" +
				"<span class='gray' style='width:75px;display:inline-block'>Assigned to:</span><span style='width:250px;padding-left:10px;' class='assignee' data-accountable='" + accountable + "' data-todo='" + todo + "'  ><span data-todo='" + todo + "' class='btn btn-link owner'>" + owner + "</span></span>" +
				"<div >" +
					"<span class='gray' style='width:75px;display:inline-block'>Due date:</span>" +
					"<span style='width:250px;padding-left:10px;' class='duedate' data-accountable='" + accountable + "' data-todo='" + todo + "' >" +
						"<span class='date' style='display:inline-block' data-date='" + dateFormatter(due) + "' data-date-format='m-d-yyyy'>" +
							"<input type='text' data-todo='" + todo + "' class='form-control datePicker' value='" + dateFormatter(due) + "'/>" +
						"</span>" +
					"</span>" +
				"</div>" +
			"</div>");
		fixTodoDetailsBoxSize();
	});

	$("body").on("click", ".todoDetails .doneButton", function () { $(this).find(">input").trigger("click"); });

	$("body").on("click", ".todoDetails .message-holder .message", function () {
		var input = $("<input class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html()) + "' onblur='sendTodoMessage(this," + $(this).parent().data("todo") + ")'/>");
		$(this).parent().html(input);
		input.focusTextToEnd();
	});
	$("body").on('click', ".todoDetails .date input", function () {
		if (!$(this).attr("init")) {
			var now = new Date();
			var todo = $(this).data("todo");
			$(this).datepickerX({
				format: 'm/d/yyyy',
				todayBtn:true,
				orientation:"top left"
			}).on('changeDate', function (ev) {
				var data = { date: (ev.date).valueOf() };
				$.ajax({
					method: "POST",
					data: data,
					url: "/L10/UpdateTodoDate/" + todo,
					success: function (dd) {
						showJsonAlert(dd);
					}
				});
			});
			$(this).attr("init", 1);
			$(this).blur();
			var that = this;
			setTimeout(function () {
				$(that).datepickerX("show");
			}, 10);
		}
	});

	$("body").on("click", ".todoDetails .assignee .btn", function () {
		var that = $(this).parent();
		$.ajax({
			method: "POST",
			url: "/L10/Members/" + recurrenceId,
			success: function (data) {
				if (showJsonAlert(data)) {

					var input = $("<select data-todo='" + $(that).data("todo") + "'/>");

					for (var i = 0; i < data.Object.length; i++) {
						var d = data.Object[i];
						var selected = $(that).attr("data-accountable") == d.id ? "selected" : "";
						$(input).append("<option " + selected + " data-img='" + d.imageUrl + "' value='" + d.id + "'>" + d.name + "</option>");
					}

					$(input).on('change', function () {
						debugger;
						sendNewAccountable(this, $(this).data("todo"));
					});

					$(that).html(input);

					var item = $(input).select2({
						templateResult: imageListFormat,
						templateSelection: imageListFormat
					});
						debugger;
					$(item).parent().find(".select2").css("width", "inherit");
					//$(item).parent().find("span").css(
					  /* right: 6px; */
					$(input).select2("open");
				}
			}
		});
	});


	$("body").on("blur", ".todoDetails .todo-details", function () {
		sendTodoDetails(this, $(this).data("todo"));
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

function fixTodoDetailsBoxSize() {
	if ($(".details.todo-details").length) {
		var wh = $(window).height();
		var pos = $(".details.todo-details").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
		    footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) {}
		$(".details.todo-details").height(footerH - 20 - 140 - pos.top  );
	}
}

$(window).resize(fixTodoDetailsBoxSize);
$(window).on("page-todo",fixTodoDetailsBoxSize);
$(window).on("footer-resize",function() {
	setTimeout(fixTodoDetailsBoxSize, 250);
});

function updateTodoDueDate(todo, duedate) {
	var row = $(".todo-row[data-todo=" + todo + "]");
	row.attr("data-duedate", duedate);
	var d = new Date(duedate);
	var a=d.toISOString().substr(0, 10).split("-");
	var dispDate = new Date(a[0], a[1] - 1, a[2]);
	//var nowDateStr = new Date().toISOString().substr(0, 10).split("-");
    //var nowDate = new Date(nowDateStr[0], nowDateStr[1] - 1, nowDateStr[2]);
	var nowDateStr = new Date();
	var nowDate = new Date(nowDateStr.getYear()+1900, nowDateStr.getMonth(), nowDateStr.getDate());
		
	var found = row.find(".due-date");
	$(found).toggleClass("red", dispDate.getTime() < nowDate.getTime());
	found.html(dateFormatter(dispDate));
	//$("input[data-todo=" + todo + "]").val(dateFormatter(new Date(duedate)));
	$("input[data-todo=" + todo + "]").val(dateFormatter(dispDate));
	
}

function sendNewAccountable(self, id) {
	var val = $(self).val();
	var data = {
		accountableUser: val
	};
	var found = $(".todo .assignee[data-todo=" + id + "]");
	found.html("<span class='btn btn-link' data-todo='" + id + "'></span>");
	found.attr("data-accountable", val);
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateTodo/" + id,
		success: function (data) {
			if (showJsonAlert(data, false, true)) {
			}
		}
	});
}

function sendTodoDetails(self, id) {
	var val = $(self).val();
	var data = {
		details: val
	};
	$(".todo .todo-details[data-todo=" + id + "]").prop("disabled", true);
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateTodo/" + id,
		success: function (data) {
			if (showJsonAlert(data, false, true)) {
				$(".todo .todo-details[data-todo=" + id + "]").prop("disabled", false);
			}
		}
	});
}

function sendTodoMessage(self, id) {
	var val = $(self).val();
	$(".todoDetails .message-holder[data-todo=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateTodo/" + id,
		success: function (data) {
			if (showJsonAlert(data, false, true)) {
				$(".todoDetails .message-holder[data-todo=" + id + "]").html("<span data-todo='" + id + "' class='message editable-text'>" + val + "</span>");
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

function sortTodoBy(recurrenceId, todoList,sortBy,title,mult) {
	mult = mult || 1;

	$(".sort-button").html("Sort by " + title);

	$(todoList).children().detach().sort(function(a, b) {
		if ($(a).attr(sortBy)===$(b).attr(sortBy))
			return mult*$(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
		return mult*$(a).attr(sortBy).localeCompare($(b).attr(sortBy));
	}).appendTo($(todoList));
	updateTodoList(recurrenceId, todoList);
}
function sortTodoByUser(recurrenceId, todoList) {
	
	$(todoList).children().detach().sort(function(a, b) {
		if ($(a).attr("data-name")===$(b).attr("data-name"))
			return $(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
		return $(a).attr("data-name").localeCompare($(b).attr("data-name"));
	}).appendTo($(todoList));

	/*
	$(todoList).find("li").sort(function (a, b) {
		//if ($(a).attr("data-name") == $(b).attr("data-name"))
		//	return $(a).attr("data-message") < $(b).attr("data-message") ;
		return ($(a).attr("data-name") <= $(b).attr("data-name"));
	}).each(function () {
		$(todoList).prepend(this);
	});
	$(todoList).find("li").sort(function (a, b) {
		if ($(a).attr("data-name") != $(b).attr("data-name"))
			return true;
		return ($(a).attr("data-message") <= $(b).attr("data-message"));
	}).each(function () {
		$(todoList).prepend(this);
	});*/
	updateTodoList(recurrenceId, todoList);
}

function constructTodoRow(todo) {
    var red = "";
    var nowDateStr = new Date();
    var nowDate = new Date(nowDateStr.getYear() + 1900, nowDateStr.getMonth(), nowDateStr.getDate());
    var duedateStr = todo.duedate.split("T")[0].split("-");
    var duedate = new Date(duedateStr[0], duedateStr[1] - 1, duedateStr[2]).getTime();

    if (duedate < nowDate)
		red = "red";
	var date = new Date(new Date(todo.duedate).toUTCString().substr(0,16));
	//Accountable user name populated?
	return '<li class="todo-row dd-item arrowkey"' +
			'data-createtime="' + todo.createtime + '"' +
			'data-duedate="' + duedate + '"' +
			'data-checked="' + todo.checked + '" ' +
			'data-imageurl="' + todo.imageurl + '" ' +
			'data-name="' + todo.accountableUser + '" ' +
			'data-accountable="' + todo.accountableUserId + '" ' +
			'data-padid="' + todo.padId + '" ' +
			'data-todo="' + todo.todo + '" ' +
			'data-message="' + todo.message + '" ' +
			'data-details="' + todo.details + '">' +
			'	<input data-todo="' + todo.todo + '" class="todo-checkbox" type="checkbox" ' + (todo.checked ? "checked" : "") + '/>' +
			'	<div class="move-icon noselect dd-handle">' +
			'	<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>' +
			'	<span class="inner icon fontastic-icon-primitive-square"></span>' +
			'	</div>' +
		
			'	<div class="btn-group pull-right">'+
			'		<span class="icon fontastic-icon-pinboard issuesModal issuesButton" data-method="issuefromtodo" data-todo="'+todo.todo+'" data-recurrence="'+MeetingId+'" data-meeting="'+meetingId+'"></span>'+
			'	</div>'+
			'	<span class="profile-image">'+
			'		<span class="profile-picture">' +
			'			<span class="picture-container" title="' + todo.accountableUser + '">' +
			'				<span class="picture" style="background: url(' + todo.imageurl + ') no-repeat center center;"></span>' +
			'			</span>' +
			'		</span>'+
			'	</span>' +
			'	<div class="message" data-todo=' + todo.todo + '>' + todo.message + '</div>' +
			'	<div class="todo-details-container"><div class="todo-details" data-todo=' + todo.todo + '>' + todo.details + '</div></div>' +
			'	<div class="due-date '+red+'">'+dateFormatter(date)+'</div>'+
			'</li>';
}

function setTodoOrder(order) {
    var items = $(".todo-list li");
    items.detach();

    var len = order.length,
        temp = [];

    for (var i = 0; i < len; i++) {
        var found = $(items).filter("[data-todo="+order[i]+"]");
        temp.push(found);
    }

    $(".todo-list").append(temp);
}

function getTodoOrder() {
    return $.map($(".todo-list").sortable('serialize').toArray(), function (v) { return v.todo; });
}

function updateTodoList(recurrenceId, todoRow) {
    var order = getTodoOrder();
    var d = { todos: order, connectionId: $.connection.hub.id };
	console.log(d);
	var that = todoRow;
	$.ajax({
		url: "/l10/UpdateTodos/" + recurrenceId,
		data: JSON.stringify(d),
		contentType: "application/json; charset=utf-8",
		method: "POST",
		success: function (d) {
			if (!d.Error) {
			    oldTodoList = order;//$(".todo-list").clone(true);
			} else {
				showJsonAlert(d, false, true);
				//$(that).html("");
				setTimeout(function () {
				    setTodoOrder(oldTodoList);
					//$('.todo-container').html(oldTodoList);
					//oldTodoList = $(".todo-list").clone(true);
					refreshCurrentTodoDetails();
				}, 1);
			}
		},
		error: function (a, b) {
			clearAlerts();
			showAlert(a.statusText || b);
			//$('.dd').html("");
			setTimeout(function () {

			    setTodoOrder(oldTodoList);
				//$('.dd').html(oldTodoList);
				//oldTodoList = $(".todo-list").clone(true);
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
	$(selector).prepend(li);
	$(li).flash();
	refreshCurrentTodoDetails();
}

function updateTodoCompletion(todoId, complete) {
	var selector = ".todo-checkbox[data-todo='" + todoId + "']";
	$(selector).prop("checked", complete);
}

function updateTodoMessage(id, message) {
	$(".todo .message[data-todo=" + id + "]").html(message);
	$(".todo .todo-row[data-todo=" + id + "]").data("message", escapeString(message));
}
function updateTodoDetails(id, details) {
	$(".todo .todo-details[data-todo=" + id + "]").html(details);
	$(".todo textarea.todo-details[data-todo=" + id + "]").val(details);
	$(".todo .todo-row[data-todo=" + id + "]").data("details", escapeString(details));
}

function updateTodoAccountableUser(id, userId, name, image) {
	$(".todo [data-todo=" + id + "] .picture-container").prop("title", name);
	$(".todo [data-todo=" + id + "] .picture").css("background", "url(" + image + ") no-repeat center center");
	$(".todo .assignee .btn[data-todo=" + id + "]").html(name);
	var row = $(".todo .todo-row[data-todo=" + id + "]");
	$(row).data("name", name);
	$(row).data("accountable", userId);
	$(row).data("imageurl", image);
}


function checkFireworks() {
    debugger;
    if (typeof (seenFireworks) !== "undefined" && !window.fireworksRan && !seenFireworks) {
        var found = $(".todo-row[data-createdBefore='True']");
        var total =found.length;
        if (total>0)
        {
            var complete = found.find(".todo-checkbox:checked").length;
            if (complete / total >= .9) {
                runFireworks();
            }
        }

    }
}

function runFireworks() {
    window.fireworksRan = true;
    fc.init();

    $.ajax("/L10/ranfireworks/" + recurrenceId);
    
    var shootFirework = function(){

        if (shootFirework) {
            ////http://www.schillmania.com/projects/fireworks/
            //for (var i = 0; i < Math.random() * 2; i++) {
            if (document.hasFocus()) {
                var mult = 1;
                if (Math.random() > .5)
                    mult = -1;
                createFirework(19, 81, 4, 5, 55 + Math.random() * 10*mult, 100, 55 + Math.random() * 22*mult, 55 + Math.random() * 22, true, false, "modal");
            }
            //}
            if (Math.random() > .2) {
                setTimeout(shootFirework, Math.random() * 1000 + 2000);
            } else {
                setTimeout(shootFirework, Math.random() * 500 + 200);
            }
        }
    };
    shootFirework();

    showModal({
        icon: { icon: "icon fontastic-icon-trophy-1", title: "Congratulations!", color: "#FFA500" },
        title: "90% To-do completion!",
        close:function(){
            shootFirework=false;
        }
    });
}