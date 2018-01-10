var mode = "scan";

var canMoveCells = true;


$(".main-window-container").on("submit", ".score form", function (e) {
    e.preventDefault();
});

$(function () {
    $(".main-window-container").on("keydown", ".grid", changeInput);
    $(".main-window-container").on("click", ".grid", function (e, d) {
        console.log("clicked!");
        if (!d) { mode = "scan"; }
        $(this).focus();
    });
    $(".main-window-container").on("change", ".grid", function (e, d) { if (!d) { mode = "type"; } });
    $(".main-window-container").on("scroll", ".grid", function (e) {
        console.log("scroll!");
        if (mode == "type") {
            e.preventDefault();
        }
    });
});

function showFormula(id) {
    showModal("Edit formula", "/scorecard/formulapartial/" + id, "/scorecard/setformula?id=" + id, null, null, function () { showAlert("Formula updated"); });
}

function blurChangeTimeout(key, self, d, i, executeNow) {
    if (typeof (executeNow) === "undefined") {
        executeNow = false;
    }
    if ($(self).attr("data-isformatted") == "true" && !executeNow) {
        console.error("!canMoveCells " + $(self).val());
        if (i < 10) {
            blurChange.timeout[key] = setTimeout(function () { blurChangeTimeout(key, self, d, i += 1); }, i * 5);
        }
    } else {
        var val = $(self).val();

        setScoreTransform(self, val);
        updateScore(self);
        if (!d) {
            if ($(self).attr("data-oldval") != val) {
                updateServerScore(self);
            }
        }
    }
}

function updateCumulative(measurableId, value) {
    if (value == null)
        value = "";
    $("[data-measurable='" + measurableId + "'] .cumulative-column span").text(value);
}

function blurChange(e, d) {
    var self = this;
    var key = $(self).attr("id");
    if (d == "external") {
        blurChangeTimeout(key, self, d, 1, true);

    } else {
        if (typeof (blurChange.timeout) === "undefined")
            blurChange.timeout = {};
        clearTimeout(blurChange.timeout[key]);
        blurChange.timeout[key] = setTimeout(function () {
            blurChangeTimeout(key, self, d, 1);
        }, 5);
    }
}

//THESE ONLY WORK ON NON-ANGULAR FORMS
$("body").on("blur change", ".scorecard-table .score:not(.ng-scope) input", blurChange);
$("body").on("focus", ".scorecard-table .score:not(.ng-scope) input", function (e) {
    var val = getScoreTransform(this);
    canMoveCells = false;
    $(this).val(val);
    $(this).attr("data-isformatted", "false");
    canMoveCells = true;
    e.preventDefault();
});

function transformNumber(value, units) {
    if (typeof (units) === "undefined")
        return value;
    units = units.toLowerCase();
    var addCommasToInteger = function (val) {
        var commas, decimals, wholeNumbers;
        decimals = val.indexOf(".") == -1 ? "" : val.replace(/^-?\d+(?=\.)/, "");
        wholeNumbers = val.replace(/(\.\d+)$/, "");
        commas = wholeNumbers.replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
        return "" + commas + decimals;
    };

    var regex = /^[+\-]?((\d+(\.\d*)?)|(\.\d+))$/;
    if (!regex.test(value)) {
        return value;
    }

    if (units == "dollars" || units == "$" || units == "dollar") {
        var post = "";
        if (value >= 1000000) {
            post = "M";
            value = "" + (Math.round(value / 10000.0) / 100);
        } else if (value >= 1000) {
            value = "" + (Math.round(value));
        }

        return "$" + addCommasToInteger(value) + post;
    }
    if (units == "pounds" || units == "£" || units == "pound") {
        var post = "";
        if (value >= 1000000) {
            post = "M";
            value = "" + (Math.round(value / 10000.0) / 100);
        } else if (value >= 1000) {
            value = "" + (Math.round(value));
        }

        return "£" + addCommasToInteger(value) + post;
    } if (units == "euros" || units == "€" || units == "euro") {
        var post = "";
        if (value >= 1000000) {
            post = "M";
            value = "" + (Math.round(value / 10000.0) / 100);
        } else if (value >= 1000) {
            value = "" + (Math.round(value));
        }

        return "€" + addCommasToInteger(value) + post;
    }

    if (units == "%" || units == "percent") {
        return addCommasToInteger(value) + "%";
    }

    var post = "";
    if (value >= 1000000) {
        post = "M";
        value = "" + (Math.round(value / 10000.0) / 100);
    } else if (value >= 1000) {
        value = "" + (Math.round(value));
    }

    return addCommasToInteger(value) + post;
}

