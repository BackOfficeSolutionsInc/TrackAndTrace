


$(function () {
	InitRockstate();
	recalculatePercentage();
});

function InitRockstate() {
	if (typeof (loadRockState) === "undefined") {
		loadRockState = true;
		$(".rockstate.editor input").each(function () {
			if ($(this).val() !== "AtRisk" && $(this).val() !== "OnTrack" && $(this).val() !== "Complete") {
				console.log("Unknown value for rockstate: " + $(this).val());
				$(this).val("Indeterminate");
			}
		});

		$(document).on("click", ".editor .rockstate-val", function () {
			var parent = $(this).parent();
			if (parent.width() > 90) {
				var oldValue = parent.find("input").val();
				var newValue = $(this).data("value");

				if (oldValue !== "Indeterminate" && oldValue === newValue) {
					newValue = "Indeterminate";
				}
				$(this).parent().find("input").val(newValue).trigger('change');
				recalculatePercentage();
			}
		});
		
		$(document).on("click", ".editor .rockstate-contents", function () {
			var parent = $(this);
			if (parent.width() <= 90) {
				var oldValue = $(this).find("input").val();
				var args = ["AtRisk", "OnTrack", "Complete", "Indeterminate"];

				var idx = ((args.indexOf(oldValue) + 1) % (args.length));
				newValue = args[idx];

				$(this).parent().find("input").val(newValue).trigger('change');
				recalculatePercentage();
			}
		});


		$(document).on("change", ".rockstate.editor input", function() {
			var input = $(this);//.attr("name");
			var name = input.attr("name");
			var newValue = input.val();
			$("[name=" + name + "]").not(input).val(newValue);
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
function recalculatePercentage() {
	$(".completion-percentage").each(function () {
		var a = $(this).data("accountable");
		var parent = $(this).closest(".component");
		var inputs = parent.find(".rockstate input");
		var ontrack = 0;
		var offtrack = 0;

		for (var i = 0; i < inputs.length; i++) {
			var val = $(inputs[i]).val();
			if (val == "AtRisk" || val=="Indeterminate" || val=="OnTrack") {
				offtrack += 1;
			} else {
				ontrack += 1;
			}
		}

		var percentage = Math.round(100 * (ontrack / (ontrack + offtrack)));
		$(this).find(".ratio").html(ontrack + "/" + (ontrack + offtrack));
		$(this).find(".percentage").html(percentage + "%");

	});
}