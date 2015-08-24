


$(function () {
	InitRockstate();
});

function InitRockstate() {
	if (typeof (loadRockState) === "undefined") {
		loadRockState = true;
		$(".rockstate.editor input").each(function() {
			if ($(this).val() !== "AtRisk" && $(this).val() !== "OnTrack" && $(this).val() !== "Complete") {
				console.log("Unknown value for rockstate: " + $(this).val());
				$(this).val("Indeterminate");
			}
		});

		$(document).on("click", ".editor .rockstate-val", function() {
			var oldValue = $(this).parent().find("input").val();
			var newValue = $(this).data("value");

			if (oldValue !== "Indeterminate" && oldValue === newValue) {
				newValue = "Indeterminate";
			}
			$(this).parent().find("input").val(newValue).trigger('change');
		});

		/*$('.editor .rockstate-val').click(function () {
		var oldValue = $(this).parent().find("input").val();
		var newValue = $(this).data("value");

		if (oldValue !== "Indeterminate" && oldValue === newValue) {
			newValue = "Indeterminate";
		}
		$(this).parent().find("input").val(newValue).trigger('change');
	});*/
	}
}