function setScoreTransform(self, value) {
    $(self).attr("data-value", value);
    var unitType = $(self).closest("tr").find(".unit").html();
    var transf = transformNumber(value, unitType);
    $(self).val(transf);
    $(self).attr("data-isformatted", "true");

}

function getScoreTransform(self) {
    if (!$(self)[0].hasAttribute("data-isformatted")) {
        $(self).attr("data-isformatted", "false");
    }

    if (!$(self)[0].hasAttribute("data-value")) {
        $(self).attr("data-value", $(self).val());
    }

    if ($(self).attr("data-isformatted") == "true") {
        var val = $(self).attr("data-value");
        if (val.indexOf("$") != -1)
            debugger;

        return val;
    }
    var val = $(self).val();
    if (val.indexOf("$") != -1)
        debugger;
    return val;

}

var zoomLevel = 1;
function zoomIn() {
    zoomLevel *= 1.10;
    $(".zoomable").css("zoom", "" + (zoomLevel * 100) + "%");

}
function zoomOut() {
    zoomLevel /= 1.10;
    $(".zoomable").css("zoom", "" + (zoomLevel * 100) + "%");
}

function updateServerScore(self) {
    var m = $(self).data("measurable");
    var w = $(self).data("week");
    var id = $(self).data("scoreid");
    var val = getScoreTransform(self);
    var dom = $(self).attr("id");
    var oldVal = $(self).attr("data-oldval");
    $.ajax({
        url: "/l10/UpdateScore/" + window.recurrenceId + "?s=" + id + "&w=" + w + "&m=" + m + "&value=" + val + "&dom=" + dom + "&connection=" + $.connection.hub.id,
        success: function (data) {
            if (data.Error) {
                showJsonAlert(data);
                setScoreTransform(self, oldVal);
            } else {
                $(self).attr("data-oldval", val);
            }
        },
        error: function (data) {
            $(self).val(oldVal);
            updateScore(self);
        }
    });
}

