
$(function () {
	InitPosneg();
});

function InitPosneg() {
	$(".posneg input").each(function () {
		if ($(this).val() !== "Negative" && $(this).val() !== "Neutral" && $(this).val() !== "Positive") {
			console.log("Unknown value for posneg: " + $(this).val());
			$(this).val("Indeterminate");
		}
	});

}

$('body').on("click",'.editor .posneg-val',function () {
	var oldValue = $(this).parent().find("input").val();
	var newValue = $(this).data("value");

	if (oldValue !== "Indeterminate" && oldValue === newValue) {
		newValue = "Indeterminate";
	}
	$(this).parent().find("input").val(newValue).trigger('change');
});