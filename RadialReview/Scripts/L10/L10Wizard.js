


$(window).on("wizard:changed-page", function (e, data) {
    setTimeout(function () {
        setCompletion(data.completion * 100);
    }, 400);
});