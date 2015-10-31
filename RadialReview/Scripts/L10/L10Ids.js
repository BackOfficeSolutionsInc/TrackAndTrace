
var currentIssuesDetailsId;
$(function () {

    refreshCurrentIssueDetails();
	fixIssueDetailsBoxSize();
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
		var accountable = $(issueRow).attr("data-accountable");
		var owner = $(issueRow).attr("data-owner");

		var ownerStr = owner;
		if (!owner || owner.trim() === "")
			ownerStr = "(unassigned)";

		var detailsList = $(issueRow).find(">.dd-list").clone();
		$("#issueDetails").html("");
		$("#issueDetails").append(
			"<span class='expandContract btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs contractButton' title='Hide details'><span class='glyphicon glyphicon-resize-small'></span></span>" +
				"<span class='btn btn-default btn-xs expandButton'  title='Show details'><span class='glyphicon glyphicon-resize-full'></span></span>" +
			"</span>");
		$("#issueDetails").append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");

		$("#issueDetails").append("<div class='heading'><h4 class='message-holder clickable' data-recurrence_issue='" + recurrence_issue + "'><span class='message editable-text' data-recurrence_issue='" + recurrence_issue + "'>" + message + "</span></h4></div>");
		$("#issueDetails").append(detailsList);
		$("#issueDetails").append("<textarea class='details issue-details' data-recurrence_issue='" + recurrence_issue + "'>" + details + "</textarea>");
		$("#issueDetails").append("<div class='button-bar'>" +
			"<div style='height:28px;'>"+
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton'><input data-recurrence_issue='" + recurrence_issue + "' class='issue-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Resolved</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='copymodal' data-recurrence_issue='" + recurrence_issue + "' data-copyto='" + MeetingId + "'><span class='icon fontastic-icon-forward-1'></span> Copy To</span>" +
			"<span class='btn btn-default btn-xs createTodoButton todoModal' data-method='CreateTodoFromIssue' data-meeting='"+meetingId+"' data-issue='"+issueId+"' data-recurrence='"+MeetingId+"' ><span class='glyphicon glyphicon-unchecked todoButton'></span> To-Do</span>" +
			"</span>" +
			"</div>"+
			"<span class='clearfix'></span>" +
			"<span class='gray' style='width:75px;display:inline-block'>Assigned to:</span>"+
			"<span>"+
				"<span style='width:250px;padding-left:10px;' class='assignee' data-accountable='" + accountable + "' data-recurrence_issue='" + recurrence_issue + "'  >"+
					"<span data-recurrence_issue='" + recurrence_issue + "' class='btn btn-link owner'>" + ownerStr + "</span>"+
				"</span>" +
			"</span>" +

			"</div>");

		fixIssueDetailsBoxSize();
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

$("body").on("click", ".issueDetails .assignee .btn", function () {
		var that = $(this).parent();
		$.ajax({
			method: "POST",
			url: "/L10/Members/" + recurrenceId,
			success: function (data) {
				if (showJsonAlert(data)) {

					var input = $("<select data-recurrence_issue='" + $(that).data("recurrence_issue") + "'/>");

					for (var i = 0; i < data.Object.length; i++) {
						var d = data.Object[i];
						var selected = $(that).attr("data-accountable") == d.id ? "selected" : "";
						$(input).append("<option " + selected + " data-img='" + d.imageUrl + "' value='" + d.id + "'>" + d.name + "</option>");
					}

					$(input).on('change', function () {
						sendNewIssueAccountable(this, $(this).data("recurrence_issue"));
					});

					$(that).html(input);

					var item = $(input).select2({
						templateResult: imageListFormat,
						templateSelection: imageListFormat
					});
					$(item).parent().find(".select2").css("width", "inherit");
					//$(item).parent().find("span").css(
					  /* right: 6px; */
					$(input).select2("open");
				}
			}
		});
	});

$(window).resize(fixIssueDetailsBoxSize);
$(window).on("page-ids",fixIssueDetailsBoxSize);
$(window).on("footer-resize",function() {
	setTimeout(fixIssueDetailsBoxSize, 250);
});

function fixIssueDetailsBoxSize() {
	if ($(".details.issue-details").length) {
		var wh = $(window).height();
		var pos = $(".details.issue-details").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
			footerH = $(".footer-bar .footer-bar-container").last().offset().top;
		} catch (e) {

		}

		//$(".details.issue-details").height(wh - pos.top + st - footerH - 110);
		$(".details.issue-details").height(footerH - 20 - 110 - pos.top  );
	}
}

function sendNewIssueAccountable(self, id) {
	var val = $(self).val();
	var data = {
		owner: val
	};
	var found = $(".ids .assignee[data-recurrence_issue=" + id + "]");
	found.html("<span class='btn btn-link' data-recurrence_issue='" + id + "'></span>");
	found.attr("data-accountable", val);
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateIssue/" + id,
		success: function (data) {
			if (showJsonAlert(data, false, true)) {
			}
		}
	});
}

