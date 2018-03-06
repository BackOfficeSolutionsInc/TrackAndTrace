angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', '$location',
    'radial', 'meetingDataUrlBase'/*, 'dateFormat'*/, 'recurrenceId', "meetingCallback", "$compile", "$sce", "$q", "$window", "$filter",
function ($scope, $http, $timeout, $location, radial, meetingDataUrlBase, recurrenceId, meetingCallback, $compile, $sce, $q, $window, $filter) {

	$scope.trustAsResourceUrl = $sce.trustAsResourceUrl;
    if (recurrenceId == null)
		throw Error("recurrenceId was empty");
	$scope.disconnected = false;
	$scope.recurrenceId = recurrenceId;

	//$scope.window = $window;

	$scope.dateFormat = window.dateFormat || "MM-dd-yyyy";

	function rejoin(connection, proxy, callback) {
		try {
			if (proxy) {
				proxy.invoke("join", $scope.recurrenceId, connection.id).done(function () {
					console.log("Rejoin completed.");
					//$(".rt").prop("disabled", false);
					if (callback) {
						callback();
					}
					if ($scope.disconnected) {
						clearAlerts();
						showAlert("Reconnected.", "alert-success", "Success", 1000);
					}
					$scope.disconnected = false;
				});
			}
		} catch (e) {
			console.error(e);
		}
	}

	function updateScorecard(data) {
		console.log("Updating Scorecard.");
		$scope.ScoreLookup = $scope.ScoreLookup || {};
		var luArr = [];
        if (data.Scorecard != null && data.Scorecard.Scores != null) {

			luArr.push(data.Scorecard.Scores);
		}

		if (typeof (data.L10Scorecards) !== "undefined") {
			for (var sc in data.L10Scorecards) {
				if (arrayHasOwnIndex(data.L10Scorecards, sc)) {
					var i = data.L10Scorecards[sc];
					if (typeof (i.Contents) !== "undefined" && typeof (i.Contents.Scores) !== "undefined") {
						luArr.push(i.Contents.Scores);
					}
				}
			}
		}

		for (var luidx in luArr) {
			if (arrayHasOwnIndex(luArr, luidx)) {
				var lu = luArr[luidx];
				for (var key in lu) {
					if (arrayHasOwnIndex(lu, key)) {
						var value = lu[key];
						if (!(value.ForWeek in $scope.ScoreLookup))
							$scope.ScoreLookup[value.ForWeek] = {};
						if (value.Measurable) {
							var foundKey = $scope.ScoreLookup[value.ForWeek][value.Measurable.Id];
							var newKey = value.Key;
							if (typeof (foundKey) !== "undefined" && foundKey.localeCompare(value.Key) > 0) {
								debugger;
								newKey = foundKey;
							}
							$scope.ScoreLookup[value.ForWeek][value.Measurable.Id] = newKey;
						}
					}
				}
			}
		}
	};

	var r = radial($scope, 'meetingHub', rejoin);

	var cpr = radial($scope, {
		hubName: "coreProcessHub"
	});

	r.updater.postResolve = updateScorecard;

	$scope.functions = $scope.functions || {};
	$scope.filters = $scope.filters || {};

	if (typeof (dataDateRange) === "undefined") {
		dataDateRange = {};
	}
	if (typeof (dataDateRange.startDate) === "undefined") {
		dataDateRange.startDate = moment().add('days', 1).toDate();//.subtract('days', 6).toDate();
	} else {
		dataDateRange.startDate = moment(dataDateRange.startDate).toDate();
	}
	if (typeof (dataDateRange.endDate) === "undefined") {
		dataDateRange.endDate = moment().add('days', 1).toDate();
	} else {
		dataDateRange.endDate = moment(dataDateRange.endDate).toDate();
	}
	$scope.model = $scope.model || {};
	$scope.model.dataDateRange = dataDateRange;
	$scope._alreadyLoaded = { startDate: dataDateRange.startDate.getTime(), endDate: dataDateRange.endDate.getTime() };
	$scope.$watch('model.dataDateRange', function (newValue, oldValue) {
		//console.log("watch dataDateRange");
		if (newValue.startDate < $scope._alreadyLoaded.startDate) {
			var range1 = {
				startDate: newValue.startDate,
				endDate: Math.min($scope._alreadyLoaded.startDate, newValue.endDate)
			};
			$scope.functions.reload(true, range1);
		}
		if (newValue.endDate > $scope._alreadyLoaded.endDate) {
			var range2 = {
				startDate: Math.max(newValue.startDate, $scope._alreadyLoaded.endDate),
				endDate: newValue.endDate
			};
			$scope.functions.reload(true, range2);
		}
		$scope._alreadyLoaded.startDate = Math.min($scope._alreadyLoaded.startDate, newValue.startDate);
		$scope._alreadyLoaded.endDate = Math.max($scope._alreadyLoaded.endDate, newValue.endDate);
	});
	var dateToNumber = function (date) {
		var type = typeof (date);
        if (type == 'number') {
			return date;
		} else if (typeof (date._d) !== 'undefined') {
			return +date;
		} else if (date.getDate !== undefined) {
			return date.getTime();
		}
		console.error("Can't process:" + date);
	}

	$scope.functions.showFormula = function (id) {
	    setFormula(id);
	};

	$scope.functions.startCoreProcess = function (coreProcess) {
		$http.get("/CoreProcess/Process/StartProcess/" + coreProcess.Id)
            .then(function (data) {
            	$scope.functions.showAlert(data, true);
            }, function (data) {
            	$scope.functions.showAlert(data);
            });
	}

	$scope.functions.adjustToMidnight = function (date) {
		//adjusts local time to end of day local time
		return new Date(((+date) + (24 * 60 * 60 * 1000 - 1)));
	}

	$scope.$watch('model.LoadUrls.length', function (newValue, oldValue) {
		//console.log("watch LoadUrls");
        if (newValue != 0 && $scope.model && $scope.model.LoadUrls && $scope.model.LoadUrls.length) {
			var urls = [];
			for (var u in $scope.model.LoadUrls) {
				if (arrayHasOwnIndex($scope.model.LoadUrls, u)) {
                    if ($scope.model.LoadUrls[u].Data != null) {
						urls.push($scope.model.LoadUrls[u].Data);
					}
					$scope.model.LoadUrls[u].Data = null;
				}
			}
			$scope.model.LoadUrls = [];
			$timeout(function () {
				for (var u in urls) {
					if (arrayHasOwnIndex(urls, u)) {
						loadDataFromUrl(urls[u]);
					}
				}
			}, 10);
		}
	});

	$scope.$watch('model.Focus', function (newValue, oldValue) {
		//console.log("watch Focus");
		if (newValue) {
			var setFocus = function (count) {
				if (!count)
					count = 1;
				if (count > 5) {
					$scope.model.Focus = null;
					return;
				}
				try {
					var toFocus = $($scope.model.Focus);
					console.log("Setting Focus: ", toFocus);
					if (toFocus.length > 0) {
						$($scope.model.Focus).focus();
						$scope.model.Focus = null;
						return;
					} else {
						$timeout(function () {
							setFocus(count + 1);
						}, 20);
					}
				} catch (e) {
					console.error("Set Focus", e);
				}
				$scope.model.Focus = null;
			};
			setFocus(1);
		}
	});

	//var tzoffset = r.updater.tzoffset;
	var firstLoad = true;
	var canUseInitialValue = true;
	function loadDataFromUrl(url) {
		var stD = new Date();
		//console.log("L10Controller-loadDataFromUrl", +new Date());

		var processSuccess = function (data, status) {
			//console.log("L10Controller-loadDataFromUrl-success", +new Date());
			//console.log("A dur: " + (+(new Date() - stD)));
			var ddr = undefined;
			if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) !== "undefined")
				ddr = $scope.model.dataDateRange;

			r.updater.convertDates(data);

			if (firstLoad) {
				r.updater.clearAndApply(data);
			} else {
				r.updater.applyUpdate(data);
			}

			if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) === "undefined")
				$scope.model.dataDateRange = ddr;

			if (meetingCallback) {
				meetingCallback();
			}

			firstLoad = false;
			$scope.isReloading = false;

		};

		var processError = function (a, b, c, d, e, f) {
			showAngularError(a, b, c, d, e, f);
			$scope.isReloading = false;
		};

		if (canUseInitialValue && $window.InitialModel) {
			console.info("Using InitialModel:", $window.InitialModel);
			var initModel = $window.InitialModel
			if (typeof (initModel) === "function") {

				var sPS = function (data) {
					$scope.$apply(function () {
						processSuccess(data);
					});
				};
				var sPE = function (a, b, c, d, e, f) {
					$scope.$apply(function () {
						processError(a, b, c, d, e, f);
					});
				};
				initModel(sPS, sPE);
			} else {
				processSuccess(initModel);
			}
		} else {
			$http({ method: 'get', url: url })
				.success(processSuccess)
				.error(processError);
		}

		canUseInitialValue = false;
	}

	$scope.isReloading = false;
	$scope.functions.reload = function (reload, range, first) {
		if ($scope.isReloading) {
			//console.log("Already reloading.");
			return;
		}
		$scope.isReloading = true;

		if (typeof (reload) === "undefined") {
			reload = true;
		}
		if (typeof (first) === "undefined") {
			firstLoad = false;
		} else {
			firstLoad = first;
		}
		if (reload) {
			Time.tzoffset();
			console.log("Reloading Data.");
			var url = meetingDataUrlBase;
            if (meetingDataUrlBase.indexOf("{0}") != -1) {
				url = url.replace("{0}", $scope.recurrenceId);
			} else {
				url = url + $scope.recurrenceId;
			}

			//var date = Time.getTimestamp();//((+new Date()) /*+ (window.tzoffset * 60 * 1000)*/);
            //if (meetingDataUrlBase.indexOf("?") != -1) {
			//    url += "&_clientTimestamp=" + date;
			//} else {
			//    url += "?_clientTimestamp=" + date;
			//}

			url = Time.addTimestamp(url);

			if (typeof (range) !== "undefined" && typeof (range.startDate) !== "undefined")
				url += "&start=" + dateToNumber(range.startDate);
			if (typeof (range) !== "undefined" && typeof (range.endDate) !== "undefined")
				url += "&end=" + dateToNumber(range.endDate);
			if (firstLoad)
				url += "&fullScorecard=true";

			loadDataFromUrl(url);
		}
	}

	$scope.functions.orderScorecard = function (reverse) {
		return function (d) {
			if (d && d.ForWeekNumber) {
				if (reverse)
					return -d.ForWeekNumber;
				return d.ForWeekNumber
			} else {
				return 0;
			}
		};
	}


	$scope.functions.reload(true, $scope.model.dataDateRange, true);

	$scope.functions.setHtml = function (element, data) {
		var newstuff = element.html(data);
		$compile(newstuff)($scope); // loads the angular stuff in the new markup
		$scope.$apply();
	};

	$scope.functions.setPage = function (page) {
		//console.info("should we be here?")
		$http.get("/meeting/SetPage/" + $scope.model.RecurrenceId + "?page=" + page + "&connection=" + $scope.connectionId);
		if (!$scope.model.FollowLeader || $scope.model.IsLeader) {
			$scope.model.CurrentPage = page;
		}
	};

	$scope.functions.subtractDays = function (date, days, shift) {
		var d = new Date(date);
			if (typeof (shift) === "undefined" || shift == true)
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
		var goal = s.Target;
		var altgoal = s.AltTarget;
		var dir = s.Direction;

		if (typeof (goal) === "undefined")
			goal = s.Measurable.Target;
		if (typeof (altgoal) === "undefined")
			altgoal = s.Measurable.AltTarget;
		if (typeof (dir) === "undefined")
			dir = s.Measurable.Direction;
		if (typeof (goal) === "undefined") {
			var item = $("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]");
			goal = item.data("goal");
			if (typeof (altgoal) === "undefined")
				altgoal = item.data("alt-goal");
			console.log("goal not found, trying element. Found: " + goal);
		}

		if (!$.trim(v)) {
			return "";
		} else {
			var met = metGoal(dir, goal, v, altgoal);
            if (met == true)
				return "success";
            else if (met == false)
				return "danger";
			else
				return "error";
		}
	};

	$scope.proxyLookup = {};
	$scope.ScoreIdLookup = null;

	$scope.functions.setValue = function (keyStr, value) {
		$scope[keyStr] = value;
	}

	$scope.functions.getFcsa = function (measurable) {
        //if (measurable.Modifiers == "Dollar") {
		//    return { prepend: "$" };
        //} else if (measurable.Modifiers == "Percent") {
		//    return { append: "%" };
        //} else if (measurable.Modifiers == "Euros") {
		//    return { prepend: "€" };
        //} else if (measurable.Modifiers == "Pound") {
		//    return { prepend: "£" };
		//}
		var builder = {
			resize: true,
			localization: $scope.localization
		};

        if (measurable.Modifiers == "Dollar") {
			builder = {
				prepend: "$",
				resize: true,
				localization: $scope.localization
			};
        } else if (measurable.Modifiers == "Percent") {
			builder = {
				append: "%",
				resize: true,
				localization: $scope.localization
			};
        } else if (measurable.Modifiers == "Euros") {
			builder = {
				prepend: "€",
				resize: true,
				localization: $scope.localization
			};
        } else if (measurable.Modifiers == "Pound") {
			builder = {
				prepend: "£",
				resize: true,
				localization: $scope.localization
			};
		}
		return builder;
	};

	$scope.functions.lookupScoreFull = function (week, measurableId, scorecardKey) {
		var scorecard = $scope.model.Lookup[scorecardKey];
		var scores = scorecard.Scores;
		for (var s in scores) {
			if (arrayHasOwnIndex(scores, s)) {
				var score = $scope.model.Lookup[scores[s].Key];
                if (score.ForWeek == week && score.Measurable.Id == measurableId) {
					if (!(week in $scope.ScoreLookup))
						$scope.ScoreLookup[week] = {};
					$scope.ScoreLookup[week][measurableId] = scores[s].Key;
					//if (week==2471 && measurableId==595)
					//	debugger;
					return scores[s].Key;
				}
			}
		}
		return null;
	};

	$scope.functions.lookupScore = function (week, measurableId, scorecardKey) {
        if ($scope.ScoreLookup == null) {
			$scope.ScoreLookup = {};
			var scorecard = $scope.model.Lookup[scorecardKey];
			for (var w in scorecard.Weeks) {
				if (arrayHasOwnIndex(scorecard.Weeks, w)) {
					var wn = scorecard.Weeks[w].ForWeekNumber;
					$scope.ScoreLookup[wn] = {};
					for (var m in scorecard.Measurables) {
						if (arrayHasOwnIndex(scorecard.Measurables, m)) {
							var mn = scorecard.Measurables[m].Id;
							$scope.ScoreLookup[wn][mn] = $scope.functions.lookupScoreFull(wn, mn, scorecardKey);
						}
					}
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
		if (!(measurableId in $scope.proxyLookup[wKey])) {
			var measurable = { Id: measurableId };
			if (("AngularMeasurable_" + measurableId) in $scope.model.Lookup)
				measurable = $scope.model.Lookup["AngularMeasurable_" + measurableId];

			$scope.proxyLookup[wKey][measurableId] = {
				Id: -1, Type: "AngularScore", ForWeek: week, Measured: null,
				Measurable: measurable,
				Target: measurable.Target, // WRONG (scores generated before nearest)
				Direction: measurable.Direction
			};
		}

		return $scope.proxyLookup[wKey][measurableId];
	};
	$scope.functions.updateAssign = function (self, assigned) {
		self.Assigned = assigned || false;
	}

	$scope.functions.updateComplete = function (self) {
		var instance = self.todo;
		if (!instance)
			instance = self.issue;

		if (!instance) {
			instance = self;
		}

		if (instance.Complete) {
			instance.CompleteTime = new Date();
		} else {
			instance.CompleteTime = null;
		}
	};

	$scope.possibleOwners = {};
	$scope.loadPossibleOwners = function (id) {
		if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.Attendees) !== "undefined") {
			$scope.possibleOwners[id] = $scope.model.Attendees;
			$scope.possibleOwners[id];
		} else {
			if (!(id in $scope.possibleOwners)) {
				$scope.possibleOwners[id] = null;
				$http.get('/Dropdown/AngularMeetingMembers/' + id + '?userId=true').success(function (data) {
					r.updater.convertDates(data);
					$scope.possibleOwners[id] = data;
				});
			}
		}
	};
	$scope.possibleDirections = [];
	$scope.loadPossibleDirections = function () {
		return $scope.possibleDirections.length ? null : $http.get('/Dropdown/Type/lessgreater').success(function (data) {
			r.updater.convertDates(data);
			$scope.possibleDirections = data;
		});
	};

	$scope.now = moment();
	$scope.rockstates = [{ name: 'Off Track', value: 'AtRisk' }, { name: 'On Track', value: 'OnTrack' }, { name: 'Done', value: 'Complete' }];

	$scope.opts = {
		ranges: {
			'Active': [moment().add(1, 'days'), moment().add(9, 'days')],
			'Today': [moment().subtract(1, 'days'), moment().add(9, 'days')],
			'Last 7 Days': [moment().subtract(6, 'days'), moment().add(9, 'days')],
			'Last 14 Days': [moment().subtract(13, 'days'), moment().add(9, 'days')],
			'Last 30 Days': [moment().subtract(29, 'days'), moment().add(9, 'days')],
			//'Last 60 Days': [moment().subtract(59,'days'), moment().add('days',1)],
			'Last 90 Days': [moment().subtract(89, 'days'), moment().add(9, 'days')]// [sevenMin, sevenMax]
		},
		separator: '  to  ',
		showDropdowns: true,
		format: 'MMM DD, YYYY',
		opens: 'left'
	};
	$scope.filters.taskFilter = function () {
		return function (item) {
            return !(item.Hide == true || item.Complete == true);
		};
	}
	$scope.filters.byRange = function (fieldName, minValue, maxValue, forceMin, period) {
		if (minValue === undefined) minValue = -Number.MAX_VALUE;
		if (maxValue === undefined) maxValue = Number.MAX_VALUE;


		if (typeof (forceMin) !== "undefined") {
			minValue = Math.min(minValue, maxValue - forceMin * 24 * 60 * 60 * 1000);
		}

		if (typeof (period) !== "undefined") {
            if (period == "Monthly" || period == "Quarterly") {
				minValue = Math.min(minValue, maxValue - (366) * 24 * 60 * 60 * 1000);
			}
		}

		return function predicateFunc(item) {
			var d = item[fieldName];
			if (!d) return true;//d = moment().add('days', 1).toDate();
			if (d instanceof Date) d = d.getTime();
			if (minValue instanceof Date) minValue = minValue.getTime();
			if (maxValue instanceof Date) maxValue = maxValue.getTime();

            if (fieldName == "ForWeek")
				d -= 7 * 24 * 60 * 60 * 1000;

            return minValue <= d && d <= maxValue || moment(d).format("MMDDYYYY") == moment(maxValue).format("MMDDYYYY");
		};
	};

	$scope.selectedTab = $location.url().replace("/", "");

	$scope.filters.completionFilterItems = [
        { name: "Incomplete", value: { Completion: true }, short: "Incomplete" },
	];

	$scope.options = {};
	$timeout(function () {
		$scope.options.l10teamtypes = $scope.loadSelectOptions('/dropdown/type/l10teamtype');
	}, 1);

	$scope.functions.sendUpdate = function (self, args) {
		var dat = angular.copy(self);
		//var _clientTimestamp = new Date().getTime();

		r.updater.convertDatesForServer(dat, Time.tzoffset());
		var builder = "";
		args = args || {};

		if (!("connectionId" in args))
			args["connectionId"] = $scope.connectionId;

		for (var i in args) {
			if (arrayHasOwnIndex(args, i)) {
				builder += "&" + i + "=" + args[i];
			}
		}

		var url = Time.addTimestamp("/L10/Update" + self.Type) + builder;

		$http.post(url, dat)
            .then(function () { }, showAngularError);
	};

	$scope.functions.checkFutureAndSend = function (self) {
		var m = self;
		var icon = { title: "Update options" };

		var values = ["None", "Dollar", "Percent", "Pound", "Euros"];
        var unitTypes = values.map(function (x) { return { text: x, value: x, checked: (x == m.Modifiers) } });

		var fields = [{
			type: "label",
			value: "Update historical goals?"
		}, {
			name: "history",
			value: "false",
			type: "yesno"
		}, {
			type: "label",
			value: "Show Cumulative?"
		}, {
			name: "showCumulative",
			value: self.ShowCumulative || false,
			type: "yesno",
			onchange: function () {
                $("#cumulativeRange").toggleClass("hidden", $(this).val() != "true");
			}
		}, {
            classes: self.ShowCumulative == true ? "" : "hidden",
			name: "cumulativeRange",
			value: self.CumulativeRange || new Date(),
			type: "date"
		}, {
			type: "label",
			value: "Unit type?"
		}, {
			name: "unitType",
			value: self.UnitType,
			type: "select",
			options: unitTypes,
			//	{ text: "None", value: "None" },
			//	{ text: "Dollars", value: "Dollar" },
			//	{ text: "Percent", value: "Percent" },
			//	{ text: "Pounds", value: "Pound" },
			//	{ text: "Euros", value: "Euros" }
			//],
			//onchange: function () {
            //	$("#cumulativeRange").toggleClass("hidden", $(this).val() != "true");
			//}
		}/*, {
			type: "label",
			value: "Show Cumulative?"
		}, {
			name: "showCumulative",
			value: self.ShowCumulative || false,
			type: "yesno",
			onchange: function () {
				$("#cumulativeRange").toggleClass("hidden", $(this).val() != "true");
			}
		}*/]

    		if (self.Direction == "Between" || self.Direction == -3) {
    			icon = "info";
    			//fields.unshift({
    			//	type: "label",
    			//	value: "Update historical goals?"
    			//});
    			fields.push({
    				type: "number",
    				text: "Lower-Boundary",
    				name: "Lower",
    				value: self.Target,
    			});
    			fields.push({
    				type: "number",
    				text: "Upper-Boundary",
    				name: "Upper",
    				value: self.AltTarget || self.Target,
    			});
    		}

    		$scope.functions.showModal({
    			icon: icon,
    			noCancel: true,
    			fields: fields,
    			success: function (model) {
    				var low = Math.min(+model.Lower, +model.Upper);
    				var high = Math.max(+model.Lower, +model.Upper);
    				if (isNaN(low))
    					low = null;
    				if (isNaN(high))
    					high = null;

    				$scope.$apply(function () {
    					m.Modifiers = model.unitType;
    					//$scope.model.Lookup[m.Key].Modifiers = model.unitType;

    					//debugger;
    					$scope.functions.sendUpdate(m, {
    						"historical": model.history,
    						"Lower": low,
    						"Upper": high,
    						"connectionId": null,
    						"cumulativeStart": model.cumulativeRange,
    						"enableCumulative": model.showCumulative,
    						//"Modifier": model.unitType
    					});
    				});
    			},
    			cancel: function () {

    			}

    		});
    	}

    	$scope.functions.removeRow = function (event, self) {
    		var dat = angular.copy(self);
    		var _clientTimestamp = new Date().getTime();
    		//self.Hide = true;
    		var origArchive = self.Archived;
    		self.Archived = true;

    		$(".editable-wrap").remove();

    		var url = Time.addTimestamp("/L10/Remove" + self.Type + "/?recurrenceId=" + $scope.recurrenceId);

    		$http.post(url, dat).error(function (data) {
    			showJsonAlert(data, false, true);
    			self.Archived = origArchive;
    			//self.Hide = false;
    		}).finally(function () {
    			// reload
    			$scope.functions.reload(true, $scope.model.dataDateRange, false);
    		});
    	};

    	$scope.functions.unarchiveRow = function (event, self) {
    		var dat = angular.copy(self);
    		var _clientTimestamp = new Date().getTime();
    		var origArchive = self.Archived;
    		self.Archived = false;
    		//self.Hide = true;

    		$(".editable-wrap").remove();

    		var url = Time.addTimestamp("/L10/Unarchive" + self.Type + "/?recurrenceId=" + $scope.recurrenceId);

    		$http.post(url, dat).error(function (data) {
    			showJsonAlert(data, false, true);
    			//self.Hide = false;
    			self.Archived = origArchive;
    		}).finally(function () {
    			// reload
    			$scope.functions.reload(true, $scope.model.dataDateRange, false);
    		});
    	};

    	$scope.functions.addRow = function (event, type, args) {
    		if (!$(event.target).hasClass("disabled")) {
    			var _clientTimestamp = new Date().getTime();
    			var controller = angular.element($("[ng-controller]"));
    			controller.addClass("loading");
    			$(event.target).addClass("disabled");

    			if (typeof (args) === "undefined")
    				args = "";

    			var url = Time.addTimestamp("/L10/Add" + type + "/" + $scope.recurrenceId + "?connectionId=" + $scope.connectionId);

    			$http.get(url + args)
                    .error(showAngularError)
                    .finally(function () {
                    	controller.removeClass("loading");
                    	$(event.target).removeClass("disabled");
                    });
    		}
    	};

    	$scope.functions.checkAllNotifications = function () {
    		var items = $scope.model.Notifications;
    		if (items) {
    			for (var i in items) {
    				if (arrayHasOwnIndex(items, i)) {
    					var item = items[i];
    					item.Seen = true;
    					$scope.functions.sendUpdate(item);
    				}
    			}
    		}
    	};

    	$scope.ShowSearch = false;
    	$scope.functions.showUserSearch = function (event) {
    		$scope.functions.showModal("Add Attendee", "/L10/AddAttendee?meetingId=" + $scope.recurrenceId, "/L10/AddAttendee");
    		//$scope.ShowSearch = true;
    		//$timeout(function () {
    		//    $(".user-list-container .livesearch-container input").focus();
    		//}, 1);
    	};
    	$scope.functions.showMeasurableSearch = function (event) {
    		$scope.functions.showModal("Add Measurable", "/L10/AddMeasurableModal?meetingId=" + $scope.recurrenceId, "/L10/AddMeasurableModal");
    	};
    	$scope.functions.showRockSearch = function (event) {
    		$scope.functions.showModal("Add Rock", "/L10/AddRockModal?meetingId=" + $scope.recurrenceId, "/L10/AddRockModal");
    	};

    	$scope.functions.addAttendee = function (selected) {
    		var event = { target: $(".user-list-container") };
    		$scope.functions.addRow(event, "AngularUser", "&userid=" + selected.item.id);
    	}

    	$scope.functions.createUser = function () {
    		$timeout(function () {
    			$scope.functions.showModal('Add managed user', '/User/AddModal', '/nexus/AddManagedUserToOrganization?meeting=' + $scope.recurrenceId + "&refresh=false");
    		}, 1);
    	}

    	$scope.functions.goto = function (url) {
    		$window.location.href = url;
    	}

    	$scope.functions.blurSearch = function (self, noHide) {
    		//$timeout(function () {
    		angular.element(".searchresultspopup").addClass("ng-hide");
    		self.visible = false;
    		$scope.ShowSearch = false;
    		$scope.model.Search = '';
    		//}, 300);
    	}

    	$scope.userSearchCallback = function (params) {
    		var defer = $q.defer();
    		var attendees = $scope.model.Attendees || [];
    		var ids = $.map(attendees, function (item) {
    			return item.Id;
    		})
    		$http.get("/User/Search?q=" + params + "&exclude=" + ids)
                .then(function (response) {
                	if (!response.data || !response.data.Object) {
                		defer.resolve([]);
                	}
                	defer.resolve(response.data.Object);
                })
                .catch(function (e) {
                	defer.reject(e);
                });

		return defer.promise;
	};

	$scope.functions.setHash = function (value) {
		$timeout(function () {
			$window.location.hash = value;
		}, 1);
	};

	$scope.functions.uploadUsers = function () {
		$window.location.href = "/upload/l10/Users?recurrence=" + $scope.recurrenceId;
	};

	$scope.scorecardSortListener = {
		accept: function (sourceItemHandleScope, destSortableScope) {
			return true;
		},
		orderChanged: function (event) {
			var mid = $scope.recurrenceId;
			if (mid <= 0)
				mid = event.source.itemScope.measurable.RecurrenceId;
			

			//Adj order
			var ordered = $scope.model.Scorecard.Measurables.slice().sort(function (a, b) { return a.Ordering - b.Ordering; })
			var adjArr = [];
			var adj = 0;
			for (var i = 0; i < ordered.length; i++) {
				var o = ordered[i];
				adjArr.push(adj);
				if (o.Id < 0 && !o.IsDivider)
					adj += 1;
			}


			var dat = {
				id: event.source.itemScope.measurable.Id,
				recurrence: mid,
				oldOrder: event.source.index - adjArr[event.source.index],
				newOrder: event.dest.index - adjArr[event.dest.index],
			}
			//event.source.itemScope.measurable.Ordering = event.dest.index;
			var url = Time.addTimestamp("/L10/OrderAngularMeasurable");

			$http.post(url, dat).then(function () { }, showAngularError);
		},
		// containment: '#board',//optional param.
		clone: false,//optional param for clone feature.
		allowDuplicates: false, //optional param allows duplicates to be dropped.
	};

	function decideOnDate(week, selector) {
    		//console.log(selector.ScorecardWeekDay);
    		//console.log(week.LocalDate);
    		//console.log(week.ForWeek);

    		var startOfWeek = selector.startOfWeek; // Monday            
    		//var dat = week.LocalDate;
    		//debugger;
    		var forWeek = week.ForWeek;
    		//forWeek = forWeek.addDays(-7);
    		var dat = $scope.functions.startOfWeek(forWeek, selector.ScorecardWeekDay);

        if (selector.Period == "Monthly" || selector.Period == "Quarterly") {
			dat = new Date(70, 0, 4);
			dat.setDate(dat.getDate() + 7 * (week.ForWeekNumber - 1));
		}
		return dat;
	}

	$scope.functions.topDate = function (week, selector) {
		//debugger;
		var dat = decideOnDate(week, selector);
			var date = $scope.functions.subtractDays(dat/*week.DisplayDate*/, 0, !(selector.Period == "Monthly" || selector.Period == "Quarterly"));
		return $filter('date')(date, selector.DateFormat1);
	};
	$scope.functions.bottomDate = function (week, selector) {
    		// debugger;
		var dat = decideOnDate(week, selector);
			var date = $scope.functions.subtractDays(/*week.DisplayDate*/dat, -6, !(selector.Period == "Monthly" || selector.Period == "Quarterly"));
        if (selector.Period == "Monthly" || selector.Period == "Quarterly") {
				date = $scope.functions.subtractDays(/*week.DisplayDate*/dat, 0, !(selector.Period == "Monthly" || selector.Period == "Quarterly"));
		}
		return $filter('date')(date, selector.DateFormat2);
	};

    	$scope.functions.startOfWeek = function (date, startOfWeek) {
    		var getWeekNumber = moment(date).weekday();
    		var diff = getWeekNumber - dayOfWeekAsInteger(startOfWeek);
    		if (diff < 0) {
    			diff += 7;
    		}

    		var date_new = $scope.functions.subtractDays(date, 1 * diff, false);
    		return date_new;
    	}

    	function dayOfWeekAsInteger(day) {
    		return ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"].indexOf(day);
    	}

}])

