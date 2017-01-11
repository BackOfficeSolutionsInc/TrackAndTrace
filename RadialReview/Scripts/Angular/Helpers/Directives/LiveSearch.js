'use strict';

angular.module("LiveSearch", ["ng"])
 .directive("liveSearch", ["$compile", "$timeout", function ($compile, $timeout) {
     return {
         restrict: 'E',
         replace: true,
         scope: {
             liveSearchCallback: '=',
             liveSearchSelect: '=?',
             liveSearchSelectCallback: '=', // do not use
             liveSearchSelectionCallback:'=',
             blur: '&ngBlur',
			 ttOnSelect:'&ttOnSelect',
             liveSearchItemTemplate: '@',
             liveSearchWaitTimeout: '=?',
             liveSearchMaxResultSize: '=?',
             liveSearchMaxlength: '=?',
             placeholder: "@",
             noResults: "@"
         },
         template: "<input type='text' ng-model-options='{debounce:250}' placeholder='{{placeholder}}'/>",
         link: function (scope, element, attrs, controller) {
             var timeout;

             scope.results = [];
             scope.visible = false;
             scope.ShowSearch = false;
             scope.selectedIndex = -1;

             scope.select = function (index) {
                 scope.visible = false;
                 scope.ShowSearch = false;
                 scope.selectedIndex = index;
                 console.log("live clicked");
                 onSelection();
             };

             scope.isSelected = function (index) {
                 return (scope.selectedIndex === index);
             };

             var onSelection = function () {
                 if (scope.selectedIndex != -1) {
                     var item = scope.results[scope.selectedIndex];
                     if (attrs.liveSearchSelectionCallback) {
                         var value = scope.liveSearchSelectionCallback.call(null, { items: scope.results, item: item });
                         element.val(value);
                     }
                 }
                 if (scope.ttOnSelect) {
                 	scope.ttOnSelect();
                 }
             }

             scope.$watch("selectedIndex", function (newValue, oldValue) {
                 var item = scope.results[newValue];
                 if (item) {
                     if (attrs.liveSearchSelectCallback) {
                         var value = scope.liveSearchSelectCallback.call(null, { items: scope.results, item: item });
                         element.val(value);
                     }
                     else {
                         if (attrs.liveSearchSelect) {
                             element.val(item[attrs.liveSearchSelect]);
                         }
                         else {
                             element.val(item);
                         }
                     }
                 }
                 if ('undefined' !== element.controller('ngModel')) {
                     element.controller('ngModel').$setViewValue(element.val());
                 }
             });

             scope.$watch("visible", function (newValue, oldValue) {
                 if (newValue === false) {
                     return;
                 }
                 //if (newValue === true && oldValue === false) {
                 //    scope.results = [];
                 //    scope.search = "";
                 //}
                 scope.width = element[0].clientWidth;
                 var offset = getPosition(element[0]);
                 scope.top = offset.y + element[0].clientHeight + 1 + 'px';
                 scope.left = offset.x + 'px';
             });

             //element[0].onclick = function (e) {
                
             //}

             element[0].onkeydown = function (e) {
                 //keydown
                 if (e.keyCode == 40) {
                     if (scope.selectedIndex + 1 === scope.results.length) {
                         scope.selectedIndex = 0;
                     }
                     else {
                         scope.selectedIndex++;
                     }
                 }
                     //keyup
                 else if (e.keyCode == 38) {
                     if (scope.selectedIndex === 0) {
                         scope.selectedIndex = scope.results.length - 1;
                     }
                     else if (scope.selectedIndex == -1) {
                         scope.selectedIndex = 0;
                     }
                     else scope.selectedIndex--;
                 }
                 //keydown or keyup
                 if (e.keyCode == 13) {
                     scope.visible = false;
                     scope.blur();
                     console.log("live enter");
                     onSelection();
                 }

                 //unmanaged code needs to force apply
                 scope.$apply();
             };

             element[0].onkeyup = function (e) {
                 if (e.keyCode == 13 || e.keyCode == 37 || e.keyCode == 38 || e.keyCode == 39 || e.keyCode == 40) {
                     return false;
                 }
                 var target = element;
                 // Set Timeout
                 $timeout.cancel(timeout);
                 // Set Search String
                 var vals = target.val().split(",");
                 var search_string = vals[vals.length - 1].trim();
                 // Do Search
                 if (search_string.length < 3 ||
                     (scope.liveSearchMaxlength !== null && search_string.length > scope.liveSearchMaxlength)) {
                     scope.visible = false;
                     //unmanaged code needs to force apply
                     scope.$apply();
                     return;
                 }
                 timeout = $timeout(function () {
                     var results = [];
                     var promise = scope.liveSearchCallback.call(null, search_string);
                     promise.then(function (dataArray) {
                         if (dataArray) {
                             results = dataArray.slice(0, (scope.liveSearchMaxResultSize || 20) - 1);
                         }
                         scope.visible = true;
                     });
                     promise.finally(function () {
                         scope.selectedIndex = -1;
                         scope.results = results.filter(function (elem, pos) {
                             return results.indexOf(elem) == pos;
                         });
                     });
                 }, scope.liveSearchWaitTimeout || 100);
             };

             var getPosition = function (element) {
                 var xPosition = 0;
                 var yPosition = 0;

                 while (element && !element.classList.contains("search-box-container")) {
                     xPosition += (element.offsetLeft - element.scrollLeft + element.clientLeft);
                     yPosition += (element.offsetTop - element.scrollTop + element.clientTop);
                     element = element.offsetParent;
                 }
                 return { x: xPosition, y: yPosition };
             };

             var itemTemplate = element.attr("live-search-item-template") || "{{result}}";
             var template = "<ul ng-show='visible' ng-style=\"{'top':top,'left':left,'width':width}\" class='searchresultspopup'><li ng-class=\"{ 'selected' : isSelected($index) }\" ng-click='select($index)' ng-repeat='result in results'>" + itemTemplate + "</li><li class='no-results' ng-show='results.length==0'></li></ul>";
             var searchPopup = $compile(template)(scope);

             /** FIND THE PARENT MODAL TO APPEND TO, OTHERWISE APPEND TO BODY **/
             var parentElement = document.getElementsByClassName("search-box-container")[0] || document.body;
             parentElement.appendChild(searchPopup[0]);

         }
     };
 }]);