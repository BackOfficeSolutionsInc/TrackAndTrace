
var last = null;

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

function clickDetails() {
    //if (allowed() || confirm("You need to include data in the review. Add some feedback and charts before viewing. You may turn off this indicator by turning off hints. Continue anyway?")) {
    window.location = "/Review/ClientDetails/" + ReviewId + "?reviewing=true";
    //}

}

function clickAuthorize() {
    //if (allowed() || $("#Authorized").hasClass("on") || confirm("You need to include data in the review. You should add some feedback and charts before authorizing. You may turn off this indicator by turning off hints. Continue anyway?")) {
    setAuthorize();
    //} else {
    //    ;
    //}
}

function SetManagerAnswers() {
    var on = !$(".ManagerAnswers").hasClass("on");
    var dat = { reviewId:ReviewId/* "@Model.Review.Id"*/, on: on };
    $.ajax({
        url: "/Review/SetIncludeManagerAnswers",
        data: dat,
        method: "GET",
        success: function (data) {
            if (data.Object) {
                if (data.Object.On) {
                    $(".ManagerAnswers").addClass("on");
                } else {
                    $(".ManagerAnswers").removeClass("on");
                }
            }
        }
    });
}

function UpdateInclude(url, on, inputs) {
    inputs.prop("disabled", true);
    var dat = { reviewId: ReviewId/* "@Model.Review.Id"*/, on: on };
    $.ajax({
        url: url,
        data: dat,
        method: "GET",
        success: function (data) {
            if (data.Object && on == data.Object.On) {
                inputs.prop("checked", data.Object.On);
            } else {
                clearAlerts();
                showAlert("An error occurred.");
                console.log(data.Object);
                location.href = "#top";
            }
        },
        complete: function () {
            UpdateFeedbacks();
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

function OnclickSelfAnswers(self) {
    debugger;
    console.error("wat");
}

function OnclickScatter(self) {
    UpdateInclude("/Review/SetIncludeScatter", $(self).is(':checked'), $(".includeScatter"));
}

function SetSelfAnswers() {
    var on = !$(".SelfAnswers").hasClass("on");
    var dat = { reviewId: ReviewId/* "@Model.Review.Id"*/, on: on };
    $.ajax({
        url: "/Review/SetIncludeSelfAnswers",
        data: dat,
        method: "GET",
        success: function (data) {
            if (data.Object) {
                if (data.Object.On) {
                    $(".SelfAnswers").addClass("on");
                } else {
                    $(".SelfAnswers").removeClass("on");
                }
            }
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
    /*if (on === undefined) {
        on = !$(".feedback_" + id + " .check").hasClass("on");
    }*/
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
                //if (data.Object.On) {
                //    $(".feedback_" + data.Object.FeedbackId + " .check").addClass("on");
                //    var text = $(".feedback_" + data.Object.FeedbackId + " .text").html();
                //    var toAppend = '<li class="feedback feedback_' + data.Object.FeedbackId + '">' + text + '<span onclick="SetFeedback(' + data.Object.FeedbackId + ',false)" title="Remove" class="pull-right glyphicon glyphicon-remove red strokeRed clickable"></span></li>';
                //    $("#feedbacks").append(toAppend);
                //} else {
                //    $(".feedback_" + data.Object.FeedbackId + " .check").removeClass("on");
                //    $("#feedbacks .feedback_" + data.Object.FeedbackId).remove();
                //}
            }
        },
        complete: function () {
            UpdateFeedbacks();
            $(".feedback_input_" + id /*+ " .check"*/).prop("disabled", false);
        }
    })
}

function UpdateFeedbacks() {
    $(".feedback .check").each(function (i, e) {
        if ($(e).hasClass("on"))
            $(e).html('<span title="Included" class="glyphicon glyphicon glyphicon-ok   green strokeGreen"></span>');
        else
            $(e).html('<span title="Excluded" class="glyphicon glyphicon-ban-circle     red strokeRed"></span>');
    });
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
            } else {
                $("#Authorize").prop('checked', false);
                $(".authorized").removeClass("on");
            }
        }
    });
}

$(function () {
    UpdateFeedbacks();
});

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
    })
}

