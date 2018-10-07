//Server can show alert
RealTime.client.showAlert = function (mObj) {
	showAlert(mObj);
};

//Server can notify users of upcoming meetings
RealTime.client.NotifyOfMeetingStart = function (recurrenceId, recurrenceName) {
	if (window.location.href.toLowerCase().indexOf("/l10/meeting/") == -1) {
		var key = 'NotifyOfMeetingStart_' + recurrenceId;

		if (Cookies.get(key) === undefined) {
			showAlert("Attendees are waiting. <u><a href='/L10/Meeting/" + recurrenceId + "'>Join " + recurrenceName + "</a></u>", 60000);
			Cookies.set(key, 'true', { expires: (1.0 / 96.0) });
		} else {
			console.log("Skipping meeting start notification, already fired.")
		}
	}
};