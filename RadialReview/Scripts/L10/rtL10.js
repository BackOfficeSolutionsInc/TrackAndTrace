var meetingHub;
var userMeeting = null;
var typed = "";
var selectionStarted = false;
var meetingItemId = 0;
var disconnected = false;
var isUnloading = false;
var skipBeforeUnload = false;

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
	meetingHub.client.concludeMeeting = concludeMeeting;

	meetingHub.client.setHash = setHash;

	meetingHub.client.deserializeIssues = deserializeIssues;
	meetingHub.client.appendIssue = appendIssue;
	meetingHub.client.updateIssueCompletion = updateIssueCompletion;

	meetingHub.client.deserializeTodos = deserializeTodos;
	meetingHub.client.appendTodo = appendTodo;
	meetingHub.client.updateTodoList = updateTodoList;
	meetingHub.client.updateTodoCompletion = updateTodoCompletion;
	meetingHub.client.updateRockCompletion = updateRockCompletion;
	meetingHub.client.updateRockName = updateRockName;
	meetingHub.client.updateRocks = updateRocks;
	meetingHub.client.removeRock = removeRock;

	meetingHub.client.updateTodoMessage = updateTodoMessage;
	meetingHub.client.updateTodoDetails = updateTodoDetails;
	meetingHub.client.updateTodoAccountableUser = updateTodoAccountableUser;
	meetingHub.client.updateTodoDueDate = updateTodoDueDate;
	meetingHub.client.setTodoOrder = setTodoOrder;

	meetingHub.client.updateIssueMessage = updateIssueMessage;
	meetingHub.client.updateIssueDetails = updateIssueDetails;
	meetingHub.client.updateIssueOwner = updateIssueOwner;
	meetingHub.client.updateIssuePriority = updateIssuePriority;
	meetingHub.client.updateIssueRank = updateIssueRank;
	meetingHub.client.setIssueOrder = setIssueOrder;

	meetingHub.client.createNote = createNote;
	meetingHub.client.updateNoteName = updateNoteName;
	meetingHub.client.updateNoteContents = updateNoteContents;

	meetingHub.client.updateMeasurable = updateMeasurable;
	meetingHub.client.updateArchiveMeasurable = updateArchiveMeasurable;
	meetingHub.client.addMeasurable = addMeasurable;
	meetingHub.client.reorderMeasurables = reorderMeasurables;
	meetingHub.client.reorderRecurrenceMeasurables = reorderRecurrenceMeasurables;
	meetingHub.client.removeMeasurable = removeMeasurable;

	meetingHub.client.addLogRow = addLogRow;
	meetingHub.client.editLogRow = editLogRow;
	meetingHub.client.addOrEditLogRow = addOrEditLogRow;

	meetingHub.client.appendHeadline = appendHeadline;
	meetingHub.client.updateHeadlineMessage = updateHeadlineMessage;

	meetingHub.client.addTranscription = addTranscription;

	meetingHub.client.showAlert = showAlert;

	meetingHub.client.userEnterMeeting = userEnterMeeting;
	meetingHub.client.userExitMeeting = userExitMeeting;

	meetingHub.client.addVideoProvider = addVideoProvider;
	meetingHub.client.setSelectedVideoProvider = setSelectedVideoProvider;
	meetingHub.client.joinVideoConference = joinVideoConference;
	//meetingHub.client.setLeader = setLeader;
	meetingHub.client.disableItem = disableItem;
	meetingHub.client.updateCumulative = updateCumulative;

	meetingHub.client.updateIssueAwaitingSolve = updateIssueAwaitingSolve;
	meetingHub.client.updateModedIssueSolve = updateModedIssueSolve;

	console.log("StartingHub ");

	$.connection.hub.start(Constants.StartHubSettings).done(initConnection);

	$.connection.hub.disconnected(function () {
		if (!isUnloading) {
			clearAlerts();
			setTimeout(function () {
				showAlert("Connection lost. Reconnecting.");
				disconnected = true;
				setTimeout(function () {
					$.connection.hub.start(Constants.StartHubSettings).done(initConnection);
				}, 5000); // Restart connection after 5 seconds.
			}, 1000);
		}
	});

	window.onbeforeunload = function (e) {
		if ($(":focus").length) {
			$(":focus").blur();
		}
		if (isLeader && meetingStart && !skipBeforeUnload) {
			return 'You have not concluded the meeting.';
		}
		disconnected = false;
		isUnloading = true;
		$.connection.hub.stop();

	};
	window.onunload = function () {

		if ($(":focus").length) {
			$(":focus").blur();
		}

		disconnected = false;
		isUnloading = true;
		$.connection.hub.stop();
	};
});

