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
$("body").on("click", ".online-component .toggle", function () {
	$(this).closest(".online-component").toggleClass("on");
	$(this).find(".glyphicon").toggleClass("glyphicon-chevron-right");
	$(this).find(".glyphicon").toggleClass("glyphicon-chevron-down");
});

$("body").on("click", ".online-component .user-picture", function () {
	var icon = $(this).find(".notification-icon");
	var check = icon.find(".checkmark");

	if (check.length>0) {
		check.remove();
	} else {
		icon.append("<span class='glyphicon glyphicon-ok-circle checkmark'></span>");	
	}

	var userId = $(this).data("userid");
	if (userEnterMeeting && userEnterMeeting.notifications) {
		userEnterMeeting.notifications[userId] = check.length>0?false:true; //flipflopped
	}
});

$("body").on("click", ".online-component .options .option.uncheck", function () {
	$(".notification-icon .checkmark").remove();
});
$("body").on("click", ".online-component .options .option.randomize", function () {

	function shuffle(that,childElem) {
		return that.each(function () {
			var $this = $(that);
			var elems = $this.children(childElem);

			elems.sort(function () { return (Math.round(Math.random()) - 0.5); });

			$this.remove(childElem);

			for (var i = 0; i < elems.length; i++)
				$this.append(elems[i]);

		});
	}



	shuffle($(".online-component .user-picture-container"), ".user-picture")

});

