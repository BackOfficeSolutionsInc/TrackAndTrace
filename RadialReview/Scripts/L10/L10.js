
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

	resizing();
	if (meetingStart && (isLeader || !followLeader)) {
		loadPage(window.location.hash.replace("#", ""));
	}

	if (!meetingStart) {

		loadPage("startmeeting");
	}

	$(window).resize(resizing);

	$(".agenda .agenda-items a").click(function () {
		var loc = $(this).data("location");
		loadPage(loc);
	});
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

	$(".timer-bar").hide();
}

function ms2Time(ms) {
	var secs = ms / 1000;
	ms = Math.floor(ms % 1000);
	var minutes = secs / 60;
	secs = Math.floor(secs % 60);
	var hours = minutes / 60;
	minutes = Math.floor(minutes % 60);
	hours = Math.floor(hours/* % 24 */);
	var out = {
		hours: Math.max(0, hours),
		minutes: Math.max(0, minutes),
		seconds: Math.max(0, secs),
		ms: Math.max(0, ms)

	};
	out.totalMinutes = out.hours * 60 + out.minutes + out.seconds / 60 + out.ms / 60000;
	return out;
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
			$(".elapsed-time .minute").html(elapsed.hours * 60 + elapsed.minutes);
			$(".elapsed-time .second").html(pad(elapsed.seconds, 2));
			$(".elapsed-time").show();
			$(".timer-bar").show();
			updateTimebar(elapsed.totalMinutes);

		} else {
			$(".elapsed-time").hide();
			$(".timer-bar").hide();
			$(".clock-container").removeClass("overtime");
			$(".clock-container").removeClass("extra-overtime");
		}

		var nowUtc = new Date().getTime();
		if (typeof currentPage != 'undefined' && currentPage != null && typeof countDown != 'undefined') {
			var ee = ms2Time(nowUtc - currentPageStartTime);
			setPageTime(currentPage, (ee.minutes + ee.hours * 60 + currentPageBaseMinutes + ee.seconds / 60));
		}
	}
}
var expectedMeetingDuration = false;
var lastTimeBarPercentage = false;
function updateTimebar(elapsedMin) {
	try {
		if (expectedMeetingDuration == false || typeof (expectedMeetingDuration) === "undefined") {
			var sum = 0;
			$(".page-time").each(function () {
				var over = $(this).data("over")
				if (typeof (over) === "string")
					over = over.replace(",", ".");
				sum += +over;
			})
			expectedMeetingDuration = sum;
		}

		var percentage = Math.round(Math.min(1, Math.max(0.01, (elapsedMin) / expectedMeetingDuration)) * 100);
		if (lastTimeBarPercentage != percentage) {
			//$(".timer-bar").show();
			$(".timer-bar").css("width", percentage + "%");
			lastTimeBarPercentage = percentage;
		}

		if ((elapsedMin) / expectedMeetingDuration > 1) {
			$(".clock-container").addClass("overtime");
		}

		if ((elapsedMin) / expectedMeetingDuration > 1.1) {
			$(".clock-container").addClass("extra-overtime");
		}


	} catch (e) {
		console.error(e);
	}

}

function fixPageName(pageName) {
	if (pageName.length && pageName[0] == "/")
		pageName = pageName.substring(1);
	return pageName;
}


