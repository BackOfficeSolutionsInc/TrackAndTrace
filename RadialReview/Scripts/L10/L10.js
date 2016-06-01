﻿
var myPage = "";
var followLeader = true;
var meetingStart = meetingStart || false;
//var isLeader = false;
//var meetingStart = false;



//////SETUP UNDO//////
//////////////////////

$(".clocks").removeClass("hidden");
updateTime();
setInterval(updateTime, 100);

function initL10() {

    try {
        $(window).bind('hashchange', function (e) {
            e.preventDefault();
        });
    } catch (e) {
        console.error(e);
    }


    //updateTime();
    resizing();
    if (meetingStart && (isLeader || !followLeader)) {

        loadPage(window.location.hash.replace("#", ""));
    }

    if (!meetingStart) {

        loadPage("startmeeting");
    }

    //setInterval(updateTime, 100);
    $(window).resize(resizing);

    $(".agenda .agenda-items a").click(function () {
        var loc = $(this).data("location");
        loadPage(loc);
    });

    //$("body").on("click", ".issuesModal:not(.disabled)", function () {
    //    var dat = $(this).data();
    //    var parm = $.param(dat);
    //    var m = $(this).data("method");
    //    if (!m)
    //        m = "Modal";
    //    var title = dat.title || "Add an issue";
    //    showModal(title, "/Issues/" + m + "?" + parm, "/Issues/" + m);
    //});

    //$("body").on("click", ".todoModal:not(.disabled)", function () {
    //    var dat = $(this).data();
    //    var parm = $.param(dat);
    //    var m = $(this).data("method");
    //    if (!m)
    //        m = "Modal";
    //    var title = dat.title || "Add a to-do";
    //    showModal(title, "/Todo/" + m + "?" + parm, "/Todo/" + m, null, function () {
    //        debugger;
    //        if ($('#modalBody').find(".select-user").val() == null)
    //            return "You must select at least one to-do owner.";
    //        return true;
    //    });
    //});


}

function highlight(item) {
    if (!item.hasClass("editable-bg-transition")) {
        item.addClass("editable-bg-transition");
        setTimeout(function () {
            item.css("background-color", "rgba(0,0,0,0)");
            setTimeout(function () {
                item.removeClass("editable-bg-transition");
                item.css("background-color", "rgba(0,0,0,0)");
            }, 1700);
        }, 15);
        item.css("background-color", "#FFFF80");
    }
}

function replaceAll(find, replace, str) {
    return str.split(find).join(replace);
}

function setFollowLeader(val) {
    followLeader = val;
    resetClickables();
}

function resetClickables() {
    console.log("resetClickables");
    $(".agenda .agenda-items a").removeClass("clickable");
    $(".agenda .agenda-items a").removeClass("lockedPointer");
    $(".agenda .agenda-items a").prop("title", "");
    $(".agenda .agenda-items a").prop("href", "#");
    if (isLeader || !followLeader || !meetingStart) {
        $(".agenda .agenda-items a").addClass("clickable");
        $(".agenda .agenda-items a").each(function () {
            $(this).prop("href", "#" + $(this).data("location"));
        });

        $(".agenda .agenda-items a").prop("title", "");
    } else {
        if (meetingStart) {
            $(".agenda .agenda-items a").prop("title", "You are following the meeting leader. Unlock to change the page.");
            $(".agenda .agenda-items a").addClass("lockedPointer");
        }
    }
}

function ms2Time(ms) {
    var secs = ms / 1000;
    ms = Math.floor(ms % 1000);
    var minutes = secs / 60;
    secs = Math.floor(secs % 60);
    var hours = minutes / 60;
    minutes = Math.floor(minutes % 60);
    hours = Math.floor(hours/* % 24 */);
    return {
        hours: Math.max(0, hours),
        minutes: Math.max(0, minutes),
        seconds: Math.max(0, secs),
        ms: Math.max(0, ms)
    };
}

var lessThan10 = true;

var lastS;
function updateTime() {
    var now = new Date();
    if (now.getSeconds() != lastS) {
        lastS = now.getSeconds();
        var h = now.getHours() % 12 || 12;
        lessThan10 = h < 10;
        $(".current-time .hour").html(h);
        $(".current-time .minute").html(pad(now.getMinutes(), 2));
        $(".current-time .second").html(pad(now.getSeconds(), 2));

        if (typeof startTime != 'undefined') {
            var elapsed = ms2Time(now - startTime);
            //if (elapsed.hours == 0)
            //    $(".elapsed-time .hour-item").hide();
            //else
            //    $(".elapsed-time .hour-item").show();
            $(".elapsed-time .hour").html(elapsed.hours);
            $(".elapsed-time .minute").html(pad(elapsed.minutes, 2));
            $(".elapsed-time .second").html(pad(elapsed.seconds, 2));
            $(".elapsed-time").show();
        } else {
            $(".elapsed-time").hide();
        }


        var nowUtc = new Date().getTime();
        if (typeof currentPage != 'undefined' && currentPage != null && typeof countDown != 'undefined') {
            var ee = ms2Time(nowUtc - currentPageStartTime);
            setPageTime(currentPage, (ee.minutes + ee.hours * 60 + currentPageBaseMinutes + ee.seconds / 60));
        }
    }

}

