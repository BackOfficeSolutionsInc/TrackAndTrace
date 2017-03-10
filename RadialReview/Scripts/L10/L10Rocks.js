$(function () {
	$(document).on("change", ".rocks-container .rockstate input", function () {
		var name = $(this).prop("name");
		var rockId = parseInt(name.split("_")[1]);
		var oldValue = $(this).closest(".rockstate").data("old-value");

		var selector = "input[name='" + name + "']";
		$.ajax({
			url: "/l10/UpdateRockCompletion/" + window.recurrenceId,
			method: "post",
			data: { rockId: rockId, state: $(this).val(), connectionId: $.connection.hub.id },
			success: function (data) {
				showJsonAlert(data, false, true);
				//$(selector).val((!data.Error ? data.Object : "Indeterminate"));
			},
			error: function () {
				$(selector).val(oldValue || "Indeterminate");
			}
		});
	});

	var clickRockRow = function (evt) {
		if ($(evt.target).is(".rockstate,.rockstate *"))
			return;
		if ($(evt.target).is(".buttonHolder"))
			return;

		var rockRow = $(this);
		$(".rock-row.selected").removeClass("selected");
		$(rockRow).addClass("selected");
		currentTodoDetailsId = $(rockRow).data("todo");
		//var createtime = $(rockRow).data("createtime");
		var duedate = +$(rockRow).attr("data-duedate");
		var accountable = $(rockRow).data("accountable");
		var owner = $(rockRow).data("name");
		var message = $(rockRow).data("message");
		var details = $(rockRow).data("details");
		//var padId = $(rockRow).data("padid");
		var rockId = $(rockRow).data("rockid");

		var due = new Date(new Date(duedate).toUTCString().substr(0, 16));
		//var checked = $(rockRow).find(".todo-checkbox").is(":checked");

		var detailsContents = $("<div class='component'></div>");
		var milestoneDetailsContents = $("<div class='component'></div>");

		$(detailsContents).append("<span class='expandContract btn-group pull-right'></span>");
		//$(detailsContents).append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");

		$(detailsContents).append("<div class='heading'><h4 class='message-holder clickable on-edit-enabled' data-rockid='" + rockId + "'><span data-rockid='" + rockId + "' class='message editable-text '>" + message + "</span></h4></div>");
		$(detailsContents).append("<iframe class='details rock-details' name='embed_readwrite' src='/Rocks/Pad/" + rockId + "' width='100%' height='100%'></iframe>");

		//Context buttons
		$(detailsContents).append(
			"<div class='button-bar'>" +
				"<div style='height:28px'>" +
				"<span class='expandContract btn-group'>" +
					"<span class='btn btn-default btn-xs copyButton issuesModal on-edit-enabled' data-method='CreateRockIssue' data-rockid='" + rockId + "' data-recurrence='" + window.recurrenceId + "' data-meeting='" + window.meetingId + "'><span class='icon fontastic-icon-pinboard'></span> New Issue</span>" +
					"<span class='btn btn-default btn-xs createTodoButton todoModal on-edit-enabled' data-method='CreateRockTodo' data-meeting='" + window.meetingId + "' data-rockid='" + rockId + "' data-recurrence='" + window.recurrenceId + "' ><span class='glyphicon glyphicon-unchecked todoButton'></span> To-Do</span>" +
				"</span>" +
				"</div>" +
			"</div>");

		//Assignee / Due date
		$(detailsContents).append(
			"<span class='clearfix'></span>" +
			"<span class='gray' style='width:75px;display:inline-block'>Assigned to:</span>" +
			"<span style='width:250px;padding-left:10px;' class='assignee on-edit-enabled' data-accountable='" + accountable + "' data-rockid='" + rockId + "'  >" +
				"<span data-rockid='" + rockId + "' class='btn btn-link owner'>" + owner + "</span>" +
			"</span>" +
			"<div >" +
				"<span class='gray' style='width:75px;display:inline-block'>Due date:</span>" +
				"<span style='width:250px;padding-left:10px;' class='duedate rock-duedate on-edit-enabled' data-accountable='" + accountable + "' data-rockid='" + rockId + "' >" +
					//"<span class='date' style='display:inline-block' data-date='" + dateFormatter(due) + "' data-date-format='m-d-yyyy'>" +
					//	"<input type='text' data-rockid='" + rockId + "' class='form-control datePicker' value='" + dateFormatter(due) + "'/>" +
					//"</span>" +
				"</span>" +
			"</div>" 
		);
		generateDatepickerLocalize($(detailsContents).find(".rock-duedate"), duedate, "rock-duedate").on("change", function (e, data) {
			recalculateMilestones();
			$.ajax({
				url: "/rocks/setduedate",
				method: "POST",
				data: {
					rockId: data.containerElement.data("rockid"),
					dueDate: data.serverDate
				},
				error: function () {
					recalculateMilestones();
				}
			});
		});

		//Milestones
		$(milestoneDetailsContents).append(
			"<div class='btn btn-default btn-xs add-milestone-button' data-rockid='" + rockId + "'><span class='icon fontastic-icon-plus-3'></span>Add Milestone</div>" +
			"<h4 class='milestone-heading'>Milestones</h4>" +
			"<table class='milestone-table' data-rockid='" + rockId + "'></table>"
		);

		var fullContents = $("<div class='rock-details abstract-details-panel'></div>");
		fullContents.append(detailsContents);
		fullContents.append(milestoneDetailsContents);
		
		var w = $(window).width();
		$("#rock-details").html("");
		if (w <= modalWidth || $(".conclusion").is(":visible")) {
			var c = fullContents.clone();
			c.find("h4").addClass("form-control");
			showModal({
				contents: c,
				title: "Edit Rock",
				noCancel: true,
			});
			recalculateMilestones();
		} else {
			$("#rock-details").append(fullContents);
			recalculateMilestones();
			fixRocksDetailsBoxSize();
		}
	}
	$("body").on("click", ".rocks-container .rock-row", clickRockRow);
	$("body").on("click", ".rock-details .add-milestone-button", function () {
		var rockid = $(this).data("rockid");
		showModal({
			title: "Add milestone",
			fields: [
				{ text: "Milestone", name: "milestone", placeholder: "Enter milestone" },
				{ text: "Due Date", name: "duedate", type: "date", value: new Date() },
				{ name: "rockId", type: "hidden", value: rockid }
			],
			push: "/Milestone/Add"
		});
	});
});



