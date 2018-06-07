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
				recalculateMilestones()
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
		//var duedate = parseJsonDate($(rockRow).attr("data-duedate"), true);
        var duedate = +$(rockRow).attr("data-duedate");
		var accountable = $(rockRow).data("accountable");
		var owner = $(rockRow).data("name");
		var message = $(rockRow).data("message");
		var details = $(rockRow).data("details");
		//var padId = $(rockRow).data("padid");
		var rockId = $(rockRow).data("rockid");

       

		//var due = new Date(new Date(duedate).toUTCString().substr(0, 16));
        var due = new Date(duedate);
		//var checked = $(rockRow).find(".todo-checkbox").is(":checked");

        console.log('dueDate=>', new Date(duedate))
        console.log('dueDate time convert=>', due)

		var detailsContents = $("<div class='component detail-component'></div>");
		var milestoneDetailsContents = $("<div class='component milestone-component'></div>");

		$(detailsContents).append("<span class='expandContract btn-group pull-right'></span>");
		//$(detailsContents).append("<div class='createTime'>" + dateFormatter(new Date(createtime)) + "</div>");

		$(detailsContents).append("<div class='heading'><h4 class='message-holder clickable on-edit-enabled' data-rockid='" + rockId + "'><span data-rockid='" + rockId + "' class='message editable-text '>" + message + "</span></h4></div>");
		$(detailsContents).append("<iframe class='details rock-details on-edit-enabled' name='embed_readwrite' src='/Rocks/Pad/" + rockId + "' width='100%' height='100%'></iframe>");

		//Context buttons
		$(detailsContents).append(
			"<div class='button-bar'>" +
				"<div style='height:28px'>" +
				"<span class='expandContract btn-group'>" +
					"<span class='btn btn-default btn-xs copyButton issuesModal on-edit-enabled' data-method='CreateRockIssue' data-rock='" + rockId + "' data-recurrence='" + window.recurrenceId + "' data-meeting='" + window.meetingId + "'><span class='icon fontastic-icon-pinboard'></span> New Issue</span>" +
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
            "<span class='date' style='display:inline-block' data-date='" + clientDateFormat(due) + "' data-date-format='m-d-yyyy'>" +
            "<input type='text' data-rockid='" + rockId + "' class='form-control datePicker' value='" + clientDateFormat(due) + "'/>" +
					"</span>" +
				"</span>" +
			"</div>"
		);

		//Milestones
		$(milestoneDetailsContents).append(
			"<div class='btn btn-default btn-xs add-milestone-button on-edit-enabled' data-rockid='" + rockId + "'><span class='icon fontastic-icon-plus-3'></span>Add Milestone</div>" +
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

	var clickMilestoneRow = function (evt) {

		var mid = $(this).closest(".milestone").data("milestoneid");
		var ms = getMilestone(mid);

		showModal({
			title: "Edit Milestone",
			fields: [
				{ name: "Name", text: "Milestone", placeholder: "Enter milestone", value: ms.Name },
				{ name: "DueDate", text: "Due date", type: "date", value: ms.DueDate, localize: true },
				{ name: "Status", text: "Completed", type: "checkbox", value: ms.Status == "Done" },
				{ name: "Id", type: "hidden", value: ms.Id },
			],
			success: function (formData) {

				formData["Status"] = formData["Status"] == "True" ? "Done" : "NotDone";
				setMilestone(formData);

				$.ajax({
					url: "/milestone/edit",
					method: "post",
					data: formData
				});
			}
		});
	};

	$("body").on("click", ".delete-milestone", function () {
		var id = $(this).data("milestoneid");
		$.ajax({
			url: "/milestone/delete/" + id,
			success: function () {
				//	milestones.remove();
			}
		});
	});

	$("body").on("click", ".rocks-container .rock-row", clickRockRow);
	$("body").on("click", ".company-rock-container .rock-row", clickRockRow);
	$("body").on("click", ".milestone-table .milestone .milestone-name", clickMilestoneRow);
	$("body").on("click", ".milestone-marker", clickMilestoneRow);
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
	$("body").on("mouseout", ".milestone", function () {
		$(".milestone").removeClass("mouseover");
	});
	$("body").on("mouseover", ".milestone", function () {
		var id = $(this).data("milestoneid");
		$(".milestone[data-milestoneid=" + id + "]").addClass("mouseover");
	});


	$("body").on("click", "#rock-details .message-holder .message", function () {
		var input = $("<textarea class='message-input' value='" + escapeString($(this).html()) + "' data-old='" + escapeString($(this).html()) + "' onblur='sendRockMessage(this," + $(this).parent().data("rockid") + ")'>" + ($(this).html()) + "</textarea>");
		$(this).parent().html(input);
		input.focusTextToEnd();
    });

    $("body").on('click', ".rock-details .rock-duedate .date input", function () {
        if (!$(this).attr("init")) {
            var now = new Date();
            var rockId = $(this).data("rockid");
            
            $(this).datepickerX({
                format: 'dd-mm-yyyy',
                todayBtn: true,
                orientation: "top left"
            }).on('changeDate', function (ev) {
                //DO NOT CONVERT TO SERVER TIME...
                var data_timeStamp = ev.date.addDays(.99999).valueOf();
                $(".rock-id-" + rockId).data("duedate", data_timeStamp);

                recalculateMilestones();
                $.ajax({
                    url: "/rocks/setduedate",
                    method: "POST",
                    data: {
                        rockId: rockId,
                        dueDate: data_timeStamp
                    }, error: function () {
                        recalculateMilestones();
                    }
                });
            });
            $(this).attr("init", 1);
            $(this).blur();
            var that = this;
            setTimeout(function () {
                $(that).datepickerX("show");
            }, 10);
        }
    });

});


