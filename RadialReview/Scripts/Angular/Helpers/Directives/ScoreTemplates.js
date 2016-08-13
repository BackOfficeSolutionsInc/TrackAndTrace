
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
            fcsa: '<?fcsa'
        },
        link: function ($scope, $element, attrs, ngModelCtrl) {
            //$scope.starClass = "";         
            //ngModelCtrl.$viewChangeListeners.push(function () {
            //    $scope.$parent.$eval(attrs.ngChange);
            //});  
            var scorecardId = function (s) {
                if (!s)
                    return "sc_" + $scope.measurable.Id + "_" + $scope.week.ForWeek;
                return "sc_" + s.Id;
            };

            $scope.lastValue = $scope.score.Measured;
            
            $scope.changeFunc = function (type) {
                console.log("changefunc: " + type + " <" + $scope.lastValue + " - " + $scope.score.Measured +">");
                if ($scope.lastValue != $scope.score.Measured) {
                    if ($scope.change) {
                        $scope.change();
                    }
                }
                $scope.lastValue = $scope.score.Measured;
            }
           
            var currWeekNumber= getWeekSinceEpoch(new Date().addDays(13));

            var scorecardColor = function (s) {
                if (!s)
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
                    var item =$("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]");
                    goal = item.data("goal");
                    if (typeof(altgoal)==="undefined")
                        altgoal = item.data("alt-goal");
                    console.log("goal not found, trying element. Found: " + goal + " -- "+s.Id+","+s.Measurable.Id);
                }

                //if (typeof (goal) === "undefined") {
                //    var item =$("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]");
                //    goal = item.data("goal");
                //    if (typeof(altgoal)==="undefined")
                //        altgoal = item.data("alt-goal");
                //    }
                //}

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
               // console.log("Firing");
                if (newVal !== oldVal) {
                    var newColor = scorecardColor($scope.score);
                    if (newColor !== $scope.scoreColor) {
                        $scope.scoreColor = newColor;
                        //if(typeof($scope.change)==="function")
                        //    $scope.change();
                    }
                    var newFsca = $scope.getFcsa($scope.measurable);
                    if (newFsca !== $scope.fsca) {
                        $scope.fsca = newFsca;
                    }
                    console.log(oldVal + " vs " + newVal);
                }
            }

            $scope.$watch("score.Measured", refreshMeasurable);
            $scope.$watch("score.Measurable.Direction", refreshMeasurable);
            $scope.$watch("score.Measurable.Target", refreshMeasurable);
            $scope.$watch("score.Measurable.AltTarget", refreshMeasurable);
            $scope.$watch("score.Direction", refreshMeasurable);
            $scope.$watch("score.Target", refreshMeasurable);
            $scope.$watch("score.AltTarget", refreshMeasurable);
        },
        controller: ["$scope", "$element", "$attrs", function ($scope, $element, $attrs) {
            $scope.getFcsa = function (measurable) {
                var builder = {};
                //var resize = 2;
                //if ($scope.score.Measured >= 100000) {
                //    maxDecimals = 0;
                //}

                if (measurable.Modifiers == "Dollar") {
                    builder = {
                        prepend: "$",
                        resize: true,
                        localization: {radix:","}
                    };
                } else if (measurable.Modifiers == "Percent") {
                    builder = {
                        append: "%",
                        resize: true
                    };
                } else if (measurable.Modifiers == "Euros") {
                    builder = {
                        prepend: "€",
                        resize: true
                    };
                } else if (measurable.Modifiers == "Pound") {
                    builder = {
                        prepend: "£",
                        resize: true
                    };
                }

                return builder;
            };

            $scope.measurable = $scope.score.Measurable;
            //$scope.week = week;//$scope.score.Week;
            $scope.fcsa = $scope.getFcsa($scope.measurable);
        }],
        template: "<input data-goal='{{score.Target}}' data-alt-goal='{{score.AltTarget}}' data-goal-dir='{{score.Direction}}'" +
                  " data-row='{{$parent.$index}}' data-col='{{$index}}'" +
                  " type='text' placeholder='' ng1-model-options='{debounce:{\"default\":75,\"blur\":0}}' ng-disabled='measurable.Disabled'" +
                  " ng-model='score.Measured'" +
                  //" ng-model='functions.lookupScore(week.ForWeekNumber,measurable.Id,@(scorecardKey)).Measured'"+
                  " class='grid rt1 ww_{{::week.ForWeekNumber}} {{scoreColor}}'" +
                  " data-scoreid='{{::Id}}' data-measurable='{{::measurable.Id}}' data-week='{{::week.ForWeekNumber}}'" +
                  " fcsa-number='{{fcsa}}'" +
                  " ng-change='changeFunc(\"change\")'" +
                  " ng-blur='changeFunc(\"blur\")'" +
                  " id='{{scoreId}}' />"
    };
}]);