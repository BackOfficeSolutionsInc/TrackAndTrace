function sliderToText(value) {
    var answers = ["Never", "Seldom", "Sometimes", "Usually", "Mostly", "Always", "Above and Beyond"];
    var div = 100 / answers.length;
    var i = Math.floor(value / div);
    var answer = answers[i];
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
});