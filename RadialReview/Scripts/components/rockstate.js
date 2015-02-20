
$(function () {
	InitRockstate();
});

function InitRockstate() {
	$(".rockstate.editor input").each(function () {
		if ($(this).val() !== "AtRisk" && $(this).val() !== "OnTrack" && $(this).val() !== "Complete") {
			console.log("Unknown value for rockstate: " + $(this).val());
			$(this).val("Indeterminate");
		}
	});

	$('.editor .rockstate-val').click(function () {
		var oldValue = $(this).parent().find("input").val();
		var newValue = $(this).data("value");

		if (oldValue !== "Indeterminate" && oldValue === newValue) {
			newValue = "Indeterminate";
		}
		$(this).parent().find("input").val(newValue).trigger('change');
	});
}