var statusTimeout;
$(function() {
	try {
		var messageHub = $.connection.messageHub;

		messageHub.client.showAlert = function(data, showSuccess) {
			debugger;
			showJsonAlert(data, showSuccess);
		};
		$.connection.hub.start().done(function() {
			console.log("realtime connected");
		});
	} catch (e) {
		console.error(e);
	}
});
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