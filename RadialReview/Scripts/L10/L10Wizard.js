


$(window).on("wizard:changed-page", function (e, data) {
	setTimeout(function () {
		try {
			var scope = angular.element("[vs-repeat]").scope();
			scope.$emit("vs-repeat-resize");
			scope.$digest();
		} catch (e) {
			console.error(e);
		}
		setCompletion(data.completion * 100);

	}, 400);

	setTimeout(function () {
		try {
			var scope = angular.element("[vs-repeat]").scope();
			scope.$emit("vs-repeat-resize");
			scope.$digest();
		} catch (e) {
			console.error(e);
		}
	},200)


	$(".backButton.disabled,.nextButton.disabled").removeClass("disabled");
});
$(window).on("wizard:first-page", function (e, data) {
	$(".backButton").addClass("disabled");
});
$(window).on("wizard:last-page", function (e, data) {
	$(".nextButton").addClass("disabled");
});