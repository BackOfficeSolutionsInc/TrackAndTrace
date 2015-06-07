
var currentIssuesDetailsId;
$(function () {

	$("body").on("click", ".issues-list>.issue-row", function () {
		var issueRow = $(this);//.closest(".issue-row");
		$(".issue-row.selected").removeClass("selected");
		$(issueRow).addClass("selected");
		currentIssuesDetailsId = $(issueRow).data("recurrence_issue");
		var createtime = $(issueRow).data("createtime");
		var message = $(issueRow).data("message");
		var details = $(issueRow).data("details");
		var issueId = $(issueRow).data("issue");
		var recurrence_issue = $(issueRow).data("recurrence_issue");
		var checked = $(issueRow).find(".issue-checkbox").prop("checked");

		var detailsList = $(issueRow).find(">.dd-list").clone();
		$("#issueDetails").html("");
		$("#issueDetails").append(
			"<span class='expandContract btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs contractButton' title='Hide details'><span class='glyphicon glyphicon-resize-small'></span></span>" +
				"<span class='btn btn-default btn-xs expandButton'  title='Show details'><span class='glyphicon glyphicon-resize-full'></span></span>" +
			"</span>");
		$("#issueDetails").append("<div class='createTime'>" + new Date(createtime).toLocaleDateString() + "</div>");

		$("#issueDetails").append("<div class='heading'><h4 class='message-holder clickable' data-recurrence_issue='" + recurrence_issue + "'><span class='message editable-text' data-recurrence_issue='" + recurrence_issue + "'>" + message + "</span></h4></div>");
		$("#issueDetails").append(detailsList);
		$("#issueDetails").append("<textarea class='details issue-details' data-recurrence_issue='" + recurrence_issue + "'>" + details + "</textarea>");
		$("#issueDetails").append("<div class='button-bar'>" +
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton'><input data-recurrence_issue='" + recurrence_issue + "' class='issue-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Done</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='copymodal' data-recurrence_issue='" + recurrence_issue + "' data-copyto='" + MeetingId + "'><span class='icon fontastic-icon-forward-1'></span> Copy To</span>" +
			"<span class='btn btn-default btn-xs createTodoButton todoModal' data-method='CreateTodoFromIssue' data-meeting='"+meetingId+"' data-issue='"+issueId+"' data-recurrence='"+MeetingId+"' ><span class='glyphicon glyphicon-unchecked todoButton'></span> Todo</span>" +
			"</span>" +
			"<span class='clearfix'></span>" +
			"</div>");
	});

	$("body").on("click", ".issueDetails .message-holder .message", function() {
		var input = $("<input class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html())+ "' onblur='sendIssueMessage(this," + $(this).parent().data("recurrence_issue") + ")'/>");
		$(this).parent().html(input);
		input.focusTextToEnd();
	});


	$("body").on("click", ".detailsBtn", function () {
		if ($(this).is(".sm")) {
			$(".issues-list ol li").slideUp(400);
		} else {
			$(".issues-list ol li").slideDown(400);
		}
	});
	$("body").on("click", ".issueDetails .message", function () { $(this).siblings(".issue-details-container").slideToggle(400); });
	$("body").on("click", ".issueDetails .expandButton", function () { $(".issueDetails .issue-details-container").slideDown(400); });
	$("body").on("click", ".issueDetails .contractButton", function () { $(".issueDetails .issue-details-container").slideUp(400); });
	$("body").on("click", ".issueDetails .doneButton", function () { $(this).find(">input").trigger("click"); });

	$("body").on("change", ".issue-checkbox", function () {
		var issueId = $(this).data("recurrence_issue");
		var checked = $(this).prop("checked");
		var selector = ".issue-checkbox[data-recurrence_issue='" + issueId + "']";
		var selector2 = ".issue-row[data-recurrence_issue='" + issueId + "']";
		var that = this;
		$(selector).prop("disabled", true);
		$(selector).prop("checked", checked);
		$(selector2).data("checked", checked);
		$.ajax({
			url: "/l10/UpdateIssueCompletion/" + recurrenceId,
			method: "post",
			data: { issueId: issueId, checked: checked, connectionId: $.connection.hub.id },
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

function updateIssueCompletion(issueId, complete) {
	var selector = ".issue-checkbox[data-recurrence_issue='" + issueId + "']";
	$(selector).prop("checked", complete);
}

function deserializeIssues(selector, issueList) {
	var sub = "";
	for (var i = 0; i < issueList.issues.length; i++) {
		sub += constructRow(issueList.issues[i]);
	}
	$(selector).html(sub);
	refreshCurrentIssueDetails();
}
function appendIssue(selector, issue) {
	var li = $(constructRow(issue));
	$(selector).prepend(li);
	$(li).flash();
	refreshCurrentIssueDetails();
}

function constructRow(issue) {
	var sub = "";
	if (issue.children) {
		for (var i = 0; i < issue.children.length; i++) {
			sub += constructRow(issue.children[i]);
		}
	}
	var details = "";
	if (issue.details)
		details = issue.details;

	return '<li class="issue-row dd-item arrowkey" data-createtime="' + issue.createtime + '" data-recurrence_issue="' + issue.recurrence_issue + '" data-issue="' + issue.issue + '" data-checked="' + issue.checked + '"  data-message="' + issue.message + '"  data-details="' + issue.details + '">'
		+ '	<input data-recurrence_issue="' + issue.recurrence_issue + '" class="issue-checkbox" type="checkbox" ' + (issue.checked ? "checked" : "") + '/>'
		+ '	<div class="move-icon noselect dd-handle">'
		+ '		<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>'
		+ '		<span class="inner icon fontastic-icon-primitive-square"></span>'
		+ '	</div>'
		+ '<div class="btn-group pull-right"><span class="issuesButton issuesModal icon fontastic-icon-forward-1" data-copyto="' + recurrenceId + '" data-recurrence_issue="' + issue.issue + '" data-method="copymodal"></span></div>'
		+ '	<span class="profile-image">'
		+ '		<span class="profile-picture">' 
		+	'			<span class="picture-container" title="' + issue.owner + '">' 
		+	'				<span class="picture" style="background: url(' + issue.imageUrl + ') no-repeat center center;"></span>' 
		+	'			</span>' 
		+	'		</span>'
		+	'	</span>' 
		+ '	<div class="message" data-recurrence_issue='+issue.issue+'>' + issue.message + '</div>'
		+ '	<div class="issue-details-container"><div class="issue-details" data-recurrence_issue='+issue.issue+'>' + details + '</div></div>'
		+ '<ol class="dd-list">'
		+ sub
		+ '</ol>'
		+ '</li>';
}

function updateIssuesList(recurrenceId, issueRow) {
	var d = { issues: $(issueRow).sortable('serialize').toArray(), connectionId: $.connection.hub.id };
	console.log(d);
	var that = issueRow;
	$.ajax({
		url: "/l10/UpdateIssues/" + recurrenceId,
		data: JSON.stringify(d),
		contentType: "application/json; charset=utf-8",
		method: "POST",
		success: function (d) {
			if (!d.Error) {
				oldIssueList = $(".issues-list").clone(true);
			} else {
				showJsonAlert(d, false, true);
				$(that).html("");
				setTimeout(function () {
					$('.issues-container').html(oldIssueList);
					oldIssueList = $(".issues-list").clone(true);
					refreshCurrentIssueDetails();
				}, 1);
			}
		},
		error: function (a, b) {
			clearAlerts();
			showAlert(a.statusText || b);
			$('.dd').html("");
			setTimeout(function () {
				$('.dd').html(oldIssueList);
				oldIssueList = $(".issues-list").clone(true);
				refreshCurrentIssueDetails();
			}, 1);
		}
	});
}


function refreshCurrentIssueDetails() {
	$(".issue-row[data-recurrence_issue=" + currentIssuesDetailsId + "]")
		.closest(".issues-list>.issue-row").find(">.message")
		.trigger("click");
	$(".issue-row[data-recurrence_issue=" + currentIssuesDetailsId + "]").addClass("selected");
}

function updateIssueMessage(id, message) {
	$(".ids .message[data-recurrence_issue=" + id + "]").html(message);
	$(".ids .issue-row[data-recurrence_issue=" + id + "]").data("message",escapeString(message));
}
function updateIssueDetails(id, details) {
	$(".ids .issue-details[data-recurrence_issue=" + id + "]").html(details);
	$(".ids textarea.issue-details[data-recurrence_issue=" + id + "]").val(details);
	$(".ids .issue-row[data-recurrence_issue=" + id + "]").data("details",escapeString(details));
}

function sendIssueDetails(self,id) {
	var val = $(self).val();
	var data = {
		details: val
	};
	$(".ids .details[data-recurrence_issue=" + id + "]").prop("disabled", true);
	$.ajax({
		method:"POST",
		data:data,
		url: "/L10/UpdateIssue/" + id,
		success:function(data) {
			showJsonAlert(data, false, true);
			$(".ids .details[data-recurrence_issue=" + id + "]").prop("disabled", false);
		}
	});
}

function sendIssueMessage(self,id) {
	var val = $(self).val();
	if (val.trim() == "") {
		$(".issueDetails .message-holder[data-recurrence_issue="+id+"]").html("<span data-recurrence_issue='"+id+"' class='message editable-text'>"+$(self).data("old")+"</span>");
		return;
	}
	
	$(".ids .message-holder[data-recurrence_issue=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};
	$.ajax({
		method:"POST",
		data:data,
		url: "/L10/UpdateIssue/" + id,
		success:function(data) {
			showJsonAlert(data, false, true);
			if (!data.Error) {
				$(".issueDetails .message-holder[data-recurrence_issue="+id+"]").html("<span data-recurrence_issue='"+id+"' class='message'>"+val+"</span>");
			}
		}
	});
}

$("body").on("blur", ".issueDetails .details", function() {
	sendIssueDetails(this,$(this).data("recurrence_issue"));
});