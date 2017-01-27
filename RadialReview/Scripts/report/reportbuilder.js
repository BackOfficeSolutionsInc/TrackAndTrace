$(function () {
	collapseAll();
	InitUpdateRock();
});
var last = null;

function InitUpdateRock() {
	$(document).on("change", ".approvereject input", function () {
		var id = $(this).attr("name").split("_")[1];
		var that = $(this);
		$(this).addClass("disabled");
		$.ajax({
			url: "/Review/EditRockCompletion/" + id + "?val=" + $(this).val(),
			complete: function (data) {
				$(that).removeClass("disabled");
			}
		});
	});


	$("textarea[name^='arr_']").keyup($.debounce(500, updateRockComment));
}

function updateRockComment() {

	$(this).val($(this).val().trim());
	var notes = $(this).val();
	var dat = new Object();
	var id = $(this).attr("name").split("_")[1];
	dat.id = id;// "@Model.Review.Id";
	dat.val = notes;
	var that = $(this);
	if (notes != $(that).data("last")) {
		$(that).data("last", notes);
		//$(that).addClass("saving");
		$.ajax({
			url: "/Review/SetRockCompletionComment",
			data: dat,
			method: "POST",
			complete: function () {
				/*setTimeout(function () {
					$(that).removeClass("saving");
				}, 1000);*/
			}
		});
	}
}

function removeEmpty(array, deleteValue) {
	for (var i = 0; i < array.length; i++) {
		if (array[i] == deleteValue) {
			array.splice(i, 1);
			i--;
		}
	}
	return array;
};

function collapseAll() {
	$('.panel-collapse').collapse('hide');
}

function expandAll() {
	$('.panel-collapse').collapse('show');
}

function Group() {
}

function saveText() {
	var notes = $("#ManagerNotes").val();
	var dat = new Object();
	dat.id = ReviewId;// "@Model.Review.Id";
	dat.notes = notes;
	if (notes != last) {
		last = notes;
		$(".save").addClass("saving");
		$.ajax({
			url: "/Review/SetNotes",
			data: dat,
			method: "POST",
			complete: function () {
				setTimeout(function () {
					$(".save").removeClass("saving");
				}, 1000);
			}
		});
	}
}

function Advanced() {
	$(".advanced").toggleClass("hidden");
}

function clickDetails(self) {
	//if (allowed() || confirm("You need to include data in the review. Add some feedback and charts before viewing. You may turn off this indicator by turning off hints. Continue anyway?")) {
	w = window.open("/Review/Plot/" + ReviewId + "?r=" + (+new Date()));
	//}
	setTimeout(function () {
		$(self).parent().removeClass("active");
	}, 10);
}

function clickPrint(self) {
	//w = window.open("/Review/ClientDetails/" + ReviewId + "?reviewing=true&printing=true");
	w = window.open("/Review/Plot/" + ReviewId + "?r=" + (+new Date()));
	w.focus();
	w.print();
	setTimeout(function () {
		$(self).parent().removeClass("active");
	}, 10);
	//w.loading
	//setTimeout(function () {
	//	w.close();
	//},4000)
}

function clickAuthorize() {
	setAuthorize();
}

function UpdateChart() {
	var groups = [];
	var filters = [];
	var extraClasses = [];
	var legendFunc = legend;

	$(".group:checked").each(function (x) {
		var split = $(this).data("class").split(" ");
		if (split[0] == "")
			legendFunc = legendReview;

		extraClasses.push($(this).data("addclass"));
		for (var i in split) {
			groups.push(split[i]);
		}
	});

	IncludePrevious = $(".showPrevious").is(":checked");
	var dat = {
		reviewId: ReviewId,
		aggregateBy: groups.join(" "),
		includePrevious: IncludePrevious,

	};

	$.ajax({
		url: "/Review/UpdateScatterChart",
		data: dat,
		method: "GET",
		success: function (data) {
			if (data.Object && data.Error === false) {
				console.log("UPDATED CHART");
			} else {
				clearAlerts();
				showAlert("An error occurred.");
				console.log(data.Object);
				location.href = "#top";
			}
		},
		complete: function () {
			//UpdateFeedbacks();
		},
		error: function (jqXHR, textStatus, errorThrown) {
			clearAlerts();
			showAlert("An error occurred.");
			console.log("Error:" + textStatus);
			console.log(errorThrown);
			location.href = "#top";
		}
	});

}

function UpdateInclude(url, on, inputClass) {
	var inputs = $("." + inputClass);
	inputs.prop("disabled", true);
	var dat = { reviewId: ReviewId/* "@Model.Review.Id"*/, on: on };
	$.ajax({
		url: url,
		data: dat,
		method: "GET",
		success: function (data) {
			if (data.Object && on == data.Object.On) {
				inputs.prop("checked", data.Object.On);
				$(".panel-class-" + inputClass).toggleClass("isIncluded", on);
			} else {
				clearAlerts();
				showAlert("An error occurred.");
				console.log(data.Object);
				location.href = "#top";
			}
		},
		complete: function () {
			inputs.prop("disabled", false);
		},
		error: function (jqXHR, textStatus, errorThrown) {
			clearAlerts();
			showAlert("An error occurred.");
			console.log("Error:" + textStatus);
			console.log(errorThrown);
			location.href = "#top";
		}
	});
}