var slider;
$(function () {
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

function legend(legendData, chart) {

    if (legendData.length > 0) {
        $("#legend").html("<div class='container'><div class='title'>Legend:</div><div class='contents'></div></div>");

        for (var i in legendData) {
            var item = legendData[i];
            $("#legend .contents").append("<div><div class='" + item.Class + " circle inlineBlock'></div><div class='inlineBlock'>" + item.Name + "</div></div>");
        }
    }
}
function legendReview(legendData, chart) {

    if (legendData.length > 0) {
        $("#legend").html("<div class='container'><div class='title'>Legend:</div><div class='contents'></div></div>");
        $("#legend .contents").append("<div><div class='about-NoRelationship circle inlineBlock'></div><div class='inlineBlock'>Review Average</div></div>");
    }
}

var chart = new ScatterChart("chart");

function update(reset) {
    var date1 = new Date(+$("#DateSlider").val()[0]);
    var date2 = new Date(+$("#DateSlider").val()[1]);
    var date3 = new Date();

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

    $(".filters:checked").each(function (x) {
        var split = $(this).data("class");//.split(" ");
        extraClasses.push($(this).data("addclass"));
        filters.push(split);/*
                    for (var i in split) {
                        filters.push(split[i]);
                    }*/
    });
    //var groups = [$(".group:checked").map(function () { return  })];



    chart.Plot(AllScatterData, {
        //mouseout: mouseout,
        //mouseover: mouseover,
        animate: true,
        reset: reset,
        xAxis: $("#xAxis option:selected").text(),
        yAxis: $("#yAxis option:selected").text(),
        xDimensionId: $("#xAxis").val(),
        yDimensionId: $("#yAxis").val(),
        startTime: Math.min(date1, date2),//new Date(parseInt($("#slider").val())),
        endTime: Math.max(date1, date2),//new Date(parseInt($("#date").val()))
        time: date3,//new Date(parseInt($("#date").val()))
        groups: [groups],
        filters: filters,
        legendFunc: legendFunc,
        extraClasses : extraClasses,
    });
}

var dataUrl = "/Data/ReviewScatter/" + ForUserId + "?reviewsId=" + ForReviewsId;//"@Model.Review.ForReviewsId";
chart.Pull(dataUrl, null, function (dat) {
    AllScatterData = dat;
    for (var key in dat.Dimensions) {
        var item = dat.Dimensions[key];
        $("#xAxis").append("<option value=\"" + item.Id + "\">" + item.Name + "</option>");
        $("#yAxis").append("<option value=\"" + item.Id + "\">" + item.Name + "</option>");
    }

    $("#xAxis").val(dat.InitialXDimension);
    $("#yAxis").val(dat.InitialYDimension);

    for (var i = 0; i < dat.Filters.length; i++) {
        var filter = dat.Filters[i];
        var checked = "";
        if (filter.On)
            checked = "checked";
        $("#filterSet").append("<li><input class='filters update' type='checkbox' " + checked + " data-class='" + filter.Class + "'/><label>" + filter.Name + "</label></li>");
    }

    if (dat.Filters.length > 0) {
        $("#filtersContainer").removeClass("hidden");
    }



    $(".update").change(false, function (d) {
        update(d.data);
    });

    //$(".date").attr("min",);
    //$(".date").attr("max", );

    /*$("#date1").change(function () { update(false); });
    $("#date2").change(function () { update(false); });
    $("#date3").change(function () { update(false); });*/
    $("#xAxis").change(function () { update(false); });
    $("#yAxis").change(function () { update(false); });
    slider.noUiSlider({
        range: [this.GetDate(AllScatterData.MinDate).getTime() - 86400000, this.GetDate(AllScatterData.MaxDate).getTime() + 86400000],
        start: [this.GetDate(AllScatterData.MinDate).getTime() - 86400000, this.GetDate(AllScatterData.MaxDate).getTime() + 86400000],
        slide: function () { update(false); }
    }, true);
    slider.change(function () { update(false); });

    update(true);
});

document.getElementById("controls").addEventListener("click", chart.update, false);
document.getElementById("controls").addEventListener("keyup", chart.update, false);
document.getElementById("xAxis").addEventListener("change", chart.update, false);
document.getElementById("yAxis").addEventListener("change", chart.update, false);
//document.getElementById("grouped").addEventListener("change", chart.update, false);
