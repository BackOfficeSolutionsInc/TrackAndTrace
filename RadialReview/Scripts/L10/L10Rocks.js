$(function() {
	$(document).on("change", ".rocks-container .rockstate input", function() {
		var name = $(this).prop("name");
		var rockId = parseInt(name.split("_")[1]);

		var selector = "input[name='" + name + "']";
		$.ajax({
			url: "/l10/UpdateRockCompletion/" + recurrenceId,
			method: "post",
			data: { rockId: rockId, state: $(this).val(), connectionId: $.connection.hub.id },
			success: function (data) {
				showJsonAlert(data, false, true);
				//$(selector).val((!data.Error ? data.Object : "Indeterminate"));
			},
			error: function () {
				$(selector).val("Indeterminate");
			}
		});
	});
});

function updateRockCompletion(meetingRockId, state, rockId) {
	$("input[name='rock_" + meetingRockId + "']").val(state);
	if (rockId!==undefined) {
		$("input[name='for_rock_" + rockId + "']").val(state);
	}
}

function updateRockName(rockId, message) {
	$(".message[data-rock='" + rockId + "']").html(message);
}


function updateRocks(html) {
    $(".rocks-container").html(html);
    $(".rock-empty-holder").addClass("hidden");
    $(".rocks-container").removeClass("hidden");
}


function removeRock(rockId) {
    var row = $(".rock-row.rock-id-" + rockId);
    var accountableUser = $(row).data("owner");
    row.remove();
    if ($(".rock-row.user-id-" + accountableUser).length == 0) {
        $(".rock-group.rock-group-user-id-" + accountableUser).remove();
    }

}