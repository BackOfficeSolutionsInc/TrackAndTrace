$(function() {
	setInterval(function() {
		var now = new Date();
		$(".current-time .hour").html(now.getHours() % 12 || 12);
		$(".current-time .minute").html(pad(now.getMinutes(),2));
		$(".current-time .second").html(pad(now.getSeconds(), 2));
	}, 100);

	$(".agenda a").click(function () {
		showLoader();
		var loc = $(this).data("location");
		loadPage(loc);
	});

	loadPage(window.location.hash.replace("#",""));

	$(window).resize(resizing);
	resizing();
});

function loadPage(location) {
	$.ajax({
		url: "/L10/Load/" + MeetingId + "?page=" + location,
		success: function (data) {
			replaceMainWindow(data);
		},
		error: function () {
			setTimeout(function () {
				$("#alerts").html("");
				showAlert("Page could not be loaded.");
				replaceMainWindow("");
			}, 1000);
		}
	});
}

function resizing() {
	var clock = $(".current-time");
	if (clock.width() < 221) {
		$(".current-time .second").css("display", "none");
	} else {
		$(".current-time .second").css("display", "inherit");
	}
	if (clock.width() < 192) {
		$(".current-time .big").css({ "font-size": "40px", "line-height": "85px" });
	} else {
		$(".current-time .big").css({ "font-size": "60px", "line-height": "" });
	}
}

function pad(num, size) {
	var s = num + "";
	while (s.length < size) s = "0" + s;
	return s;
}

function showLoader() {
	replaceMainWindow("<div class='loader centered'><div class='component '><div>Loading</div><img src='/Content/select2-spinner.gif' /></div></div>");
}

function replaceMainWindow(html) {
	$("#main-window").fadeOut(200,function () {
		$("#main-window").html(html);
		$("#main-window").fadeIn(200);

	});
}


//Table
//http://stackoverflow.com/questions/7433377/keeping-the-row-title-and-column-title-of-a-table-visible-while-scrolling
function moveScroll(table,window) {
	return function() {
		var scroll_top = $(window).scrollTop();
		var scroll_left = $(window).scrollLeft();
		var anchor_top = $(table).position().top;
		var anchor_left = $(table).position().left;
		var anchor_bottom = $("#bottom_anchor").offset().top;

		$("#clone").find("thead").css({
			width: $(table).find("thead").width() + "px",
			position: 'absolute',
			left: -scroll_left + 'px'
		});

		$(table).find(".first").css({
			position: 'absolute',
			left: scroll_left + anchor_left + 'px'
		});

		if (scroll_top >= anchor_top && scroll_top <= anchor_bottom) {
			clone_table = $("#clone");
			if (clone_table.length == 0) {
				clone_table = $(table)
					.clone()
					.attr('id', 'clone')
					.css({
						width: $(table).width() + "px",
						position: 'fixed',
						pointerEvents: 'none',
						left: $(table).offset().left + 'px',
						top: 0
					})
					.appendTo($("#table_container"))
					.css({
						visibility: 'hidden'
					})
					.find("thead").css({
						visibility: 'visible'
					});
			}
		} else {
			$("#clone").remove();
		}
	};
}