function sendRockMessage(self, id) {
	var val = $(self).val();
	$(self).closest(".form-control").removeClass("focus");
	if (val.trim() == "") {
		$("#rock-details .message-holder[data-rockid=" + id + "]").html("<span data-rockid='" + id + "' class='message editable-text'>" + $(self).data("old") + "</span>");
		return;
	}

	$(".rocks .message-holder[data-rockid=" + id + "] input").prop("disabled", true);
	var data = {
		message: val
	};

	$.ajax({
		method: "POST",
		data: data,
		url: "/L10/UpdateRock/" + id,
		success: function (data) {
			showJsonAlert(data, false, true);
			if (!data.Error) {
				$("#rock-details .message-holder[data-rockid=" + id + "]").html("<span data-rockid='" + id + "' class='message'>" + val + "</span>");
			}
		}
	});
}

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

		var rockDetailsHeight = Math.max(116, footerH - 20 - 140 - pos.top - (msHeight + 30) - 40);
		$(".details.rock-details").height(rockDetailsHeight);
        
		var detailHeight = $(".detail-component").height();

		$(".milestone-table").css("max-height", wh - detailHeight - 160 - 72 - (wh-footerH));
	}
}

$(window).resize(fixRocksDetailsBoxSize);
$(window).on("footer-resize", function () {
	setTimeout(fixRocksDetailsBoxSize, 250);
});

function updateRockCompletion(rockId, state, rockId1) {
	$("input[name='rock_" + rockId + "']").val(state);
	if (rockId1 !== undefined) {
		$("input[name='for_rock_" + rockId1 + "']").val(state);
	}
    recalculateMilestones();
    recalculatePercentage();
}

function updateRockName(rockId, message) {
	$(".message[data-rock='" + rockId + "']").html(message);
}

function updateRockDueDate(rockId, duedate) {
    
    var row = $(".rock-id-" + rockId);
    row.attr("data-duedate", duedate);

    var d = new Date(duedate);
    d = Time.parseJsonDate(d);
    var dispDate = d;
    var found = $(".rock-duedate[data-rockid='" + rockId + "']");
    var _duedate = found.find('.date');
    _duedate.attr("data-duedate", clientDateFormat(dispDate));
    $("input[data-rockid=" + rockId + "]").val(clientDateFormat(dispDate));
}



var rockGetter = function () {
	var rocks = [];
	$(".rock-row").each(function () {
	    rocks.push({
	        Status: $(this).find(".rockstate input").val(),
			DueDate: $(this).data("duedate"),
			Id: $(this).data("rockid")
		});
	});
	return rocks;
}

