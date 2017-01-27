var currentIssuesDetailsId;

var modalWidth = 991;
$(function () {
	var CheckOffIssue = Undo.Command.extend({
		constructor: function (row) {
			this.row = row;
			this.issueRow = $(row).closest(".issue-row");
			if ($(this.issueRow).hasClass("selected")) {
				$(this.issueRow).addClass("wasSelected");
				if ($(window).width() > modalWidth) {
					var next = $(this.issueRow);
					while (true) {
						next = next.next();
						if (next.find("input:checked").length > 0)
							continue;
						break;
					}
					if (next.length == 0) {
						var next = $(this.issueRow);
						while (true) {
							next = next.prev();
							if (next.find("input:checked").length > 0)
								continue;
							break;
						}
					}

					setTimeout(function () {
						try {
							if (next.length) {
								$(next).click();
							} else {
								$("#issueDetails").html("");
							}
						} catch (e) {
						}
					}, 500);
				}
			}
			this.checked = $(row).prop("checked");
			this.func = function (checked) {
				var issueId = $(this.row).data("recurrence_issue");
				var selector = ".issue-checkbox[data-recurrence_issue='" + issueId + "']";
				var selector2 = ".issue-row[data-recurrence_issue='" + issueId + "']";
				var that = this.row;
				$(selector).prop("disabled", true);
				$(selector).prop("checked", checked);
				$(selector2).data("checked", checked);
				$.ajax({
					url: "/l10/UpdateIssueCompletion/" + window.recurrenceId,
					method: "post",
					data: { issueId: issueId, checked: checked, connectionId: $.connection.hub.id },
					success: function (data) {
						showJsonAlert(data, false, true);
						$(selector).prop("checked", (!data.Error ? data.Object : !checked));
						$(selector2).data("checked", (!data.Error ? data.Object : !checked));
						$(selector2).attr("data-checked", (!data.Error ? data.Object : !checked));
						$(selector2).toggleClass("skipNumber", (!data.Error ? data.Object : !checked));
						refreshCurrentIssueDetails();
						refreshRanks();
					},
					error: function () {
						$(selector).prop("checked", !checked);
						$(selector2).data("checked", !checked);
						$(selector2).attr("data-checked", !checked);
						$(selector2).toggleClass("skipNumber", !checked);
						refreshCurrentIssueDetails();
					},
					complete: function () {
						$(selector).prop("disabled", false);
					}
				});
			}
		},
		execute: function () {
			this.func(this.checked);
		},
		undo: function () {
			this.func(!this.checked);
			if ($(this.issueRow).hasClass("wasSelected")) {
				var selected = $(this.issueRow).removeClass("wasSelected");
				if ($(window).width() > modalWidth) {
					$(selected).click();
				}
			}
		}
	});


	var MoveIssueToVTO = Undo.Command.extend({
		constructor: function (row) {
			this.rowId = row.data("recurrence_issue");
		},
		execute: function () {
			var row = $("[data-recurrence_issue='" + this.rowId + "']");
			var rowId = this.rowId;
			var that = this;
			$.ajax({
				url: "/l10/moveissuetovto/" + rowId,
				success: function (data) {
					if (showJsonAlert(data)) {
						that.revertId = data.Object;
						$(row).remove();
						refreshCurrentIssueDetails();
						refreshRanks();
					}
				}
			});
		},
		undo: function () {
			var row = $("[data-recurrence_issue='" + this.rowId + "']");
			var rowId = this.rowId;
			$.ajax({
				url: "/l10/MoveIssueFromVto/" + this.revertId,
				success: function (data) {
					if (showJsonAlert(data)) {
						$(row).removeClass("skipNumber");
						refreshCurrentIssueDetails();
						refreshRanks();
					}
				}
			});
		}
	});

	refreshCurrentIssueDetails();
	fixIssueDetailsBoxSize();
	$("body").on("click", ".issues-list>.issue-row:not(.undoable)", function (evt) {
		if ($(evt.target).hasClass("rank123"))
			return;
		if ($(evt.target).hasClass("issue-checkbox"))
			return;
		if ($(evt.target).hasClass("todoButton"))
			return;
		if ($(evt.target).hasClass("issuesButton"))
			return;
		if ($(evt.target).hasClass("vtoButton"))
			return;
		var issueRow = $(this);
		$(".issue-row.selected").removeClass("selected");
		$(issueRow).addClass("selected");
		var tempRowId = $(issueRow).data("recurrence_issue");

		var w = $(window).width();


		if (tempRowId == currentIssuesDetailsId && w > modalWidth)
			return;
		currentIssuesDetailsId = tempRowId;

		var createtime = $(issueRow).data("createtime");
		var message = $(issueRow).data("message");
		var details = $(issueRow).data("details");
		var issueId = $(issueRow).data("issue");
		var recurrence_issue = $(issueRow).data("recurrence_issue");
		var checked = $(issueRow).find(".issue-checkbox").prop("checked");
		var accountable = $(issueRow).attr("data-accountable");
		var owner = $(issueRow).attr("data-owner");
		var padid = $(issueRow).data("padid");

		var ownerStr = owner;
		if (!owner || owner.trim() === "")
			ownerStr = "(unassigned)";

		var detailsList = $(issueRow).find(">.dd-list").clone();

		var detailsContents = $("<div class='issueDetails abstract-details-panel'></div>");

		// $("#issueDetails").html("");
		$(detailsContents).append("<div class='heading'><h4 class='message-holder clickable on-edit-enabled' data-recurrence_issue='" + recurrence_issue + "'><span class='message editable-text' data-recurrence_issue='" + recurrence_issue + "'>" + message + "</span></h4></div>");
		$(detailsContents).append(detailsList);
		$(detailsContents).append("<iframe class='details issue-details' name='embed_readwrite' src='/Issues/Pad/" + issueId + "' width='100%' height='100%'></iframe>");

		$(detailsContents).append("<div class='button-bar'>" +
			"<div style='height:28px;'>" +
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton on-edit-enabled'><input data-recurrence_issue='" + recurrence_issue + "' class='issue-checkbox hidden' type='checkbox' " + (checked ? "checked" : "") + "/> Resolve</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal on-edit-enabled' data-method='copymodal' data-recurrence_issue='" + recurrence_issue + "' data-copyto='" + window.recurrenceId + "'><span class='icon fontastic-icon-forward-1' title='Move issue to another L10'></span> Move To</span>" +
			"<span class='btn btn-default btn-xs createTodoButton todoModal on-edit-enabled' data-method='CreateTodoFromIssue' data-meeting='" + window.meetingId + "' data-issue='" + issueId + "' data-recurrence='" + window.recurrenceId + "' ><span class='glyphicon glyphicon-unchecked todoButton'></span> To-Do</span>" +
			"</span>" +
			"</div>" +
			"<span class='clearfix'></span>" +
			"<span class='gray' style='width:75px;display:inline-block'>Owned By:</span>" +
			"<span>" +
				"<span style='width:250px;padding-left:10px;' class='assignee on-edit-enabled' data-accountable='" + accountable + "' data-recurrence_issue='" + recurrence_issue + "'  >" +
					"<span data-recurrence_issue='" + recurrence_issue + "' class='btn btn-link owner'>" + ownerStr + "</span>" +
				"</span>" +
			"</span>" +
			"</div>");
		$("#issueDetails").html("");
		if (w <= modalWidth) {
			var c = detailsContents.clone();
			c.find("h4").addClass("form-control");
			showModal({
				contents: c,
				title: "Edit Issue",
				noCancel: true,
			});
		} else {
			$("#issueDetails").append(detailsContents);
			fixIssueDetailsBoxSize();
		}
	});


	$("body").on("click", ".issues-list>.issue-row .vtoButton:not(.disabled)", function () {
		var row = $(this).closest(".issue-row");
		undoStack.execute(new MoveIssueToVTO(row));
	});

	$("body").on("click", ".issueDetails .message-holder .message", function () {
		var input = $("<textarea class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html()) + "' onblur='sendIssueMessage(this," + $(this).parent().data("recurrence_issue") + ")'>" + $(this).html() + "</textarea>");
		$(this).closest(".form-control").addClass("focus");
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
	$("body").on("click", ".issueDetails .doneButton input", function () { /*e.preventDefault();*/ });
	$("body").on("click", ".issueDetails .doneButton", function () { $(".issue-row.selected").find(">input").trigger("click"); $("#modal").modal("hide"); });

	$("body").on("change", ".issue-checkbox", function () {
		undoStack.execute(new CheckOffIssue(this, $(this).prop("checked")));
	});
});

$("body").on("click", ".issueDetails .assignee .btn", function () {
	var that = $(this).parent();
	$.ajax({
		method: "POST",
		url: "/L10/Members/" + window.recurrenceId,
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
				$(input).select2("open");
			}
		}
	});
});

$(window).resize(fixIssueDetailsBoxSize);
$(window).on("page-ids", fixIssueDetailsBoxSize);
$(window).on("footer-resize", function () {
	setTimeout(fixIssueDetailsBoxSize, 250);
});

function fixIssueDetailsBoxSize() {
	if ($(".details.issue-details").length) {
		var wh = $(window).height();
		var pos = $(".details.issue-details").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
			footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) {
		}
		$(".details.issue-details").height(footerH - 20 - 110 - pos.top);
	}
}

