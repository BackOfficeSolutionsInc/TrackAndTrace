$(window).resize(fixChatLogBoxSize);
$(window).on("load", function() {
	fixChatLogBoxSize(250);
});

$(window).on("footer-resize",function(e,opening) {
	fixChatLogBoxSize(250);
});

function fixChatLogBoxSize(time) {
	var cc = $(".chat-container");
	if (cc.length) {
		var wh =  $(window).height();;
		cc.height(wh  -  cc.offset().top);
	}
	if (!time) {
		time = 0;
	}
	setTimeout(function() {
		var c = $(".chat-container .component");
		if (c.length) {
			var wh = $(window).height();
			var pos = c.offset();
			var st = $(window).scrollTop();
			var footerH = wh;
			try {
			    footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
			} catch (e) {

			}

			//$(".details.issue-details").height(wh - pos.top + st - footerH - 110);
			c.animate({ "height": (footerH - 0 - pos.top) }, 50);
		}
	}, time);
}


function addLogRow(id, html, type) {
	var row = $("<li id='" + id + "' data-type='" + type + "' class='" + type + "'/>").html(html);
	$(".chat-container .log").prepend(row);
}

function editLogRow(id, html, type) {
	var found = $("#" + id);
	if (found.length > 0) {
		if (type) {
			var clss = found.attr("data-type");
			found.removeClass(clss);
			found.attr("data-type", type);
			found.addClass(clss);
		}
		found.html(html);
	}
}


function addOrEditLogRow(id, html, type) {
	var found = $("#" + id);
	if (found.length > 0) {
		editLogRow(id, html, type);
	} else {
		addLogRow(id, html, type);
	}
}