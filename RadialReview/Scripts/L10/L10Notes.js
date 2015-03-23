$(function () {

	$("body").on("click", ".notes .tab:not(.add)", function () {
		$(".notes .active").removeClass("active");
		
		$(".notes textarea").data("id", "");
		$(".notes textarea").prop("disable", true);
		$(".notes textarea").animate({"background-color":"#ddd","color":"transparent"},150,function() {
			$(".notes textarea").val("");
		});

		var that = this;
		$.ajax({
			url: "/L10/Note/" + $(this).data("id"),
			success: function (data) {
				showJsonAlert(data, false, true);
				if (!data.Error) {
					$(".notes textarea").data("id", data.Object.NoteId);
					$(".notes textarea").data("name", data.Object.Name);
					$(that).addClass("active");
					$(".notes textarea").prop("disable", false);
					$(".notes textarea").stop().animate({"background-color":"#fff","color":"black"},150);
					$(".notes textarea");
					$(".notes textarea").val(data.Object.Contents);
				}
			}
			
		});
	});
	
	$("body").on("click", ".notes .tab.add", function () {
		showModal("Add page", "/L10/CreateNote?recurrence=" + MeetingId, "/L10/CreateNote");
	});
	$("body").on("click", ".notes .overlay", function () {
		$(".notes").fadeOut();
		$(this).css("z-index", null);
	});

	$("body").on("click", ".notesButton", function () {
		$(this).css("z-index", 20);
		$(".notes").css({ right: 10, top: 40 });
		$(".notes").fadeIn();

		$(".notes .tab:not(.add):first").trigger("click");
	});


	$("body").on("keyup", ".notes textarea", $.throttle(250, sendNoteContents));

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

function createNote(id, name) {
	var row = $("<div class='tab' data-id='" + id + "'>" + name + "</div>");
	$(".notes .active").removeClass("active");
	$(".notes .tabs").append(row);
	$(row).addClass("active");
	$(".notes textarea").val("");
	$(".notes textarea").data("id", id);
	$(".notes textarea").data("name", name);
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