window.milestoneAccessor = new MilestoneAccessor(function () { return window.milestones }, rockGetter, {
	callbacks: {
		remove: function (milestoneId) { $(".milestone[data-milestoneid='" + milestoneId + "']").remove(); },
		recalculateRock: function (rock, model) {

			var sliderPaddingLeft = .1;
			var sliderPaddingRight = .1;
			var sliderPaddingSkipRight = .05;

			var startP = sliderPaddingLeft;
			var nowP = rock.nowPercentage;
			var endP = 1 - sliderPaddingRight - sliderPaddingSkipRight; //Default dueP
			var dueP = endP;

			function shiftPercent(p) {
				//percentage to pad
				var percent = p * (1 - (sliderPaddingLeft + sliderPaddingRight + sliderPaddingSkipRight)) + sliderPaddingLeft;
				return (percent * 100) + "%";
			}


			var r = $(".rock-row[data-rockid=" + rock.rockId + "]");

			var now = new Date();

			var container = $(r).find(".milestone-marker-container");

			container.find(".milestone-marker").remove();
			container.find(".milestone-date-marker").remove();
			container.find(".milestone-date-container").remove();
			container.find(".pre-line,.post-line").remove();

			var detailsBox = $(".milestone-table[data-rockid=" + rock.rockId + "]");

			if (model.recreate) {
				detailsBox.html("");
			}
			for (var m in rock.markers) {
				if (arrayHasOwnIndex(rock.markers, m)) {
					var mm = rock.markers[m];
					var marker = $("<div class='milestone-marker milestone' title='" + escapeString(mm.name) + "' data-milestoneid='" + mm.milestoneId + "'></div>");
					var p = shiftPercent(mm.percentage)
					$(marker).css("left", p);
					

					$(marker).toggleClass("past-due", mm.dueDate < now);
					$(marker).toggleClass("future-due", mm.dueDate >= now);
					if (typeof(mm.status)!=="undefined") {
						$(marker).addClass("status-" + mm.status);
					}

					$(marker).toggleClass("rock-is-done", rock.rockStatus=="Complete");

					container.append(marker);


					if (model.recreate) {
						var row = $("<tr class='milestone' data-milestoneid='" + mm.milestoneId + "'></tr>");
						var statusBox = $("<input name type='checkbox'" + (mm.status == "Done" ? "checked" : "") + " data-milestoneid='" + mm.milestoneId + "'/>");

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
						row.append($("<td class='milestone-name-cell'><div class='milestone-name'>" + mm.name + "</div></td>"));

						var dateCell = $("<td class='milestone-duedate-cell'><div class='milestone-duedate'></div></td>");
						if (mm.dueDate < now && mm.status != "Done") {
							dateCell.find(".milestone-duedate").addClass("overdue");
						}
						row.append(dateCell);
						row.append("<td class='milestone-delete-cell'><span class='glyphicon glyphicon-trash gray clickable delete-milestone' data-milestoneid='" + mm.milestoneId + "'></span></td>");

						$(detailsBox).append(row);
						var dateElement = dateCell.find('.milestone-duedate');
						dateElement.data("milestoneid", mm.milestoneId);
						generateDatepickerLocalize(dateElement, mm.dueDate, "milestone-date-" + mm.milestoneId)
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
			}

			var anyMilestones = rock.markers.length > 0;
			if (anyMilestones) {
				var dateEllapseMarker = $("<div class='milestone-date-marker'></div>");
				if (rock.anyPastDue) {
					dateEllapseMarker.addClass("past-due");
					if (!rock.allPastDueDone) {
						dateEllapseMarker.addClass("status-NotDone");
					}
				}
				if (anyMilestones && (rock.allDone || rock.rockStatus == "Complete")) {
				    dateEllapseMarker.addClass("status-Done");
				    if (rock.rockStatus == "Complete") {
				        dateEllapseMarker.removeClass("status-NotDone");
				    }
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



			fixRocksDetailsBoxSize();
			setTimeout(fixRocksDetailsBoxSize, 1);
		}
	}
});

setMilestone = window.milestoneAccessor.setMilestone;
deleteMilestone = window.milestoneAccessor.deleteMilestone;
getMilestone = window.milestoneAccessor.getMilestone;
getMilestones = window.milestoneAccessor.getMilestones;
recalculateMilestones = window.milestoneAccessor.recalculateMarkers;

function updateRocks(html) {
	$(".rocks-container").html(html);
	$(".rock-empty-holder").addClass("hidden");
	$(".rocks-container").removeClass("hidden");
	console.log("accept milestones javascript");
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