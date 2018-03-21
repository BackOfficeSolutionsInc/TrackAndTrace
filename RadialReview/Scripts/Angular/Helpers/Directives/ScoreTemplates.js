
angular.module('scoreTemplates', ['fcsa-number']).directive("score", ["$compile", function ($compile) {
	return {
		restrict: "E",
		require: "score",
		scope: {
			week: '=week',
			score: '=score',
			change: '&?ttOnchange',
			scoreColor: '=?scoreColor',
			measurable: '=?measurable',
			localization: '=?localization',
			fcsa: '<?fcsa'
		},
		link: function ($scope, $element, attrs, ngModelCtrl) {

			var scorecardId = function (s) {
				if (!s)
					return "sc_" + $scope.measurable.Id + "_" + $scope.week.ForWeek;
				return "sc_" + s.Id;
			};

			$scope.lastValue = $scope.score.Measured;

			$scope.changeFunc = function (type) {
				console.log("changefunc: " + type + " <" + $scope.lastValue + " - " + $scope.score.Measured + ">");
				if ($scope.lastValue != $scope.score.Measured) {
					if ($scope.change) {
						$scope.change();
					}
				}
				$scope.lastValue = $scope.score.Measured;
			}

			var currWeekNumber = getWeekSinceEpoch(new Date().addDays(13));

			var scorecardColor = function (s) {
				if (!s)
					return "";

				if (s.Measurable && s.Measurable.IsDivider)
					return "";

				var v = s.Measured;

				var useMeasurableTarget = s.ForWeek >= currWeekNumber
				var goal = undefined;
				var altgoal = undefined;
				var dir = undefined;
				if (!useMeasurableTarget) {
					goal = s.Target;//s.Measurable.Target;
					altgoal = s.AltTarget;//s.Measurable.Target;
					dir = s.Direction;//s.Measurable.Direction;
				}
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
					console.log("goal not found, trying element. Found: " + goal + " -- " + s.Id + "," + s.Measurable.Id);
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

			$scope.scoreColor = scorecardColor($scope.score);
			$scope.scoreId = scorecardId($scope.score, $scope.row, $scope.col);

			var refreshMeasurable = function (newVal, oldVal) {
				if (newVal !== oldVal) {
					console.log("refresh:" + $scope.score.Id);
					var newColor = scorecardColor($scope.score);
					if (newColor !== $scope.scoreColor) {
						$scope.scoreColor = newColor;
					}
					var newFcsa = $scope.getFcsa($scope.measurable);
					if (newFcsa !== $scope.fcsa) {
						$scope.fcsa = newFcsa;
					}
					console.log(oldVal + " vs " + newVal);
				}
			}

			$scope.$watch("score.Measured", refreshMeasurable);
			$scope.$watch("score.Measurable.Direction", refreshMeasurable);
			$scope.$watch("score.Measurable.Target", refreshMeasurable);
			$scope.$watch("score.Measurable.AltTarget", refreshMeasurable);
			$scope.$watch("score.Measurable.Modifiers", function (x, y) {
				//console.warn("asdf");
				refreshMeasurable(x, y);
			});
			$scope.$watch("score.Direction", refreshMeasurable);
			$scope.$watch("score.Target", refreshMeasurable);
			$scope.$watch("score.AltTarget", refreshMeasurable);
			$scope.$watch("localization", refreshMeasurable);
			$scope.score.hidden = false;


			//////KEY PRESS

			$element.bind("click", function (event) {
				window.ScorecardMode = "scan";
			});



			window.ScoreChangeCellTimeout = window.ScoreChangeCellTimeout || null;
			window.ScoreChangeTimeout = window.ScoreChangeTimeout || null;

			$element.bind("keydown", function (event) {
				var found;
				var goingLeft = false;
				var goingRight = false;
				$input = $element.find("input");
				if (window.ScorecardMode == "scan" ||
                     event.which == 38 ||	//pressing up
                     event.which == 40 ||	//pressing down
                     event.which == 13 ||	//pressing enter
                     ($input[0].selectionStart == 0 && (event.which == 37)) || //all the way left
                     ($input[0].selectionEnd == $input.val().length && (event.which == 39)) // all the way right
                 ) {
					if (event.which == 37) { //left
						found = $input.closest("score").closest("td").prev().find("score input");
						goingLeft = true;
					} else if (event.which == 38) { //up
						var curRow = $input.closest("score").closest("tr");
						var curCell = $input.closest("score").closest("td");
						var curCol = curRow.find("td").index(curCell);
						while (true) {
							curRow = curRow.prev();
							if (curRow && !curRow.hasClass("divider")) {
								found = $(curRow.find("td")[curCol]).find("score input");
								break;
							}
							if (!curRow) {
								break;
							}
						}
					} else if (event.which == 39) { //right
						found = $input.closest("score").closest("td").next().find("score input");
						//found = $(".grid[data-col=" + (+$input.data("col") + 1) + "][data-row=" + $input.data("row") + "]");
						//found = $(".grid[data-col=" + (curColumn + 1) + "][data-row=" + curRow + "]");
						goingRight = true;
					} else if (event.which == 40 || event.which == 13) { //down
						var curRow = $input.closest("score").closest("tr");
						var curCell = $input.closest("score").closest("td");
						var curCol = curRow.find("td").index(curCell);
						while (true) {
							curRow = curRow.next();
							if (curRow && !curRow.hasClass("divider")) {
								found = $(curRow.find("td")[curCol]).find("score input");
								break;
							}
							if (!curRow) {
								break;
							}
							//  curRow += 1;
						}
					}
					var keycode = event.which;
					var validPrintable =
                        (keycode > 47 && keycode < 58) || // number keys
                        keycode == 32 || keycode == 13 || // spacebar & return key(s) (if you want to allow carriage returns)
                        (keycode > 64 && keycode < 91) || // letter keys
                        (keycode > 95 && keycode < 112) || // numpad keys
                        (keycode > 185 && keycode < 193) || // ;=,-./` (in order)
                        (keycode > 218 && keycode < 223);   // [\]' (in order)

					if (validPrintable) {
						window.ScorecardMode = "type";
					}
				} else {
					//Tab
					if (event.which == 9 /*|| event.which == 13*/) {
						window.ScorecardMode = "scan";
					}

				}

				var input = this;
				var noop = [38, 40, 13, 37, 39];
				//if (noop.indexOf(event.which) == -1) {
				//    setTimeout(function () {
				//        updateScore(input);
				//    }, 1);
				//}

				if (found) {
					if ($(found)[0]) {
						clearTimeout(window.ScoreChangeTimeout);
						window.ScoreChangeTimeout = setTimeout(function () {
							changeCells(found, input);
						}, 1);
					}
				}


			});

			function changeCells(found, input) {
				var scrollPosition = [$(found).parents(".table-responsive").scrollLeft(), $(found).parents(".table-responsive").scrollTop()];

				var parent = $(found).parents(".table-responsive");
				var parentWidth = $(parent).width();
				var foundWidth = $(found).width();
				var foundPosition = $(found).position();
				var scale = parent.find("table").width() / parentWidth;

				$(found).focus();
				//curColumn = $(found).data("col");
				//curRow = $(found).data("row");
				clearTimeout(window.ScoreChangeCellTimeout);
				window.ScoreChangeCellTimeout = setTimeout(function () {
					$(found).select();
					//updateScore(input);
				}, 1);
			}
			//////END KEYPRESS





		},
		controller: ["$scope", "$element", "$attrs", function ($scope, $element, $attrs) {
			$scope.getFcsa = function (measurable) {
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

			$scope.measurable = $scope.score.Measurable;

			$scope.fcsa = $scope.getFcsa($scope.measurable);
		}],
		template: "<span ng-if='score.hidden' ng-click='score.hidden=false'>hidden</span>" +
                  "<input ng-if='!score.hidden' data-goal='{{score.Target}}' data-alt-goal='{{score.AltTarget}}' data-goal-dir='{{score.Direction}}'" +
                  " data-row='{{$parent.$index}}' data-col='{{$index}}'" +
                  " type='text' placeholder='' ng1-model-options='{debounce:{\"default\":300,\"blur\":0}}' ng-disabled='measurable.Disabled'" +
                  " ng-model='score.Measured'" +
                  " class='grid rt1 ww_{{::week.ForWeekNumber}} {{scoreColor}} scrollOver'" +
                  " data-scoreid='{{::Id}}' data-measurable='{{::measurable.Id}}' data-week='{{::week.ForWeekNumber}}'" +
                  " fcsa-number='{{fcsa}}'" +
                  " ng-change='changeFunc(\"change\")'" +
                  " ng-blur='changeFunc(\"blur\")'" +
                  " id='{{scoreId}}' />"
	};
}]);