function SetQuestionTable() {
	var on = !$(".QuestionTable").hasClass("on");
	var dat = { reviewId: ReviewId/* "@Model.Review.Id"*/, on: on };
	$.ajax({
		url: "/Review/SetIncludeTable",
		data: dat,
		method: "GET",
		success: function (data) {
			if (data.Object) {
				if (data.Object.On) {
					$(".QuestionTable").addClass("on");
				} else {
					$(".QuestionTable").removeClass("on");
				}
			}
		}
	});
}

function OnclickFeedback(self, id) {
	SetFeedback(id, $(self).is(':checked'));
}

function SetFeedback(id, on) {
	var dat = new Object();
	dat.feedbackId = id;
	dat.reviewId = ReviewId/* "@Model.Review.Id"*/;

	dat.on = on;

	$(".feedback_input_" + id /*+ " .check"*/).prop("disabled", true);

	$.ajax({
		url: "/Review/SetFeedback",
		data: dat,
		method: "GET",
		success: function (data) {
			if (data.Object) {
				if (on != data.Object.On) {
					showAlert("An error occurred.");
					location.href = "#top";
				}
				$(".feedback_input_" + data.Object.FeedbackId /*+ " .check"*/).prop("checked", data.Object.On);
			}
		},
		complete: function () {
			// UpdateFeedbacks();
			$(".feedback_input_" + id /*+ " .check"*/).prop("disabled", false);
		}
	})
}


function allowed() {
	return ($("#charts tr").size() != 0 || $("#feedbacks li").size() != 0 || $("#includes li .includable.on").size() != 0);
}

function setAuthorize(auth) {
	var dat = new Object();
	dat.Authorized = auth;
	dat.ReviewId = ReviewId/* "@Model.Review.Id"*/;

	$.ajax({
		url: "/Review/Authorize",
		data: dat,
		method: "GET",
		success: function (data) {
			showJsonAlert(data, false);
			if (data.Object.Authorized) {
				$("#Authorize").prop('checked', true);
				$(".authorized").addClass("on");
				$('.switch').bootstrapSwitch('state', true, true);

			} else {
				$("#Authorize").prop('checked', false);
				$(".authorized").removeClass("on");
				$('.switch').bootstrapSwitch('state', false, true);
			}
		}
	});
}


function IncludeChart() {
	var xId = $("#xAxis").val().substring(9);
	var yId = $("#yAxis").val().substring(9);

	var groups = $("#groupSet input:checked").map(function () { return $(this).val(); });
	var filters = $("#filterSet input:checked").map(function () { return $(this).data("class"); });
	var groupStr, filterStr;

	if (groups.length == 0)
		groupStr = "";
	else
		groupStr = groups.get().join();
	if (filters.length == 0)
		filterStr = "";
	else
		filterStr = filters.get().join();


	var dat = new Object();
	dat.X = xId;
	dat.Y = yId;
	dat.groups = groupStr;
	dat.filters = filterStr;
	dat.ReviewId = ReviewId;//"@Model.Review.Id";
	dat.Start = $("#DateSlider").val()[0];
	dat.End = $("#DateSlider").val()[1];
	// var joined = removeEmpty([groupStr, filterStr],"").join(";");

	$.ajax({
		url: "/Review/AddChart",
		data: dat,
		method: "GET",
		success: function (data) {

			var joined = data.Object.Title;

			/*if (joined.trim() != "")
                joined = "(" + joined + ")";*/
			var toAppend = '<tr class="chart_' + data.Object.ChartId + '"><td>' + data.Object.Title + '</td><td><span onclick="RemoveChart(' + data.Object.ChartId + ')" title="Remove" class="pull-right glyphicon glyphicon-remove red strokeRed clickable"></span></td></tr>';
			$("#charts").append(toAppend);
		}
	});
}

function RemoveChart(chartId) {
	var dat = new Object();
	dat.chartId = chartId;
	dat.reviewId = ReviewId;//"@Model.Review.Id";
	$.ajax({
		url: "/Review/RemoveChart",
		data: dat,
		method: "GET",
		success: function (data) {
			$(".chart_" + data.Object.ChartId).remove();
		}
	});
}


