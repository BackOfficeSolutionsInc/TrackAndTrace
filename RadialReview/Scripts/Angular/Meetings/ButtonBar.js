angular.module('buttonbar', []).directive("buttonbar", function() {
	return{
		restrict: "E",
		scope: {
			recurrence:"@recurrence",
			meeting:"@meeting"
		},
		template:
			"<div class=\"btn-group\">"+
				"<div class=\"btn btn-default btn-xs notesButton\" >"+
				"<span class=\"icon fontastic-icon-align-left-2\"><\/span> Notes"+
				"<\/div>"+
			"<\/div>"+
			"<div class=\"btn-group\">"+
				"<div class=\"btn btn-default btn-xs issuesModal\""+
				" data-method=\"createissue\""+
				" data-meeting=\"{{meeting}}\""+
				" data-recurrence=\"{{recurrence}}\">"+
					"<span class=\"icon fontastic-icon-pinboard\"><\/span> New Issue"+
				"<\/div>"+
				"<div class=\"btn btn-default btn-xs todoModal\""+
				" data-method=\"createtodo\""+
				" data-meeting=\"{{meeting}}\""+
				" data-recurrence=\"{{recurrence}}\">"+
					"<span class=\"glyphicon glyphicon-unchecked\"><\/span> New To-Do"+
				"<\/div>"+
			"<\/div>"+
			""+
			"<div class=\"btn-group\">"+
				"<a class=\"btn btn-default btn-xs\" href=\"/L10/Edit/{{recurrence}}?return=meeting\" >"+
				"<span class=\"glyphicon glyphicon-cog\"><\/span>"+
				"<\/a>"+
			"<\/div>"

	};
});