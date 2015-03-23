$(function() {
	$("body").on("change", ".rocks-container .rockstate input", function() {
		var name = $(this).prop("name");
		var rockId = parseInt(name.split("_")[1]);

		var selector = "input[name='" + name + "']";
		debugger;
		$.ajax({
			url: "/l10/UpdateRockCompletion/" + recurrenceId,
			method: "post",
			data: { rockId: rockId, state: $(this).val(), connectionId: $.connection.hub.id },
			success: function (data) {
				showJsonAlert(data, false, true);
				$(selector).val((!data.Error ? data.Object : "Indeterminate"));
			},
			error: function () {
				$(selector).val("Indeterminate");
			}
		});
	});
});

function updateRockCompletion(rockId, state) {
	$("input[name='rock_" + rockId + "']").val(state);
}