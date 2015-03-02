var oldIssueList;
$(function () {
	oldIssueList = $(".ids-list").clone(true);

	$("body").on("click", ".ids-list>.issue-row", function() {
		var message = $(this).data("message");
		var details= $(this).data("details");

		var found = $(this).find(">.dd-list").clone();
		$("#issueDetails").html("");
		
		$("#issueDetails").append("<div class='heading'><h4>" + message + "</h2></div>");
		$("#issueDetails").append(found);
		
		$("#issueDetails").append("<textarea class='details'>" + details + "</textarea>");
	});
});

function deserializeIssues(selector, issueList) {
	debugger;
	var sub = "";
	for (var i = 0; i < issueList.issues.length; i++) {
		sub += constructRow(issueList.issues[i]);
	}
	$(selector).html(sub);

}

function constructRow(issue) {
	var sub = "";
	if (issue.children) {
		for (var i = 0; i < issue.children.length; i++) {
			sub += constructRow(issue.children[i]);
		}
	}

	return '<li class="issue-row dd-item" data-issue="' + issue.issue + '"  data-message="' + issue.message + '">'
		+ '<div class="move-icon noselect dd-handle">'
		+ '<span class="outer icon fontastic-icon-three-bars icon-rotate"></span>'
		+ '<span class="inner icon fontastic-icon-primitive-square"></span>'
		+ '</div>'
		+ '<div class="message">'
		+ issue.message
		+ '</div>'
		+ '<ol class="dd-list">'
		+ sub
		+ '</ol>'
		+ '</li>';
}

function updateIssuesList(recurrenceId, issueRow) {
	var d = { issues: $(issueRow).nestable('serialize'), connectionId: $.connection.hub.id };
	console.log(d);
	var that = issueRow;
	$.ajax({
		url: "/l10/UpdateIssues/" + recurrenceId,
		data: JSON.stringify(d),
		contentType: "application/json; charset=utf-8",
		method: "POST",
		success: function (d) {
			if (!d.Error) {
				oldIssueList = $(".ids-list").clone(true);
			} else {
				showJsonAlert(d, false, true);
				$(that).html("");
				setTimeout(function () {
					$('.dd').html(oldIssueList);
					oldIssueList = $(".ids-list").clone(true);
				}, 1);
			}
		},
		error: function (a, b) {
			clearAlerts();
			showAlert(a.statusText || b);
			$('.dd').html("");
			setTimeout(function () {
				$('.dd').html(oldIssueList);
				oldIssueList = $(".ids-list").clone(true);
			}, 1);
		}
	});
}