
angular.module('priorityTemplates', []).directive("priority", ["$compile", function ($compile) {
    return {
        restrict: "E",
        require: "ngModel",
        scope: {
           // ngModel: "="
        },
        //link: function (scope, element, attrs) {
        //    debugger;
        //    var starClass = [];
        //    var p = scope.value;
        //    var html = "";
        //    if (p > 3) {
        //        starClass.append("multiple");
        //        html=("<span class='icon fontastic-icon-star-3'></span> x" + p);
        //    } else if (p > 0 && p <= 3) {
        //        starClass.append("single");
        //        starClass.append("single-" + p);

        //        for (var i = 0; i < p; i++) {
        //            html += "<span class='icon fontastic-icon-star-3'></span>";
        //        }
        //        if (p == 1)
        //            html += "<span class='hoverable'>+</span>";
        //    } else if (p == 0) {
        //        starClass.append("none");
        //        html=("<span class='icon fontastic-icon-star-empty'></span>");
        //    }

        //},
        link: function ($scope, $element, attrs, ngModelCtrl)
        {
            $scope.starClass = "";
            /*ngModelCtrl.$viewChangeListeners.push(function () {
                $scope.$eval(attrs.ngChange);
            });*/
            ngModelCtrl.$viewChangeListeners.push(function () {
                $scope.$parent.$eval(attrs.ngChange);
            });
            var refresh = function (value) {
                //console.log("ValChange:" + value);
                var html = "";
                starClass = [];
                angular.element($element[0].querySelector(".priority")).empty();
                var p = ngModelCtrl.$modelValue;
                if (p > 3) {
                    starClass.push("multiple");
                    html = ("<span><span class='icon fontastic-icon-star-3'></span> x" + p+"</span>");
                } else if (p > 0 && p <= 3) {
                    starClass.push("single");
                    starClass.push("single-" + p);

                    for (var i = 0; i < p; i++) {
                        html += "<span class='icon fontastic-icon-star-3'></span>";
                    }
                    if (p == 1)
                        html += "<span class='hoverable'>+</span>";
                } else if (p == 0) {
                    starClass.push("none");
                    html = ("<span class='icon fontastic-icon-star-empty'></span>");
                }
                $scope.starClass = starClass.join(" ");
                if (html != "") {
                    var el = $compile(html)($scope);
                    angular.element($element[0].querySelector(".priority")).append(el);
                }
            }
            /*$scope.refresh = function () {
                
            };*/ngModelCtrl.$modelValue
            $scope.add = function () {
                console.log("add");
                $scope.priority += 1;
                if ($scope.priority > 9)
                    $scope.priority = 0;
                ngModelCtrl.$setViewValue($scope.priority);
               // console.log($scope.ngModel);
               // $scope.$parent.$eval($attrs.ngChange);
                //$scope.refresh();
            };
            $scope.remove = function () {
                console.log("remove");
                $scope.priority = Math.max(0, $scope.priority - 1);

                ngModelCtrl.$setViewValue($scope.priority);
               // console.log($scope.ngModel);
              //  $scope.$parent.$eval($attrs.ngChange);
                // $scope.refresh();
            };
            ngModelCtrl.$render = function () {
                $scope.priority = ngModelCtrl.$modelValue;
            };

            $scope.$watch(function () { return ngModelCtrl.$modelValue; }, refresh);
            //refresh(ngModelCtrl.$modelValue);
        },
        controller: ["$scope", "$element","$attrs", function ($scope, $element, $attrs) {
           /* $scope.add = function () {
                console.log("add");
                $scope.ngModel += 1;
                console.log($scope.ngModel);
                // $scope.$parent.$eval($attrs.ngChange);
                //$scope.refresh();
            };
            $scope.remove = function () {
                console.log("remove");
                $scope.ngModel = Math.max(0, $scope.ngModel - 1);
                console.log($scope.ngModel);
                //  $scope.$parent.$eval($attrs.ngChange);
                // $scope.refresh();
            };*/

            // $scope.refresh();
        }],
        template: "<div class='priority-container'>" +
			        "<div class='number-priority'>" +
                        "<span class='priority {{starClass}}' data-priority='{{priority}}' ng-click='add()' ng-right-click='remove()'></span>" +
                    "</div>" +
			      "</div>"
    };
}]);