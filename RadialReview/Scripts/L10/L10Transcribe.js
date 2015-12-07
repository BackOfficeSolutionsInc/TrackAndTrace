
function clickMuteTranscribe(self) {
    var shouldBeActive = !$(self).hasClass("fontastic-icon-mic");
    if (shouldBeActive) {
        beginSpeech();
        $(self).addClass("fontastic-icon-mic");
        $(self).removeClass("fontastic-icon-mic-no");
    } else {
        speechRecog.Stop();
        $(self).addClass("fontastic-icon-mic-no");
        $(self).removeClass("fontastic-icon-mic");
        $(".transcript-container .transcribing").stop().css("opacity", 0);
    }
}

function beginSpeech() {
    if (speechRecog.Init()) {
        speechRecog.Start();
        $(".transcript-container").removeClass("hidden");
    }
}

function getSelectionText() {
    var text = "";
    if (window.getSelection) {
        text = window.getSelection().toString();
    } else if (document.selection && document.selection.type != "Control") {
        text = document.selection.createRange().text;
    }
    return text;
}

function getSelectedTextWithin(el) {
    var selectedText = "";
    if (typeof window.getSelection != "undefined") {
        var sel = window.getSelection(), rangeCount;
        if ((rangeCount = sel.rangeCount) > 0) {
            var range = document.createRange();
            for (var i = 0, selRange; i < rangeCount; ++i) {
                range.selectNodeContents(el);
                selRange = sel.getRangeAt(i);
                if (selRange.compareBoundaryPoints(range.START_TO_END, range) == 1 && selRange.compareBoundaryPoints(range.END_TO_START, range) == -1) {
                    if (selRange.compareBoundaryPoints(range.START_TO_START, range) == 1) {
                        range.setStart(selRange.startContainer, selRange.startOffset);
                    }
                    if (selRange.compareBoundaryPoints(range.END_TO_END, range) == -1) {
                        range.setEnd(selRange.endContainer, selRange.endOffset);
                    }
                    selectedText += range.toString();
                }
            }
        }
    } else if (typeof document.selection != "undefined" && document.selection.type == "Text") {
        var selTextRange = document.selection.createRange();
        var textRange = selTextRange.duplicate();
        textRange.moveToElementText(el);
        if (selTextRange.compareEndPoints("EndToStart", textRange) == 1 && selTextRange.compareEndPoints("StartToEnd", textRange) == -1) {
            if (selTextRange.compareEndPoints("StartToStart", textRange) == 1) {
                textRange.setEndPoint("StartToStart", selTextRange);
            }
            if (selTextRange.compareEndPoints("EndToEnd", textRange) == -1) {
                textRange.setEndPoint("EndToEnd", selTextRange);
            }
            selectedText = textRange.text;
        }
    }
    return selectedText;
}

function reselectNearset() {
    var s = window.getSelection();
}

function sortOrder(a, b) {
    return (+$(a).attr("data-order")) < (+$(b).attr("data-order")) ? 1 : -1;
}

function addTranscription(text, name, order, id, nofade) {
    var lookup = {
        "to do to ": "todo-text",
        "to do ": "todo-text",
        "issue to ": "issue-text",
        "issue for ": "issue-text",
        "issue ": "issue-text",
    };

    var tt = text;
    for (var i in lookup) {
        var ind = tt.indexOf(i);
        var len = i.length;
        if (ind > 0) {
            var selectedText = tt.substring(ind + len);
            if (selectedText.length > 0)
                selectedText = selectedText.replace(/^./, selectedText[0].toUpperCase());
            tt = tt.substring(0, ind + len) + "<span  class='" + lookup[i] + "' >" + selectedText + "</span>";
            break;
        }
    }

    var newLi = $("<li data-transcript-id='" + id + "' title='" + escapeString(name) + "' data-order='" + order + "'>" + tt + "</li>");
    if (typeof (nofade) === "undefined" || nofade == false) {
        newLi = newLi.hide().fadeIn(800);
    }

    $(".transcription-contents").prepend(newLi);//.sort(sortOrder).appendTo('.transcription-contents';

    //var elem = $( + tt + "</li>");
    //$(".transcription-contents").prepend(elem);
}

var speechRecog = null;

function InitTranscribe(thisRecurrenceId, thisMeetingId, shouldStartTranscribe) {
    $(document).on("click", ".todo-text", function () {
        var text = encodeURIComponent($(this).text());
        var tid = $(this).parent("li").attr("data-transcript-id");
        showModal("Add a to-do",
                  "/Todo/CreateTodo?recurrence=" + thisRecurrenceId + "&meeting=" + thisMeetingId + "&todo=" + text + "&modelid=" + tid + "&modeltype=Transcript",
                  "/Todo/CreateTodo");
    });
    $(document).on("click", ".issue-text", function () {
        var text = encodeURIComponent($(this).text());
        var tid = $(this).parent("li").attr("data-transcript-id");
        showModal("Add an issue",
                  "/Issues/CreateIssue?recurrence="+thisRecurrenceId+"&meeting="+this.meetingItemId+"&issue=" + text + "&modelid=" + tid + "&modeltype=Transcript",
                  "/Issues/CreateIssue");
    });
    speechRecog = new SpeechRecog();
    var speech = speechRecog;
    speech.ignore_onend = true;
    speech.onfinalresult = function (e) {
        var style = "";
        var text = e.transcript;
        var data = {
            MeetingId: meetingId,
            RecurrenceId: thisRecurrenceId,
            Text: text,
            ConnectionId: $.connection.hub.id
        };
        $.ajax({
            url: "/transcript/add/",
            data: data,
            method: "post",
            success: function (data) {
                if (showJsonAlert(data)) {
                    $(".temp").html("");
                    $(".transcript-container .transcribing").stop().css("opacity", 0);
                    addTranscription(e.transcript, "You", data.Object.date, data.Object.id, true);
                }
            }
        });
    };

    speech.oninterimresult = function (e) {
        var style = "";
        if (e.indexOf("to do") > 0) {
        }
        if (+$(".transcript-container .transcribing").css("opacity") == 0) {
            $(".transcript-container .transcribing").animate({ "opacity": .4 }, 550);
        }
        $(".temp").html("<li style='" + style + "'>" + e + "</li>");
    };
    $(".transcript-container").appendTo(".footer-bar");
    if (shouldStartTranscribe) {
        beginSpeech();
    }

}

$(document).ready(function () {

    var selectingTranscription = false;

    $(document).mouseup(function (e) {
        if (selectingTranscription && shouldStartTranscribe) {
            selectingTranscription = false;

            var selected = getSelectedTextWithin($(".transcription-contents")[0]);
            console.log(selected);
        }
    });

    $(document).mousedown(function (e) {
        selectingTranscription = true;
    });
});