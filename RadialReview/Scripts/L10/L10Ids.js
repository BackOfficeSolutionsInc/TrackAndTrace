﻿
var currentIssuesDetailsId;



$(function () {

    var CheckOffIssue = Undo.Command.extend({
        constructor: function (row) {
            this.row = row;
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
                    url: "/l10/UpdateIssueCompletion/" + recurrenceId,
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
        }
    });


    var MoveIssueToVTO = Undo.Command.extend({
        constructor: function (row) {
            //this.row = row;
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
                        //$(row).addClass("skipNumber");
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
                        // $(row).show();
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
        var issueRow = $(this);//.closest(".issue-row");
        $(".issue-row.selected").removeClass("selected");
        $(issueRow).addClass("selected");
        var tempRowId = $(issueRow).data("recurrence_issue");
        if (tempRowId == currentIssuesDetailsId)
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
        $("#issueDetails").html("");
        /*$("#issueDetails").append(
			"<span class='expandContract btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs contractButton' title='Hide details'><span class='glyphicon glyphicon-resize-small'></span></span>" +
				"<span class='btn btn-default btn-xs expandButton'  title='Show details'><span class='glyphicon glyphicon-resize-full'></span></span>" +
			"</span>");
		$("#issueDetails").append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");
        */
        $("#issueDetails").append("<div class='heading'><h4 class='message-holder clickable' data-recurrence_issue='" + recurrence_issue + "'><span class='message editable-text' data-recurrence_issue='" + recurrence_issue + "'>" + message + "</span></h4></div>");
        $("#issueDetails").append(detailsList);
        //$("#issueDetails").append("<textarea class='details issue-details' data-recurrence_issue='" + recurrence_issue + "'>" + details + "</textarea>");
        $("#issueDetails").append("<iframe class='details issue-details' name='embed_readwrite' src='https://notes.traction.tools/p/" + padid + "?showControls=true&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + encodeURI(UserName) + "' width='100%' height='100%'></iframe>");

        $("#issueDetails").append("<div class='button-bar'>" +
			"<div style='height:28px;'>" +
			"<span class='btn-group pull-right'>" +
				"<span class='btn btn-default btn-xs doneButton'><input data-recurrence_issue='" + recurrence_issue + "' class='issue-checkbox' type='checkbox' " + (checked ? "checked" : "") + "/> Resolved</span>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs copyButton issuesModal' data-method='copymodal' data-recurrence_issue='" + recurrence_issue + "' data-copyto='" + MeetingId + "'><span class='icon fontastic-icon-forward-1' title='Move issue to another L10'></span> Move To</span>" +
			"<span class='btn btn-default btn-xs createTodoButton todoModal' data-method='CreateTodoFromIssue' data-meeting='" + meetingId + "' data-issue='" + issueId + "' data-recurrence='" + MeetingId + "' ><span class='glyphicon glyphicon-unchecked todoButton'></span> To-Do</span>" +
			"</span>" +
			"</div>" +
			"<span class='clearfix'></span>" +
			"<span class='gray' style='width:75px;display:inline-block'>Owned By:</span>" +
			"<span>" +
				"<span style='width:250px;padding-left:10px;' class='assignee' data-accountable='" + accountable + "' data-recurrence_issue='" + recurrence_issue + "'  >" +
					"<span data-recurrence_issue='" + recurrence_issue + "' class='btn btn-link owner'>" + ownerStr + "</span>" +
				"</span>" +
			"</span>" +

			"</div>");

        fixIssueDetailsBoxSize();
    });


    $("body").on("click", ".issues-list>.issue-row .vtoButton", function () {
        var row = $(this).closest(".issue-row");
        undoStack.execute(new MoveIssueToVTO(row));
    });

    $("body").on("click", ".issueDetails .message-holder .message", function () {
        var input = $("<input class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html()) + "' onblur='sendIssueMessage(this," + $(this).parent().data("recurrence_issue") + ")'/>");
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
    $("body").on("click", ".issueDetails .doneButton", function () { $(this).find(">input").trigger("click"); });



    $("body").on("change", ".issue-checkbox", function () {
        undoStack.execute(new CheckOffIssue(this, $(this).prop("checked")));
        /*var issueId = $(this).data("recurrence_issue");
		var checked = $(this).prop("checked");
		var selector = ".issue-checkbox[data-recurrence_issue='" + issueId + "']";
		var selector2 = ".issue-row[data-recurrence_issue='" + issueId + "']";
		var that = this;
		$(selector).prop("disabled", true);
		$(selector).prop("checked", checked);
		$(selector2).data("checked", checked);


		$(".undoable").slideUp('slow', function () {
		    $(this).remove();
		});

		$.ajax({
			url: "/l10/UpdateIssueCompletion/" + recurrenceId,
			method: "post",
			data: { issueId: issueId, checked: checked, connectionId: $.connection.hub.id },
			success: function (data) {
				showJsonAlert(data, false, true);
				$(selector).prop("checked", (!data.Error ? data.Object : !checked));
				$(selector2).data("checked", (!data.Error ? data.Object : !checked));
				if (checked == true) {
				    var row = $(that).closest(".issue-row");
				    row.addClass("undoable");
				    row.data("undo-url", "/l10/UpdateIssueCompleted/" + issueId +
                        "?checked=false");
				    row.data("undo-action", "unclass");
				}
				refreshCurrentIssueDetails();
			},
			error: function () {
				$(selector).prop("checked", !checked);
				$(selector2).data("checked", !checked);
			},
			complete: function () {
				$(selector).prop("disabled", false);
			}
		});*/
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

        //$(".details.issue-details").height(wh - pos.top + st - footerH - 110);
        $(".details.issue-details").height(footerH - 20 - 110 - pos.top);
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
function sortIssueByCurrent(recurrenceId, issueList) {
    if ($(".meeting-page").hasClass("prioritization-Rank"))
        return sortIssueBy(recurrenceId, issueList, "data-rank", "Priority");
    else
        return sortIssueBy(recurrenceId, issueList, "data-priority", "Votes", -1);
}

function sortIssueBy(recurrenceId, issueList, sortBy, title, mult) {
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
    updateIssuesList(recurrenceId, issueList, sortBy);
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
            debugger;
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
            debugger;
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

    return '<li class="issue-row dd-item arrowkey undoable-stripped" data-padid="' + issue.padid + '" data-createtime="' + issue.createtime + '" data-recurrence_issue="' + issue.recurrence_issue + '" data-issue="' + issue.issue + '" data-checked="' + issue.checked + '"  data-message="' + issue.message + '"  data-details="' + issue.details + '"  data-owner="' + issue.owner + '" data-accountable="' + issue.accountable + '"  data-priority="' + issue.priority + '"  data-rank="' + issue.rank + '">\n'
        + ' <span class="undo-button">Undo</span>'
        + '	<input data-recurrence_issue="' + issue.recurrence_issue + '" class="issue-checkbox" type="checkbox" ' + (issue.checked ? "checked" : "") + '/>\n'
		+ '	<div class="move-icon noselect dd-handle">\n'
		+ '		<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>\n'
		+ '		<span class="inner icon fontastic-icon-primitive-square"></span>\n'
		+ '	</div>\n'
		+ '<div class="btn-group pull-right">\n'
		+ ' <span class="issuesButton issuesModal icon fontastic-icon-forward-1" data-copyto="' + recurrenceId + '" data-recurrence_issue="' + issue.issue + '" data-method="copymodal" style="padding-right: 5px"></span>\n'
		+ ' <span class="glyphicon glyphicon-unchecked todoButton issuesButton todoModal" data-issue="' + issue.issue + '" data-meeting="' + issue.createdDuringMeetingId + '" data-recurrence="' + recurrenceId + '" data-method="CreateTodoFromIssue" style="padding-right:5px;"></span>\n'
        + ' <span class="glyphicon glyphicon-vto vtoButton"></span>'
        + '</div>\n'
		+ '<div class="number-priority">\n'
		+ ' <span class="number"></span>\n'
        + ' <span class="priority" data-priority="' + issue.priority + '"></span>\n'
        + ' <span class="rank123 badge" data-rank="' + issue.rank + '">IDS</span>\n'
		+ '</div>\n'
		+ '<span class="profile-image">\n'
		+ '		<span class="profile-picture">\n'
		+ '			<span class="picture-container" title="' + issue.owner + '">\n'
		+ '				<span class="picture" style="background: url(' + issue.imageUrl + ') no-repeat center center;"></span>\n'
		+ '			</span>\n'
		+ '		</span>\n'
		+ '	</span>\n'
		+ '	<div class="message" data-recurrence_issue=' + issue.issue + '>' + issue.message + '</div>\n'
		+ '	<div class="issue-details-container"><div class="issue-details" data-recurrence_issue=' + issue.issue + '>' + details + '</div></div>\n'
		+ '<ol class="dd-list">'
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
    //var ouput = [];
    for (var i = 0; i < parentOrders.length; i++) {
        var p = $(all).filter("[data-recurrence_issue=" + parentOrders[i].id + "]");
        var ol = p.find("ol");
        var children = _setIssueOrder(ol, parentOrders[i].children, all);
        /*for (var j = 0; j < children.length; j++) {
            ol.append(children[j]);
        }*/
        parentSelector.append(p);
    }
}

function setIssueOrder(order) {
    //var items = $(".issues-list li");
    //items.detach();
    debugger;
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

function updateIssuesList(recurrenceId, issueRow, orderby) {
    var order = getIssueOrder();
    var d = { issues: order, connectionId: $.connection.hub.id, orderby: orderby };
    console.log(d);
    var that = issueRow;
    $.ajax({
        url: "/l10/UpdateIssues/" + recurrenceId,
        data: JSON.stringify(d),
        contentType: "application/json; charset=utf-8",
        method: "POST",
        success: function (d) {
            if (!d.Error) {
                oldIssueList = order;//$(".issues-list").clone(true);
            } else {
                showJsonAlert(d, false, true);
                $(that).html("");
                setTimeout(function () {
                    setIssueOrder(oldIssueList);
                    //$('.issues-container').html(oldIssueList);
                    //oldIssueList = $(".issues-list").clone(true);
                    //refreshCurrentIssueDetails();
                }, 1);
            }
        },
        error: function (a, b) {
            clearAlerts();
            showAlert(a.statusText || b);
            //$('.dd').html("");
            setTimeout(function () {
                //$('.dd').html(oldIssueList);
                setTodoOrder(oldTodoList);
                //oldIssueList = $(".issues-list").clone(true);
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
    $(".issue-row[data-recurrence_issue=" + currentIssuesDetailsId + "]")
		.closest(".issues-list>.issue-row").find(">.message")
		.trigger("click");
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
            //var rt = $(a).data("rank-time") - $(b).data("rank-time");
            //if (rt<=0)
            return $(a).data("rank") - $(b).data("rank");
            //return -1*rt;
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
    console.log("A - " + last);
    //var last = 0;
    for (var i = 0; i < ranks.length; i++) {
        var r = ranks[i];
        var cur = $(r).data("rank");
        console.log(" B - " + cur);
        if (cur != last + 1) {
            console.log("  C - " + (last + 1));
            $(r).data("rank", last + 1);
            $(r).attr("data-rank", last + 1);
            //$(r).html(last + 1)
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
                data: JSON.stringify(refreshRankArr) ,
                method: "POST",
                success: function (d) {
                    showJsonAlert(d);
                }
            });
            refreshRankArr = [];
        }, 50);
    }
    //currentRank = last + 1;
}

function updateIssueRank(id, rank, skipRefresh) {
    var dom = $(".ids .issue-row[data-recurrence_issue=" + id + "] > .number-priority > .rank123");
    dom.data("rank", rank);
    dom.attr("data-rank", rank);
    //dom.html(rank);
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

/*$(document).on("click", ".issue-row .priority", function (e) {
    debugger;
    e.preventDefault();
    return false;
});
*/
$(function () {

    var priorityTimer = {};
    $("body").on("mousedown", ".issue-row .priority", function (e) {
        var p = +$(this).data("priority");
        console.log("current priority:" + p);
        if (e.button == 0) {
            p += 1;
        } else if (e.button == 2 || e.which==3) {
            p -= 1;
            p = Math.max(0, p);
        } else {
            return false;
        }
        // $(this).data("priority", p);
        console.log("new priority:" + p);
        var id = $(this).parents(".issue-row").attr("data-recurrence_issue");

        updateIssuePriority(id, p);
        //refreshCurrentIssueDetails();
        //refreshPriority(this);

        ////DEBOUNCE
        if (priorityTimer[id]) {
            clearTimeout(priorityTimer[id]);
        }
        var that = this;
        priorityTimer[id] = setTimeout(function () {
            var pp = +$(that).data("priority");
            var d = { priority: pp, time: new Date().getTime() };
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
            // if (d != 0)
            //    last = Math.min(last, d);
        });

        $(".rank-solve-message").remove();


        currentRank += 1;
        //var last = 10000;

        if (e.button == 0 || e.button == 2) {
            if (p == 0) {
                p = currentRank;
                //if (currentRank == 3) {
                //    $("[data-rank='0'] .rank123").tooltip({ title: "Solve the top three issues first." });
                //}

                if (currentRank >= 4) {
                    //$(e.target).tooltip({ title: "Solve the top three issues first." });
                    //$(e.target).tooltip('show');
                    //setTimer(function () {
                    //    $(e.target).tooltip('hide');
                    //    $(e.target).tooltip('destroy');
                    //}, 1500);
                    //clearAlerts();
                    //showAlert("Solve the top three issues first.", "alert-info rank-solve-message", "Info:");
                    showModal({
                        icon: "primary",
                        title: "Solve the top three issues first."
                    });
                    return;
                }
                //currentRank += 1;

            } else {
                last = p;
                p = 0;
                //refreshRanks();
            }
        } else {
            return false;
        }
        // $(this).data("priority", p);
        console.log("new rank:" + p);
        var id = $(this).parents(".issue-row").attr("data-recurrence_issue");

        //updateIssuePriority(id, p);
        //refreshCurrentIssueDetails();
        //refreshPriority(this);

        updateIssueRank(id, p, true);
        refreshRanks(last);
        //if (p >= 3)
        //    $(".ids").addClass("rank-full");
        //else
        //    $(".ids").removeClass("rank-full");

        ////DEBOUNCE
        if (rankTimer[id]) {
            clearTimeout(rankTimer[id]);
        }
        var that = this;
        rankTimer[id] = setTimeout(function () {
            var pp = +$(that).data("rank");
            var d = { rank: pp, time: new Date().getTime() };
            console.log("D - " + pp);
            $.ajax({
                url: "/L10/UpdateIssue/" + id,
                data: d,
                method: "POST",
                success: function (d) {
                    showJsonAlert(d);
                }
            });
           // refreshRanks();
        }, 500);

        e.preventDefault();
        return false;
    });

});
