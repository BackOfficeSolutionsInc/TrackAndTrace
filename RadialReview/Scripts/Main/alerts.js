
function UnstoreJsonAlert() {
	var data = localStorage.getItem("Alert");
	localStorage.setItem("Alert", null);

	var alert = JSON.parse(data);
	if (alert !== undefined && alert != null && alert != "null") {
		clearAlerts();
		var type = alert.type;
		var title = alert.title;
		var message = alert.message;
		if (type === undefined) type = "alert-success";
		if (title === undefined) title = "Success!";
		if (message === undefined) message = "";
		showAlert(message, type, title);
	}
}

function StoreJsonAlert(json) {
	var alert = new Object();
	alert.message = json.Message;
	if (!json.MessageType)
		json.MessageType = "danger";
	alert.type = "alert-" + json.MessageType.toLowerCase();
	alert.title = json.Heading;
	localStorage.setItem("Alert", JSON.stringify(alert));
}

function showHtmlErrorAlert(html, defaultMessage) {
	var message = defaultMessage;
	if (typeof (html) === "object" && typeof (html.responseText) === "string") {
		var ele = $($(html.responseText)[1]);
		if (ele.is("title")) {
			message = ele.text();
		}
	} else if (typeof (html) === "string") {
		message = $(html).text();
	}

	if (typeof (message) === "undefined" || message == null || message == "") {
		message = "An error occurred.";
	}
	showAlert(message);
}

function showAlert(message, alertType, preface, duration) {
	if (typeof (alertType) === "number" && typeof (preface) === "undefined" && typeof (duration) === "undefined")
		duration = alertType;
	else if (typeof (preface) === "number" && typeof (duration) === "undefined")
		duration = preface;

	if (alertType === undefined)
		alertType = "alert-danger";
	if (preface === undefined)
		preface = "Warning!";
	if (Object.prototype.toString.call(message) === '[object Array]') {
		if (message.length > 1) {
			var msg = "<ul style='margin-bottom:0px;'>";
			for (var i in message) {
				if (arrayHasOwnIndex(message, i)) {
					msg += "<li>" + message[i] + "</li>";
				}
			}
			message = msg + "</ul>"
		} else {
			message = message.join("");
		}
	}

	var alert = $("<div class=\"alert " + alertType + " alert-dismissable start\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + preface + "</strong> <span class=\"message\">" + message + "</span></div>");
	$("#alerts").prepend(alert);
	setTimeout(function () { alert.removeClass("start"); }, 1);

	if (typeof (duration) !== "number") {
		duration = 3000;
	}
	setTimeout(function () {
		$(alert).remove();
	}, duration);
}
var alertsTimer = null;
function clearAlerts() {
	var found = $("#alerts .alert").remove();
}

function showAngularError(d, status, headers, config, statusTxt) {
	if (typeof (d) === "undefined" || d == null) {
		showJsonAlert();
		return;
	}

	if (typeof (d.Message) !== "undefined" && d.Message != null) {
		showJsonAlert(d);
	} else if (typeof (d.data) !== "undefined" && d.data != null) {
		showJsonAlert(d.data);
	} else {
		if (typeof (d.statusText) !== "undefined" && d.statusText !== "") {
			showAlert(d.statusText);
		} else {
			if (typeof (d) === "string" && d.indexOf("<body") == -1) {
				showAlert(d);
			} else {
				console.warn("No suitable option for showAngularError");
				debugger;
				showJsonAlert();
			}
		}
	}
}

function showJsonAlert(data, showSuccess, clearOthers) {
	try {
		if (clearOthers) {
			clearAlerts();
		}
		var stdError = "Something went wrong.";

		if (!data) {
			showAlert(stdError);
		} else if (typeof (data) === "string") {
			if (data.trim().length < 300)
				showAlert(data.trim(), "alert-danger", "Error");
			else {
				showAlert(stdError);
			}
		} else {
			var showDetails = typeof (data.NoErrorReport) === "undefined" || !data.NoErrorReport;
			var message = data.Message;
			if (message === undefined)
				message = "";
			if (data.Trace && showDetails) {
				console.error(data.TraceMessage);
				console.error(data.Trace);
			}
			console.log(data.Message);
			if (!data.Silent && (data.MessageType !== undefined && data.MessageType != "Success" || showSuccess)) {
				var mType = data.MessageType || "danger";
				showAlert(message, "alert-" + mType.toLowerCase(), data.Heading);
			}
			if (data.Error) {
				if (showDetails) {
					sendErrorReport();
				}
			}

		}
	} catch (e) {
		console.error(e);
	}
	if (!data)
		return false;
	return !data.Error;
}