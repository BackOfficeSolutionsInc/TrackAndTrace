function checktree(selector, data, onchange) {
	var allUncheck = _checktree(selector, data, onchange, false);
	onchange = onchange || function() {};
	$(selector).find("input").on("change", onchange);
	$(selector).find(".expander").on("click", function() {
		var on = $(this).hasClass("expanded");
		if (on) {
			$(this).trigger("collapse");
		} else {
			$(this).trigger("expand");
		}
	});

	var allIds = $(selector).find("input[type='checkbox']").select(function(c) {
		return $(c).data("checktree-id");
	});


	setTimeout(function () {

		for (var i = 0; i < allIds.length; i++) {
			$(allIds[i]).prop("checked", false);
			//updateChecktree.call($(allIds[i]));
			$(allIds[i]).trigger("change");
		}

		for (var i = 0; i < allIds.length; i++) {
			if (allUncheck.indexOf($(allIds[i]).data("checktree-id")) != -1) {
				$(allIds[i]).prop("checked", true);
				updateChecktree.call($(allIds[i]));
			}
		}
	}, 1);

	$(selector).find(".expander").on("collapse", function() {
		$(this).siblings(".subtree").addClass("hidden");
		$(this).removeClass("expanded");
		return true;
	});
	$(selector).find(".expander").on("expand", function() {
		$(this).siblings(".subtree").removeClass("hidden");
		$(this).addClass("expanded");
		return true;
	});
	$(".expander").trigger("collapse");
	$(".expander").first().trigger("expand");
}

function _checktree(selector, data, init) {
	init = init || false;
	var output = [];
	if (data) {
		for (var i = 0; i < data.length; i++) {
			var d = data[i];
			var branch = $("<div class='branch'></div>");
			$(selector).append(branch);
			if (init) {
				$(branch).append("<div class='horizontal'/>");
			}
			var leaf = "leaf";
			if (d.subgroups && d.subgroups.length > 0) {
				leaf = "branch";
				$(branch).append("<div class='expander expanded'></div>");
			}

			var input = $("<input checked id='checkbox_" + d.id + "' class='" + leaf + "' data-checktree-id='" + d.id + "' type='checkbox'/>");
			$(input).click(updateChecktree);

			$(branch).append(input);
			$(branch).append("<label data-checktree-id='" + d.id + "' for='checkbox_" + d.id + "' class='" + leaf + "'>" + d.title + "</label>");
			var subtree = $("<div class='subtree'></div>");
			$(branch).append(subtree);
			if (!d.hidden) {
				output.push(d.id);
			}
			output = output.concat(_checktree(subtree, d.subgroups, true));
		}
	}
	return output;
}

function updateChecktree() {
	var id = $(this).data("checktree-id");
	var val = $(this).is(':checked');
	checkChecktree(id, val);
}

function checkChecktree(checktreeId, value) {
	_checkChecktree(checktreeId, value);
	$(".checktree .updated").removeClass("updated");
	var allInputs = $(".checktree input.branch");

	for (var i = 0; i < allInputs.length; i++) {
		var any = false;
		var all = true;

		var e = $(allInputs[i]);
		$(e).siblings(".subtree").find("input.leaf").each(function (i, v) {
			if ($(v).is(':checked')) {
				any = true;
			} else {
				all = false;
			}
		});

		$(e).prop("indeterminate", false);
		if (any && all) {
			$(e).prop("checked", true);
		}
		if (any && !all) {
			$(e).prop("indeterminate", true);
		}
		if (!any && !all) {
			$(e).prop("checked", false);
		}

	}
}

function _checkChecktree(checktreeId, value) {
	var elems = $("[data-checktree-id=" + checktreeId + "]");
	$(elems).prop("checked", value);
	//each element
	for (var i = 0; i < elems.length; i++) {
		var e = $(elems[i]);
		if (!$(e).hasClass("updated")) {
			$(e).addClass("updated");
			$(e).trigger("change");
			

			var children = $(e).siblings(".subtree").find("input");
			for (var c = 0; c < children.length; c++) {
				var childId = $(children[c]).data("checktree-id");
				_checkChecktree(childId, value);
			}
			//$(e).addClass("updated");

		}
	}
}

function toggleSubtree() {
	
}

