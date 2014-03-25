var statusTimeout;
var alertHub = $.connection.alertHub;

alertHub.client.jsonAlert = function (data, showSuccess) {
    showJsonAlert(data, showSuccess);
};

alertHub.client.unhide = function (selector) {
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
};

$.connection.hub.start().done(function () {
});