var firstSetPageTime = true;
var pausePageTimer = false;
function setPageTime(pageName, minutes) {
	//if (pausePageTimer) {
	//	console.warn("Page timer paused (" + pageName + ")");
	//	return;
	//}

	
	pageName = fixPageName(pageName);

	try {
		if (typeof (meetingStart) !== "undefined" && meetingStart == true) {
			var over = $(".page-" + pageName + " .page-time,.page-item." + pageName + " .page-time").data("over")
			if (typeof (over) === "string")
				over = over.replace(",", ".");
			var sec = Math.floor(60 * (minutes - Math.floor(minutes)));
			var displayMinutes = Math.floor(minutes);
			if (countDown) {
				displayMinutes = over - Math.floor(minutes);
			}

			$(".page-" + pageName + " .page-time,.page-item." + pageName + " .page-time").html(displayMinutes + "m<span class='second'>" + sec + "s</span>");
			if (minutes >= over) {
				var p = $(".current.page-" + pageName + " .page-time,.current.page-item." + pageName + " .page-time");
				if (!firstSetPageTime && p.length && !p.hasClass("over")) {
					var audio1 = new Audio('https://s3.amazonaws.com/Radial/audio/pop+pop.mp3');
					audio1.play();
					//setTimeout(function () {
					//    var audio2 = new Audio('https://s3.amazonaws.com/Radial/audio/pop+pop.mp3');
					//    //audio.currentTime = 0;
					//    audio2.play();
					//}, 250);
				} else if ($(".page-" + pageName + " .page-time,.page-item." + pageName + " .page-time").length) {
					firstSetPageTime = false;
				}
				$(".page-" + pageName + " .page-time,.page-item." + pageName + " .page-time").addClass("over");
			} else {
				$(".page-" + pageName + " .page-time,.page-item." + pageName + " .page-time").removeClass("over");
			}
		}
	} catch (e) {
		console.error(e);
	}

}
function setupMeeting(_startTime, leaderId) {
	console.log("setupmeeting");
	$(".over").removeClass("over");
	$(".page-item .page-time").each(function () {
		var o = $(this).data("over");
		$(this).html("<span class='gray'>" + (Math.round(100 * o) / 100.0) + "m</span>");

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
	$(".timer-bar").hide();
}

function setCurrentPage(pageName, startTime, baseMinutes) {

	pageName = fixPageName(pageName);

	if (pageName == "") {
		pageName = "segue";
	}
	pausePageTimer = true;
	currentPage = pageName;
	currentPageStartTime = new Date().getTime();//startTime;
	currentPageBaseMinutes = baseMinutes;
	console.log("setCurrentPage:" + pageName + " " + currentPageStartTime + " " + baseMinutes);
	$(".page-item.current").removeClass("current");
	$(".page-item.page-" + pageName + ",.page-item." + pageName).addClass("current");
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

	location = fixPageName(location);

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
				pausePageTimer = false;
			}
		}, 4000);
		$.ajax({
			url: "/L10/Load/" + window.recurrenceId + "?page=" + location + "&connection=" + $.connection.hub.id,
			success: function (data) {
				replaceMainWindow(data, function () {
					var type = $(".page-item." + location).data("pagetype");
					if (typeof (type) === "undefined"|| type==null)
						type = location;
					try{
						$(window).trigger("page-" + /*location*/type.toLowerCase());
					} catch (e) {
						console.log(e);
					}
					fixSidebar();
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
				pausePageTimer = false;
			}
		});
	} else {
		console.log("Already Loading: " + location);
		pausePageTimer = false;
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
	if ($(document).width() < 767) {
		$(".fixed-pos").css("top", 0);
		$(".fixed-pos.fader").css("background-color", "rgba(238, 238, 238, 0)");
		$(".fixed-pos.fader").css("box-shadow", "none");
		$(".fixed-pos.fader").css("border-bottom", "1px solid #eeeeee");
		$(".fixed-pos.fader").css("z-index", "-1");
		$(".fixed-pos.fader").css("padding-bottom", "9px");
	} else {
		$(".fixed-pos").css("top", $(".slider-container.level-10").scrollTop());
		$(".fixed-pos.fader").css("background-color", "rgba(238, 238, 238, " + (($(".slider-container.level-10").scrollTop() / 70)) + ")");
		$(".fixed-pos.fader").css("box-shadow", "0 4px 2px -2px rgba(128, 128, 128, " + Math.min(.2, $(".slider-container.level-10").scrollTop() / 350) + ")");
		//$(".fixed-pos.fader").css("border-bottom", "none");
		$(".fixed-pos.fader").css("z-index", 2);//$(".slider-container.level-10").scrollTop() - 10);
		$(".fixed-pos.fader").css("padding-bottom", (1 - $(".slider-container.level-10").scrollTop() / 70) * 9);


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
	showModal("Generate Quarterly Printout", "/Quarterly/Modal/" + window.recurrenceId, "/Quarterly/Printout/" + window.recurrenceId, "callbackPrint");
}
function callbackPrint() {

	$("#modalForm").unbind("submit");
	$("#modalForm").attr("target", "_blank");
	$("#modalForm").attr("method", "post");
	$("#modalForm").attr("action", "/Quarterly/Printout/" + window.recurrenceId);
	$("#modalForm").bind("submit", function () {
		$("#modal").modal('hide');
	});
}

function lockConcludeButton() {
	$("#conclude_meeting_button").prop("disabled", "disabled");
}



function fixExternalPageBoxSize() {
	if ($(".externalpage-box").length) {
		var wh = $(window).height();
		var pos = $(".externalpage-box").offset();
		var st = $(window).scrollTop();
		var footerH = wh;
		try {
			footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) { }

		$(".externalpage-box").height(Math.max(200, footerH /*- 20 */- 50 - pos.top));
	}
}

$(window).resize(fixExternalPageBoxSize);
$(window).on("page-externalpage", fixExternalPageBoxSize);
$(window).on("footer-resize", function () {
	setTimeout(fixExternalPageBoxSize, 250);
});



$(window).on("page-segue", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting");
});
$(window).on("page-scorecard", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting#/Scorecard");
});
$(window).on("page-rocks", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting#/Rocks");
});
$(window).on("page-headlines", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting#/Headlines");
});
$(window).on("page-todo", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting#/Todos");
});
$(window).on("page-ids", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting#/Issues");
});
$(window).on("page-conclusion", function () {
	$("#edit_meeting_link").attr("href", "/L10/Wizard/" + window.recurrenceId + "?return=meeting");
});