
$(function () {
	//setTimeout(function () {
		InitFivestate();
	//},1);
	
});

function InitFivestate() {
	var wasLimited = -1;

	function getStates() {
		var states = ['Indeterminate', 'Always', 'Mostly', "Rarely", "Never"];
		if (window.LimitFiveState)
			states = ['Indeterminate', 'Always', 'Never'];

		if (window.LimitFiveState != wasLimited) {
			$("body").removeClass("fivestate-limit-" + wasLimited);
			wasLimited = window.LimitFiveState;
			
			$(".fivestate .slider").attr("data-slider-range", "0," + (states.length - 1));
			$("body").addClass("fivestate-limit-" + wasLimited);
		}
		return states;
	}
	getStates();

	$(".fivestate input.data").each(function () {
		if ($(this).val() == "True")
			$(this).val("Always");
		if ($(this).val() == "False")
			$(this).val("Never");


		if ($(this).val() !== "Always" &&
			$(this).val() !== "Mostly"&& 
			$(this).val() !== "Rarely"&& 
			$(this).val() !== "Never") 
		{
			console.log("Unknown value for fivestate: " + $(this).val());
			$(this).val("Indeterminate");
		}
	});

	$('.editor.fivestate .fill').click(function (e) {
		var states = getStates();//['Indeterminate', 'Always', 'Mostly', "Rarely", "Never"];
		var oldValue = $(this).closest(".fivestate").find("input.data").val();
		var oldIndex = $.inArray(oldValue, states);
		var newIndex = (oldIndex + 1) % states.length;
		var newValue = states[newIndex];
		$(this).closest(".fivestate").find("input.data").val(newValue).trigger('change');
		$(this).closest(".fivestate").find("input.slider").simpleSlider("setValue", newIndex);
		e.preventDefault();
	});

	$('.editor.fivestate').bind("contextmenu",function(e){
		var states = getStates();//var states = ['Indeterminate', 'Always', 'Mostly',"Rarely","Never"];
		var oldValue = $(this).find("input.data").val();
		var oldIndex = $.inArray(oldValue, states);
		var newIndex = (oldIndex + states.length - 1) % states.length;
		var newValue = states[newIndex];
		$(this).find("input.data").val(newValue).trigger('change');
		$(this).find("input.slider").simpleSlider("setValue", newIndex);
		e.preventDefault();
	}); 
	
	$("input.slider").bind("slider:changed", function (e, data) {
		if (data.trigger == "setValue")
			return;
		var states = getStates();//var states = ['Indeterminate', 'Always', 'Mostly',"Rarely","Never"];
		var newValue = states[data.value];
		$(this).closest(".fivestate").find("> .fivestate-contents > input").val(newValue).trigger('change');
		
		e.preventDefault();
	});
}