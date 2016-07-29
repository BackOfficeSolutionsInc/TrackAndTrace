var adjustCellHeight = function () {
    if (isMobile()) {
        //$(".grid-tile .scroller").each(function () {
        //    debugger;
        //    var h = $(this).height();
        //    $(this).closest(".grid-tile").css("height",h+"px !important");
        //});
    } else {

    }
}

var isMobile = function () {
    return $(window).width() < 768;
}

adjustCellHeight();


$(window).resize(adjustCellHeight);