function fixRocksDetailsBoxSize() {
	if ($(".details.rock-details").length) {
		var wh = $(window).height();
		var pos = $(".details.rock-details").offset();
		var st = $(window).scrollTop();
		var footerH = wh;


		var msHeight = $(".milestone-table").height();

		try {
			footerH = $(".footer-bar .footer-bar-container:not(.hidden)").last().offset().top;
		} catch (e) { }
		$(".details.rock-details").height(footerH - 20 - 140 - pos.top - (msHeight + 30) - 40);
	}
}

$(window).resize(fixRocksDetailsBoxSize);
$(window).on("footer-resize", function () {
	setTimeout(fixRocksDetailsBoxSize, 250);
});

function updateRockCompletion(meetingRockId, state, rockId) {
	$("input[name='rock_" + meetingRockId + "']").val(state);
	if (rockId !== undefined) {
		$("input[name='for_rock_" + rockId + "']").val(state);
	}
}

function updateRockName(rockId, message) {
	$(".message[data-rock='" + rockId + "']").html(message);
}


function setMilestone(milestone) {
	var found = getMilestone(milestone.Id);
	if (found) {
		$.extend(found, milestone);
	} else {
		window.milestones.push(milestone);
	}
	recalculateMilestones();
}

function getMilestone(milestoneId) {
	for (var i in window.milestones) {
		var milestone = window.milestones[i];
		if (milestone.Id == milestoneId)
			return milestone;
	}
	return false;
}

