﻿
var myPage = "";
var followLeader = true;
//var isLeader = false;
//var meetingStart = false;

function imageListFormat(state) {
	if (!state.id) {
		return state.text;
	}
	var $state = $('<span><img style="max-width:32;max-height:32px"  src="' + $(state.element).data("img") + '" class="img-flag" /> ' + state.text + '</span>');
	return $state;
};

function initL10() {
	updateTime();
	resizing();
	if (meetingStart && (isLeader || !followLeader)) {
		loadPage(window.location.hash.replace("#", ""));
	}

	if (!meetingStart) {

		loadPage("startmeeting");
	}

	setInterval(updateTime, 100);
	$(window).resize(resizing);


	$(".agenda a").click(function () {
		var loc = $(this).data("location");
		loadPage(loc);
	});

	$("body").on("click", ".issuesModal", function () {
		var dat = $(this).data();
		var parm = $.param(dat);
		var m=$(this).data("method");
		if (!m)
			m = "Modal";
		var title = dat.title || "Add an issue";
		showModal(title, "/Issues/"+m+"?" + parm, "/Issues/"+m);
	});

	$("body").on("click", ".todoModal", function () {
		var dat = $(this).data();
		var parm = $.param(dat);
		var m=$(this).data("method");
		if (!m)
			m = "Modal";
		var title = dat.title || "Add a to-do";
		showModal(title, "/Todo/"+m+"?" + parm, "/Todo/"+m);
	});

}

