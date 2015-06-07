var mode = "scan";

$(function () {

	/*window.onbeforeunload = function(e) {
		return 'dialog';
		if (window.onbeforeunload) {
			debugger;
			console.log("unload exists");
			window.onbeforeunload();
		}
		if ($(":focus").length) {
			$(":focus").blur();
		}


	};*/

	$(".main-window-container").on("change", ".score input", function (e, d) {
		updateScore(this);
		if (!d) {
			updateServerScore(this);
		}
	});
	$('.main-window-container').on('keydown', ".grid", changeInput);
	$('.main-window-container').on('click', ".grid", function (e, d) { if (!d) mode = "scan"; });
	$('.main-window-container').on('change', ".grid", function (e, d) { if (!d) mode = "type"; });
	$('.main-window-container').on('scroll', ".grid", function (e) {
		if (mode == "type") {
			e.preventDefault();
		}
	});

	/*$("body").on("click", ".scorecard-table .target.direction", function(e, d) {
		$(this).html("<select><option>");
	});*/

});


function updateServerScore(self) {
	var m = $(self).data("measurable");
	var w = $(self).data("week");
	var id = $(self).data("scoreid");
	var val = $(self).val();
	var dom = $(self).attr("id");
	$.ajax({
		url: "/l10/UpdateScore/" + MeetingId + "?s=" + id + "&w=" + w + "&m=" + m + "&value=" + val + "&dom=" + dom,
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

function addMeasurable(data,smallTable) {
	//var row = $("<tr></tr>");
	//row.append("<td>")
	$("#ScorecardTable").append(data);
	$("#ScorecardTable_Over").append(smallTable);
	updateScore($("#ScorecardTable").find(".score input").last());
}

function updateArchiveMeasurable(id, name, text, value) {
	var sel = $("[data-measurable='" + id + "'][data-name='" + name + "']");

	sel.html(text);
	if (typeof value === 'undefined')
		value = text;
	sel.attr("data-value", value);
	highlight(sel);
}

function updateMeasurable(id, name, text, value) {
	var sel = $("[data-pk='" + id + "'][data-name='" + name + "']");

	sel.html(text);
	if (typeof value === 'undefined')
		value = text;
	sel.attr("data-value", value);
	highlight(sel);
}

//pass in a .score input
function updateScore(self,skipChart) {
	
	var goal = $(self).attr("data-goal");
	var dir = $(self).attr("data-goal-dir");
	var v = $(self).val();
	var id = $(self).attr("data-measurable");

	var r1 = "";
	var r2 = "";
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
	var arr = [];
	var row = $("tr[data-measurable=" + id + "]");
	var min = goal;
	var max = goal;
	row.find("td.score").each(function (i) {
		var v = parseFloat($(this).find("input").val());
		if (Number.isNaN(v))
			arr.push(null);
		else {
			min = Math.min(min, v);
			max = Math.max(max, v);

			if (dir == "GreaterThan") {
				if (+v >= +goal)
					arr.push(v);
				else
					arr.push(v);
			} else {
				if (+v < +goal)
					arr.push(v);
				else
					arr.push(v);
			}
		}
	});

	var range;
	var green = 'rgb(92 ,184,92)';
	var red = 'rgb(217 ,83, 79)';
	if (dir == "GreaterThan") {
		var d = {};
		d[(":" + goal)] = red;
		d[(goal + ":")] = green;
		range = $.range_map(d);
	} else {
		var d = {};
		d[(goal + ":")] = red;
		d[(":"+goal)] =green;
		range = $.range_map(d);
	}

	if (goal < 150) {
		min = Math.min(0,min);
	}

	var delta = (max - min);

	if (!skipChart) {
		row.find(".inlinesparkline").sparkline(arr, {
			type: 'bar',
			nullColor: 'rgb(230,230,230)',
			zeroAxis: true,
			colorMap: range,
			disableTooltips: true,
			chartRangeMin: min - delta * .1,
			chartRangeMax: max + delta * .1,
			barWidth: 3
		});
	}
}

//var functionLock = false;
//var functionCallbacks = [];
//var lockingFunction = function(callback) {
//	if (functionLock) {
//		functionCallbacks.push(callback);
//	} else {
//		functionCallbacks.push(callback);
//		while (functionCallbacks.length) {
//			var thisCallback = functionCallbacks.pop();
//			thisCallback();
//		}
//	}
//};
////function changeInput() {
//	var that = this;
//	lockingFunction(function() {
//		changeInput_lock.apply(that);
//	});
//}

var curColumn = -1;
var curRow = -1;

function changeInput() {
	var found;
	var goingLeft = false;
	var goingRight = false;
	if (mode == "scan" ||
		event.which == 38 ||	//pressing up
		event.which == 40 ||	//pressing down
		event.which == 13 ||	//pressing enter
		($(this)[0].selectionStart == 0 && (event.which == 37)) || //all the way left
		($(this)[0].selectionEnd == $(this).val().length && (event.which == 39)) // all the way right
		) {
		if (event.which == 37) { //left
			found = $(".grid[data-col=" + (+$(this).data("col") - 1) + "][data-row=" + $(this).data("row") + "]");
			//found = $(".grid[data-col=" + (curColumn - 1) + "][data-row=" + curRow + "]");
			goingLeft = true;
		} else if (event.which == 38) { //up
			found = $(".grid[data-row=" + (+$(this).data("row") - 1) + "][data-col=" + $(this).data("col") + "]");
			//found = $(".grid[data-row=" + (curRow - 1) + "][data-col=" + curColumn + "]");
		} else if (event.which == 39) { //right
			found = $(".grid[data-col=" + (+$(this).data("col") + 1) + "][data-row=" + $(this).data("row") + "]");
			//found = $(".grid[data-col=" + (curColumn + 1) + "][data-row=" + curRow + "]");
			goingRight = true;
		} else if (event.which == 40 || event.which == 13) { //down
			found = $(".grid[data-row=" + (+$(this).data("row") + 1) + "][data-col=" + $(this).data("col") + "]");
			//found = $(".grid[data-row=" + (curRow + 1) + "][data-col=" + curColumn + "]");
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
	var noop = [38, 40, 13, 37, 39];
	if (noop.indexOf(event.which) == -1) {
		setTimeout(function() {
			updateScore(input);
		}, 1);
	}


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
			curColumn = $(found).data("col");
			curRow = $(found).data("row");

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