function initConnection() {
	/*$('#sendmessage').click(function () {
		// Call the Send method on the hub. 
		chat.server.send($('#displayname').val(), $('#message').val());
		// Clear text box and reset focus for next comment. 
		$('#message').val('').focus();
	});*/
	console.log("called initConnection");

	rejoin(function () {
		console.log("Logged in: " + $.connection.hub.id);
		afterLoad();
	});


	$("body").on("kypress", ".rt", function () { typed = typed + String.fromCharCode(event.charCode); });
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
}

var rejoinTimer = false;
var reconnectionCount = 0;
function rejoin(callback) {
	console.log("called rejoin");
	try {
		if (meetingHub) {
			reconnectionCount += 1;
			meetingHub.server.join(window.recurrenceId, $.connection.hub.id).done(function () {
				//update(d);
				if (rejoinTimer) {
					showAlert("Successfully joined.", "alert-danger", "Error",1500);
					clearTimeout(rejoinTimer);
				}
				reconnectionCount = 0;

				console.log("rejoin completed");
				$(".rt").prop("disabled", false);
				if (callback) {
					console.log("calling rejoin callback");
					callback();
				}
				if (disconnected) {
					clearAlerts();
					showAlert("Reconnected.", "alert-success", "Success");
				}
				disconnected = false;
			}).fail(function (d) {
				console.error('Could not connect. Join failed');
				console.error(d);
				showAlert("Join meeting failed. Could not connect with server.", "alert-danger", "Error", 1500);

				if (rejoinTimer) {
					clearTimeout(rejoinTimer);
				}

				if (reconnectionCount >= 6) {
					showAlert("Could not connect after 6 attempts.", "alert-danger", "Error", 2000);
					setTimeout(function () {
						location.reload();
					}, 2000);

					return;
				}


				setTimeout(function () {
					var attempt = "";
					if (reconnectionCount > 1) {
						attempt = " Attempt "+(reconnectionCount+1)+".";
					}
					console.log("Attempt #" + reconnectionCount);

					showAlert("Attempting to rejoin."+attempt, "alert-danger", "Error", Math.max(3000,1000 + Math.pow(1.5, reconnectionCount)*1000));
					rejoinTimer = setTimeout(function () {
						rejoin(callback);
					}, 3000 + Math.pow(1.5, reconnectionCount) * 1000);
				},2000)
			});
		}
	} catch (e) {
		console.error(e);
		showAlert("Could not connect with server.", "alert-danger", "Error");
		setTimeout(function (){
			location.reload();
		}, 2000)
		//callback();
	}
}

function sendTextContents() {
	var val = $(this).val();
	var id = $(this).attr('id');
	meetingHub.server.updateTextContents(window.recurrenceId, id, val);
	console.log("send Contents for " + id);
}
function sendDisable(id, disabled) {
	meetingHub.server.sendDisable(window.recurrenceId, id, disabled);
	console.log("sendDisabled:" + id + " " + disabled);
}

function disableItem(id, disabled) {
	$("#" + id).attr("disabled", disabled ? "disabled" : false);
	console.log("receivedDisable:" + id + " " + disabled);
}

function sendFocus() {
	var id = $(this).attr('id');
	meetingHub.server.updateUserFocus(window.recurrenceId, id);
	console.log("sendFocus");
}
function sendUnfocus() {
	setTimeout(function () {
		if (!$(':focus').is(".rt")) {
			meetingHub.server.updateUserFocus(window.recurrenceId, "");
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


function userEnterMeeting(id, connectionId, name, url, initials) {
	$("user-status-container-" + id).append("<span class='user-status-" + connectionId + " icon fontastic-icon-monitor green' />")
	$("user-picture-container").append("<span class='user-picture-" + connectionId + "'>" + profilePicture(url, name, initials) + "</span>");
}
function userExitMeeting(connectionId) {
	$("user-status-" + connectionId).remove();
	$("user-picture-" + connectionId).remove();
}