function makeXEditable_Scorecard(selector) {
    var placement = $(this).attr("data-placement") || "left";
    var mode = "popup";
    $(selector).editable({
        mode: mode,
        savenochange: true,
        validate: function (value) {
            if ($(this).hasClass("numeric")) {
                var regex = /^[+\-]?((\d+(\.\d*)?)|(\.\d+))$/;
                if (!regex.test(value)) {
                    return 'This field must be a number';
                }
            }
            if ($.trim(value) == "") {
                return 'This field is required';
            }
        },
        onblur: "submit",
        showbuttons: false,
        placement: placement,
        display: function (value, sourceData) {
            if ($(this).hasClass("unit")) {

                $("[data-measurable=" + $(this).closest(".target.value").attr("data-measurable") + "].target.value .unit").each(function () {
                    var parent = $(this).closest(".target.value");
                    var tv = parent.find(".target-value");
                    tv.css("paddingLeft", 0);

                    if ((value != null && (value.toLowerCase() == "dollar" || value.toLowerCase() == "dollars" || value == "$")) ||
                        (value == null && ($(this).text().toLowerCase() == "dollar" || $(this).text().toLowerCase() == "dollars" || $(this).text().toLowerCase() == "$"))

                    ) {
                        //Only edit dollars
                        $(this).text("$");
                        $(parent.find(".unit")).insertBefore(parent.find(".numeric"));
                        // parent.attr("data-unit", "Dollar");
                    } else if ((value != null && (value.toLowerCase() == "pound" || value.toLowerCase() == "pounds" || value == "£")) ||
                        (value == null && ($(this).text().toLowerCase() == "pound" || $(this).text().toLowerCase() == "pounds" || $(this).text().toLowerCase() == "£"))) {

                        $(this).text("£");
                        // parent.attr("data-unit", "Pound");
                        $(parent.find(".unit")).insertBefore(parent.find(".numeric"));
                    } else if ((value != null && (value.toLowerCase() == "euro" || value.toLowerCase() == "euros" || value == "€")) ||
                        (value == null && ($(this).text().toLowerCase() == "euro" || $(this).text().toLowerCase() == "euros" || $(this).text().toLowerCase() == "€"))) {

                        $(this).text("€");
                        $(parent.find(".unit")).insertBefore(parent.find(".numeric"));
                        // parent.attr("data-unit", "Euros");
                    } else {
                        if ((value != null && (value.toLowerCase() == "percent" || value.toLowerCase() == "percentage" || value == "%")) ||
                            (value == null && ($(this).text().toLowerCase() == "percent" || $(this).text().toLowerCase() == "percentage" || $(this).text().toLowerCase() == "%"))
                        ) {
                            $(this).text("%");
                            // parent.attr("data-unit", "Percent");
                        }

                        if (value != null && (value.toLowerCase() == "none")) {
                            $(this).text("u");
                            //parent.attr("data-unit", "None");
                        }

                        tv.css("paddingLeft", 8);
                        $(parent.find(".numeric")).insertBefore(parent.find(".unit"));

                    }
                });
            }

            if ($(this).hasClass("target-value")) {
                $(this).text(transformNumber(value, ""));
            }
            if ($(this).hasClass("who") && $(this).hasClass("accountable")) {
                var aid = value || $(this).data("accountable");
                var name = "";
                var initials = "n/a";
                var url = null;
                if (aid in picturesLookup) {
                    url = picturesLookup[aid].url;
                    name = picturesLookup[aid].name;
                    initials = picturesLookup[aid].initials;

                    var mid = $(this).closest("tr").data("measurable");
                    if (typeof (picturesLookup) !== "undefined")
                        $("tr[data-measurable='" + mid + "'] .who.accountable").html(profilePicture(url, name, initials));
                }
            }
            if ($(this).hasClass("adminSelection"))
                $(this).text("/" + getInitials($(this).text()));
            return null;
        },
        success: function (data, newVal) {
            var items = $(".grid[data-measurable=" + $(this).data("measurable") + "]");
            if ($(this).data("name") == "direction") {
                $(items).filter(".future-score").attr("data-goal-dir", $(this).attr("data-value"));
            } else if ($(this).data("name") == "target") {
                $(items).filter(".future-score").attr("data-goal", $(this).attr("data-value"));
            }
            var isUnit = $(this).hasClass("unit");
            if (isUnit) {
                $("[data-measurable=" + $(this).data("measurable") + "] .unit").html((newVal || "").toLowerCase());
            }
            $(items).each(function (d) {
                updateScore(this);
                if (isUnit) {
                    setScoreTransform(this, getScoreTransform(this));
                }
            });
        }
    });
}

function addMeasurable(data, smallTable) {
    $("#ScorecardTable tbody").append(data);
    $("#ScorecardTable_Over tbody").append(smallTable);

    makeXEditable_Scorecard("#ScorecardTable .inlineEdit:not(.editable)");
    makeXEditable_Scorecard("#ScorecardTable_Over .inlineEdit:not(.editable)");

    updateScore($("#ScorecardTable").find(".score input").last());
    updateScorecardNumbers();

    $(".scorecard-holder").removeClass("hidden");
    $(".scorecard-empty-holder").addClass("hidden");
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

    $($("tr[data-meetingmeasurable='" + id + "'] .score.future input")).each(function (d) {
        if (name == "target")
            $(this).attr("data-goal", value);
        if (name == "direction")
            $(this).attr("data-goal-dir", value);
        updateScore(this, false);
    });
}