function sendNewIssueAccountable(self, id) {
	var val = $(self).val();
	var data = {
		owner: val
	};
	var found = $(".ids .assignee[data-recurrence_issue=" + id + "]");
	//found.html("<span class='btn btn-link' data-recurrence_issue='" + id + "'></span>");
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
	} else if (type == "Prioritize (2 Columns)") {
		$(".ids .issues-container").addClass("columns");
		$(".ids .issues-container").addClass("double");
	} else if (type == "Prioritize (4 Columns)") {
		$(".ids .issues-container").addClass("columns");
		$(".ids .issues-container").addClass("quadruple");
	} else if (type == "Reorder") {

	}
	refreshCurrentIssueDetails();
}
function sortIssueByCurrent(recurId, issueList) {
	if ($(".meeting-page").hasClass("prioritization-Rank"))
		return sortIssueBy(recurId, issueList, "data-rank", "Priority");
	else
		return sortIssueBy(recurId, issueList, "data-priority", "Votes", -1);
}

function sortIssueBy(recurId, issueList, sortBy, title, mult) {
	mult = mult || 1;

	//$(".sort-button").html("Sort by " + title);

	$(issueList).children().detach().sort(function (a, b) {
		if (sortBy == "data-priority") {
			if ($(a).data("priority") === $(b).data("priority"))
				return mult * $(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
			return mult * ($(a).data("priority") - $(b).data("priority"));
		} else if (sortBy == "data-rank") {
			var aa = +$(a).attr(sortBy);
			var bb = +$(b).attr(sortBy);
			if (aa == 0)
				aa = Number.MAX_VALUE;
			if (bb == 0)
				bb = Number.MAX_VALUE;
			if (aa === bb)
				return mult * $(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
			return mult * (aa - bb);
		} else {
			if ($(a).attr(sortBy) === $(b).attr(sortBy))
				return mult * $(a).attr("data-message").toUpperCase().localeCompare($(b).attr("data-message").toUpperCase());
			return mult * $(a).attr(sortBy).localeCompare($(b).attr(sortBy));
		}
	}).appendTo($(issueList));
	updateIssuesList(recurId, issueList, sortBy);
	refreshCurrentIssueDetails();

}

function updateIssueCompletion(issueId, complete) {
	var selector = ".issue-checkbox[data-recurrence_issue='" + issueId + "']";
	$(selector).prop("checked", complete);
	$(selector).parents(".issue-row").attr("data-checked", complete);
	$(selector).parents(".issue-row").data("checked", complete);

	refreshCurrentIssueDetails();
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
	if (typeof (order) !== "undefined") {
		if (order == "data-priority") {
			var priority = issue.priority;
			if (typeof (priority) == "undefined")
				priority = 0;
			var found = $(">li", selector).filter(function () {
				return +$(this).data("priority") > priority;
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
		if (order == "data-rank") {
			var rank = issue.rank;
			if (typeof (rank) == "undefined")
				rank = 0;
			var found = $(">li:not(.skipNumber):not([data-checked='true']):not([data-checked='True'])", selector).filter(function () {
				return +$(this).data("rank") > rank;
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

	return '<li class="issue-row dd-item arrowkey undoable-stripped"  data-createtime="' + issue.createtime + '" data-recurrence_issue="' + issue.recurrence_issue + '" data-issue="' + issue.issue + '" data-checked="' + issue.checked + '"  data-message="' + issue.message + '"  data-details="' + issue.details + '"  data-owner="' + issue.owner + '" data-accountable="' + issue.accountable + '"  data-priority="' + issue.priority + '"  data-rank="' + issue.rank + '">\n'
        + '<span class="undo-button">Undo</span>'
        + '<input data-recurrence_issue="' + issue.recurrence_issue + '" class="issue-checkbox" type="checkbox" ' + (issue.checked ? "checked" : "") + '/>\n'
		+ '<div class="move-icon noselect dd-handle">\n'
		+ '<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>\n'
		+ '<span class="inner icon fontastic-icon-primitive-square"></span>\n'
		+ '</div>\n'
		+ '<div class="btn-group pull-right">\n'
		+ '<span class="issuesButton issuesModal icon fontastic-icon-forward-1" data-copyto="' + window.recurrenceId + '" data-recurrence_issue="' + issue.recurrence_issue + '" data-method="copymodal" style="padding-right: 5px"></span>\n'
		+ '<span class="glyphicon glyphicon-unchecked todoButton issuesButton todoModal" data-issue="' + issue.issue + '" data-meeting="' + issue.createdDuringMeetingId + '" data-recurrence="' + window.recurrenceId + '" data-method="CreateTodoFromIssue" style="padding-right:5px;"></span>\n'
        + '<span class="glyphicon glyphicon-vto vtoButton"></span>'
        + '</div>\n'
		+ '<div class="number-priority">\n'
		+ '<span class="number"></span>\n'
        + '<span class="priority" data-priority="' + issue.priority + '"></span>\n'
        + '<span class="rank123 badge" data-rank="' + issue.rank + '">IDS</span>\n'
		+ '</div>\n'
		+ '<span class="profile-image">\n'
        + '' + profilePicture(issue.imageUrl, issue.owner) + ''
		//+ '		<span class="profile-picture">\n'
		//+ '			<span class="picture-container" title="' + issue.owner + '">\n'
		//+ '				<span class="picture" style="background: url(' + issue.imageUrl + ') no-repeat center center;"></span>\n'
		//+ '			</span>\n'
		//+ '		</span>\n'
		+ '</span>\n'
		+ '<div class="message" data-recurrence_issue=' + issue.recurrence_issue + '>' + issue.message + '</div>\n'
		+ '<div class="issue-details-container"><div class="issue-details" data-recurrence_issue=' + issue.issue + '>' + details + '</div></div>\n'
		+ '<ol class="dd-list"> '
		+ sub
		+ '</ol>\n'
		+ '</li>';
}

function _detatchAllIssues() {
	var items = $(".issues-list li");
	items.detach();
	for (var i = 0; i < items.length; i++) {
		items.find("ol li").remove();
	}
	return items;
}

function _setIssueOrder(parentSelector, parentOrders, all) {
	for (var i = 0; i < parentOrders.length; i++) {
		var p = $(all).filter("[data-recurrence_issue=" + parentOrders[i].id + "]");
		var ol = p.find("ol");
		var children = _setIssueOrder(ol, parentOrders[i].children, all);
		parentSelector.append(p);
	}
}

function setIssueOrder(order) {
	var allIssues = _detatchAllIssues();
	_setIssueOrder($(".issues-list"), order, allIssues);
	refreshCurrentIssueDetails();
}

function _getIssueOrder(issue) {
	var children = [];
	for (var i = 0; i < issue.children.length; i++) {
		children.push(_getIssueOrder(issue.children[i]));
	}
	return {
		id: issue.recurrence_issue,
		children: children
	};
}

function getIssueOrder() {
	var items = $(".issues-list").sortable('serialize').toArray()
	var output = [];
	for (var i = 0; i < items.length; i++) {
		output.push(_getIssueOrder(items[i]));
	}
	return output;
}

function updateIssuesList(recurId, issueRow, orderby) {
	var order = getIssueOrder();
	var d = { issues: order, connectionId: $.connection.hub.id, orderby: orderby };
	console.log(d);
	var that = issueRow;
	$.ajax({
		url: "/l10/UpdateIssues/" + recurId,
		data: JSON.stringify(d),
		contentType: "application/json; charset=utf-8",
		method: "POST",
		success: function (d) {
			if (!d.Error) {
				oldIssueList = order;
			} else {
				showJsonAlert(d, false, true);
				$(that).html("");
				setTimeout(function () {
					setIssueOrder(oldIssueList);
				}, 1);
			}
		},
		error: function (a, b) {
			clearAlerts();
			showAlert(a.statusText || b);
			setTimeout(function () {
				setTodoOrder(oldTodoList);
				refreshCurrentIssueDetails();
			}, 1);
		}
	});
}


function refreshPriority(priorityDom) {

	var p = $(priorityDom).data("priority");
	$(priorityDom).removeClass("multiple");
	$(priorityDom).removeClass("none");
	$(priorityDom).removeClass("single");
	$(priorityDom).removeClass("single-1");
	$(priorityDom).removeClass("single-2");
	$(priorityDom).removeClass("single-3");

	$(priorityDom).parents(".issue-row").toggleClass("prioritize", p > 0);

	if (p > 3) {
		$(priorityDom).addClass("multiple");
		$(priorityDom).html("<span class='icon fontastic-icon-star-3'></span> x" + p);
	} else if (p > 0 && p <= 3) {
		$(priorityDom).addClass("single");
		$(priorityDom).addClass("single-" + p);
		var str = "";
		for (var i = 0; i < p; i++) {
			str += "<span class='icon fontastic-icon-star-3'></span>";
		}
		if (p == 1)
			str += "<span class='hoverable'>+</span>";
		$(priorityDom).html(str);
	} else if (p == 0) {
		$(priorityDom).addClass("none");
		$(priorityDom).html("<span class='icon fontastic-icon-star-empty'></span>");
	}
}

function refreshCurrentIssueDetails() {


	console.log("called refreshCurrentIssueDetails")
	if ($(window).width() > modalWidth) {
		$(".issue-row[data-recurrence_issue=" + currentIssuesDetailsId + "]")
			.closest(".issues-list>.issue-row").find(">.message")
			.trigger("click");
	}
	$(".issue-row[data-recurrence_issue=" + currentIssuesDetailsId + "]").addClass("selected");


	$(".issues-list > .issue-row:not(.undoable):not([data-checked='True']):not([data-checked='true']):not(.skipNumber) > .number-priority > .number").each(function (i) {
		$(this).html(i + 1);
	});

	$(".issues-list > .issue-row > .number-priority > .priority").each(function (i) {
		refreshPriority(this);
	});
}

function updateIssueMessage(id, message) {
	$(".ids .message[data-recurrence_issue=" + id + "]").html(message);
	$(".ids .issue-row[data-recurrence_issue=" + id + "]").data("message", escapeString(message));
}
function updateIssueDetails(id, details) {
	$(".ids .issue-details[data-recurrence_issue=" + id + "]").html(details);
	$(".ids textarea.issue-details[data-recurrence_issue=" + id + "]").val(details);
	$(".ids .issue-row[data-recurrence_issue=" + id + "]").data("details", escapeString(details));
}

function sendIssueDetails(self, id) {
	var val = $(self).val();
	var data = {
		details: val
	};
	$(".ids .details[data-recurrence_issue=" + id + "]").prop("disabled", true);
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateIssue/" + id,
		success: function (data) {
			showJsonAlert(data, false, true);
			$(".ids .details[data-recurrence_issue=" + id + "]").prop("disabled", false);
		}
	});
}

function sendIssueMessage(self, id) {
	var val = $(self).val();
	$(self).closest(".form-control").removeClass("focus");
	if (val.trim() == "") {
		$(".issueDetails .message-holder[data-recurrence_issue=" + id + "]").html("<span data-recurrence_issue='" + id + "' class='message editable-text'>" + $(self).data("old") + "</span>");
		return;
	}

	$(".ids .message-holder[data-recurrence_issue=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateIssue/" + id,
		success: function (data) {
			showJsonAlert(data, false, true);
			if (!data.Error) {
				$(".issueDetails .message-holder[data-recurrence_issue=" + id + "]").html("<span data-recurrence_issue='" + id + "' class='message'>" + val + "</span>");
			}
		}
	});
}

function unstarAll() {
	$(".ids .issue-row").filter(function () {
		var i = $(this).find(".number-priority > .priority");
		return i.data("priority") > 0;
	}).each(function () {
		sendPriority($(this).data("recurrence_issue"), 0);
	});

	$(".ids .issue-row").filter(function () {
		var i = $(this).find(".number-priority > .rank123 ");
		return i.data("rank") > 0;
	}).each(function () {
		var id = $(this).attr("data-recurrence_issue");
		updateIssueRank(id, 0, true);
		refreshRanks();

		////DEBOUNCE
		var pp = +$(this).data("rank");
		var d = { rank: pp, time: new Date().getTime() };
		//console.log("D - " + pp);
		$.ajax({
			url: "/L10/UpdateIssue/" + id,
			data: d,
			method: "POST",
			success: function (d) {
				showJsonAlert(d);
			}
		});
	});

}

$("body").on("blur", ".issueDetails .details", function () {
	sendIssueDetails(this, $(this).data("recurrence_issue"));
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
	var dom = $(".ids .issue-row[data-recurrence_issue=" + id + "] > .number-priority > .priority").data("priority", priority);
	var row = $(".ids .issue-row[data-recurrence_issue=" + id + "]");
	$(row).data("priority", priority);
	refreshPriority(dom);
}

var refreshRankTimer = null;
var refreshRankArr = [];
function refreshRanks(last) {
	var ranks = $(".issues-list >.issue-row:not('.skipNumber'):not([data-checked='true']):not([data-checked='True']) > .number-priority > .rank123")
        .filter(function () { return $(this).data("rank") > 0 }).sort(function (a, b) {
        	return $(a).data("rank") - $(b).data("rank");
        });

	if (typeof (last) === "undefined")
		last = 100000;

	for (var i = 0; i < ranks.length; i++) {
		last = Math.min($(ranks[i]).data("rank"), last);
	}
	if (last == 100000)
		last = 0;
	else
		last -= 1;

	var diff = 0;
	// console.log("A - " + last);
	for (var i = 0; i < ranks.length; i++) {
		var r = ranks[i];
		var cur = $(r).data("rank");
		//console.log(" B - " + cur);
		if (cur != last + 1) {
			// console.log("  C - " + (last + 1));
			$(r).data("rank", last + 1);
			$(r).attr("data-rank", last + 1);
			cur = last + 1;
			var row = $(r).closest(".issue-row");
			row.data("rank", cur);
			row.attr("data-rank", cur);
			var id = $(row).attr("data-recurrence_issue");
			var d = { id: id, rank: cur, time: new Date().getTime() };
			refreshRankArr.push(d);
		}
		last = cur;
	}

	if (refreshRankArr.length > 0) {
		if (refreshRankTimer != null)
			clearTimeout(refreshRankTimer);
		refreshRankTimer = setTimeout(function () {
			$.ajax({
				url: "/L10/UpdateIssuesRank/",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				data: JSON.stringify(refreshRankArr),
				method: "POST",
				success: function (d) {
					showJsonAlert(d);
				}
			});
			refreshRankArr = [];
		}, 50);
	}
}

function updateIssueRank(id, rank, skipRefresh) {
	var dom = $(".ids .issue-row[data-recurrence_issue=" + id + "] > .number-priority > .rank123");
	dom.data("rank", rank);
	dom.attr("data-rank", rank);
	var row = $(".ids .issue-row[data-recurrence_issue=" + id + "]");
	$(row).data("rank", rank);
	$(row).attr("data-rank", rank);
	$(row).data("rank-time", +new Date());
	if (!skipRefresh)
		refreshRanks();
}

$("body").on("contextmenu", ".issue-row .priority", function (e) {
	e.preventDefault();
	return false;
});

$("body").on("contextmenu", ".issue-row .rank123", function (e) {
	e.preventDefault();
	return false;
});

function sendPriority(id, priority) {
	var d = { priority: priority, time: new Date().getTime() };
	$.ajax({
		url: "/L10/UpdateIssue/" + id,
		data: d,
		method: "POST",
		success: function (d) {
			showJsonAlert(d);
		}
	});
}

$(function () {
	var priorityTimer = {};
	$("body").on("mousedown", ".issue-row .priority", function (e) {
		var p = +$(this).data("priority");
		console.log("current priority:" + p);
		if (e.button == 0) {
			p += 1;

			if (p > 6)
				p = 0;
		} else if (e.button == 2 || e.which == 3) {
			p -= 1;
			p = Math.max(0, p);
		} else {
			return false;
		}


		console.log("new priority:" + p);
		var id = $(this).parents(".issue-row").attr("data-recurrence_issue");

		updateIssuePriority(id, p);

		////DEBOUNCE
		if (priorityTimer[id]) {
			clearTimeout(priorityTimer[id]);
		}
		var that = this;
		priorityTimer[id] = setTimeout(function () {
			var pp = +$(that).data("priority");
			sendPriority(id, pp);
		}, 500);
		e.preventDefault();
		return false;
	});
	var rankTimer = {};
	var currentRank = 1;



	$("body").on("mousedown", ".issue-row .rank123", function (e) {
		var p = +$(this).data("rank");
		console.log("current rank:" + p);

		currentRank = 0;
		var last = undefined;
		$(".issues-list > .issue-row:not(.skipNumber):not([data-checked='True']):not([data-checked='true']) .rank123").each(function () {
			var d = $(this).data("rank");
			currentRank = Math.max(currentRank, d);
		});

		$(".rank-solve-message").remove();


		currentRank += 1;

		if (e.button == 0 || e.button == 2) {
			if (p == 0) {
				p = currentRank;
				if (currentRank >= 4) {
					showModal({
						icon: "primary",
						title: "Solve the top three issues first."
					});
					return;
				}
			} else {
				last = p;
				p = 0;
			}
		} else {
			return false;
		}
		console.log("new rank:" + p);
		var id = $(this).parents(".issue-row").attr("data-recurrence_issue");

		updateIssueRank(id, p, true);
		refreshRanks(last);

		////DEBOUNCE
		if (rankTimer[id]) {
			clearTimeout(rankTimer[id]);
		}
		var that = this;
		rankTimer[id] = setTimeout(function () {
			var pp = +$(that).data("rank");
			var d = { rank: pp, time: new Date().getTime() };
			//console.log("D - " + pp);
			$.ajax({
				url: "/L10/UpdateIssue/" + id,
				data: d,
				method: "POST",
				success: function (d) {
					showJsonAlert(d);
				}
			});
		}, 500);
		e.preventDefault();
		return false;
	});
});
