var statusTimeout;

var messageHub;
$(function () {
	messageHub = $.connection.messageHub;
	$.connection.hub.start(Constants.StartHubSettings).done(function () {
		messageHub.server.joinUser($.connection.hub.id).done(function () {
			console.log("connected to messageHub")
		});
	});

	messageHub.client.showAlert = function (mObj) {
		showAlert(mObj);
	};

	messageHub.client.NotifyOfMeetingStart = function (recurrenceId, recurrenceName) {
		if (window.location.href.toLowerCase().indexOf("/l10/meeting/") == -1) {
			var key = 'NotifyOfMeetingStart_'+recurrenceId;
			
			if (Cookies.get(key) === undefined) {
				showAlert("Attendees are waiting. <u><a href='/L10/Meeting/" + recurrenceId + "'>Join " + recurrenceName + "</a></u>", 60000);
				Cookies.set(key, 'true', { expires: (1.0 / 96.0) });
			} else {
				console.log("Skipping meeting notification, already fired.")
			}
		}
	};
});

//$(function () {
//	//try {
//	//	var messageHub = $.connection.messageHub;
//	//	messageHub.client.showAlert = function(data, showSuccess) {
//	//		debugger;
//	//		showJsonAlert(data, showSuccess);
//	//	};
//	//	$.connection.hub.start(Constants.StartHubSettings).done(function () {
//	//		console.log("realtime connected");
//	//	});
//	//} catch (e) {
//	//	console.error(e);
//	//}
//});
/*
messageHub.client.unhide = function (selector) {
    $(selector).show();
};
alertHub.client.status = function (text) {

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
        })
    }, 2000);
};*/