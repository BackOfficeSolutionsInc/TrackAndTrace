﻿/*angular.module('anywhereButHere', []).directive('clickAnywhereButHere', ['$document', function ($document) {
    return {
        link: function postLink(scope, element, attrs) {
            var onClick = function (event) {
                var isChild = $(element).has(event.target).length > 0;
                var isSelf = element[0] == event.target;
                var isInside = isChild || isSelf;
                if (!isInside) {
                    scope.$apply(attrs.clickAnywhereButHere)
                }
            }
            scope.$watch(attrs.isActive, function (newValue, oldValue) {
                if (newValue !== oldValue && newValue == true) {
                    $document.bind('click', onClick);
                }
                else if (newValue !== oldValue && newValue == false) {
                    $document.unbind('click', onClick);
                }
            });
        }
    };
}]);*/

angular.module('anywhereButHere', []).factory('clickAnywhereButHereService', ["$document",function ($document) {
    var tracker = [];

    return function ($scope, expr) {
        var i, t, len;
        for (i = 0, len = tracker.length; i < len; i++) {
            t = tracker[i];
            if (t.expr === expr && t.scope === $scope) {
                return t;
            }
        }
        var handler = function () {

	        if ($document.AnywhereButHereStatus == "Active") {
		        $scope.$apply(expr);
				
	            $document.AnywhereButHereStatus = "Inactive";
	        }

        };

        $document.on('click', handler);

        // IMPORTANT! Tear down this event handler when the scope is destroyed.
        $scope.$on('$destroy', function () {
            $document.off('click', handler);
        });

        t = { scope: $scope, expr: expr };
        tracker.push(t);
        return t;
    };
}]).directive('clickAnywhereButHere', ["$document","clickAnywhereButHereService",function ($document, clickAnywhereButHereService) {
    return {
        restrict: 'A',
        link: function (scope, elem, attr, ctrl) {
            var handler = function (e) {
	            $document.AnywhereButHereStatus = "Active";
                e.stopPropagation();
            };
            elem.on('click', handler);

            scope.$on('$destroy', function () {
                elem.off('click', handler);
            });

            clickAnywhereButHereService(scope, attr.clickAnywhereButHere);
        }
    };
}]);