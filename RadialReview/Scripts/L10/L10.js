
var mode = "scan";
var myPage = "";
var followLeader = true;
var isLeader = false;
var meetingStart = false;

$(function () {
	updateTime();
	resizing();

	if (isLeader || !followLeader) {
		loadPage(window.location.hash.replace("#", ""));
	}

	setInterval(updateTime, 100);
	$(window).resize(resizing);

	$(".main-window-container").on("change", ".score input", function (e, d) {
		updateScore(this);
		if (!d) {
			updateServerScore(this);
		}
	});
	$('.main-window-container').on('keydown', ".grid", changeInput);
	$('.main-window-container').on('click', ".grid", function (e,d) {if (!d)mode = "scan";});
	$('.main-window-container').on('change', ".grid", function (e,d) { if (!d) mode = "type"; });
	$('.main-window-container').on('scroll', ".grid", function (e) { if (mode == "type") { e.preventDefault(); } });


	$(".agenda a").click(function () {
		var loc = $(this).data("location");
		loadPage(loc);
	});

	$("body").on("click", ".issuesButton", function() {
		showModal("Add to Issues", "/Issues/Modal", "/Issues/Modal");
	});

});

function resetClickables() {
	$(".agenda a").removeClass("clickable");
	$(".agenda a").prop("href", "#");
	if (isLeader || !followLeader) {
		$(".agenda a").addClass("clickable");
		$(".agenda a").each(function() {
			$(this).prop("href", "#" + $(this).data("location"));
		});
	}
}

function updateServerScore(self) {
	var m = $(self).data("measurable");
	var w = $(self).data("week");
	var id = $(self).data("scoreid");
	var val = $(self).val();
	var dom = $(self).attr("id");
	$.ajax({
		url: "/l10/UpdateScore/" + MeetingId + "?s=" + id + "&w=" + w + "&m=" + m + "&value=" + val+"&dom="+dom,
		success: function (data) {

			if (data.Error) {
				showJsonAlert(data);
			}

			/*console.log(data);
			if (data.Object != val) {
				console.log("err:"+data.Object);
				$(self).val("");
			}*/
		}
	});
}


//pass in a .score input
function updateScore(self) {
	var goal = $(self).data("goal");
	var dir = $(self).data("goal-dir");
	var v = $(self).val();
	//Empty?
	$(self).removeClass("error");
	$(self).removeClass("success");
	$(self).removeClass("danger");
	if (!$.trim(v)) {
		//$(self).removeClass("error");
		//do nothing
	} else if ($.isNumeric(v)) {
		if (dir == "GreaterThan") {
			if (+v >= +goal)
				$(self).addClass("success");
			else
				$(self).addClass("danger");
		} else {
			if (+v < +goal)
				$(self).addClass("success");
			else
				$(self).addClass("danger");
		}

	} else {
		$(self).addClass("error");
	}
}