function myIsNaN(o) {
    return o !== o;
}
var usCounter = 0;
var chartCounter = 0;
//pass in a .score input
function updateScore(self, skipChart) {
    usCounter += 1;
    setTimeout(function () {
        var goal = $(self).attr("data-goal");
        var altgoal = $(self).attr("data-alt-goal");
        var dir = $(self).attr("data-goal-dir");
        var v = getScoreTransform(self);
        var id = $(self).attr("data-measurable");

        var r1 = "";
        var r2 = "";
        //Empty?

        var parentCell = $(self).closest("td");

        $(parentCell).removeClass("error");
        $(parentCell).removeClass("success");
        $(parentCell).removeClass("danger");
        if (!$.trim(v)) {
            //do nothing
        } else {
            var met = metGoal(dir, goal, v, altgoal);
            if (met == true)
                $(parentCell).addClass("success");
            else if (met == false)
                $(parentCell).addClass("danger");
            else
                $(parentCell).addClass("error");
        }

        if (!skipChart && includeChart) {
            var arr = [];
            var row = $("tr[data-measurable=" + id + "]");
            var min = goal;
            var max = goal;

            row.find("td.score").each(function (i) {
                var v = parseFloat(getScoreTransform($(this).find("input")));
                if (myIsNaN(v))
                    arr.push(null);
                else {
                    min = Math.min(min, v);
                    max = Math.max(max, v);
                    arr.push(v);
                }
            });
            var range;
            var green = 'rgb(92 ,184,92)';
            var red = 'rgb(217 ,83, 79)';
            if (metGoal(dir, 0, 1)) {
                var d = {};
                d[(":" + goal)] = red;
                d[(goal + ":")] = green;
                range = $.range_map(d);
            } else {
                var d = {};
                d[(goal + ":")] = red;
                d[(":" + goal)] = green;
                range = $.range_map(d);
            }

            if (goal < 150) {
                min = Math.min(0, min);
            }

            var delta = (max - min);

            chartCounter += 1;
            row.find(".inlinesparkline").sparkline(arr, {
                type: 'bar',
                nullColor: 'rgb(230,230,230)',
                zeroAxis: true,
                colorMap: range,
                disableTooltips: true,
                chartRangeMin: min - delta * 0.1,
                chartRangeMax: max + delta * 0.1,
                barWidth: 3
            });

        }
    }, 1);
}

var curColumn = -1;
var curRow = -1;

function changeInput(event) {
    var found;
    var goingLeft = false;
    var goingRight = false;
    console.log("changeInput");
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
			var curRow = (+$(this).data("row") - 1);
			while (true) {
				var $row = $("tr[data-row=" + curRow + "]");
			    
			    if ($row.length > 0 && !$row.hasClass("divider") && !$row.is("[data-editable='False']")) {
					found = $(".grid[data-row=" + (curRow) + "][data-col=" + $(this).data("col") + "]");
					break;
				}
				if ($row.length == 0) {
					break;
				}
				curRow -= 1;
			}
		} else if (event.which == 39) { //right
			found = $(".grid[data-col=" + (+$(this).data("col") + 1) + "][data-row=" + $(this).data("row") + "]");
			goingRight = true;
		} else if (event.which == 40 || event.which == 13) { //down
			var curRow = (+$(this).data("row") + 1);
			while (true) {
				var $row = $("tr[data-row=" + curRow + "]");
				if ($row.length > 0 && !$row.hasClass("divider") && !$row.is("[data-editable='False']")) {
					found = $(".grid[data-row=" + (curRow) + "][data-col=" + $(this).data("col") + "]");
					break;
				}
				if ($row.length == 0) {
					break;
				}
				curRow += 1;
			}
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
        setTimeout(function () {
            updateScore(input);
        }, 1);
    }


    if (found) {
        if ($(found)[0]) {
            clearTimeout(changeInput.timeout);
            changeInput.timeout = setTimeout(function () {
                changeCells(found, input);
            }, 1);
        }
    }
}

