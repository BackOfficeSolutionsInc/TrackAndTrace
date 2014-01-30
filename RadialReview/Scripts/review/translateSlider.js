var possibleAnswers = ["Never", "Seldom", "Sometimes", "Usually", "Mostly", "Always", "Above and Beyond"];
var possibleColors = ["#FF0000", "#F45E00", "#E9B500", "#BADF00", "#5FD400", "#0CC900", "#00BF3D"];

function sliderIndex(value)
{
    var div = 100 / possibleAnswers.length;
    value = Math.min(99.99999, value);
    value = Math.max(0, value);
    var i = Math.floor(value / div);
    return i;
}

function sliderToText(value) {
    var i=sliderIndex(value);   
    var answer = possibleAnswers[i];
    if (value == 0)
        answer = "No Answer";
    return answer;
}

function sliderToColor(value) {
    var i = sliderIndex(value);
    var answer = possibleColors[i];

    if (value == 0)
        answer = "No Answer";
    return answer;
}

$(function () {
    $(".percentage").each(function () {
        var value = parseFloat($(this).html()) * 100;
        var text = sliderToText(value);
        $(this).html(text);
    });

    $(".color-percentage").each(function () {
        var value = parseFloat($(this).html()) * 100;
        var text = sliderToText(value);
        var color = sliderToColor(value);

        $(this).css("background-color", color);
        $(this).attr("title", text);
        $(this).html("");

    });
});