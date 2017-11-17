
//$(function () {

//    //All Scripts loaded
//    cpHub = $.connection.coreProcessHub;

//    $.connection.hub.start().done(function () {
//        console.log("CoreProcess start");
//        cpHub.server.join($.connection.hub.id).done(function () {
//            console.log("CoreProcess connected");
//        });
//    });

//    cpHub.client.showMessage = function (data) {
//        showAlert(data);
//        console.warn("CoreProcess Message:", data);
//    };
//});