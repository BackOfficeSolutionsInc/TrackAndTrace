var adjustCellHeight = function () {
    if (isMobile()) {
        $(".grid-tile .scroller").each()
    } else {

    }
}

var isMobile = function () {
    return $(window).width() < 768;
}

adjustCellHeight();

$(window).resize(adjustCellHeight);

