var meetingHub;
var userMeeting = null;
var typed = "";
var selectionStarted = false;
var meetingItemId = 0;

$(".rt").prop("disabled", true);
$(function () {

	//All Scripts loaded
	meetingHub = $.connection.meetingHub;
	/*meetingHub.error = function (error) {
		console.log(error);
	};*/

	meetingHub.client.jsonAlert = function (data, showSuccess) { showJsonAlert(data, showSuccess); };
	meetingHub.client.unhide = function (selector) { $(selector).show(); };
	meetingHub.client.status = function (text) {
		$(".statusContainer").css("bottom", "0px");
		$(".statusContainer").css("display", "block");
		$(".statusContainer").css("opacity", "1");
		$("#status").html(text);
		clearTimeout(statusTimeout);
		statusTimeout = setTimeout(function () {
			$(".statusContainer").animate({
				opacity: 0,
				bottom: "-20px"
			}, 500, function () {
				$(".statusContainer").css("display", "none");
			});
		}, 2000);
	};

	//meetingHub.client.insert = insert;
	//meetingHub.client.remove = remove;

	meetingHub.client.updateUserFocus = updateUserFocus;
	meetingHub.client.updateTextContents = updateTextContents;
	meetingHub.client.setCurrentPage = setCurrentPage;
	meetingHub.client.setPageTime = setPageTime;
	meetingHub.client.setupMeeting = setupMeeting;

	meetingHub.client.setHash = setHash;

	meetingHub.client.deserializeIssues = deserializeIssues;
	meetingHub.client.appendIssue = appendIssue;
	meetingHub.client.updateIssueCompletion = updateIssueCompletion;
	
	meetingHub.client.deserializeTodos = deserializeTodos;
	meetingHub.client.appendTodo = appendTodo;
	meetingHub.client.updateTodoList = updateTodoList;
	meetingHub.client.updateTodoCompletion = updateTodoCompletion;
	meetingHub.client.updateRockCompletion = updateRockCompletion;

	meetingHub.client.createNote = createNote;
	meetingHub.client.updateNoteName = updateNoteName;
	meetingHub.client.updateNoteContents = updateNoteContents;


	$.connection.hub.start().done(function () {
		/*$('#sendmessage').click(function () {
		// Call the Send method on the hub. 
		chat.server.send($('#displayname').val(), $('#message').val());
		// Clear text box and reset focus for next comment. 
		$('#message').val('').focus();
	});*/

		rejoin();
		afterLoad();
		$("body").on("kypress", ".rt", function() { typed = typed + String.fromCharCode(event.charCode); });
		$("body").on("keyup", ".rt", $.throttle(250, sendTextContents));
		$("body").on("focus", ".rt", $.throttle(250, sendFocus));
		$("body").on("blur", ".rt", $.throttle(250, sendUnfocus));

		/*
		$(".rt").keypress(function () { typed = typed + String.fromCharCode(event.charCode); });
		$(".rt").keyup($.throttle(250, sendTextContents));
		//$(".rt").change(sendTextContents);
		//$(".rt").click($.throttle(250, sendFocus));
		$(".rt").focus($.throttle(250, sendFocus));
		$(".rt").blur($.throttle(250, sendUnfocus));*/
	});

	window.onbeforeunload = function (e) {
		$.connection.hub.stop();
	};
});

function rejoin() {
	try {
		if (meetingHub) {
			meetingHub.server.join(MeetingId, $.connection.hub.id).done(function(d) {
				//update(d);
				$(".rt").prop("disabled", false);
			});
		}
	} catch (e) {
		console.log(e);
	}
}

function sendTextContents() {
	var val = $(this).val();
	var id = $(this).attr('id');
	meetingHub.server.updateTextContents(MeetingId, id, val);
	console.log("send Contents for " + id);
}

function sendFocus() {
	var id = $(this).attr('id');
	meetingHub.server.updateUserFocus(MeetingId, id);
	console.log("sendFocus");
}
function sendUnfocus() {
	setTimeout(function() {
		if (!$(':focus').is(".rt")) {
			//var id = $(this).attr('id');
			meetingHub.server.updateUserFocus(MeetingId, "");
			console.log("sendUnfocus");
		}
	}, 1);
}

function updateUserFocus(id, userId) {
	console.log("updating Focus for " + userId);
	$(".lock_" + userId).removeClass("lock").removeClass("lock_" + userId);
	$("#" + id).addClass("lock").addClass("lock_" + userId);

}

function updateTextContents(id, contents) {
	console.log("updating Contents for " + id);
	$("#" + id).val(contents);
	$("#" + id).trigger("change", ["external"]);
}


/*
function insert(newUserMeeting, index, characters, id) {
	userMeeting = newUserMeeting;
	$(id).each(function() {
		var message = $(this).val();
		$(this).val(message.substr(0, index) + characters + message.substr(index, message.length));
		$(this).get(0).setSelectionRange(userMeeting.StartSelection, userMeeting.EndSelection);
	});
}


function remove(newUserMeeting, startIndex, endIndex,id) {
	userMeeting = newUserMeeting;
	$(id).each(function() {
		var message = $(id).val();
		$(id).val(message.substr(0, startIndex) + message.substr(endIndex, message.length - (endIndex - startIndex)));
	});
};

function update(newUserMeeting) {
	userMeeting = newUserMeeting;
}

function edit(event,id) {
	//typed = typed + String.fromCharCode(event.charCode);
	var message = $("#message")[0];

	if (typed != "" || event.keyCode == 8 || event.keyCode == 46) {
		var temp = typed;
		typed = "";
		//Type text
		if (userMeeting.selectionStart == userMeeting.selectionEnd) {
			if (temp != "") {//Insertion
				meetingHub.server.insertion($.connection.hub.id, MeetingId, userId, meetingItemId, userMeeting.EndSelection, temp).done(update);
			} else {//deletion
				meetingHub.server.removal($.connection.hub.id, MeetingId, userId, meetingItemId, userMeeting.StartSelection, message.selectionStart).done(update);
			}
		} else { //Overwrite text
			meetingHub.server.removal($.connection.hub.id, MeetingId, userId, meetingItemId, userMeeting.StartSelection, userMeeting.EndSelection)
				.done(function (data) {
					meetingHub.server.insertion($.connection.hub.id, MeetingId, userId, meetingItemId, userMeeting.EndSelection, temp).done(update);
				});
			f
		}
	} else {
		if (message.selectionStart == message.selectionEnd) {
			meetingHub.server.shift(MeetingId, userId, userMeeting.EndSelection, message.selectionStart).done(update);
			selectionStarted = false;
		}
		if (message.selectionStart != message.selectionEnd) {
			if (selectionStarted) {
				meetingHub.server.selectionStart(MeetingId, userId, userMeeting.EndSelection, message.selectionStart).done(update);
				selectionStarted = true;
			} else {
				meetingHub.server.selectionEnd(MeetingId, userId, userMeeting.EndSelection, message.selectionStart).done(update);
			}
		}
	}
	//lastSelectionStart = message.selectionStart;
	//lastSelectionEnd = message.selectionEnd;
}*/