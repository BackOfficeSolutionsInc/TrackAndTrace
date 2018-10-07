var userMeeting = null;
var typed = "";
var selectionStarted = false;
var meetingItemId = 0;
var disconnected = false;
var isUnloading = false;
var skipBeforeUnload = false;

var pingTimeout = 2 * 60 * 1000; //1.5 minutes in ms


$(".rt").prop("disabled", true);
$(function () {

	var setup = function (i) {
		//All Scripts loaded
	
		RealTime.client.jsonAlert = function (data, showSuccess) { showJsonAlert(data, showSuccess); };
		RealTime.client.unhide = function (selector) { $(selector).show(); };
		RealTime.client.status = function (text) {
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
			
		RealTime.client.updateUserFocus = updateUserFocus;
		RealTime.client.updateTextContents = updateTextContents;
		RealTime.client.setCurrentPage = setCurrentPage;
		RealTime.client.setPageTime = setPageTime;
		RealTime.client.setupMeeting = setupMeeting;
		RealTime.client.concludeMeeting = concludeMeeting;

		RealTime.client.setHash = setHash;

		RealTime.client.receiveUpdateScore = receiveUpdateScore;
		RealTime.client.updateScoresGoals = updateScoresGoals;

		RealTime.client.deserializeIssues = deserializeIssues;
		RealTime.client.appendIssue = appendIssue;
		RealTime.client.updateIssueCompletion = updateIssueCompletion;

		RealTime.client.deserializeTodos = deserializeTodos;
		RealTime.client.appendTodo = appendTodo;
		RealTime.client.updateTodoList = updateTodoList;
		RealTime.client.updateTodoCompletion = updateTodoCompletion;
		RealTime.client.updateRockCompletion = updateRockCompletion;
		RealTime.client.updateRockName = updateRockName;
		RealTime.client.updateRockDueDate = updateRockDueDate;
		RealTime.client.updateRocks = updateRocks;
		RealTime.client.removeRock = removeRock;

		RealTime.client.updateTodoMessage = updateTodoMessage;
		RealTime.client.updateTodoDetails = updateTodoDetails;
		RealTime.client.updateTodoAccountableUser = updateTodoAccountableUser;
		RealTime.client.updateTodoDueDate = updateTodoDueDate;
		RealTime.client.setTodoOrder = setTodoOrder;

		RealTime.client.updateIssueMessage = updateIssueMessage;
		RealTime.client.updateIssueDetails = updateIssueDetails;
		RealTime.client.updateIssueOwner = updateIssueOwner;
		RealTime.client.updateIssuePriority = updateIssuePriority;
		RealTime.client.updateIssueRank = updateIssueRank;
		RealTime.client.setIssueOrder = setIssueOrder;

		RealTime.client.createNote = createNote;
		RealTime.client.updateNoteName = updateNoteName;
		RealTime.client.updateNoteContents = updateNoteContents;

		RealTime.client.updateMeasurable = updateMeasurable;
		RealTime.client.updateArchiveMeasurable = updateArchiveMeasurable;
		RealTime.client.addMeasurable = addMeasurable;
		RealTime.client.reorderMeasurables = reorderMeasurables;
		RealTime.client.reorderRecurrenceMeasurables = reorderRecurrenceMeasurables;
		RealTime.client.removeMeasurable = removeMeasurable;
		RealTime.client.removeDivider = removeDivider;

		RealTime.client.addLogRow = addLogRow;
		RealTime.client.editLogRow = editLogRow;
		RealTime.client.addOrEditLogRow = addOrEditLogRow;

		RealTime.client.appendHeadline = appendHeadline;
		RealTime.client.updateHeadlineMessage = updateHeadlineMessage;

		RealTime.client.addTranscription = addTranscription;

		RealTime.client.showAlert = showAlert;

		RealTime.client.userEnterMeeting = userEnterMeeting;
		RealTime.client.userExitMeeting = userExitMeeting;
		RealTime.client.stillAlive = stillAlive;

		RealTime.client.addVideoProvider = addVideoProvider;
		RealTime.client.setSelectedVideoProvider = setSelectedVideoProvider;
		RealTime.client.joinVideoConference = joinVideoConference;
		//RealTime.client.setLeader = setLeader;
		RealTime.client.disableItem = disableItem;
		RealTime.client.updateCumulative = updateCumulative;
		RealTime.client.updateAverage = updateAverage;

		RealTime.client.updateIssueAwaitingSolve = updateIssueAwaitingSolve;
		RealTime.client.updateModedIssueSolve = updateModedIssueSolve;
		RealTime.client.removeIssueRow = removeIssueRow;
		RealTime.client.floatTopThreeIssues = floatTopThreeIssues;


		RealTime.client.setMilestone = setMilestone;
		RealTime.client.deleteMilestone = deleteMilestone;

		//console.log("StartingHub ");

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
			if ($('body').is('.meeting-preview')) {
				$.ajax({
					url: "/l10/ForceConclude/" + window.recurrenceId,
					method: "POST",
					success: function () {
					},
					error: function () {
					}
				});
				return undefined;
			}

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
	}
	setTimeout(function () { setup(0); }, 1);
});

function initConnection() {
	/*$('#sendmessage').click(function () {
		// Call the Send method on the hub. 
		chat.server.send($('#displayname').val(), $('#message').val());
		// Clear text box and reset focus for next comment. 
		$('#message').val('').focus();
	});*/
	//console.log("called initConnection");

	rejoin(function () {
		//console.log("Logged in: " + $.connection.hub.id);
		afterLoad();
	});


	$("body").on("kypress", ".rt", function () { typed = typed + String.fromCharCode(event.charCode); });
	$("body").on("keyup", ".rt", $.throttle(250, sendTextContents));
	$("body").on("focus", ".rt", $.throttle(250, sendFocus));
	$("body").on("blur", ".rt", $.throttle(250, sendUnfocus));
	$("body").on("click", "[type='number'].rt", $.throttle(250, sendTextContents));

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
	//console.log("called rejoin");
	window.recurrenceId;

	try {
		reconnectionCount += 1;
		RealTime.join({ recurrenceIds: [window.recurrenceId] }, function () {
			if (rejoinTimer) {
				showAlert("Successfully joined.", "alert-danger", "Error", 1500);
				clearTimeout(rejoinTimer);
			}
			reconnectionCount = 0;

			//console.log("rejoin completed");
			$(".rt").prop("disabled", false);
			if (callback) {
				//console.log("calling rejoin callback");
				callback();
			}
			if (disconnected) {
				clearAlerts();
				showAlert("Reconnected.", "alert-success", "Success", 1000);
			}
			disconnected = false;
		}, function (d) {
			console.error('Could not connect. Join failed',d);			
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
					attempt = " Attempt " + (reconnectionCount + 1) + ".";
				}
				console.log("Attempt #" + reconnectionCount);

				showAlert("Attempting to rejoin." + attempt, "alert-danger", "Error", Math.max(3000, 1000 + Math.pow(1.5, reconnectionCount) * 1000));
				rejoinTimer = setTimeout(function () {
					rejoin(callback);
				}, 3000 + Math.pow(1.5, reconnectionCount) * 1000);
			}, 2000)
		});
				
	} catch (e) {
		console.error(e);
		showAlert("Could not connect with server.", "alert-danger", "Error");
		setTimeout(function () {
			location.reload();
		}, 2000);
	}
}

