
$(function () {
	InitTristate();
});

function InitTristate() {
	$(".tristate input").each(function () {
		if ($(this).val() !== "True" && $(this).val() !== "False") {
			console.log("Unknown value for tristate: " + $(this).val());
			$(this).val("Indeterminate");
		}
	});

	$('.editor.tristate').click(function () {
		var states = ['Indeterminate', 'True', 'False'];
		var oldValue = $(this).find("input").val();
		var oldIndex = $.inArray(oldValue, states);
		var newIndex = (oldIndex + 1) % states.length;
		var newValue = states[newIndex];
		$(this).find("input").val(newValue).trigger('change');
	});
}