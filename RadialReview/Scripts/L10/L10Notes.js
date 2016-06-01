$(function () {

    $("body").on("click", ".notes .tab:not(.add)", function () {
        fixNotesHeight();
		$(".notes .active").removeClass("active");
		
		$(".notes iframe").data("id", "");
		$(".notes iframe").attr("disabled", true);
		$(".notes iframe").animate({"background-color":"#ddd","color":"transparent"},150,function() {
			$(".notes iframe").val("");
		});

		
		var padid = $(this).data("padid");

	    $(".notes iframe").attr("src", notesUrl + "p/" + padid + "?showControls=true&showChat=false");
		$(".notes iframe").data("id",  $(this).data("id"));
		$(this).addClass("active");
		//$(".notes iframe").data("name", data.Object.Name);
	    /*
		var that = this;
		$.ajax({
			url: "/L10/Note/" + $(this).data("id"),
			success: function (data) {
				showJsonAlert(data, false, true);
				if (!data.Error) {
					$(".notes iframe").data("id", data.Object.NoteId);
					$(".notes iframe").data("name", data.Object.Name);
					$(that).addClass("active");
					$(".notes iframe").attr("disabled", false);
					$(".notes iframe").stop().animate({"background-color":"#fff","color":"black"},150);
					$(".notes iframe");
					$(".notes iframe").val(data.Object.Contents);
				}
			}
			
		});*/
    });
	
	$("body").on("click", ".notes .tab.add", function () {
		showModal("Add page", "/L10/CreateNote?recurrence=" + MeetingId, "/L10/CreateNote");
	});
	$("body").on("click", ".notes .overlay", function () {
		$(".notes").fadeOut();
		$(this).css("z-index", null);
	});

	$("body").on("click", ".notesButton:not(.disabled)", function () {
		$(this).css("z-index", 20);
		$(".notes").css({ right: 10, top: 40 });
		$(".notes").fadeIn();

		var found = $(".notes .tab:not(.add):first");
		
		if ($(found).length == 0) {
			$(".notes iframe").attr("disabled", true);
		} else {
			$(found).trigger("click");
		}
	});


	//$("body").on("keyup", ".notes textarea:not(.disabled)", $.throttle(250, sendNoteContents));

});

function fixNotesHeight() {
    var wh = $(window).height();
    var pos = $(".notes iframe").offset();
    var st = $(window).scrollTop();
    var footerH = wh;
    try {
        footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
    } catch (e) {

    }

    //$(".details.issue-details").height(wh - pos.top + st - footerH - 110);
    $(".notes iframe").height(footerH - 30 - pos.top);

    //$(".notes textarea").height($(window).height() - 80);

}

$(window).on("resize", fixNotesHeight);
$(window).on("footer-resize", function () {
    setTimeout(fixNotesHeight, 250);
});

function sendNoteContents() {
	var data = {
		Name: $(this).data("name"),
		Contents: $(this).val(),
		NoteId: $(this).data("id"),
		ConnectionId : $.connection.hub.id,
		SendTime : new Date().getTime()
	};
	var that = this;

	$.ajax({
		url: "/L10/EditNote/",
		method: "POST",
		data: data,
		success: function (data) {
			showJsonAlert(data, false, true);
			if (!data.Error) {
				//$(".notes textarea").val(data.Object.Contents);
				//$(".notes textarea").data("id", data.Object.NoteId);
				//$(".notes textarea").data("name", data.Object.Name);
				//$(that).addClass("active");
				//$(".notes textarea").prop("disable", false);
			}
		}
	});
}

function createNote(id, name,padId) {
	var row = $("<div class='tab' data-id='" + id + "' data-padid='"+padId+"'>" + name + "</div>");
	$(".notes .active").removeClass("active");
	$(".notes .tabs").append(row);
	$(row).addClass("active");
	$(".notes iframe").val("");
	$(".notes iframe").data("id", id);
	$(".notes iframe").data("name", name);
	$(".notes iframe").attr("disabled", false);
	$(row).click();
	$(".notes-instruction").hide();

}
function updateNoteName(id, name) {
	$(".notes .tab[data-id='" + id + "']").html(name);

	if ($(".notes .tab[data-id='" + id + "']").is(".active")) {
		$(".notes textarea").data("name", name);
	}
}


var lastNoteUpdate = {};
function updateNoteContents(id, contents,time) {
	if ($(".notes .tab[data-id='" + id + "']").is(".active")) {
		if (!lastNoteUpdate[id] || lastNoteUpdate[id] <= time) {
			$(".notes textarea").val(contents);
			lastNoteUpdate[id] = time;
		}
	}
}

