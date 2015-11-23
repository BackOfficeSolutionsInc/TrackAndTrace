$(function () {

    $('body').on("click", '.videoconference-container .start-video', function (e) {
        this.disabled = true;
        $(this).addClass("hidden");
        //debugger;
        connection.leave();
        connection.session.oneway = false;
        connection.openOrJoin();
        jQuery.dequeue($("#main-window"), "fx");
    });

    // ................FileSharing/TextChat Code.............
    $('body').on('click', '.videoconference-container .video-container', function () {
        $(".video-overlay").addClass("fade1");
        var self = this;
        setTimeout(function () {
            console.log("FIVE");
            $(".video-overlay video").attr("src", $(self).find("video").attr("src"));
            $(".video-overlay video")[0].muted = $(self).find("video")[0].muted;
            $(".video-overlay").removeClass("hidden1");
            setTimeout(function () {
                $(".video-overlay").removeClass("fade1");
            }, 100);
        }, 150);
    });
    $('.video-overlay').click(function () {
        $(".video-overlay").addClass("hidden1");
        $(".video-overlay video").attr("src", "");
    });
    $('body').on('click', '.videoconference-container .start-video:not(.disabled)', function () {
        $(".start-video").addClass("disabled");
        //TODO Start video
    });
    /*$('body').on('click', '.uncollapser .clicker', function () {
        $(".video-bar").toggleClass("shifted");
        $(this).parent().toggleClass("shifted");
    });*/

    $("body").on("click", ".share-file", function (e) {
        var fileSelector = new FileSelector();
        fileSelector.selectSingleFile(function (file) {
            connection.send(file);
        });
    });

    $("body").on("keyup", ".input-text-chat", function (e) {
        if (e.keyCode != 13) return;
        // removing trailing/leading whitespace
        this.value = this.value.replace(/^\s+|\s+$/g, '');
        if (!this.value.length) return;
        connection.send(this.value);
        appendDIV(this.value);
        this.value = '';
    });

    /*$(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-slash-close");
    $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-2");
    $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic-no");
    $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic");

    if (video && _mediaStream.getVideoTracks().length > 0) {
        $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-2");
    } else {
        $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-slash-close");
    }
    if (audio && _mediaStream.getAudioTracks().length > 0) {
        $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic");
    } else {
        $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic-no");
    }*/

    function selectStream(type, callback) {
        for (var i = 0; i < connection.attachStreams.length; i++) {
            var cur = connection.attachStreams[i];
            var e = connection.streamEvents[cur.streamid]
            if (e.type == type) {
                callback(cur);
            }
        }
    }
    var videoMuted = false;
    $('body').on('click', '.videoconference-container .sendVideo', function (e) {
        /*if (_mediaStream.getVideoTracks().length == 1) {
            var old = _mediaStream.getVideoTracks()[0].enabled;
            var newState = !old;
            _mediaStream.getVideoTracks()[0].enabled = newState;
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-slash-close");
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-2");
            if (newState) {
                $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-2");
            } else {
                $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-slash-close");
            }
        }*/
        var isMute = $(".videoconference-container .sendVideo").hasClass("fontastic-icon-eye-slash-close");

        if (isMute) { // UNMUTE
            selectStream('local', function (cur) {
                cur.getVideoTracks()[0].enabled = true;
            });
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-slash-close");
            $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-2");
        } else {
            selectStream('local', function (cur) {
                cur.getVideoTracks()[0].enabled = false;
            });
            $(".videoconference-container .sendVideo").removeClass("fontastic-icon-eye-2");
            $(".videoconference-container .sendVideo").addClass("fontastic-icon-eye-slash-close");
        }


    });
    $('body').on('click', '.videoconference-container .sendAudio', function (e) {
               var isMute = $(".videoconference-container .sendAudio").hasClass("fontastic-icon-mic-no");

        if (isMute) { // UNMUTE
            selectStream('local', function (cur) {
                cur.getAudioTracks()[0].enabled = true;
            });
            $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic-no");
            $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic");
            $(".videoconference-container .mine").removeClass("no-mic");
        } else {
            selectStream('local', function (cur) {
                cur.getAudioTracks()[0].enabled = false;
            });
            $(".videoconference-container .sendAudio").removeClass("fontastic-icon-mic");
            $(".videoconference-container .sendAudio").addClass("fontastic-icon-mic-no");
            $(".videoconference-container .mine").addClass("no-mic");
        }
    });
});