function changeMode(type) {
    $(".ids .issues-container").removeClass("columns");
    $(".ids .issues-container").removeClass("double");
    $(".ids .issues-container").removeClass("triple");
    $(".ids .issues-container").removeClass("quadruple");

    if (type == "Prioritize (3 Columns)") {
        $(".ids .issues-container").addClass("columns");
        $(".ids .issues-container").addClass("triple");
    }else if (type == "Prioritize (2 Columns)") {
        $(".ids .issues-container").addClass("columns");
        $(".ids .issues-container").addClass("double");
    }else if (type == "Prioritize (4 Columns)") {
        $(".ids .issues-container").addClass("columns");
        $(".ids .issues-container").addClass("quadruple");
    } else if (type == "Reorder") {

    }
    refreshCurrentIssueDetails();
}

function sortIssueBy(recurrenceId, issueList,sortBy,title,mult) {
	mult = mult || 1;

	//$(".sort-button").html("Sort by " + title);

	$(issueList).children().detach().sort(function(a, b) {
		if ($(a).attr(sortBy)===$(b).attr(sortBy))
			return mult*$(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
		return mult*$(a).attr(sortBy).localeCompare($(b).attr(sortBy));
	}).appendTo($(issueList));
	updateIssuesList(recurrenceId, issueList, sortBy);
	refreshCurrentIssueDetails();

}

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
function appendIssue(selector, issue, order) {
	var li = $(constructRow(issue));
	if (typeof(order) !== "undefined") {
		if (order == "data-priority") {
			var found = $(">li", selector).filter(function() {
				return +$(this).attr("data-priority") > 0;
			}).last();
			if (found.length == 0) {
				$(selector).prepend(li);
			} else {
				$(li).insertAfter(found);
			}
			$(li).flash();
			refreshCurrentIssueDetails();
			return;
		}
	}
	//fallback version
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

	return '<li class="issue-row dd-item arrowkey" data-createtime="' + issue.createtime + '" data-recurrence_issue="' + issue.recurrence_issue + '" data-issue="' + issue.issue + '" data-checked="' + issue.checked + '"  data-message="' + issue.message + '"  data-details="' + issue.details + '"  data-owner="' + issue.owner + '" data-accountable="' + issue.accountable + '"  data-priority="' + issue.priority + '">\n'
		+ '	<input data-recurrence_issue="' + issue.recurrence_issue + '" class="issue-checkbox" type="checkbox" ' + (issue.checked ? "checked" : "") + '/>\n'
		+ '	<div class="move-icon noselect dd-handle">\n'
		+ '		<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>\n'
		+ '		<span class="inner icon fontastic-icon-primitive-square"></span>\n'
		+ '	</div>\n'
		+ '<div class="btn-group pull-right">\n'
		+ ' <span class="issuesButton issuesModal icon fontastic-icon-forward-1" data-copyto="' + recurrenceId + '" data-recurrence_issue="' + issue.issue + '" data-method="copymodal" style="padding-right: 5px"></span>\n'	
		+ ' <span class="glyphicon glyphicon-unchecked todoButton issuesButton todoModal" data-issue="'+issue.issue+'" data-meeting="'+issue.createdDuringMeetingId+'" data-recurrence="'+recurrenceId+'" data-method="CreateTodoFromIssue"></span>\n'
		+ '</div>\n'
		+ '<div class="number-priority">\n'
		+	' <span class="number"></span>\n'
        +	' <span class="priority" data-priority="'+issue.priority+'"></span>\n'
		+ '</div>\n'
		+ '<span class="profile-image">\n'
		+ '		<span class="profile-picture">\n' 
		+	'			<span class="picture-container" title="' + issue.owner + '">\n' 
		+	'				<span class="picture" style="background: url(' + issue.imageUrl + ') no-repeat center center;"></span>\n' 
		+	'			</span>\n' 
		+	'		</span>\n'
		+	'	</span>\n' 
		+ '	<div class="message" data-recurrence_issue='+issue.issue+'>' + issue.message + '</div>\n'
		+ '	<div class="issue-details-container"><div class="issue-details" data-recurrence_issue='+issue.issue+'>' + details + '</div></div>\n'
		+ '<ol class="dd-list">'
		+ sub
		+ '</ol>\n'
		+ '</li>';
}

function updateIssuesList(recurrenceId, issueRow,orderby) {
	var d = { issues: $(issueRow).sortable('serialize').toArray(), connectionId: $.connection.hub.id,orderby:orderby };
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


	$(".issues-list > .issue-row > .number-priority > .number").each(function (i) {
	    $(this).html(i + 1);
	});

	$(".issues-list > .issue-row > .number-priority > .priority").each(function (i) {
	    var p = $(this).attr("data-priority");
	    $(this).removeClass("multiple");
	    $(this).removeClass("none");
	    $(this).removeClass("single");
	    $(this).removeClass("single-1");
	    $(this).removeClass("single-2");
	    $(this).removeClass("single-3");

	    $(this).parents(".issue-row").toggleClass("prioritize", p > 0);

	    if (p > 3) {
	        $(this).addClass("multiple");
	        $(this).html("<span class='icon fontastic-icon-star-3'></span> x" +p);
	    } else if (p > 0 && p<=3) {
	        $(this).addClass("single");
	        $(this).addClass("single-"+p);
	        var str = "";
	        for (var i = 0; i < p; i++) {
	            str += "<span class='icon fontastic-icon-star-3'></span>";
	        }
		    if (p == 1)
			    str += "<span class='hoverable'>+</span>";
	        $(this).html(str);
	    } else if (p == 0) {
	        $(this).addClass("none");
	        $(this).html("<span class='icon fontastic-icon-star-empty'></span>");
	    }

	});
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


function updateIssueOwner(id, userId, name, image) {
    $(".ids [data-recurrence_issue=" + id + "] .picture-container").prop("title", name);
    $(".ids [data-recurrence_issue=" + id + "] .picture").css("background", "url(" + image + ") no-repeat center center");
    $(".ids .assignee .btn[data-recurrence_issue=" + id + "]").html(name);
    var row = $(".ids .issue-row[data-recurrence_issue=" + id + "]");
    $(row).attr("data-owner", name);
    $(row).attr("data-accountable", userId);
    $(row).attr("data-imageurl", image);
}

function updateIssuePriority(id, priority) {
    $(".ids .issue-row[data-recurrence_issue=" + id + "] > .number-priority > .priority").attr("data-priority", priority)
    var row = $(".ids .issue-row[data-recurrence_issue=" + id + "]");
    $(row).attr("data-priority", priority);
}

$("body").on("contextmenu", ".issue-row .priority", function (e) {
    e.preventDefault();
    return false;
});
$("body").on("mousedown", ".issue-row .priority", function (e) {
    debugger;
    var p = +$(this).attr("data-priority");
    if (e.button==0){
        p += 1;
    }else if (e.button==2){
        p -= 1;
        p = Math.max(0, p);
    }else {
        return false;
    }
    $(this).attr("data-priority", p);
    var id = $(this).parents(".issue-row").attr("data-recurrence_issue");
    updateIssuePriority(id,p);
    refreshCurrentIssueDetails();
    var d = { priority: p,time:new Date().getTime() };
    $.ajax({
        url: "/L10/UpdateIssue/" + id,
        data: d,
        method: "POST",
        success: function (d) {
            showJsonAlert(d);
        }
    });
    e.preventDefault();
    return false;
});
