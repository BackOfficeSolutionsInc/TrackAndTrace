var adjustCellHeight = function () {
    if (isMobile()) {
        //$(".grid-tile .scroller").each(function () {
        //    debugger;
        //    var h = $(this).height();
        //    $(this).closest(".grid-tile").css("height",h+"px !important");
        //});
    } else {
    	//zoomScorecards();

    }


}

function zoomScorecards() {
	$(this).find(".scorecard-list-container,.l10-scorecard-tile").each(function () {
		var availHeight = $(this).closest(".tile").height();
		var myHeight = $(this).height();
		$(this).css("zoom", Math.max(1, availHeight / myHeight));
	});
}

var isMobile = function () {
    return $(window).width() < 768;
}

//This is commented because no way to know when the scorecard is loaded.
//zoomScorecards();
//$(window).resize($.debounce(250, zoomScorecards));
//$("body").on("resize-tile", ".grid-tile", $.debounce(250,zoomScorecards));
//$("body").on("loaded", ".grid-tile",  $.debounce(250,zoomScorecards));