function setPageTime(pageName, minutes) {
    if (typeof(meetingStart)!=="undefined" && meetingStart == true) {
        var over = $(".page-" + pageName + " .page-time").data("over");
        var sec = Math.floor(60 * (minutes - Math.floor(minutes)));
        var displayMinutes = Math.floor(minutes);
        if (countDown) {
            displayMinutes = over - Math.floor(minutes);
        }

        $(".page-" + pageName + " .page-time").html(displayMinutes + "m<span class='second'>" + sec + "s</span>");
        //$(".page-time.page-" + pageName).prop("title", Math.floor(minutes) + "m" + pad(sec, 2) + "s");
        if (minutes >= over) {
            $(".page-" + pageName + " .page-time").addClass("over");
        } else {
            $(".page-" + pageName + " .page-time").removeClass("over");
        }
    }

}
function setupMeeting(_startTime, leaderId) {
    console.log("setupmeeting");
    $(".page-item .page-time").each(function () {
        var o = $(this).data("over");
        $(this).html("<span class='gray'>" + (Math.round(100 * o) / 100.0) + "m</span>")
    });
    meetingStart = true;
    isLeader = (leaderId == userId);
    startTime = _startTime;
    resetClickables();
}

function concludeMeeting() {
    isLeader = false;
    meetingStart = false;
    resetClickables();
    delete startTime;// = undefined;
    loadPage("stats");
}

function setCurrentPage(pageName, startTime, baseMinutes) {
    if (pageName == "") {
        pageName = "segue";
    }
    currentPage = pageName;
    currentPageStartTime = startTime;
    currentPageBaseMinutes = baseMinutes;
    $(".page-item.current").removeClass("current");
    $(".page-item.page-" + pageName).addClass("current");
    if (followLeader && !isLeader) {
        loadPageForce(pageName);
    }
}

function setHash(hash) {
    //location.hash = $hash;
    window.location.hash = '#' + hash;
    /*
	if (history.pushState) {
		history.pushState(null, null, '#' + hash);
	}
		//Else use the old-fashioned method to do the same thing,
		//albeit with a flicker of content
	else {
		location.hash = $hash;
		window.location.hash = '#' + hash;
	}*/
}

function loadPage(location) {
    if (!followLeader || isLeader || !meetingStart) {
        loadPageForce(location);
    }
    //window.location.hash = location;
}
var locationLoading = null;
function loadPageForce(location) {
    console.log("Loading:" + location);
    $(".issues-list").sortable("destroy");
    $(".todo-list").sortable("destroy");
    $(".issues-list").sortable("refresh");
    $(".todo-list").sortable("refresh");
    setHash(location);
    //window.location.hash = location;
    location = location.toLowerCase();
    //if (location != myPage) {
    showLoader();
    myPage = location;
    if (locationLoading != location) {
        locationLoading = location;
        setTimeout(function () {
            if (locationLoading == location) {
                locationLoading = null;
            }
        }, 4000);
        $.ajax({
            url: "/L10/Load/" + MeetingId + "?page=" + location + "&connection=" + $.connection.hub.id,
            success: function (data) {
                replaceMainWindow(data, function () {
                    $(window).trigger("page-" + location.toLowerCase());
                });

            },
            error: function () {
                setTimeout(function () {
                    $("#alerts").html("");
                    showAlert("Page could not be loaded.");
                    replaceMainWindow("");
                }, 1000);
            },
            complete: function () {
                locationLoading = null;
            }
        });
    } else {
        console.log("Already Loading: " + location);
    }
    //}
}

function resizing() {
    var clock = $(".elapsed-time");
    var maxSec = 221;
    var maxResize = 192;
    //if (lessThan10) {
    //    maxSec = 166;
    //    maxResize = 0;
    //}
    if (clock.width() < maxSec) {
        $(".elapsed-time .second").css("display", "none");
    } else {
        $(".elapsed-time .second").css("display", "inherit");
    }
    if (clock.width() < maxResize) {
        $(".elapsed-time").css({ "font-size": "40px", "line-height": "60px" });
    } else {
        $(".elapsed-time").css({ "font-size": "60px", "line-height": "" });
    }
}

function pad(num, size) {
    var s = num + "";
    while (s.length < size) s = "0" + s;
    return s;
}

function showLoader() {
    replaceMainWindow("<div class='loader centered'><div class='component '><div>Loading</div><img src='/Content/select2-spinner.gif' /></div></div>");
}

