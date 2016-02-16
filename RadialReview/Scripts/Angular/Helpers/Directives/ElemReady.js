﻿angular.module("elementReady", []).directive('elemReady', ["$parse",function ($parse) {
    return {
        restrict: 'A',
        link: function( $scope, elem, attrs ) {    
            elem.ready(function(){
                $scope.$apply(function(){
                    var func = $parse(attrs.elemReady);
                    func($scope);
                })
            })
        }
    }
}]);