function changeInput() {
	var found;
	var goingLeft = false;
	var goingRight=false;
	if (mode == "scan" ||
		event.which == 38 ||	//pressing up
		event.which == 40 ||	//pressing down
		event.which == 13 ||	//pressing enter
		($(this)[0].selectionStart == 0 && (event.which == 37)) || //all the way left
		($(this)[0].selectionEnd == $(this).val().length && (event.which == 39)) // all the way right
		) {
		if (event.which == 37) { //left
			found = $(".grid[data-col=" + (+$(this).data("col") - 1) + "][data-row=" + $(this).data("row") + "]");
			goingLeft = true;
		} else if (event.which == 38) { //up
			found = $(".grid[data-row=" + (+$(this).data("row") - 1) + "][data-col=" + $(this).data("col") + "]");
		} else if (event.which == 39) { //right
			found = $(".grid[data-col=" + (+$(this).data("col") + 1) + "][data-row=" + $(this).data("row") + "]");
			goingRight = true;
		} else if (event.which == 40 || event.which==13) { //down
			found = $(".grid[data-row=" + (+$(this).data("row") + 1) + "][data-col=" + $(this).data("col") + "]");
		}
		var keycode = event.which;
		var validPrintable =
			(keycode > 47 && keycode < 58) || // number keys
			keycode == 32 || keycode == 13 || // spacebar & return key(s) (if you want to allow carriage returns)
			(keycode > 64 && keycode < 91) || // letter keys
			(keycode > 95 && keycode < 112) || // numpad keys
			(keycode > 185 && keycode < 193) || // ;=,-./` (in order)
			(keycode > 218 && keycode < 223);   // [\]' (in order)

		if (validPrintable) {
			mode = "type";
		}
	} else {
		//Tab
		if (event.which == 9 /*|| event.which == 13*/) {
			mode = "scan";
		}

	}

	var input = this;
	setTimeout(function () {
		updateScore(input);
	}, 1);


	if (found) {
		if ($(found)[0]) {
			var scrollPosition = [$(found).parents(".table-responsive").scrollLeft(), $(found).parents(".table-responsive").scrollTop()];

			//var visible = isElementInViewport(found[0]);
			var parent = $(found).parents(".table-responsive");
			var parentWidth = $(parent).width();
			var foundWidth = $(found).width();
			var foundPosition = $(found).position();
			var scale = parent.find("table").width() / parentWidth;

			$(found).focus();


			setTimeout(function () {
				$(found).select();
				//$(found).ScrollTo({ onlyIfOutside: true });
				/*if (goingRight) {
					console.log("right: " + (foundPosition.left + foundWidth ) + ", " + scrollPosition[0]);
					$(parent).scrollLeft(Math.max((foundPosition.left + foundWidth)*scale , scrollPosition[0]));
				}
				if (goingLeft) {
					console.log("left:  " + (foundPosition.left ) + ", " + scrollPosition[0]);
					$(parent).scrollLeft(Math.max((foundPosition.left) * scale, scrollPosition[0]));
				}*/

				updateScore(input);

			}, 1);
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
		hours: hours,
		minutes: minutes,
		seconds: secs,
		ms :ms
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
function setupMeeting() {
	$(".page-item .page-time").html("");
}
function setCurrentPage(pageName, startTime,baseMinutes) {
	currentPage = pageName;
	currentPageStartTime = startTime;
	currentPageBaseMinutes = baseMinutes;
	$(".page-item.current").removeClass("current");
	$(".page-item.page-" + pageName).addClass("current");
	if (followLeader && !isLeader) {
		loadPageForce(pageName);
	}

}

function loadPage(location) {
	if (!followLeader || isLeader || !meetingStart) {
		loadPageForce(location);
	}
	//window.location.hash = location;
}

function loadPageForce(location) {
	window.location.hash = location;
	location = location.toLowerCase();
	//if (location != myPage) {
	showLoader();
	myPage = location;
	$.ajax({
		url: "/L10/Load/" + MeetingId + "?page=" + location,
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


//Table
//http://stackoverflow.com/questions/7433377/keeping-the-row-title-and-column-title-of-a-table-visible-while-scrolling
function moveScroll(table, window) {
	return function () {
		var scroll_top = $(window).scrollTop();
		var scroll_left = $(window).scrollLeft();
		var anchor_top = $(table).position().top;
		var anchor_left = $(table).position().left;
		var anchor_bottom = $("#bottom_anchor").offset().top;

		$("#clone").find("thead").css({
			width: $(table).find("thead").width() + "px",
			position: 'absolute',
			left: -scroll_left + 'px'
		});

		$(table).find(".first").css({
			position: 'absolute',
			left: scroll_left + anchor_left + 'px'
		});

		if (scroll_top >= anchor_top && scroll_top <= anchor_bottom) {
			clone_table = $("#clone");
			if (clone_table.length == 0) {
				clone_table = $(table)
					.clone()
					.attr('id', 'clone')
					.css({
						width: $(table).width() + "px",
						position: 'fixed',
						pointerEvents: 'none',
						left: $(table).offset().left + 'px',
						top: 0
					})
					.appendTo($("#table_container"))
					.css({
						visibility: 'hidden'
					})
					.find("thead").css({
						visibility: 'visible'
					});
			}
		} else {
			$("#clone").remove();
		}
	};
}

function isElementInViewport(el) {

	//special bonus for those using jQuery
	if (typeof jQuery === "function" && el instanceof jQuery) {
		el = el[0];
	}

	var rect = el.getBoundingClientRect();

	return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) && /*or $(window).height() */
        rect.right <= (window.innerWidth || document.documentElement.clientWidth) /*or $(window).width() */
    );
}

