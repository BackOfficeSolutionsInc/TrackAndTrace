//shift key extra options
$(function () {
	var keyAllowed = {};
	$(document).keydown(function (e) {
		if (keyAllowed[e.which] === false) return;
		keyAllowed[e.which] = false;
		if (e.keyCode == 16) {
			$(".shift-visible").removeClass("hidden").addClass("visible");
			$(".shift-hidden").addClass("hidden").removeClass("visible");
			console.log("shift pressed");
		}
	});

	$(document).keyup(function (e) {
		keyAllowed[e.which] = true;
		if (e.keyCode == 16) {
			$(".shift-visible").addClass("hidden").removeClass("visible");
			$(".shift-hidden").removeClass("hidden").addClass("visible");
			console.log("shift released");
		}
	});
	$(document).focus(function (e) {
		keyAllowed = {};
	});
});