
function fixHeadlinesBoxSize() {
    if ($(".headlines-notes").length) {
        var wh = $(window).height();
        var pos = $(".headlines-notes").offset();
        var st = $(window).scrollTop();
        var footerH = wh;
        try {
            footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
        } catch (e) { }

        $(".headlines-notes").height(Math.max(200, footerH - 20 - 50 - pos.top));
    }
}

$(window).resize(fixHeadlinesBoxSize);
$(window).on("page-headlines", fixHeadlinesBoxSize);
$(window).on("footer-resize", function () {
    setTimeout(fixHeadlinesBoxSize, 250);
});