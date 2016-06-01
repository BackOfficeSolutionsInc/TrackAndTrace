


$(window).on("wizard:changed-page", function (e, data) {
    setTimeout(function () {
        setCompletion(data.completion * 100);
    }, 400);
    $(".backButton.disabled,.nextButton.disabled").removeClass("disabled");
});
$(window).on("wizard:first-page", function (e, data) {
    $(".backButton").addClass("disabled");
});
$(window).on("wizard:last-page", function (e, data) {
    $(".nextButton").addClass("disabled");
});