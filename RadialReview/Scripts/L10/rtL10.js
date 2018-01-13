var meetingHub;
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

    var setup = function(i) {
		//All Scripts loaded
		meetingHub = $.connection.meetingHub;
		/*meetingHub.error = function (error) {
			console.log(error);
		};*/

		if (typeof (meetingHub) === "undefined") {
		    if (i==20){
		        showAlert("Error. Please try refreshing.");
		        return;
		    }
		    console.log("Hub undefined. Trying again. Attempt " + (i + 2));
		    setTimeout(function(){setup(i+1);},500);
		    return;
		}


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

		meetingHub.client.receiveUpdateScore = receiveUpdateScore;
		meetingHub.client.updateScoresGoals = updateScoresGoals;

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
		meetingHub.client.removeDivider = removeDivider;

		meetingHub.client.addLogRow = addLogRow;
		meetingHub.client.editLogRow = editLogRow;
		meetingHub.client.addOrEditLogRow = addOrEditLogRow;

		meetingHub.client.appendHeadline = appendHeadline;
		meetingHub.client.updateHeadlineMessage = updateHeadlineMessage;

		meetingHub.client.addTranscription = addTranscription;

		meetingHub.client.showAlert = showAlert;

		meetingHub.client.userEnterMeeting = userEnterMeeting;
		meetingHub.client.userExitMeeting = userExitMeeting;
		meetingHub.client.stillAlive = stillAlive;

		meetingHub.client.addVideoProvider = addVideoProvider;
		meetingHub.client.setSelectedVideoProvider = setSelectedVideoProvider;
		meetingHub.client.joinVideoConference = joinVideoConference;
		//meetingHub.client.setLeader = setLeader;
		meetingHub.client.disableItem = disableItem;
		meetingHub.client.updateCumulative = updateCumulative;

		meetingHub.client.updateIssueAwaitingSolve = updateIssueAwaitingSolve;
		meetingHub.client.updateModedIssueSolve = updateModedIssueSolve;
		meetingHub.client.removeIssueRow = removeIssueRow;


		meetingHub.client.setMilestone = setMilestone;
		meetingHub.client.deleteMilestone = deleteMilestone;

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
	console.log("called initConnection");

	rejoin(function () {
		console.log("Logged in: " + $.connection.hub.id);
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
	console.log("called rejoin");
	try {
		if (meetingHub) {
			reconnectionCount += 1;
			meetingHub.server.join(window.recurrenceId, $.connection.hub.id).done(function () {
				//update(d);
				if (rejoinTimer) {
					showAlert("Successfully joined.", "alert-danger", "Error", 1500);
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
					showAlert("Reconnected.", "alert-success", "Success", 1000);
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
						attempt = " Attempt " + (reconnectionCount + 1) + ".";
					}
					console.log("Attempt #" + reconnectionCount);

					showAlert("Attempting to rejoin." + attempt, "alert-danger", "Error", Math.max(3000, 1000 + Math.pow(1.5, reconnectionCount) * 1000));
					rejoinTimer = setTimeout(function () {
						rejoin(callback);
					}, 3000 + Math.pow(1.5, reconnectionCount) * 1000);
				}, 2000)
			});
		}
	} catch (e) {
		console.error(e);
		showAlert("Could not connect with server.", "alert-danger", "Error");
		setTimeout(function () {
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

var reping =function () {
	console.log("Repinging - "+ new Date());
	meetingHub.server.ping();
};

var myPing = setInterval(reping, pingTimeout - 5000);

function removeOnTimeout(connectionId) {
	return function () {
		console.warn("User timed out: " + connectionId +" (not removing)");
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
		var pix = $("<span class='user-picture user-picture-" + connectionId + " user-picture-"+id+"' data-userid='" + id + "'>" + profilePicture(url, name, initials) + "</span>");
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
		},10000);
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

	if (!found.any() || $(".user-picture-"+id).length == 0){
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
