$(function() {

});

function showReason(self) {
	$(".reasonPopup").addClass("hidden");
	$(self).find("~ .reasonPopup").removeClass("hidden");
	$(self).find("~ .reasonPopup textarea").focus();
	$(self).find("~ .reasonPopup textarea").blur(function() {
		$(self).find("~ .reasonPopup").addClass("hidden");
		var any = $(self).find("~ .reasonPopup textarea").val();
		if (any && any.trim() != "") {
			$(self).addClass("on");
		} else {
			$(self).removeClass("on");
		}
	});
}