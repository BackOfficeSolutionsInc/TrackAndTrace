var steps = [
    { message: "Select accountable users (Do not select header)", func: validateUsers },
    { message: "Select measurable titles (Do not select header)", func: validateMeasurables },
    { message: "Select goals (Do not select header)", func: validateGoal },
    { message: "Select dates (Start of week)", func: validateDate },
    //{ message: "All Done!", func: function () { } },
   // { message: "Select measurable titles (Do not select header)", func: validateMeasurables }
]

var step = 0;
var validationFunc = null;
var errors = [];

function nextStep() {
    $(".ui-selected").addClass("wasSelected");
    if (step >= steps.length) {
        $("#instruction").html("All done!");
        $("#nextButton").attr("disabled", "true");
        validationFunc = null;
        submitScorecard();
        $("#window").html("<center>Please Wait</center>");
        return;
    }

    $("#nextButton").attr("disabled", "true");
    $("#errors").hide();
    errors = [];
    $("#instruction").html(steps[step].message).show();
    //$("#table").selectable("refresh");
    $("#table").css("display", null);
    validationFunc = steps[step].func;
    step += 1;
}

function backStep() {
    step = Math.max(0, step - 1);
    nextStep();
}

function submitScorecard(){
    var data = {
        userRect: userRect,
        dateRect: dateRect,
        measurableRect: measurableRect,
        goalRect: goalRect,
        RecurrenceId: $("[name='RecurrenceId']").val(),
        Path: $("[name='Path']").val(),
    };
    $.ajax({
        url: "/upload/UploadScorecardSelected",
        method: "post",
        data: JSON.stringify(data),
        //dataType: "json",
        contentType: 'application/json; charset=utf-8',
        success: function (html) {
            debugger;
            $("#window").html(html);
        }
    });
}

function updateTables() {
    step = 0;
    //try{
    //    $("#table").selectable("destroy");
    //}catch(e){
    //}
    //$("#table").selectable({
    //    filter: ".tdItem",
    //    //autoRefresh: false,
    //    stop: function (event, ui) {
    //        if (validationFunc != null) {

    //            if (validationFunc(event, ui)) {   
    //                $("#nextButton").attr("disabled", null);
    //            } else {
    //                $("#nextButton").attr("disabled", true);
    //                $("#errors").html(errors.join("<br/>")).show();
    //                $("#table").selectable("refresh");
    //                errors = [];

    //            }
    //        }
    //    }
    //});
    var start = null, end;
    //$("*:not(#table td)").mousedown(function () {
    //    start = null;
    //});

    $("#table td").mousedown(function () {
        $(".ui-selected").removeClass("ui-selected");
        start = [$(this).data("col"), $(this).data("row")];
    });

    $("#table td").mouseover(function () {
        var cur = [$(this).data("col"), $(this).data("row")];

        if (start != null) {
            end = cur;
            var minx = Math.min(cur[0], start[0]);
            var miny = Math.min(cur[1], start[1]);
            var maxx = Math.max(cur[0], start[0]);
            var maxy = Math.max(cur[1], start[1]);
            $(".ui-selecting").removeClass("ui-selecting");
            var str = [];
            for (var i = minx; i <= maxx ; i++) {
                for (var j = miny; j <= maxy ; j++) {
                    str.push("#table td[data-row=" + j + "][data-col=" + i + "]");
                }
            }

            $(str.join(",")).addClass("ui-selecting");

        }
    });


    $(document).mouseup(function () {
        if (start != null && end != null) {
            start = null; end = null;
            $(".ui-selecting").addClass("ui-selected").removeClass("ui-selecting");
            $("#nextButton").attr("disabled", true);

            if (validationFunc != null) {

                if (validationFunc()) {
                    $("#errors").html("").hide();
                    $("#nextButton").attr("disabled", null);
                } else {
                    $("#nextButton").attr("disabled", true);
                    $("#errors").html(errors.join("<br/>")).show();
                    //$("#table").selectable("refresh");
                    errors = [];

                }
            }
        } else if (start!=null && end ==null){
            $("#nextButton").attr("disabled", true);
            start = null; end = null;
        }else{
            start = null; end = null;
            // $(".ui-selected").removeClass("ui-selected");
        }
    });

    nextStep();
}

function getRect() {
    var minx = 10000000;
    var miny = 10000000;
    var maxx = 0;
    var maxy = 0;
    var count = 0;
    $(".ui-selecting,.ui-selected").each(function () {
        var y = $(this).data("row");
        var x = $(this).data("col");
        miny = Math.min(miny, y);
        minx = Math.min(minx, x);
        maxy = Math.max(maxy, y);
        maxx = Math.max(maxx, x);
        count += 1;
    });
    if (count == 0)
        return null;
    return [minx, miny, maxx, maxy, count];
}

function ensureAtLeastOne(rect) {
    if (rect != null)
        return true;

    errors.push("You must select at least one cell.");
    return false;
}

function ensureColumnOrRow(rect) {
    if (rect[0] == rect[2] || rect[1] == rect[3])
        return true;
    errors.push("You must select either a row or column.");
    return false;
}
function ensureSimilar(rect1, rect2) {
    if ((rect1[0] == rect1[2] && rect1[1] == rect2[1] && rect1[3] == rect2[3]) || (rect1[1] == rect1[3] && rect1[0] == rect2[0] && rect1[2] == rect2[2]))
        return true;
    errors.push("Invalid selection. Cells must match previous selection.");
    return false;
}

function ensureAboveBelow(rect1, rect2) {
    if (rect1[1] > rect2[1] || rect1[3] < rect2[3])
        return true;
    errors.push("Invalid selection. Cells must be above or below the other selection.");
    return false;
}
function ensureRightOf(rect1, rect2) {
    if (rect1[2] < rect2[0])
        return true;
    errors.push("Invalid selection. Cells must be left or right the other selection.");
    return false;
}

function validateUsers() {

    var allTrue = true;
    var rect = getRect();

    allTrue = allTrue && ensureAtLeastOne(rect);
    allTrue = allTrue && ensureColumnOrRow(rect);

    userRect = rect;
    return allTrue;
}

function validateMeasurables() {
    var allTrue = true;
    var rect = getRect();
    allTrue = allTrue && ensureAtLeastOne(rect);
    allTrue = allTrue && ensureColumnOrRow(rect);
    allTrue = allTrue && ensureSimilar(userRect, rect);

    measurableRect = rect;
    return allTrue;
}

function validateGoal(e, ui) {
    var allTrue = true;
    var rect = getRect();
    allTrue = allTrue && ensureAtLeastOne(rect);
    allTrue = allTrue && ensureColumnOrRow(rect);
    allTrue = allTrue && ensureSimilar(userRect, rect);

    goalRect = rect;
    return allTrue;
}

function validateDate() {
    var allTrue = true;
    var rect = getRect();
    allTrue = allTrue && ensureAtLeastOne(rect);
    allTrue = allTrue && ensureColumnOrRow(rect);
    allTrue = allTrue && ensureAboveBelow(userRect, rect);

    var bounds = [
        Math.min(userRect[0], measurableRect[0], goalRect[0]),
        Math.min(userRect[1], measurableRect[1], goalRect[1]),
        Math.max(userRect[2], measurableRect[2], goalRect[2]),
        Math.max(userRect[3], measurableRect[3], goalRect[3])
    ];

    allTrue = allTrue && ensureRightOf(bounds, rect);

    dateRect = rect;
    return allTrue;
}