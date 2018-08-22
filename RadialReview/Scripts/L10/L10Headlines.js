﻿
var modalWidth = 991;
function fixHeadlinesBoxSize() {
	if ($(".headlines-notes").length) {
		var wh = $(window).height();
		var pos = $(".headlines-notes").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
			footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) { }

		$(".headlines-notes").height(Math.max(200, footerH - 20 - 50 - pos.top));
	}
}

$(window).resize(fixHeadlinesBoxSize);
$(window).on("page-headlines", fixHeadlinesBoxSize);
$(window).on("footer-resize", function () {
	setTimeout(fixHeadlinesBoxSize, 250);
});

var clickHeadlineRow = function (evt) {
    
	if ($(evt.target).hasClass("todoButton"))
		return;
	if ($(evt.target).hasClass("issuesButton"))
		return;


	var headlineRow = $(this);
	$(".headline-row.selected").removeClass("selected");
	$(headlineRow).addClass("selected");
	currentHeadlineDetailsId = $(headlineRow).data("headline");
	var createtime = $(headlineRow).data("createtime");
	var duedate = +$(headlineRow).attr("data-closetime");
	var owner = $(headlineRow).data("owner");
	var about = $(headlineRow).data("about");
	var message = $(headlineRow).data("message");
	var padId = $(headlineRow).data("padid");
	var aboutName = $(headlineRow).data("aboutname");
	var aboutPosition = $(headlineRow).data("position");
	//var issueId = $(headlineRow).data("issue");
	var headline = $(headlineRow).data("id");

	if (aboutName == null)
		aboutName = "n/a";

	var due = new Date(new Date(duedate).toUTCString().substr(0, 16));
	//var recurrence_issue = $(headlineRow).data("recurrence_issue");
	var checked = $(headlineRow).find(".issue-checkbox").prop("checked");

	var detailsContents = $("<div class='headlineDetails abstract-details-panel'></div>");
	//$("#headlineDetails").html("");
	$(detailsContents).append("<span class='expandContract btn-group pull-right'></span>");
	$(detailsContents).append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");

	$(detailsContents).append("<div class='heading on-edit-enabled'><h4 class='message-holder clickable' data-headline='" + headline + "'><span data-headline='" + headline + "' class='message editable-text'>" + message + "</span></h4></div>");
    //$(detailsContents).append("<textarea id='headlineDetailsField' class='details headline-details' data-headline='" + headline + "'>" + details + "</textarea>");	
	
    
	//    $(detailsContents).append("<iframe class='details headline-details on-edit-enabled' name='embed_readwrite' src='https://notes.traction.tools/p/" + padId + "?showControls=true&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + encodeURI(UserName) + "' width='100%' height='100%'></iframe>");	
	$(detailsContents).append("<iframe class='details headline-details on-edit-enabled' name='embed_readwrite' src='/headlines/Pad/" + headline + "' width='100%' height='100%'></iframe>");
	

	$(detailsContents).append(
		"<div class='button-bar'>" +
			"<div style='height:28px'>" +
			"<span class='btn-group pull-right'>" +
			"</span>" +
			"<span class='expandContract btn-group'>" +
			"<span class='btn btn-default btn-xs issuesModal on-edit-enabled' data-method='CreateHeadlineIssue'   data-headline='" + headline + "' data-recurrence='" + window.recurrenceId + "' data-meeting='" + window.meetingId + "' title='Create a Context-Aware Issue™'><span class='icon fontastic-icon-pinboard'></span> New Issue</span>" +
			"<span class='btn btn-default btn-xs todoModal on-edit-enabled' data-method='CreateTodoFromHeadline'  data-headline='" + headline + "' data-recurrence='" + window.recurrenceId + "' data-meeting='" + window.meetingId + "' title='Create a Context-Aware To-do™'><span class='glyphicon glyphicon-unchecked'></span> New To-do</span>" +
			"<span class='btn btn-default btn-xs headlineModal on-edit-enabled' data-method='copymodal'   data-headlineid='" + headline + "' data-copyto='" + window.recurrenceId + "' data-meeting='" + window.meetingId + "' title='Copy Headline'><span class='icon fontastic-icon-forward-1'></span> Copy to</span>" +
			"</span>" +
			"</div>" +
			"<span class='clearfix'></span>" +
			"<div class='gray' style='display:inline-block;padding-top: 6px;'>About:</div><div style='' class='about' data-about='" + about + "' data-headline='" + headline + "'  >" + aboutName + "</div>" +
			//"<div >" +
			//	"<span class='gray' style='width:75px;display:inline-block'>Due date:</span>" +
			//	"<span style='width:250px;padding-left:10px;' class='duedate' data-accountable='" + accountable + "' data-headline='" + headline + "' >" +
			//		"<span class='date' style='display:inline-block' data-date='" + dateFormatter(due) + "' data-date-format='m-d-yyyy'>" +
			//			"<input type='text' data-headline='" + headline + "' class='form-control datePicker' value='" + dateFormatter(due) + "'/>" +
			//		"</span>" +
			//	"</span>" +
			//"</div>" +
		"</div>");
	var w = $(window).width();
	$("#headlineDetails").html("");
	if (w <= modalWidth) {
		var c = detailsContents.clone();
		c.find("h4").addClass("form-control");
		showModal({
			contents: c,
			title: "Edit People Headline",
			noCancel: true,
		});
	} else {
		$("#headlineDetails").append(detailsContents);
		fixHeadlineDetailsBoxSize();
	}
}
$("body").on("click", ".headlines-list>.headline-row", clickHeadlineRow);