function changeCells(found, input) {
    var scrollPosition = [$(found).parents(".table-responsive").scrollLeft(), $(found).parents(".table-responsive").scrollTop()];

    var parent = $(found).parents(".table-responsive");
    var parentWidth = $(parent).width();
    var foundWidth = $(found).width();
    var foundPosition = $(found).position();
    var scale = parent.find("table").width() / parentWidth;

    $(found).focus();
    curColumn = $(found).data("col");
    curRow = $(found).data("row");
    clearTimeout(changeCells.timeout);
    changeCells.timeout = setTimeout(function () {
        $(found).select();
        updateScore(input);

    }, 1);
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

function reorderMeasurables(order) {
    for (var i = 0; i < order.length; i++) {
        var found = $("tr[data-meetingmeasurable='" + order[i] + "']");
        $(found).attr("data-order", i);
        $(found).find(".grid").data("row", i);
        $(found).closest("tr").data("row", i);
        $(found).find(".grid").attr("data-row", i);
        $(found).closest("tr").attr("data-row", i);
    }
    $(".scorecard-table").each(function () {
        $(this).find("tbody").children("tr").detach().sort(function (a, b) {
            if ($(a).attr("data-order") == $(b).attr("data-order"))
                return $(b).attr("data-measurable") - $(a).attr("data-measurable");

            return $(a).attr("data-order") - $(b).attr("data-order");
        }).appendTo($(this));
    });
    updateScorecardNumbers();
}

function updateScoresGoals(startWeek, measurableId, s) {
    debugger;
    console.error("updateScoresGoals");
    $("[data-measurable=" + measurableId + "].score-value").each(function () {
        var found = $(this);
        if (+found.attr("data-week") > startWeek) {
            found.attr("data-goal-dir", s.GoalDir);
            found.attr("data-alt-goal", s.AltGoal);
            found.attr("data-goal", s.Goal);
            updateScore(found, true);
        }
    });

    //for (var i = 0; i < scores.length; i++) {
    //	var s = scores[i];
    //	var found = $("[data-scoreid=" + s.Id + "]");
    //	if (found.length > 0) {
    //		debugger;
    //	}
    //}

}


function receiveUpdateScore(newScore) {
	//console.info(newScore);
    if (Array.isArray(newScore)) {
        newScore.map(function (x) {
            receiveUpdateScore(x);
        });
        return;
    }
	$("[data-scoreid='" + newScore.Id + "']").val(newScore.Measured);
}

function reorderRecurrenceMeasurables(order) {
    console.log("reorderRecurrenceMeasurables");
    for (var i = 0; i < order.length; i++) {
        var found = $("tr[data-measurable='" + order[i] + "']");
        $(found).attr("data-order", i);
        $(found).find(".grid").data("row", i);
        $(found).closest("tr").data("row", i);
        $(found).find(".grid").attr("data-row", i);
        $(found).closest("tr").attr("data-row", i);
    }
    $(".scorecard-table").each(function () {
        $(this).find("tbody").children("tr").detach().sort(function (a, b) {
            if ($(a).attr("data-order") == $(b).attr("data-order"))
                return $(b).attr("data-measurable") - $(a).attr("data-measurable");

            return $(a).attr("data-order") - $(b).attr("data-order");
        }).appendTo($(this));
    });
    updateScorecardNumbers();
}

function updateScorecardNumbers() {
    $(".scorecard-table").each(function () {
        $(this).find("tbody").find("tr:not(.divider)").each(function (i) {
            $(this).find(".number").html(i + 1);
        });
    });
}

function addDivider(id) {
    $.ajax({
        url: "/L10/AddMeasurableDivider?recurrence=" + id,
    });
}

function deleteDivider(id) {
    console.log("deleteDivider" + id);
    $.ajax({
        url: "/L10/RemoveMeasurableDivider/" + id,
    });
}

function removeDivider(id) {
    console.log("remove divider " + id);
    $("tr[data-meetingmeasurable='" + id + "']").remove();
}

function removeMeasurable(id) {
    console.log("remove measurable " + id);
    $("tr[data-measurable='" + id + "']").remove();
}

function onClickScChart(self) {
    var state = !$(self).is(".selected");
    $(self).toggleClass("selected", state);
    var mid = $(self).closest("[data-measurable]").attr("data-measurable");
    if (state) {
        addScChart(mid);
    } else {
        removeScChart(mid);
    }
}

function removeScChart(id) {
    try {
        delete addScChart.chart[id];
        reapplyScChart();
    } catch (e) {
    }
}

function addScChart(id) {
    var datas = [];
    var row = $("tr[data-measurable='" + id + "']");
    row.find(".score input")
    .map(function (x) {
        var v = $(this).attr("data-value");
        //v = v === "" ? null : +v;
        if (v !== "") {
            datas.push({
                value: +v,
                date: new Date(+$(this).attr("data-week"))
            });
        } /*else {
            if (datas.length > 0) {
                var last = datas[datas.length - 1]
                datas.push({ value: last.value, date: last.date + 1 });
            }
        }*/
    });
    var name = row.find(".measurable").first().text();
    if (typeof (addScChart.chart) === "undefined")
        addScChart.chart = {};
    addScChart.chart[id] = { data: datas, legend: name };
    reapplyScChart();
}

function reapplyScChart() {
    var datas = [];
    var legends = [];
    Object.keys(addScChart.chart).map(function (x) {
        datas.push(addScChart.chart[x].data);
        legends.push(addScChart.chart[x].legend);
    });

    var update = {
        data: datas,
        legend: legends,
        area: false,
        right: 100,
        top: 30,
        linked: true,
        aggregate_rollover:true
        // chart_type: 'point',
        //missing_is_hidden: true,
        //time_frame: 'many-days'
    };

    if (typeof (addScChart.mainChart) === "undefined") {
        addScChart.scUpdate = $("body").on("change.reapplyScChart", ".score input", function () {
            var mid = $(this).closest("[data-measurable]").attr("data-measurable");
            setTimeout(function () {
                addScChart(mid);
            }, 200);
        });
        addScChart.mainChart = $(".sc-chart-container").appendGraph(update);
    } else {
        addScChart.mainChart.update(update);
    }

    if (datas.length == 0) {
        addScChart.mainChart.remove();
        addScChart.mainChart = undefined;
        $("body").off(".reapplyScChart");
    }

}

$(window).on("page-scorecard", function () {
    scrollRight();

    if (isIOS()) {
        $('input').css("pointer-events", "none");
    }
});


//function showScorecardOptions(me) {
//	var self = $(me).closest("tr").data();
//	var icon = { title: "Update options" };
//	var fields = [{
//		type: "label",
//		value: "Update historical goals?"
//	}, {
//		name: "history",
//		value: "false",
//		type: "yesno"
//	}, {
//		type: "label",
//		value: "Show Cumulative?"
//	}, {
//		name: "showCumulative",
//		value: self.showcumulative || false,
//		type: "yesno",
//		onchange: function () {
//			$("#cumulativeRange").toggleClass("hidden", $(this).val() != "true");
//		}
//	}, {
//		classes: self.showcumulative == true ? "" : "hidden",
//		name: "cumulativeRange",
//		value: self.cumulativerange || new Date(),
//		type: "date"
//	}/*, {
//			type: "label",
//			value: "Show Cumulative?"
//		}, {
//			name: "showCumulative",
//			value: self.ShowCumulative || false,
//			type: "yesno",
//			onchange: function () {
//				$("#cumulativeRange").toggleClass("hidden", $(this).val() != "true");
//			}
//		}*/]

//	var direction = $(me).closest("tr").find("[data-name='direction']").attr("data-value");
//	//var target = $(me).closest("tr").find("[data-name='direction']").attr("data-value");

//	if (direction == "Between" || direction == -3) {
//		icon = "info";
//		//fields.unshift({
//		//	type: "label",
//		//	value: "Update historical goals?"
//		//});
//		fields.push({
//			type: "number",
//			text: "Lower-Boundary",
//			name: "Lower",
//			value: self.target,
//		});
//		fields.push({
//			type: "number",
//			text: "Upper-Boundary",
//			name: "Upper",
//			value: self.alttarget || self.target,
//		});
//	}

//	showModal({
//		icon: icon,
//		noCancel: true,
//		fields: fields,
//		success: function (model) {
//			var low = Math.min(+model.Lower, +model.Upper);
//			var high = Math.max(+model.Lower, +model.Upper);
//			if (isNaN(low))
//				low = null;
//			if (isNaN(high))
//				high = null;

//			$.ajax({
//				url: "/L10/UpdateAngularMeasurable?historical=" + model.history + "&Lower=" + low + "&Upper=" + high + "connectionId=null&cumulativeStart=" + model.cumulativeRange + "&enableCumulative=" + model.showCumulative,
//				method: "post",
//				data: {id:self.measurable}
//			});


//		},
//		cancel: function () {

//		}
//	});
//}
