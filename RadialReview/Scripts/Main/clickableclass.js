
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Create issues, todos, headlines, rocks
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

$("body").on("click", ".issuesModal:not(.disabled)", function () {
	debugger;
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add an issue";
	showModal(title, "/Issues/" + m + "?" + parm, "/Issues/" + m);
});
$("body").on("click", ".todoModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a to-do";
	showModal(title, "/Todo/" + m + "?" + parm, "/Todo/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		if (found.length && found.val() == null)
			return "You must select at least one to-do owner.";
		return true;
	});
});
$("body").on("click", ".headlineModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a people headline";
	showModal(title, "/Headlines/" + m + "?" + parm, "/Headlines/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});

$("body").on("click", ".rockModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "EditModal";
	var title = dat.title || "Add a rock";
	showModal(title, "/Rocks/" + m + "?" + parm, "/Rocks/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});
$("body").on("click", ".milestoneModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a milestone";
	showModal(title, "/Milestone/" + m + "?" + parm, "/Milestone/" + m, null, function () {
		//var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});