function fixHeadlineDetailsBoxSize() {
	if ($(".details.headline-details").length) {
		var wh = $(window).height();
		var pos = $(".details.headline-details").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
			footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) { }
		$(".details.headline-details").height(footerH - 20 - 140 - pos.top);
	}
}

function appendHeadline(selector, headline) {
	var li = $(constructHeadlineRow(headline));
	$(selector).append(li);
	$(li).flash();
	refreshCurrentHeadlineDetails();
}

function refreshCurrentHeadlineDetails() {
	//TODO
}

$("body").on("click", ".headlineDetails .message-holder .message", function () {
	var input = $("<textarea class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html()) + "' onblur='sendHeadlineMessage(this," + $(this).parent().data("headline") + ")'>" + $(this).html() + "</textarea>");
	$(this).parent().html(input);
	input.focusTextToEnd();
});

function updateHeadlineMessage(id, message) {
	$(".headlines .message[data-headline=" + id + "]").html(message);
	$(".headlines .headline-row[data-id=" + id + "]").attr("data-message", escapeString(message));

}

function sendHeadlineMessage(self, id) {
	var val = $(self).val();
	$(".headlineDetails .message-holder[data-headline=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};
	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateHeadline/" + id,
		success: function (data) {
			if (showJsonAlert(data, false, true)) {
				$(".headlineDetails .message-holder[data-headline=" + id + "]").html("<span data-headline='" + id + "' class='message editable-text'>" + val + "</span>");
			}
		}
	});
}

function constructHeadlineRow(headline) {
	var red = "";
	var nowDateStr = new Date();
	var nowDate = new Date(nowDateStr.getYear() + 1900, nowDateStr.getMonth(), nowDateStr.getDate());
	//var duedateStr = todo.duedate.split("T")[0].split("-");
	//var duedate = new Date(duedateStr[0], duedateStr[1] - 1, duedateStr[2]).getTime();


	//var date = new Date(new Date(todo.duedate).toUTCString().substr(0,16));
	var Model = headline;
	var closeTime = null;
	if (Model.CloseTime != null) {
		closeTime = +new Date(Model.CloseTime)
	}

	if (Model.MeetingId == -1)
		Model.MeetingId= window.MeetingId || Model.MeetingId;

	if (!Model.Message)
		Model.Message = "";
	//Accountable user name populated?
	return '<li class="headline-row dd-item arrowkey"' +
		'data-createtime="' + (+new Date(Model.CreateTime)) + '"' +
		'data-closetime="' + (closeTime) + '"' +
		'data-id="' + Model.Id + '"' +
		'data-message="' + escapeString(Model.Message) + '"' +
		'data-owner="' + Model.Owner.Id + '"' +
		'data-ownername="' + escapeString(Model.Owner.Name) + '"' +
		'data-ownerimage="' + Model.Owner.ImageUrl + '"' +
	//	'data-padurl="'+Model.DetailsUrl+'"'+
		'data-padid="' + Model.HeadlinePadId + '"' +
		'data-about="' + Model.About.Id + '"' +
		'data-aboutname="' + escapeString(Model.About.Name) + '"' +
		'data-aboutimage="' + Model.About.ImageUrl + '">' +
		'<div class="btn-group pull-right">     ' +
		'<span class="issuesButton icon fontastic-icon-forward-1 headlineModal" style="padding-right: 5px"'+
			  'title="Send to another L10"'+
			  'data-copyto="' + Model.RecurrenceId + '"' +
			  'data-headlineid="' + Model.Id + '"' +
			  'data-title="Copy Headline"'+
			  'data-method="copymodal"></span> '+
			'<span class="icon fontastic-icon-pinboard issuesModal issuesButton"' +
				'data-method="CreateHeadlineIssue"' +
				'data-headline="' + Model.Id + '"' +
				'data-recurrence="' + Model.RecurrenceId + '"' +
				'data-meeting="' + Model.MeetingId + '"' +
				'title="Create a Context-Aware Issue™"></span>' +
			'<span class="glyphicon glyphicon-unchecked todoButton issuesButton todoModal" style="padding-right: 5px"' +
				'title="Create a Context-Aware To-Do™"' +
				'data-headline="' + Model.Id + '"' +
				'data-meeting="' + Model.MeetingId + '"' +
				'data-recurrence="' + Model.RecurrenceId + '"' +
				'data-method="CreateTodoFromHeadline"></span>' +
		'</div>' +
		'<span class="profile-image desaturate">' + profilePicture(Model.Owner.ImageUrl, Model.Owner.Name) + '</span> ' +
		'<span class="profile-image">' + profilePicture(Model.About.ImageUrl, Model.About.Name) + '</span> ' +
		'<div class="message" data-headline="' + Model.Id + '">' + Model.Message + '</div>' +
		'<div class="date-created">' + Model.CreateTime + '</div>' +
	'</li>';
}