function sendTextContents() {
	var val = $(this).val();
	var id = $(this).attr('id');
	RealTime.server.updateTextContents(window.recurrenceId, id, val);
	console.log("send Contents for " + id);
}
function sendDisable(id, disabled) {
	RealTime.server.sendDisable(window.recurrenceId, id, disabled);
	console.log("sendDisabled:" + id + " " + disabled);
}

function disableItem(id, disabled) {
	$("#" + id).attr("disabled", disabled ? "disabled" : false);
	console.log("receivedDisable:" + id + " " + disabled);
}

function sendFocus() {
	var id = $(this).attr('id');
	RealTime.server.updateUserFocus(window.recurrenceId, id);
	console.log("sendFocus");
}
function sendUnfocus() {
	setTimeout(function () {
		if (!$(':focus').is(".rt")) {
			RealTime.server.updateUserFocus(window.recurrenceId, "");
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

var reping = function () {
	console.log("Repinging - " + new Date());
	RealTime.server.ping();
};

var myPing = setInterval(reping, pingTimeout - 5000);

function removeOnTimeout(connectionId) {
	return function () {
		console.warn("User timed out: " + connectionId + " (not removing)");
		//userExitMeeting(connectionId);
	};
}

function userEnterMeeting(connection) {
	var id = connection.User.Id;
	var connectionId = connection.Id;
	var name = connection.User.Name;
	var url = connection.User.ImageUrl;
	var initials = connection.User.Initials;

	userEnterMeeting.notifications = userEnterMeeting.notifications || {};

	//Add to online list	
	userEnterMeeting.existing = userEnterMeeting.existing || [];
	if (!Enumerable.from(userEnterMeeting.existing).any(function (x) { return x.id == id; })) {
		$(".user-status-container-" + id).append("<span class='user-status-" + connectionId + " icon fontastic-icon-monitor green' />")
		var notif = $("<span class='notification-icon'></span>");
		var pix = $("<span class='user-picture user-picture-" + connectionId + " user-picture-" + id + "' data-userid='" + id + "'>" + profilePicture(url, name, initials) + "</span>");
		if (id in userEnterMeeting.notifications && userEnterMeeting.notifications[id]) {
			notif.append("<span class='glyphicon glyphicon-ok-circle checkmark'></span>");
		}
		pix.append(notif);
		$(".user-picture-container").append(pix);


	}
	var tout = setTimeout(removeOnTimeout(connectionId), pingTimeout);

	//Check the attendance button
	userEnterMeeting.checkedOnce = userEnterMeeting.checkedOnce || [];
	if (!Enumerable.from(userEnterMeeting.checkedOnce).any(function (x) { return x == id; })) {

		waitUntilVisible(".start-meeting", function () {
			$(".user-attendence-box-" + id).prop("checked", true);
			userEnterMeeting.checkedOnce.push(id);
		}, 10000);
	}

	userEnterMeeting.existing.push({
		id: id,
		connectionId: connectionId,
		timeout: tout
	});
}

function stillAlive(connection) {

	var id = connection.User.Id;
	var connectionId = connection.Id;
	var name = connection.User.Name;
	var url = connection.User.ImageUrl;
	var initials = connection.User.Initials;

	console.warn("Still alive: " + id);
	var found = Enumerable.from(userEnterMeeting.existing).where(function (x) {
		return x.id == id;
	});

	if (found.any()) {
		found.forEach(function (e) {
			clearTimeout(e.timeout);
			e.timeout = setTimeout(removeOnTimeout(connectionId), pingTimeout)
		});
	}

	if (!found.any() || $(".user-picture-" + id).length == 0) {
		userEnterMeeting(connection);
	}

}

function userExitMeeting(connectionId) {
	$(".user-status-" + connectionId).remove();
	$(".user-picture-" + connectionId).remove();

	userEnterMeeting.existing = userEnterMeeting.existing || [];

	//Before Removal
	Enumerable.from(userEnterMeeting.existing).where(function (x) { return x.connectionId == connectionId; }).forEach(function (e) {
		clearTimeout(e.timeout);
	});

	//Remove
	userEnterMeeting.existing = Enumerable.from(userEnterMeeting.existing).where(function (x) {
		return x.connectionId != connectionId;
	}).toArray();

}
