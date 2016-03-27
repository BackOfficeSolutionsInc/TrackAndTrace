angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', 'radial', 'meetingDataUrlBase', 'meetingId',"meetingCallback","$compile","$sce",
function ($scope, $http, $timeout, radial, meetingDataUrlBase, meetingId,meetingCallback,$compile,$sce) {
	 $scope.trustAsResourceUrl = $sce.trustAsResourceUrl;
	if (meetingId == null)
		throw Error("MeetingId was empty");
	$scope.disconnected = false;
	$scope.meetingId = meetingId;

	function rejoin(connection, proxy, callback) {
		try {
			if (proxy) {
				proxy.invoke("join", $scope.meetingId, connection.id).done(function () {
					console.log("rejoin");
					//$(".rt").prop("disabled", false);
					if (callback) {
						callback();
					}
					if ($scope.disconnected) {
						clearAlerts();
						showAlert("Reconnected.", "alert-success", "Success");
					}
					$scope.disconnected = false;
				});
			}
		} catch (e) {
			console.error(e);
		}
	}

	function updateScorecard(data) {
	    console.log("updateScorecard");
	    $scope.ScoreLookup = $scope.ScoreLookup || {};
		var lu = data.Lookup;
		if (lu != null) {
			for (var key in lu) {
				var value = lu[key];
				if (value != null && value.Type == "AngularScore") {
					if (!(value.ForWeek in $scope.ScoreLookup))
						$scope.ScoreLookup[value.ForWeek] = {};

					$scope.ScoreLookup[value.ForWeek][value.Measurable.Reference.Id] = value.Key;
				}
			}
		}
	};

	//function update(data, status) {

	//	console.log("update:");
	//	console.log(data);
	//	//angular.merge($scope.model, data);
	//	baseExtend($scope.model, [data], true);

	//	updateScorecard(data);

	//	convertDates($scope.model);
	//	removeDeleted($scope.model);
	//}

	//var meetingHub = signalR('meetingHub', function (connection, proxy) {
	//	console.log('trying to connect to service');
	//	$scope.connectionId = connection.id;
	//	rejoin(connection, proxy, function () {
	//		console.log("Logged in: " + connection.id);
	//	});
	//});

	//meetingHub.on('update', update);

	var r = radial($scope, 'meetingHub', rejoin);

	r.updater.postExtend = updateScorecard;
	//r.rejoin = rejoin;
	
	$scope.functions = {};
	$scope.filters = {};
	$scope.functions.reload = function (reload) {
	    if (typeof (reload) == "undefined") {
	        reload = true;
	    }
	    if (reload) {

	        if (!window.tzoffset)
	        {
	            var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
	            window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
                             (jul.getTime() % 24 * 60 * 60 * 1000)
                             ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
	        }


	        console.log("reloading...");
	        $http({ method: 'get', url: meetingDataUrlBase + $scope.meetingId + "?_clientTimestamp="+((+new Date())+(window.tzoffset*60*1000))})
            .success(function (data, status) {
               // r.updater.clearAndApply(data, status);
                r.updater.convertDates(data);
                $scope.model = data;
                
                if (meetingCallback) {
                    setTimeout(function () {
                        meetingCallback();
                    }, 1);
                }
            }).error(function (data, status) {
                console.log("Error");
                console.error(data);
            });
	    }
    }

    $scope.functions.reload(true);

	$scope.functions.setHtml = function(element,data) {
		var newstuff = element.html(data);
        $compile(newstuff)($scope); // loads the angular stuff in the new markup
		$scope.$apply();
	};

	$scope.functions.setPage = function (page) {
		$http.get("/meeting/SetPage/" + $scope.model.RecurrenceId + "?page=" + page + "&connection=" + $scope.connectionId);
		if (!$scope.model.FollowLeader || $scope.model.IsLeader) {
			$scope.model.CurrentPage = page;
		}
	};

	$scope.functions.subtractDays = function (date, days) {
	    var d = new Date(date);
	    d = new Date(moment(d).add(new Date().getTimezoneOffset(), "minutes").valueOf())
		d.setDate(d.getDate() - days);
		return d;
	};
	$scope.functions.scorecardId = function (s, measurableId, weekId) {
		if (!s)
		    return "sc_" + measurableId + "_" + weekId;
		return "sc_" + s.Id;
	};
	$scope.functions.scorecardColor = function (s) {
		if (!s)
			return "";

		var v = s.Measured;
		var goal = s.Measurable.Target;
		var dir = s.Measurable.Direction;
		if (!$.trim(v)) {
			return "";
		} else if ($.isNumeric(v)) {
			if (dir == "GreaterThan" || dir == 1) {
				if (+v >= +goal)
					return "success";
				else
					return ("danger");
			} else {
				if (+v < +goal)
					return ("success");
				else
					return ("danger");
			}

		} else {
			return ("error");
		}
	};

	$scope.proxyLookup = {};

	$scope.ScoreIdLookup =null;


	$scope.functions.getFcsa = function(measurable) {
		if (measurable.Modifiers == "Dollar") {
			return { prepend: "$" };
		}else if (measurable.Modifiers == "Percent") {
			return { append: "%" };
		}
	};

	$scope.functions.lookupScoreFull = function(week, measurableId,scorecardKey) {
	    var scorecard = $scope.model.Lookup[scorecardKey];
		var scores = scorecard.Scores;
		for (var s in scores) {
			var score = $scope.model.Lookup[scores[s].Key];
			if (score.ForWeek == week && score.Measurable.Id == measurableId) {
				if (!(week in $scope.ScoreLookup))
					$scope.ScoreLookup[week] = {};
				$scope.ScoreLookup[week][measurableId] = scores[s].Key;

				return  scores[s].Key;
			}
		}
		return null;
	};

	$scope.functions.lookupScore = function (week, measurableId,scorecardKey) {

		if ($scope.ScoreLookup == null) {
			$scope.ScoreLookup = {};
			var scorecard = $scope.model.Lookup[scorecardKey];
			//var scores = scorecard.Scores;
			for (var w in scorecard.Weeks) {
				var wn = scorecard.Weeks[w].ForWeekNumber;
				$scope.ScoreLookup[wn] = {};
				for (var m in scorecard.Measurables) {
					var mn = scorecard.Measurables[m].Id;
					$scope.ScoreLookup[wn][mn] = $scope.functions.lookupScoreFull(wn, mn, scorecardKey);
				}
			}
		}
		
		if (week in $scope.ScoreLookup && measurableId in $scope.ScoreLookup[week]) {
			var lu = $scope.model.Lookup[$scope.ScoreLookup[week][measurableId]];
			if (lu != null)
				return lu;
		}

		var wKey = week;
		if (!(wKey in $scope.proxyLookup))
			$scope.proxyLookup[wKey] = {};
		if (!(measurableId in $scope.proxyLookup[wKey]))
			$scope.proxyLookup[wKey][measurableId] = { Id: -1, Type: "AngularScore", Measurable: { Id: measurableId }, ForWeek: week, Measured: null };

		return $scope.proxyLookup[wKey][measurableId];
	};

	$scope.functions.updateComplete = function (self) {
		var instance = self.todo;
		if (!instance)
			instance = self.issue;

		if (instance.Complete) {
			instance.CompleteTime = new Date();
		} else {
			instance.CompleteTime = null;
		}
	};

	//var sevenMin = moment().subtract('days', 6).toDate();
	//var sevenMax = moment().add('days', 2).toDate();
    	
	$scope.now = moment();

	$scope.rockstates = [{ name: 'Off Track', value: 'AtRisk' }, { name: 'On Track', value: 'OnTrack' }, { name: 'Complete', value: 'Complete' }];

	$scope.opts = {
		ranges: {
			'Incomplete': [moment().add('days', 1), moment().add('days', 9)],
			'Today': [moment().subtract('days', 1), moment().add('days', 9)],
			'Last 7 Days': [moment().subtract('days', 6), moment().add('days',9)],
			'Last 14 Days': [moment().subtract('days', 13), moment().add('days', 9)],
			'Last 30 Days': [moment().subtract('days', 29), moment().add('days', 9)],
			//'Last 60 Days': [moment().subtract('days', 59), moment().add('days',1)],
			'Last 90 Days': [moment().subtract('days', 89), moment().add('days',9)]// [sevenMin, sevenMax]
		},
		separator: '  to  ',
		showDropdowns: true,
		format: 'MMM DD, YYYY',
		opens: 'left'
	};

	
	$scope.filters.byRange = function (fieldName, minValue, maxValue, forceMin) {
		if (minValue === undefined) minValue = -Number.MAX_VALUE;
		if (maxValue === undefined) maxValue = Number.MAX_VALUE;
		if (typeof (forceMin) !== "undefined") {
			minValue = Math.min(minValue, maxValue - forceMin * 24 * 60 * 60 * 1000);
		}

		return function predicateFunc(item) {
			var d = item[fieldName];
			if (!d) return true;//d = moment().add('days', 1).toDate();
			if (d instanceof Date) d = d.getTime();
			if (minValue instanceof Date) minValue = minValue.getTime();
			if (maxValue instanceof Date) maxValue = maxValue.getTime();
			return minValue <= d && d <= maxValue || moment(d).format("MMDDYYYY")==moment(maxValue).format("MMDDYYYY");
		};
	};

	$scope.functions.sendUpdate = function (self) {
		var dat = angular.copy(self);
		var _clientTimestamp = new Date().getTime();

		$http.post("/L10/Update" + self.Type + "?connectionId=" + $scope.connectionId+"&_clientTimestamp="+_clientTimestamp, dat).error(function (data) {
			showJsonAlert(data, true, true);
		});
	};

	//$scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
	//	showModal(title, pull, push, callback, validation, onSuccess);
	//};
	
}]);