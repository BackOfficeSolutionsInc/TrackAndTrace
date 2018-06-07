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

if (msieversion() == false) {
	setInterval(fixTileHeight, 750);
}

function fixTileHeight() {

	if (msieversion() == false) {
		$(".tile-height-resize").each(function () {
			var tile = $(this).closest(".tile");
			var scrollHeight = Math.max(Math.max($(this).children().height(), $(this).height()), tile[0].scrollHeight);

			if (tile.height() >= scrollHeight) {
				var self = $(this);
				var position = self.position();
				var a = tile.height() - position.top - (self.outerHeight() - self.innerHeight());
				var diff = +(self.data("tile-resize-diff") || 0);
				$(this).height((a - diff));
			} else {
				$(this).height("");
			}
		});
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

var printTileData = function (id) {
    window.open("/L10/PrintoutTodoList/" + id, '_blank');
}

//This is commented because no way to know when the scorecard is loaded.
//zoomScorecards();
//$(window).resize($.debounce(250, zoomScorecards));
//$("body").on("resize-tile", ".grid-tile", $.debounce(250,zoomScorecards));
//$("body").on("loaded", ".grid-tile",  $.debounce(250,zoomScorecards));



