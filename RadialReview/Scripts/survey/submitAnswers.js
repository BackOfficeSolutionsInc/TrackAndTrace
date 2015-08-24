$(document).ready(function () {
	// Get the initial value
	var $el = $('.answer');
	$el.data('oldVal', $el.val());
});
$(".answer").on('mouseup', function () {
	var old = $('input[name=' + $(this).prop("name") + ']:checked').val();
	console.log("Saving value " + old);
	$(this).data('oldval', old);
}).on("change", function () {
	var name = $(this).prop("name").substr(7);
	var old = $(this).data("oldval");
	var that = $(this);
	debugger;
	$.ajax({
		url: "/Survey/Set/" + name + "?value=" + $(this).val(),
		success: function (data) {
			debugger;
			showJsonAlert(data);
			$(that).data('oldVal', $(that).val());
		},
		error: function (d) {
			debugger;
			var $radios = $('input:radio[name=' + name + ']');
			if ($radios.is(':checked') === false) {
				$radios.filter('[value=' + old + ']').prop('checked', true);
			}
		}
	});
});

$("body").on("keyup", ".answerString", $.throttle(250, sendTextContents));

function sendTextContents() {
	var name = $(this).prop("name").substr(7);
	var old = $(this).val();
	$.ajax({
		method: "post",
		url: "/Survey/Set/" + name + "?",
		data: { "str": $(this).val() },
		success: function (data) {
			debugger;
			showJsonAlert(data);
		},
		error: function (d) {
			debugger;
			var $radios = $('input:radio[name=' + name + ']');
			if ($radios.is(':checked') === false) {
				$radios.filter('[value=' + old + ']').prop('checked', true);
			}
		}
	});
}