function highlight(item) {
	if (!item.hasClass("editable-bg-transition")) {
		item.addClass("editable-bg-transition");
		setTimeout(function() {
			item.css("background-color", "rgba(0,0,0,0)");
			setTimeout(function() { item.removeClass("editable-bg-transition"); }, 1700);
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
	$(".agenda a").removeClass("clickable");
	$(".agenda a").removeClass("lockedPointer");
	$(".agenda a").prop("title", "");
	$(".agenda a").prop("href", "#");
	if (isLeader || !followLeader || !meetingStart) {
		$(".agenda a").addClass("clickable");
		$(".agenda a").each(function() {
			$(this).prop("href", "#" + $(this).data("location"));
		});
	} else {
		if (meetingStart) {
			$(".agenda a").prop("title", "Unlock to change pages");
			$(".agenda a").addClass("lockedPointer");
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
		hours: Math.max(0,hours),
		minutes: Math.max(0,minutes),
		seconds: Math.max(0,secs),
		ms :Math.max(0,ms)
	};
}

var lessThan10 = true;

function updateTime() {
	var now = new Date();
	var h = now.getHours() % 12 || 12;
	lessThan10 = h < 10;
	$(".current-time .hour").html(h);
	$(".current-time .minute").html(pad(now.getMinutes(), 2));
	$(".current-time .second").html(pad(now.getSeconds(), 2));

	if (typeof startTime != 'undefined') {
		var elapsed = ms2Time(now - startTime);
		$(".elapsed-time .hour").html(elapsed.hours);
		$(".elapsed-time .minute").html(pad(elapsed.minutes, 2));
		$(".elapsed-time .second").html(pad(elapsed.seconds, 2));
		$(".elapsed-time").show();
	} else {
		$(".elapsed-time").hide();
	}


	var nowUtc = new Date().getTime();
	if (typeof currentPage != 'undefined' && currentPage!=null) {
		var ee = ms2Time(nowUtc - currentPageStartTime);
		setPageTime(currentPage, (ee.minutes + ee.hours * 60 + currentPageBaseMinutes+ee.seconds/60));
	}

}

function setPageTime(pageName, minutes) {
	var over = $(".page-" + pageName + " .page-time").data("over");	
	var sec =Math.floor(60* (minutes-Math.floor(minutes)));

	$(".page-" + pageName + " .page-time").html(Math.floor(minutes) + "m<span class='second'>"+sec+"s</span>");
	//$(".page-time.page-" + pageName).prop("title", Math.floor(minutes) + "m" + pad(sec, 2) + "s");
	if (minutes >= over) {
		$(".page-"+pageName+" .page-time").addClass("over");
	} else {
		$(".page-" + pageName + " .page-time").removeClass("over");
	}

}
function setupMeeting(_startTime,leaderId) {
	console.log("setupmeeting");
	$(".page-item .page-time").html("");
	meetingStart = true;
	isLeader = (leaderId == userId);
	startTime = _startTime;
	resetClickables();
}

function concludeMeeting() {
	isLeader = false;
	meetingStart = false;
	resetClickables();
	loadPage("stats");
}

function setCurrentPage(pageName, startTime,baseMinutes) {
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
	window.location.hash = '#'+hash;
}

function loadPage(location) {
	if (!followLeader || isLeader || !meetingStart) {
		loadPageForce(location);
	}
	//window.location.hash = location;
}

function loadPageForce(location) {
	$(".issues-list").sortable("destroy");
	$(".todo-list").sortable("destroy");
	$(".issues-list").sortable("refresh");
	$(".todo-list").sortable("refresh");
	window.location.hash = location;
	location = location.toLowerCase();
	//if (location != myPage) {
	showLoader();
	myPage = location;
	$.ajax({
		url: "/L10/Load/" + MeetingId + "?page=" + location+"&connection="+$.connection.hub.id,
		success: function(data) {
			replaceMainWindow(data);
		},
		error: function() {
			setTimeout(function() {
				$("#alerts").html("");
				showAlert("Page could not be loaded.");
				replaceMainWindow("");
			}, 1000);
		}
	});
	//}
}

function resizing() {
	var clock = $(".current-time");
	var maxSec = 221;
	var maxResize = 192;
	if (lessThan10) {
		maxSec = 166;
		maxResize = 0;
	}
	if (clock.width() < maxSec) {
		$(".current-time .second").css("display", "none");
	} else {
		$(".current-time .second").css("display", "inherit");
	}
	if (clock.width() < maxResize) {
		$(".current-time .big").css({ "font-size": "40px", "line-height": "85px" });
	} else {
		$(".current-time .big").css({ "font-size": "60px", "line-height": "" });
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

function replaceMainWindow(html) {
	$("#main-window").fadeOut(200, function () {
		$("#main-window").html(html);
		$("#main-window").fadeIn(200);
	});
}

function fixSidebar() {
	if ($(document).width() < 991) {
		$(".fixed-pos").css("top", 0);
	} else {
		$(".fixed-pos").css("top", $(document).scrollTop());
	}
}

$(document).scroll(fixSidebar);
$(document).resize(fixSidebar);

$(document).on("click", ".arrowkey", function() {
	var that = this;
	setTimeout(function() {
			if($(that).position().top < $(window).scrollTop() || $(that).position().top + $(that).height() > $(window).scrollTop() + (window.innerHeight || document.documentElement.clientHeight)){
				//scroll up
				var scr= $(that).offset().top - (window.innerHeight || document.documentElement.clientHeight) / 2.0;
				$('html, body').scrollTop(scr);
			}else if(false){
				//scroll down
				$('html, body').scrollTop($(that).position().top - (window.innerHeight || document.documentElement.clientHeight) + $(that).height() + 15);
			}
	}, 1);
});

$(document).keydown(function(event) {
	if (event.which == 27) {
		$(":focus").blur();
		$(".modal").modal('hide');
	}
	

	if ($(':focus').length || $(".modal.in").length || event.ctrlKey || event.metaKey || event.altKey) {
		return;
	}
	
	if (event.which == 73 && event.shiftKey) {
		$(".button-bar .issuesModal").click();
		return;
	}
	if (event.which == 84 && event.shiftKey) {
		$(".button-bar .todoModal").click();
		return;
	}

	var f1 = $(".arrowkey.selected,.arrowkey.selected");
	if (event.which == 38) {
		if (f1.length>0) {
			f1.prev(".arrowkey").click();
		} else {
			$(".arrowkey").last().click();
		}
	}else if (event.which == 40) {
		if (f1.length >0) {
			f1.next(".arrowkey").click();
		}else {
			$(".arrowkey").first().click();
		}
	} else if (event.which == 32||event.which == 13) {
		$(f1).find(".todo-checkbox,.issue-checkbox").click();
		return false;
	}else if (event.which == 73) {
		$(f1).find(".issuesModal").click();
	} else if (event.which == 84) {
		$(f1).find(".todoModal").click();
	}/*else if (event.which == 67) {
		$(f1).find(".todoModal").click();
	}*/else if (event.which == 9) {
		$(".message-holder .message").click();
		event.preventDefault();
	}else if (event.which == 33) {
		$(".page-item.current").prev().find("a").click();
	}else if (event.which == 34) {
		$(".page-item.current").next().find("a").click();
	}
});