function callWhenReady(selector, callback, scope) {
    var self = this;
    if ($(selector).length) {
        callback.call(scope);
    } else {
        setTimeout(function () {
            self.callWhenReady(selector, callback, scope);
        }, 1);
    }
}
var curHiddenId = 0;
function replaceMainWindow(html, callback) {
    var curId = curHiddenId;
    curHiddenId += 1;
    var name = "hiddenLoad" + curId;
    var b = $("<div/>").attr("id", name).html(html);
    var a = $("#hiddenWindow").append(b);
    $("#main-window").finish().fadeOut(200, function () {
        $("#main-window").html("");
        console.log($(b).children());
        $("#main-window").append($(b).children());
        $(b).remove();
        //callWhenReady("#main-window", function () {
        //   debugger;
        $("#main-window").finish().fadeIn(200, callback);
        //});
    });
}

function printIframe(id) {
    var iframe = document.frames ? document.frames[id] : document.getElementById(id);
    var ifWin = iframe.contentWindow || iframe;
    iframe.focus();
    ifWin.printPage();
    return false;
}

function fixSidebar() {
    //debugger;
    if ($(document).width() < 991) {
        $(".fixed-pos").css("top", 0);
    } else {
        $(".fixed-pos").css("top", $(".slider-container.level-10").scrollTop());
    }
}

$(".slider-container.level-10").scroll(fixSidebar);
$(".slider-container.level-10").resize(fixSidebar);


$(document).on("scroll-to", ".arrowkey", function () {
    var that = this;
    setTimeout(function () {
        console.log("scroll-to");
        if ($(that).position().top < $(window).scrollTop() || $(that).position().top + $(that).height() > $(window).scrollTop() + (window.innerHeight || document.documentElement.clientHeight)) {
            //scroll up
            var scr = $(that).offset().top - (window.innerHeight || document.documentElement.clientHeight) / 2.0;
            $('html, body').scrollTop(scr);
        } else if (false) {
            //scroll down
            $('html, body').scrollTop($(that).position().top - (window.innerHeight || document.documentElement.clientHeight) + $(that).height() + 15);
        }
    }, 1);
});

$(document).keydown(function (event) {
    if (event.which == 27) {
        $(":focus").blur();
        $(".modal").modal('hide');
    }

    if ($(':focus').length || $(".modal.in").length || event.ctrlKey || event.metaKey || event.altKey) {
        return;
    }

    if (event.which == 73 && event.shiftKey) {
        $(".top-button-bar .issuesModal:not(.disabled)").click();
        event.preventDefault();
        return;
    }
    if (event.which == 84 && event.shiftKey) {
        $(".top-button-bar .todoModal:not(.disabled)").click();
        event.preventDefault();
        return;
    }

    var f1 = $(".arrowkey.selected,.arrowkey.selected");
    if (event.which == 38) {
        if (f1.length > 0) {
            f1.prevAll(".arrowkey:not(.issue-row[data-checked=true]):not(.issue-row[data-checked=True])").first().click().trigger("scroll-to");
            event.preventDefault();
        } else {
            $(".arrowkey:not(.issue-row[data-checked=true]):not(.issue-row[data-checked=True])").last().click().trigger("scroll-to");
            event.preventDefault();
        }
    } else if (event.which == 40) {
        if (f1.length > 0) {
            f1.nextAll(".arrowkey:not(.issue-row[data-checked=true]):not(.issue-row[data-checked=True])").first().click().trigger("scroll-to");
            event.preventDefault();
        } else {
            $(".arrowkey:not(.issue-row[data-checked=true]):not(.issue-row[data-checked=True])").first().click().trigger("scroll-to");
            event.preventDefault();
        }
    } else if (event.which == 32 || event.which == 13) {
        $(f1).find(".todo-checkbox,.issue-checkbox").click();
        return false;
    } else if (event.which == 73) {
        $(f1).find(".issuesModal:not(.disabled)").click();
        event.preventDefault();
    } else if (event.which == 84) {
        $(f1).find(".todoModal:not(.disabled)").click();
        event.preventDefault();
    }/*else if (event.which == 67) {
		$(f1).find(".todoModal").click();
	}*/else if (event.which == 9) {
	    $(".message-holder .message").click();
	    event.preventDefault();
	} else if (event.which == 33) {
	    $(".page-item.current").prev().find("a").click();
	} else if (event.which == 34) {
	    $(".page-item.current").next().find("a").click();
	}
});



function showPrint() {
    //Html.ShowModal("Generate Printout","/Quarterly/Modal","/Quarterly/Printout/"+Model.Recurrence.Id,newTab:true)
    showModal("Generate Quarterly Printout", "/Quarterly/Modal", "/Quarterly/Printout/"+recurrenceId, "callbackPrint");
}
function callbackPrint() {

    $("#modalForm").unbind("submit");
    $("#modalForm").attr("target", "_blank");
    $("#modalForm").attr("method", "post");
    $("#modalForm").attr("action", "/Quarterly/Printout/"+recurrenceId);
    $("#modalForm").bind("submit", function () {
        $("#modal").modal('hide');
    });

    // $("#modalForm").bind("submit");
}