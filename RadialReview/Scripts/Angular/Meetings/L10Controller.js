angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', 'signalR', 'meetingDataUrlBase', 'meetingId',"meetingCallback","$compile","$sce",
function ($scope, $http, $timeout, signalR, meetingDataUrlBase, meetingId,meetingCallback,$compile,$sce) {
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
					$(".rt").prop("disabled", false);
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

	function removeDeleted(model) {
		for (var key in model) {
			if (model[key] == "`delete`")
				model[key] = null;
			if (typeof (model[key]) == 'object')
				removeDeleted(model[key]);
		}
	}

	function baseExtend(dst, objs, deep) {
		var h = dst.$$hashKey;

		for (var i = 0, ii = objs.length; i < ii; ++i) {
			var obj = objs[i];
			if (!angular.isObject(obj) && !angular.isFunction(obj)) continue;
			var keys = Object.keys(obj);
			for (var j = 0, jj = keys.length; j < jj; j++) {
				var key = keys[j];
				var src = obj[key];
				if (deep && angular.isObject(src)) {
					if (src.AngularList) {
						//Special AngularList Object
						if (src.UpdateMethod == "Add") {
							dst[key] = dst[key].concat(src.AngularList);
						} else if (src.UpdateMethod == "ReplaceAll") {
							dst[key] = src.AngularList;
						}
					} else {
						if (!angular.isObject(dst[key]))
							dst[key] = angular.isArray(src) ? [] : {};

						if (angular.isArray(dst[key])) {
							dst[key] = dst[key].concat(src);
						} else {
							if (dst[key].Key == src.Key)
								baseExtend(dst[key], [src], true);
							else
								dst[key] = src;
						}
					}
				} else {
					dst[key] = src;
				}
			}
		}
		if (h) {
			dst.$$hashKey = h;
		} else {
			delete dst.$$hashKey;
		}
		return dst;
	}

	function convertDates(obj) {
		var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
		//var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;
		var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
		for (var key in obj) {
			var value = obj[key];
			var type = typeof (value);
			if (obj[key]==null) {
				//Do nothing
			}else if (type == 'string' && dateRegex1.test(value)) {
				obj[key] = new Date(parseInt(value.substr(6)));
			} else if (type == 'string' && dateRegex2.test(value)) {
				obj[key] = new Date(obj[key]);
			} else if (obj[key].getDate!==undefined) {
				obj[key] =new Date(obj[key].getTime() /*- obj[key].getTimezoneOffset() * 60000*/);
			} else if (type == 'object') {
				convertDates(value);
			}
		}
	};

	function updateScorecard(data) {
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

	function update(data, status) {

		console.log("update:");
		console.log(data);
		//angular.merge($scope.model, data);
		baseExtend($scope.model, [data], true);

		updateScorecard(data);

		convertDates($scope.model);
		removeDeleted($scope.model);
	}

	var meetingHub = signalR('meetingHub', function (connection, proxy) {
		console.log('trying to connect to service');
		$scope.connectionId = connection.id;
		rejoin(connection, proxy, function () {
			console.log("Logged in: " + connection.id);
		});
	});

	meetingHub.on('update', update);

	
	$scope.functions = {};
	$scope.filters = {};
	$scope.functions.reload = function (reload) {
	    if (typeof (reload) == "undefined") {
	        reload = true;
	    }
	    if (reload) {
	        console.log("reloading...");
	        $http({ method: 'get', url: meetingDataUrlBase + $scope.meetingId })
            .success(function (data, status) {
                convertDates(data);
                $scope.model = data;
                /*var sevenMin = moment().subtract('days',7).toDate();
                var sevenMax = moment().add('days',9).toDate();
    
                debugger;
                if (!$scope.model)
                    $scope.model = {};
                $scope.model.date = { startDate: sevenMin, endDate: sevenMax };
                */

                if (meetingCallback) {
                    setTimeout(function () {
                        meetingCallback();
                    }, 1);
                }

                //update(data, status);
            }).error(function (data, status) {
                //$scope.model = {};
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
		d.setDate(d.getDate() - days);
		return d;
	};
	$scope.functions.scorecardId = function (s, row, column) {
		if (!s)
			return "sc_" + row + "_" + column;
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

	//debugger;
	//for(var i =0;i<scope.model.)

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

		//for (var s in scores) {
		//		var score = $scope.model.Lookup[scores[s].Key];
		//		if (!(week in $scope.ScoreLookup))
		//			$scope.ScoreLookup[week] = {};
		//		$scope.ScoreLookup[week][measurableId] = score;
		//	}
		//}
		
		if (week in $scope.ScoreLookup && measurableId in $scope.ScoreLookup[week]) {
			var lu = $scope.model.Lookup[$scope.ScoreLookup[week][measurableId]];
			if (lu != null)
				return lu;
		}
			

		//console.log("miss ls " + week + " " + measurableId);

		//var lu = $scope.functions.lookupScoreFull(week, measurableId);
		//if (lu != null)
		//	return $scope.model.Lookup[$scope.ScoreLookup[week][measurableId]];d
		//for (var s in scores) {
		//	var score = $scope.model.Lookup[scores[s].Key];
		//	if (score.ForWeek == week && score.Measurable.Id == measurableId) {
		//		if (!(week in $scope.ScoreLookup))
		//			$scope.ScoreLookup[week] = {};
		//		$scope.ScoreLookup[week][measurableId] = score;

		//		return score;
		//	}
		//}

		var wKey = week;
		if (!(wKey in $scope.proxyLookup))
			$scope.proxyLookup[wKey] = {};
		if (!(measurableId in $scope.proxyLookup[wKey]))
			$scope.proxyLookup[wKey][measurableId] = { Id: -1, Type: "AngularScore", Measurable: { Id: measurableId }, ForWeek: week, Measured: null };

		return $scope.proxyLookup[wKey][measurableId];
		//return  { Id: -1, Type: "AngularScore", Measurable: { Id: measurableId }, ForWeek: week, Measured: null };
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

	$scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
		showModal(title, pull, push, callback, validation, onSuccess);
	};
	//$scope.$watch('date', function (newDate) {
	//	console.log('New date set: ', newDate);
	//}, false);

	//$scope.$watch('Complete', function (newDate) {
	//	console.log('Complete: ', newDate);
	//}, false);
}]);