function legend(legendData, chart) {

	if (legendData.length > 0) {
		$("#legend").html("<div class='container'><div class='title'>Legend:</div><div class='contents'></div></div>");
		var self = "<svg viewBox=\"-6 -6 26 26\" width=\"12\" height=\"12\"><polygon transform=\"translate(7,7) rotate(45)\" class=\"about-Self scatter-point nearest inclusive\" points=\"" + chart.cross + "\"></svg>";
		$("#legend .contents").append("<div>" + self + " <div class='inlineBlock'>Self</div></div>");

		var manager = "<svg viewBox=\"0 0 20 20\" width=\"12\" height=\"12\"><rect x=\"-7\" y=\"-7\" width=\"14\" height=\"14\" transform=\"translate(9,9)\"  class=\"about-Manager scatter-point nearest inclusive\"/></svg>";
		$("#legend .contents").append("<div>" + manager + " <div class='inlineBlock'>Supervisor</div></div>");

		var subordinate = "<svg viewBox=\"0 0 20 20\" width=\"12\" height=\"12\"><rect x=\"-5\" y=\"-5\" width=\"10\" height=\"10\" transform=\"translate(9,9)rotate(45)\"  class=\"about-Subordinate scatter-point nearest inclusive\"/></svg>";
		$("#legend .contents").append("<div>" + subordinate + " <div class='inlineBlock'>Direct Report</div></div>");

		var peer = "<svg viewBox=\"0 0 20 20\" width=\"12\" height=\"12\"><polygon transform=\"translate(10,10)\" class=\"about-Peer scatter-point nearest inclusive\" points=\"" + chart.triangle + "\"></svg>";
		$("#legend .contents").append("<div>" + peer + " <div class='inlineBlock'>Peer</div></div>");

		var noRel = "<svg viewBox=\"0 0 20 20\" width=\"12\" height=\"12\"><circle r=\"8\" transform=\"translate(10,10)\" class=\"scatter-point nearest inclusive\"/></svg>";
		$("#legend .contents").append("<div>" + noRel + " <div class='inlineBlock'>No Relationship</div></div>");

		/*var peer = "<svg><rect class=\"about-Manager\" x=\"" + chart.triangle + "\"></svg>";

        for (var i in legendData) {
            var item = legendData[i];
            $("#legend .contents").append("<div><div class='" + item.Class + " circle inlineBlock'></div><div class='inlineBlock'>" + item.Name + "</div></div>");
        }*/
	}
}
function legendReview(legendData, chart) {

	if (legendData.length > 0) {
		$("#legend").html("<div class='container'><div class='title'>Legend:</div><div class='contents'></div></div>");
		$("#legend .contents").append("<div><div class='about-NoRelationship circle inlineBlock'></div><div class='inlineBlock'>Review Average</div></div>");
	}
}

var slider;
$(function () {

	updateChart();
	$('.switch').bootstrapSwitch();
	$('#Authorize').on('switchChange', function (e, data) {
		setAuthorize(data.value);
	});
	$('#ManagerNotes').keyup($.debounce(500, saveText));
	slider = $("#DateSlider").noUiSlider({
		range: [0, 1],
		start: [0, 0],
		handles: 2,
		step: 10,
		margin: 20,
		connect: true,
		direction: 'ltr',
		orientation: 'horizontal',
		behaviour: 'tap-drag',
		serialization: {
			resolution: 1,
			to: [function (v) {
				$("#StartDate").html(moment(+v).format("MMMM Do YYYY"));
			}, function (v) {
				$("#EndDate").html(moment(+v).format("MMMM Do YYYY"));
			}]
		}
	});
});

var chart = new ScatterImage("chart2");

IncludePrevious = $(".showPrevious").is(":checked");
var dataUrl = "/Data/ReviewScatter2/" + ForUserId + "?reviewsId=" + ForReviewsId;//"@Model.Review.ForReviewsId";

var first = true;


function updateChart() {

	var groupBy = $("input.group:checked").val();
	if (groupBy === "" || groupBy === undefined)
		groupBy = "review-*";

	var opts = {};
	opts.legendId = "legend";
	opts.legendFunc = null;
	if (groupBy === "user-*") {
		opts.drawPoints = chart.imagePoints;
		opts.useForce = true;
	} else if (groupBy === "about-*" || groupBy === "review-*") {
		opts.drawPoints = chart.shapePoints;
		opts.useForce = true;
	}
	if (groupBy === "about-*") {
		opts.legendFunc = chart.shapeLegend;
	}

	if (groupBy === "review-*") {
		opts.legendFunc = function (lid, pts) {
			var pts = [{ title: "Aggregate Score", class: "scatterPoint" }];

			return chart.shapeLegend(lid, pts);
		};
	}

	opts.rest = function (data) {
		setTimeout(function () { $(".chartPlaceholder").addClass("hidden"); }, 1);
		$("#chart2,#legend").animate({ opacity: 1 });
	};

	opts.quadrants = {
		"1": { title: "Right Person, Right Seat" },
		"2": { title: "Wrong Person, Right Seat" },
		"3": { title: "Wrong Person, Wrong Seat" },
		"4": { title: "Right Person, Wrong Seat" }
	};

	chart.PullPlot(dataUrl + "&groupBy=" + groupBy + "&includePrevious=" + IncludePrevious, null, null, opts);
}

$(".update").change(function () {
	$(".chartPlaceholder").removeClass("hidden");
	$("#chart2,#legend").animate({ opacity: 0 }, function () {
		UpdateChart();
		updateChart();
	});
});