function getMilestones(rockId) {
	var results = [];
	var allResults = typeof (rockId) === "undefined";
	for (var i in window.milestones) {
		var milestone = window.milestones[i];
		if (allResults || milestone.RockId == rockId)
			results.push(milestone);
	}

	results.sort(function (a, b) {
		return parseJsonDate(a.DueDate) - parseJsonDate(b.DueDate);
	});

	return results;
}

function recalculateMilestones(recreateTable) {

	if (typeof (recreateTable) === "undefined")
		recreateTable = true;

	var rockIds = [];
	var ms = getMilestones();
	var now = new Date();
	var minimumDate = now;
	var maximumDate = now;

	for (var m in ms) {
		var mm = ms[m];
		minimumDate = Math.min(parseJsonDate(mm.DueDate), minimumDate);
		maximumDate = Math.max(parseJsonDate(mm.DueDate), maximumDate);
	}

	$(".rock-row").each(function () {
		var dueDateStr = "" + $(this).data("duedate");
		if (dueDateStr && dueDateStr.length > 0 && +dueDateStr != 0) {
			var dueDate = new Date(+dueDateStr);
			minimumDate = Math.min(dueDate, minimumDate);
			maximumDate = Math.max(dueDate, maximumDate);
		}
	});

	var extra = (maximumDate - minimumDate) * .02;
	minimumDate = minimumDate - extra;


	var sliderPaddingLeft = .1;
	var sliderPaddingRight = .1;
	var sliderPaddingSkipRight = .05;

	function calculateMarkerPercentage(date) {
		var percentage = (1 - sliderPaddingSkipRight) * .5;//default percentage
		if (maximumDate != minimumDate) {
			percentage = (date - minimumDate) / (maximumDate - minimumDate);
		}
		//percentage to pad
		percentage = percentage * (1 - (sliderPaddingLeft + sliderPaddingRight + sliderPaddingSkipRight));
		percentage += sliderPaddingLeft;
		return percentage;
	}

	$(".rock-row").each(function () {
		var rockId = $(this).attr("data-rockid");
		var container = $(this).find(".milestone-marker-container");

		var ms = getMilestones(rockId);
		container.find(".milestone-marker").remove();
		container.find(".milestone-date-marker").remove();
		container.find(".milestone-date-container").remove();
		container.find(".pre-line,.post-line").remove();

		var allPastDueDone = true;
		var allDone = true;
		var anyPastDue = false;

		function placeMarker(marker, dueDate, status) {
			var statusUndefined = typeof (status) === "undefined";

			var percentage = calculateMarkerPercentage(dueDate);
			$(marker).css("left", (percentage * 100) + "%");

			if (dueDate < now) {
				anyPastDue = true;
				$(marker).addClass("past-due");
				if (!statusUndefined && status != "Done") {
					allPastDueDone = false;
				}
			} else {
				$(marker).addClass("future-due");
			}
			if (!statusUndefined && status != "Done")
				allDone = false;

			if (!statusUndefined) {
				$(marker).addClass("status-" + status);
			}
			container.append(marker);
		}
		var anyMilestones = ms.length > 0;

		var detailsBox = $(".milestone-table[data-rockid=" + rockId + "]");

		if (recreateTable) {
			detailsBox.html("");
		}

		//Markers
		for (var m in ms) {
			var mm = ms[m];
			var marker = $("<div class='milestone-marker' title='" + escapeString(mm.Name) + "'></div>");
			var dueDate = parseJsonDate(mm.DueDate);
			placeMarker(marker, dueDate, mm.Status);

			if (recreateTable) {
				var row = $("<tr class='milestone' data-milestoneid='" + mm.Id + "'></tr>");
				//row.append($("<td><div class='milestone-marker'></div></td>"));
				var statusBox = $("<input name type='checkbox'" + (mm.Status == "Done" ? "checked" : "") + " data-milestoneid='" + mm.Id + "'/>");

				$(statusBox).on("change", function () {
					var newVal = this.checked;
					var mid = $(this).data("milestoneid");
					var mmm = getMilestone(mid);
					mmm.Status = (newVal ? "Done" : "NotDone");
					recalculateMilestones();

					$.ajax({
						url: "/milestone/edit",
						method: "post",
						data: mmm,
						success: function () {
						},
						error: function () {
							mmm.Status = (!newVal ? "Done" : "NotDone");
							recalculateMilestones();
						},
						complete: function () {
						}
					});
				});

				var statusCell = $("<td class='milestone-status-cell'></td>");
				statusCell.append(statusBox);
				row.append(statusCell);
				row.append($("<td><div class='milestone-name'>" + mm.Name + "</div></td>"));

				var dateCell = $("<td class='milestone-duedate-cell'><div class='milestone-duedate'></div></td>");
				row.append(dateCell);

				$(detailsBox).append(row);
				var dateElement = dateCell.find('.milestone-duedate');
				dateElement.data("milestoneid", mm.Id);
				generateDatepickerLocalize(dateElement, dueDate, "milestone-date-" + mm.Id)
					.on("change", function (e, data) {
						var mmm = getMilestone(data.containerElement.data("milestoneid"));
						var old = mmm.DueDate;
						mmm.DueDate = data.serverDate;
						$.ajax({
							url: "/milestone/edit",
							method: "post",
							data: mmm,
							success: function () {
							},
							error: function () {
								mmm.DueDate = old;
							},
							complete: function () {
								recalculateMilestones();
							}
						});
					});
			}
		}


		if (anyMilestones) {
			var dateEllapseMarker = $("<div class='milestone-date-marker'></div>");
			if (anyPastDue) {
				dateEllapseMarker.addClass("past-due");
				if (!allPastDueDone) {
					dateEllapseMarker.addClass("status-NotDone");
				}
			}
			if (anyMilestones && allDone) {
				dateEllapseMarker.addClass("status-Done");
			}
			var startP = sliderPaddingLeft;
			var nowP = calculateMarkerPercentage(now);
			var endP = 1 - sliderPaddingRight - sliderPaddingSkipRight; //Default dueP
			var dueP = endP;

			var dueDateStr = "" + $(this).data("duedate");
			if (dueDateStr && dueDateStr.length > 0 && +dueDateStr != 0) {
				var dueDate = new Date(+dueDateStr);
				dueP = calculateMarkerPercentage(dueDate);
			}

			var dueMarkerW = (dueP - startP) * 100 + "%";
			var ellapseMarkerW = (Math.min(dueP, nowP) - startP) * 100 + "%";

			var preW = (startP) * 100 + "%";
			var postW = (1 - dueP - sliderPaddingSkipRight) * 100 + "%";

			var dateRangeContainer = $("<div class='milestone-date-container'></div>");
			$(dateRangeContainer).css("width", dueMarkerW);
			$(dateRangeContainer).css("left", startP * 100 + "%");

			$(dateEllapseMarker).css("width", ellapseMarkerW);
			$(dateEllapseMarker).css("left", startP * 100 + "%");

			var preline = $("<div class='pre-line'></div>");
			preline.css("width", (sliderPaddingLeft) * 100 + "%");
			preline.css("left", "0%");

			var postline = $("<div class='post-line'></div>");
			postline.css("width", postW);
			postline.css("left", dueP * 100 + "%");


			container.prepend(dateEllapseMarker);
			container.prepend(dateRangeContainer);
			container.prepend(preline);
			container.append(postline);
		}

	});
	fixRocksDetailsBoxSize();
	setTimeout(fixRocksDetailsBoxSize, 1);
}


function updateRocks(html) {
	$(".rocks-container").html(html);
	$(".rock-empty-holder").addClass("hidden");
	$(".rocks-container").removeClass("hidden");
	console.error("accept milestones javascript");
	recalculateMilestones();
}

function removeRock(rockId) {
	var row = $(".rock-row.rock-id-" + rockId);
	var accountableUser = $(row).data("owner");
	row.remove();
	if ($(".rock-row.user-id-" + accountableUser).length == 0) {
		$(".rock-group.rock-group-user-id-" + accountableUser).remove();
	}

}

$(window).on("page-rocks", function () {
	if (typeof (recalculatePercentage) === "function") {
		recalculatePercentage();
	}
});
$(window).on("page-rocks", fixRocksDetailsBoxSize);