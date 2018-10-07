/*
 new MilestoneAccessor(milestonesJson, rockJson, options)
 
 options={
		callback: { 
			remove: function(milestoneId),
			set: function(milestone),
			recalculate : function(model),
			recalculateRock : function(rock,model)
		} 
 }
 
 output = {
			recreate: (bool),
			rocks : [{ 
						allDone: (bool),
						anyPastDue: (bool), 
						allPastDueDone: (bool), 
						nowPercentage: (decimal),
						duePercentage: (decimal),
						rockId: (long),
						markers: [{
							milestoneId: (long),
							name: (string),
							pastDue: (bool),
							futureDue: (bool),
							dueDate: (date),
							done:  (bool),
							status: (string),
							percentage:  (decimal),
							milestone: (obj),
						}],
					}]
 }
 
 */

function MilestoneAccessor(milestonesList, rockList, options) {
	
	options = options || {};
	options.callbacks = options.callbacks || {};
	options.callbacks.remove = options.callbacks.remove || function (milestoneId) { };
	options.callbacks.set = options.callbacks.set || function (milestone) { };
	options.callbacks.recalculate = options.callbacks.recalculate || function (model) { };
	options.callbacks.recalculateRock = options.callbacks.recalculateRock || false;

	this.options = options;

	function _getRocks() {
		if (typeof (rockList) === "function")
			return rockList();
		return rockList;
	}

	function _getMilestones() {
		if (typeof (milestonesList) === "function")
			return milestonesList();
		return milestonesList;
	}


	function setMilestone(milestone) {
		var found = getMilestone(milestone.Id);
		if (found) {
			$.extend(found, milestone);
		} else {
			_getMilestones().push(milestone);
		}
		recalculateMarkers();
	}

	function deleteMilestone(milestoneId) {
		var ms = _getMilestones();
		for (var i = 0; i < ms.length; i++) {
			var mm = ms[i];
			if (mm.Id == milestoneId) {
				_getMilestones().splice(i, 1);
				break;
			}
		}
		options.callbacks.remove(milestoneId);
		recalculateMarkers(true);
	}

	function getMilestone(milestoneId) {
		var milestones = _getMilestones();
		for (var i in milestones) {
			if (arrayHasOwnIndex(milestones, i)) {
				var milestone = milestones[i];
				if (milestone.Id == milestoneId)
					return milestone;
			}
		}
		return false;
	}

	function getMilestones(rockId) {
		var results = [];
		var milestones = _getMilestones();
		var allResults = typeof (rockId) === "undefined";
		for (var i in milestones) {
			if (arrayHasOwnIndex(milestones, i)) {
				var milestone = milestones[i];
				if (allResults || milestone.RockId == rockId)
					results.push(milestone);
			}
		}
		results.sort(function (a, b) {
			return parseJsonDate(a.DueDate) - parseJsonDate(b.DueDate);
		});
		return results;
	}

	function recalculateMarkers(recreate) {
		if (typeof (recreate) === "undefined")
			recreate = true;

		var rockModels = [];

		var rockIds = [];
		var ms = getMilestones();

		function roundDate(date) {
			return new Date(Math.floor(+date / 8.64e+7) * 8.64e+7);
		}
			
		var now = roundDate(new Date());

		var minimumDate = now;
		var maximumDate = now;

		for (var m in ms) {
			if (arrayHasOwnIndex(ms, m)) {
				var mm = ms[m];
				minimumDate = Math.min(parseJsonDate(mm.DueDate, true), minimumDate);
				maximumDate = Math.max(parseJsonDate(mm.DueDate, true), maximumDate);
			}
		}


		var rocks = _getRocks();

		for (var r in rocks) {
			if (arrayHasOwnIndex(rocks, r)) {
				var rr = rocks[r];
				var dueDateStr = rr.DueDate;
				if (typeof (dueDateStr) !== "undefined") {
					var dueDate = parseJsonDate(dueDateStr, true);
					minimumDate = Math.min(dueDate, minimumDate);
					maximumDate = Math.max(dueDate, maximumDate);
				}
			}
		};

		var extra = 0;
		minimumDate = minimumDate - extra;

		minimumDate = roundDate(minimumDate);
		maximumDate = roundDate(maximumDate);

		var sliderPaddingLeft = 0;		
		var sliderPaddingRight = 0;		
		var sliderPaddingSkipRight = 0;	

		function calculateMarkerPercentage(date) {
			var percentage = .5;
			if (maximumDate != minimumDate) {
				percentage = (roundDate(date) - minimumDate) / (maximumDate - minimumDate);
			}
			//percentage to pad
			//percentage += sliderPaddingLeft;
			return percentage;
		}

		for (var r in rocks) {
			if (arrayHasOwnIndex(rocks, r)) {
				var rr = rocks[r];

				var rockModel = {
					allDone: false,
					anyPastDue: false,
					allPastDueDone: false,
					nowPercentage: 0,
					rockId: rr.Id,
					markers: [],
                    rockStatus: rr.Status
				};
				var markers = [];
				var rockId = rr.Id;
				var ms = getMilestones(rockId);			

				var allPastDueDone = true;
				var allDone = true;
				var anyPastDue = false;

				function placeMarker(dueDate, status, obj) {
					var output = {
						milestoneId: obj.Id,
						name : obj.Name,
						pastDue: false,
						dueDate: dueDate,
						futureDue: false,
						done: false,
						status: "undefined",
						percentage: .5,
						milestone: obj,
					};
					var statusUndefined = typeof (status) === "undefined";
					output.percentage = calculateMarkerPercentage(dueDate);

					if (dueDate < now) {
						anyPastDue = true;
						output.pastDue = true;
						if (!statusUndefined && status != "Done") {
							allPastDueDone = false;
						}
					} else {
						output.futureDue = true;
					}
					if (!statusUndefined && status != "Done") {
						allDone = false;
					}
					if (status == "Done") {
						output.done = true;
					}
					if (!statusUndefined) {
						output.status = status;
					}
					return output;
				}

				var anyMilestones = ms.length > 0;

				//Markers
				for (var m in ms) {
					if (arrayHasOwnIndex(ms, m)) {
						var mm = ms[m];
						var dueDate = parseJsonDate(mm.DueDate, true);
						var marker = placeMarker(dueDate, mm.Status, mm);
						markers.push(marker);						
					}
				}
				
				if (anyMilestones) {
					if (anyPastDue) {
						rockModel.anyPastDue = true;
						if (allPastDueDone) {
							rockModel.allPastDueDone = true;
						}
					}
					if (anyMilestones && allDone) {
						rockModel.allDone = true;
					}
					var startP = 0;
					var nowP = calculateMarkerPercentage(now);
					var endP = 1;
					var dueP = endP;

					var dueDateStr = rr.DueDate;
					if (typeof (dueDateStr) !== "undefined") {
						var dueDate = parseJsonDate(dueDateStr, true);
						dueP = calculateMarkerPercentage(dueDate);
					}

					rockModel.nowPercentage = ( nowP) /*- startP*/;
					rockModel.duePercentage =(calculateMarkerPercentage(rr.DueDate));
				}
				rockModel.markers = markers;
				rockModels.push(rockModel);
			}
		}
		var output = {
			rocks: rockModels,
			recreate: recreate
		};
		options.callbacks.recalculate(output);
		if (options.callbacks.recalculateRock!=false) {		
			for (var r in output.rocks) {
				if (arrayHasOwnIndex(output.rocks, r)) {
					var rr = output.rocks[r];
					options.callbacks.recalculateRock(rr,output);
				}
			}
		}
		return output;
	}
	
	this.setMilestone = setMilestone;
	this.deleteMilestone = deleteMilestone;
	this.getMilestone = getMilestone;
	this.getMilestones = getMilestones;
	this.recalculateMarkers = recalculateMarkers;

}
