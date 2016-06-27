
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

           

            var scorecardColor = function (s) {
                if (!s)
                    return "";

                var v = s.Measured;
                var goal = s.Measurable.Target;
                var dir = s.Measurable.Direction;
                if (typeof (goal) === "undefined") {
                    goal = $("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]").data("goal");
                    console.log("goal not found, trying element. Found: " + goal);
                }

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
          
            $scope.scoreColor = scorecardColor($scope.score);
            $scope.scoreId = scorecardId($scope.score, $scope.row, $scope.col);

            var refreshMeasurable = function (newVal, oldVal) {
               // console.log("Firing");
                if (newVal !== oldVal) {
                    var newColor = scorecardColor($scope.score);
                    if (newColor !== $scope.scoreColor) {
                        $scope.scoreColor = newColor;
                        if(typeof($scope.change)==="function")
                            $scope.change();
                    }
                    var newFsca = $scope.getFcsa($scope.measurable);
                    if (newFsca !== $scope.fsca) {
                        $scope.fsca = newFsca;
                    }
                    console.log(oldVal + " vs " + newVal);
                }
            }


            //ngModelCtrl.$modelValue
            //$scope.add = function () {
            //    console.log("add");
            //    $scope.priority += 1;
            //    if ($scope.priority > 6)
            //        $scope.priority = 0;
            //    ngModelCtrl.$setViewValue($scope.priority);
            //    // console.log($scope.ngModel);
            //    // $scope.$parent.$eval($attrs.ngChange);
            //    //$scope.refresh();
            //};
            //$scope.remove = function () {
            //    console.log("remove");
            //    $scope.priority = Math.max(0, $scope.priority - 1);

            //    ngModelCtrl.$setViewValue($scope.priority);
            //    // console.log($scope.ngModel);
            //    //  $scope.$parent.$eval($attrs.ngChange);
            //    // $scope.refresh();
            //};
            //ngModelCtrl.$render = function () {
            //    $scope.priority = ngModelCtrl.$modelValue;
            //};

            $scope.$watch("score.Measured", refreshMeasurable);
            $scope.$watch("score.Measurable.Direction", refreshMeasurable);
            $scope.$watch("score.Measurable.Target", refreshMeasurable);
            //refresh(ngModelCtrl.$modelValue);
        },
        controller: ["$scope", "$element", "$attrs", function ($scope, $element, $attrs) {
            $scope.getFcsa = function (measurable) {
                if (measurable.Modifiers == "Dollar") {
                    return { prepend: "$" };
                } else if (measurable.Modifiers == "Percent") {
                    return { append: "%" };
                } else if (measurable.Modifiers == "Euros") {
                    return { prepend: "€" };
                } else if (measurable.Modifiers == "Pound") {
                    return { prepend: "£" };
                }
            };

            $scope.measurable = $scope.score.Measurable;
            //$scope.week = week;//$scope.score.Week;
            $scope.fcsa = $scope.getFcsa($scope.measurable);
        }],
        template: "<input data-goal='{{measurable.Target}}' data-goal-dir='{{measurable.Direction}}'" +
                  " data-row='{{$parent.$index}}' data-col='{{$index}}'" +
                  " type='text' placeholder='' ng-model-options='{debounce: 75}' ng-disabled='measurable.Disabled'" +
                  " ng-model='score.Measured'" +
                  //" ng-model='functions.lookupScore(week.ForWeekNumber,measurable.Id,@(scorecardKey)).Measured'"+
                  " class='grid rt1 ww_{{::week.ForWeekNumber}} {{scoreColor}}'" +
                  " data-scoreid='{{::Id}}' data-measurable='{{::measurable.Id}}' data-week='{{::week.ForWeekNumber}}'" +
                  " fcsa-number='{{fcsa}}'" +
                  //" ng-change='ttChange()'"+
                  " id='{{scoreId}}' />"
    };
}]);