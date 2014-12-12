
$(function () {
	InitApproveReject();
});

function InitApproveReject() {
	$(".approvereject input").each(function () {
		if ($(this).val() !== "True" && $(this).val() !== "False") {
			console.log("Unknown value for approvereject: " + $(this).val());
			$(this).val("Indeterminate");
		}
	});

	$('.approvereject-val').click(function () {
		var oldValue = $(this).parent().find("input").val();
		var newVal = $(this).data("value");

		if (oldValue === newVal)
			newVal = "Indeterminate";
		

		$(this).parent().find("input").val(newVal).trigger('change');
	});
}