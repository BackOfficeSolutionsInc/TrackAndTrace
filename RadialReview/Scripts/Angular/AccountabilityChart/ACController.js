var acapp = angular.module('ACApp', ['helpers', 'panzoom', 'tree']);

//EDIT THE MD-AUTOCOMPLETE

//END EDIT THE MD-AUTOCOMPLETE





acapp.directive('mdBlur', ["$timeout", "$rootScope", function ($timeout, $rootScope) {
    var directive = {
        restrict: 'A',
        link: function (scope, element, attributes) {
            $timeout(function () {
                scope.oldValue = scope.$eval(attributes.mdSelectedItem);
                scope.focused = false;
                angular.element(element[0].querySelector("input")).bind("focus", function () {
                    //console.log("focus:",this,$(this).is(":focus"));
                    scope.focused = true;
                    var resolved = scope.$eval(attributes.mdSelectedItem);
                    if (resolved) {
                        scope.oldValue = resolved.Key;
                    } else {
                        scope.oldValue = null;
                    }
                });
                angular.element(element[0].querySelector("input")).bind("blur", function () {
                    var that = this;
                    scope.focused = false;

                    var e = element;
                    var that = this;
                    $timeout(function () {
                        ////console.log(element);
                        //var a = attributes;
                        //var resolved = scope.$eval(attributes.mdSelectedItem);
                        //var newValue = null;
                        //if (resolved) {
                        //	newValue = resolved.Key;
                        //}

                        ////var index = $(that).val().indexOf(" (Create)");
                        ////if (index != -1) {
                        ////	var a = attributes;
                        ////	$(that).val(name.substring(0, index));
                        ////}

                        //if (scope.oldValue != newValue || newValue=="AngularPosition_-2") {
                        //	//console.log("triggered");
                        //	console.log(scope.oldValue, newValue);
                        //	//scope.$eval(attributes.mdBlur);
                        //} else {
                        //	//if (newValue == null) {
                        //	//	$(that).val(null);
                        //	//}
                        //	//console.log("skip blur");
                        //}
                    }, 30);
                });
            }, 0);
        }
    };

    return directive;
}]);

acapp.directive("ttClearNoMatch", function () {
    //Maximum number of 'M's to show.

    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, element, attrs, ngModel) {

            if (typeof (ngModel) !== "undefined") {
                function clearOnNoMatch() {
                    var numMs = (attrs.ttClearNoMatch || false);
                    var mv = ngModel.$modelValue;
                    if (typeof (scope.selectedItem) !== "undefined" && scope.selectedItem == null) {
                        mv = "";
                    }
                    ngModel.$viewValue = mv;
                    ngModel.$render();
                    return mv;
                }
                ngModel.$formatters.push(clearOnNoMatch);
                element.on('blur', function () {
                    return clearOnNoMatch();
                });
                element.on('focus', function () {
                    ngModel.$viewValue = ngModel.$modelValue;
                    ngModel.$render();
                });
            }
        }
    };
});
//acapp.directive("ttRole", function () {
//	return {
//		restrict: "E",
//		require: 'ngModel',
//		scope:{
//			ngModel:"=ngModel"
//		},
//		template: "<input ng-model-options='{debounce:75}'  ng-focus='focusing()' ng-blur='blurring()'" + " ng-keydown='checkCreateRole($event,ngModel,group,$index)'" + " title='{{ngModel.Name}}' class='role' ng-if='::group.Editable!=false' ng-model=\"ngModel.Name\" ng-change=\"updating(ngModel)\">" +
//				 "<div title='{{ngModel.Name}}' class='role' ng-show='::group.Editable==false'>{{ngModel.Name}}</div>" +
//				 "<span ng-if='::group.Editable!=false' class='delete-role-row' ng-click=\"deleting(ngModel)\" tabindex='-1'></span>"

//	};
//});

acapp.directive("ttOverflow", ["$timeout", function ($timeout) {
	//Maximum number of 'M's to show.

	var letterLengthDictSarif = {
		'': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '	': 250, ' ': 250, '': 750, '': 0, ' ': 250, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, ' ': 250,
		'!': 333, '"': 408, '#': 500, '$': 500, '%': 833, '&': 778, '\'': 180, '(': 333, ')': 333, '*': 500, '+': 564, ',': 250, '-': 333, '.': 250, '/': 278, '0': 500, '1': 500, '2': 500, '3': 500, '4': 500, '5': 500, '6': 500, '7': 500, '8': 500, '9': 500, ':': 278, ';': 278, '<': 564, '=': 564, '>': 564, '?': 444, '@': 921,
		'A': 722, 'B': 667, 'C': 667, 'D': 722, 'E': 611, 'F': 556, 'G': 722, 'H': 722, 'I': 333, 'J': 389, 'K': 722, 'L': 611, 'M': 889, 'N': 722, 'O': 722, 'P': 556, 'Q': 722, 'R': 667, 'S': 556, 'T': 611, 'U': 722, 'V': 722, 'W': 944, 'X': 722, 'Y': 722, 'Z': 611,
		'[': 333, '\\': 278, ']': 333, '^': 469, '_': 500, '`': 333, 'a': 444, 'b': 500, 'c': 444, 'd': 500, 'e': 444, 'f': 333, 'g': 500, 'h': 500, 'i': 278, 'j': 278, 'k': 500, 'l': 278, 'm': 778, 'n': 500, 'o': 500, 'p': 500, 'q': 500, 'r': 333, 's': 389, 't': 278, 'u': 500, 'v': 500, 'w': 722, 'x': 500, 'y': 500, 'z': 444,
		'{': 480, '|': 200, '}': 480, '~': 541, '': 500, '': 750, '': 750, '': 750, '': 750, '': 750, '\n': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750,
		'': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, ' ': 250, '¡': 333, '¢': 500, '£': 500, '¤': 500, '¥': 500, '¦': 200, '§': 500, '¨': 333, '©': 760, 'ª': 276, '«': 500, '¬': 564, '­': 0, '®': 760, '¯': 500, '°': 400, '±': 549, '²': 300, '³': 300, '´': 333, 'µ': 576, '¶': 453, '·': 333,
		'¸': 333, '¹': 300, 'º': 310, '»': 500, '¼': 750, '½': 750, '¾': 750, '¿': 444, 'À': 722, 'Á': 722, 'Â': 722, 'Ã': 722, 'Ä': 722, 'Å': 722, 'Æ': 889, 'Ç': 667, 'È': 611, 'É': 611, 'Ê': 611, 'Ë': 611, 'Ì': 333, 'Í': 333, 'Î': 333, 'Ï': 333, 'Ð': 722, 'Ñ': 722, 'Ò': 722, 'Ó': 722, 'Ô': 722, 'Õ': 722, 'Ö': 722, '×': 564, 'Ø': 722, 'Ù': 722, 'Ú': 722,
		'Û': 722, 'Ü': 722, 'Ý': 722, 'Þ': 556, 'ß': 500, 'à': 444, 'á': 444, 'â': 444, 'ã': 444, 'ä': 444, 'å': 444, 'æ': 667, 'ç': 444, 'è': 444, 'é': 444, 'ê': 444, 'ë': 444, 'ì': 278, 'í': 278, 'î': 278, 'ï': 278, 'ð': 500, 'ñ': 500, 'ò': 500, 'ó': 500, 'ô': 500, 'õ': 500, 'ö': 500, '÷': 549, 'ø': 500, 'ù': 500, 'ú': 500, 'û': 500, 'ü': 500, 'ý': 500, 'þ': 500
	};
	var letterLengthDictSanSarif = { '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '	': 276, ' ': 276, '': 748, '': 0, ' ': 276, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, ' ': 276, '!': 276, '"': 352, '#': 556, '$': 556, '%': 888, '&': 664, '\'': 188, '(': 332, ')': 332, '*': 388, '+': 584, ',': 276, '-': 332, '.': 276, '/': 276, '0': 556, '1': 556, '2': 556, '3': 556, '4': 556, '5': 556, '6': 556, '7': 556, '8': 556, '9': 556, ':': 276, ';': 276, '<': 584, '=': 584, '>': 584, '?': 556, '@': 1016, 'A': 664, 'B': 664, 'C': 720, 'D': 720, 'E': 664, 'F': 608, 'G': 776, 'H': 720, 'I': 276, 'J': 500, 'K': 664, 'L': 556, 'M': 832, 'N': 720, 'O': 776, 'P': 664, 'Q': 776, 'R': 720, 'S': 664, 'T': 608, 'U': 720, 'V': 664, 'W': 944, 'X': 664, 'Y': 664, 'Z': 608, '[': 276, '\\': 276, ']': 276, '^': 468, '_': 556, '`': 332, 'a': 556, 'b': 556, 'c': 500, 'd': 556, 'e': 556, 'f': 276, 'g': 556, 'h': 556, 'i': 220, 'j': 220, 'k': 500, 'l': 220, 'm': 832, 'n': 556, 'o': 556, 'p': 556, 'q': 556, 'r': 332, 's': 500, 't': 276, 'u': 556, 'v': 500, 'w': 720, 'x': 500, 'y': 500, 'z': 500, '{': 332, '|': 260, '}': 332, '~': 584, '': 500, '': 748, '': 748, '': 748, '': 748, '': 748, '\n': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, ' ': 276, '¡': 332, '¢': 556, '£': 556, '¤': 556, '¥': 556, '¦': 260, '§': 556, '¨': 332, '©': 736, 'ª': 368, '«': 556, '¬': 584, '­': 0, '®': 736, '¯': 552, '°': 400, '±': 548, '²': 332, '³': 332, '´': 332, 'µ': 576, '¶': 536, '·': 332, '¸': 332, '¹': 332, 'º': 364, '»': 556, '¼': 832, '½': 832, '¾': 832, '¿': 608, 'À': 664, 'Á': 664, 'Â': 664, 'Ã': 664, 'Ä': 664, 'Å': 664, 'Æ': 1000, 'Ç': 720, 'È': 664, 'É': 664, 'Ê': 664, 'Ë': 664, 'Ì': 276, 'Í': 276, 'Î': 276, 'Ï': 276, 'Ð': 720, 'Ñ': 720, 'Ò': 776, 'Ó': 776, 'Ô': 776, 'Õ': 776, 'Ö': 776, '×': 584, 'Ø': 776, 'Ù': 720, 'Ú': 720, 'Û': 720, 'Ü': 720, 'Ý': 664, 'Þ': 664, 'ß': 608, 'à': 556, 'á': 556, 'â': 556, 'ã': 556, 'ä': 556, 'å': 556, 'æ': 888, 'ç': 500, 'è': 556, 'é': 556, 'ê': 556, 'ë': 556, 'ì': 276, 'í': 276, 'î': 276, 'ï': 276, 'ð': 556, 'ñ': 556, 'ò': 556, 'ó': 556, 'ô': 556, 'õ': 556, 'ö': 556, '÷': 548, 'ø': 608, 'ù': 556, 'ú': 556, 'û': 556, 'ü': 556, 'ý': 500, 'þ': 556, };
	var letterLengthDict = letterLengthDictSanSarif;
	var maxLetter = 1;
	for (var o in letterLengthDict) {
		if (letterLengthDict.hasOwnProperty(o)) {
			maxLetter = Math.max(maxLetter, letterLengthDict[o]);
		}
	}
	function getCharWidth(char) {
		try {
			var oo = letterLengthDict[char];
			if (typeof (oo) === "undefined")
				return maxLetter;
			return oo;
		} catch (e) {
			console.warn("letterLen[" + char + "]:" + e);
			return maxLetter;
		}

	}

	function calcMWidth(str) {
		var w = 0;
		for (var i = 0; i < str.length; i++) {
			w += getCharWidth(str[i]);
		}
		return w / (letterLengthDict["M"]);
	}
	function calcNumChars(str, Ms) {
		var w = 0;
		var c = 0;
		var mLen = letterLengthDict["M"] * Ms;
		for (var i = 0; i < str.length; i++) {
			var thisW = getCharWidth(str[i]);
			if (w + thisW > mLen) {
				return c;
			}
			c += 1;
			w += thisW;
		}
		return c;
	}

	return {
		restrict: 'A',
		require: 'ngModel',
		link: function (scope, element, attrs, ngModel) {

			if (typeof (ngModel) !== "undefined") {
				function shorten() {
					var numMs = +(attrs.ttOverflow || 17);
					//console.log("inshort");
					var mv = ngModel.$modelValue;
					if (typeof (mv) === "string" && calcMWidth(mv) > numMs) {
						var length = calcNumChars(mv, numMs);
						mv = mv.substring(0, length) + "...";
						//console.log(" - shorten");
					}
					$timeout(function () {
						ngModel.$viewValue = mv;
						ngModel.$render();
					}, 1);

					return mv;
				}
				ngModel.$formatters.push(shorten);


				element.on('blur', function () {
					return shorten();
				});
				element.on('focus', function () {
					debugger;
					ngModel.$viewValue = ngModel.$modelValue;
					ngModel.$render();
					console.info(ngModel.$viewValue);
				});
			}
		}
	};
}]);

acapp.filter('ttOverflowTxt', function () {
    return function (value, wordwise, max, tail) {
        if (!value) return '';
		max = parseInt(max, 10);
        if (!max) return value;
        if (value.length <= max) return value;


        var letterLengthDictSarif = {
            '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '	': 250, ' ': 250, '': 750, '': 0, ' ': 250, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, ' ': 250,
            '!': 333, '"': 408, '#': 500, '$': 500, '%': 833, '&': 778, '\'': 180, '(': 333, ')': 333, '*': 500, '+': 564, ',': 250, '-': 333, '.': 250, '/': 278, '0': 500, '1': 500, '2': 500, '3': 500, '4': 500, '5': 500, '6': 500, '7': 500, '8': 500, '9': 500, ':': 278, ';': 278, '<': 564, '=': 564, '>': 564, '?': 444, '@': 921,
            'A': 722, 'B': 667, 'C': 667, 'D': 722, 'E': 611, 'F': 556, 'G': 722, 'H': 722, 'I': 333, 'J': 389, 'K': 722, 'L': 611, 'M': 889, 'N': 722, 'O': 722, 'P': 556, 'Q': 722, 'R': 667, 'S': 556, 'T': 611, 'U': 722, 'V': 722, 'W': 944, 'X': 722, 'Y': 722, 'Z': 611,
            '[': 333, '\\': 278, ']': 333, '^': 469, '_': 500, '`': 333, 'a': 444, 'b': 500, 'c': 444, 'd': 500, 'e': 444, 'f': 333, 'g': 500, 'h': 500, 'i': 278, 'j': 278, 'k': 500, 'l': 278, 'm': 778, 'n': 500, 'o': 500, 'p': 500, 'q': 500, 'r': 333, 's': 389, 't': 278, 'u': 500, 'v': 500, 'w': 722, 'x': 500, 'y': 500, 'z': 444,
            '{': 480, '|': 200, '}': 480, '~': 541, '': 500, '': 750, '': 750, '': 750, '': 750, '': 750, '\n': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750,
            '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, '': 750, ' ': 250, '¡': 333, '¢': 500, '£': 500, '¤': 500, '¥': 500, '¦': 200, '§': 500, '¨': 333, '©': 760, 'ª': 276, '«': 500, '¬': 564, '­': 0, '®': 760, '¯': 500, '°': 400, '±': 549, '²': 300, '³': 300, '´': 333, 'µ': 576, '¶': 453, '·': 333,
            '¸': 333, '¹': 300, 'º': 310, '»': 500, '¼': 750, '½': 750, '¾': 750, '¿': 444, 'À': 722, 'Á': 722, 'Â': 722, 'Ã': 722, 'Ä': 722, 'Å': 722, 'Æ': 889, 'Ç': 667, 'È': 611, 'É': 611, 'Ê': 611, 'Ë': 611, 'Ì': 333, 'Í': 333, 'Î': 333, 'Ï': 333, 'Ð': 722, 'Ñ': 722, 'Ò': 722, 'Ó': 722, 'Ô': 722, 'Õ': 722, 'Ö': 722, '×': 564, 'Ø': 722, 'Ù': 722, 'Ú': 722,
            'Û': 722, 'Ü': 722, 'Ý': 722, 'Þ': 556, 'ß': 500, 'à': 444, 'á': 444, 'â': 444, 'ã': 444, 'ä': 444, 'å': 444, 'æ': 667, 'ç': 444, 'è': 444, 'é': 444, 'ê': 444, 'ë': 444, 'ì': 278, 'í': 278, 'î': 278, 'ï': 278, 'ð': 500, 'ñ': 500, 'ò': 500, 'ó': 500, 'ô': 500, 'õ': 500, 'ö': 500, '÷': 549, 'ø': 500, 'ù': 500, 'ú': 500, 'û': 500, 'ü': 500, 'ý': 500, 'þ': 500
        };
        var letterLengthDictSanSarif = { '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '	': 276, ' ': 276, '': 748, '': 0, ' ': 276, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, ' ': 276, '!': 276, '"': 352, '#': 556, '$': 556, '%': 888, '&': 664, '\'': 188, '(': 332, ')': 332, '*': 388, '+': 584, ',': 276, '-': 332, '.': 276, '/': 276, '0': 556, '1': 556, '2': 556, '3': 556, '4': 556, '5': 556, '6': 556, '7': 556, '8': 556, '9': 556, ':': 276, ';': 276, '<': 584, '=': 584, '>': 584, '?': 556, '@': 1016, 'A': 664, 'B': 664, 'C': 720, 'D': 720, 'E': 664, 'F': 608, 'G': 776, 'H': 720, 'I': 276, 'J': 500, 'K': 664, 'L': 556, 'M': 832, 'N': 720, 'O': 776, 'P': 664, 'Q': 776, 'R': 720, 'S': 664, 'T': 608, 'U': 720, 'V': 664, 'W': 944, 'X': 664, 'Y': 664, 'Z': 608, '[': 276, '\\': 276, ']': 276, '^': 468, '_': 556, '`': 332, 'a': 556, 'b': 556, 'c': 500, 'd': 556, 'e': 556, 'f': 276, 'g': 556, 'h': 556, 'i': 220, 'j': 220, 'k': 500, 'l': 220, 'm': 832, 'n': 556, 'o': 556, 'p': 556, 'q': 556, 'r': 332, 's': 500, 't': 276, 'u': 556, 'v': 500, 'w': 720, 'x': 500, 'y': 500, 'z': 500, '{': 332, '|': 260, '}': 332, '~': 584, '': 500, '': 748, '': 748, '': 748, '': 748, '': 748, '\n': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, '': 748, ' ': 276, '¡': 332, '¢': 556, '£': 556, '¤': 556, '¥': 556, '¦': 260, '§': 556, '¨': 332, '©': 736, 'ª': 368, '«': 556, '¬': 584, '­': 0, '®': 736, '¯': 552, '°': 400, '±': 548, '²': 332, '³': 332, '´': 332, 'µ': 576, '¶': 536, '·': 332, '¸': 332, '¹': 332, 'º': 364, '»': 556, '¼': 832, '½': 832, '¾': 832, '¿': 608, 'À': 664, 'Á': 664, 'Â': 664, 'Ã': 664, 'Ä': 664, 'Å': 664, 'Æ': 1000, 'Ç': 720, 'È': 664, 'É': 664, 'Ê': 664, 'Ë': 664, 'Ì': 276, 'Í': 276, 'Î': 276, 'Ï': 276, 'Ð': 720, 'Ñ': 720, 'Ò': 776, 'Ó': 776, 'Ô': 776, 'Õ': 776, 'Ö': 776, '×': 584, 'Ø': 776, 'Ù': 720, 'Ú': 720, 'Û': 720, 'Ü': 720, 'Ý': 664, 'Þ': 664, 'ß': 608, 'à': 556, 'á': 556, 'â': 556, 'ã': 556, 'ä': 556, 'å': 556, 'æ': 888, 'ç': 500, 'è': 556, 'é': 556, 'ê': 556, 'ë': 556, 'ì': 276, 'í': 276, 'î': 276, 'ï': 276, 'ð': 556, 'ñ': 556, 'ò': 556, 'ó': 556, 'ô': 556, 'õ': 556, 'ö': 556, '÷': 548, 'ø': 608, 'ù': 556, 'ú': 556, 'û': 556, 'ü': 556, 'ý': 500, 'þ': 556, };
        var letterLengthDict = letterLengthDictSanSarif;
        var maxLetter = 1;
        for (var o in letterLengthDict) {
            if (letterLengthDict.hasOwnProperty(o)) {
                maxLetter = Math.max(maxLetter, letterLengthDict[o]);
            }
        }
        function getCharWidth(char) {
            try {
                var oo = letterLengthDict[char];
                if (typeof (oo) === "undefined")
                    return maxLetter;
                return oo;
            } catch (e) {
                console.warn("letterLen[" + char + "]:" + e);
                return maxLetter;
            }

        }

        function calcMWidth(str) {
            var w = 0;
            for (var i = 0; i < str.length; i++) {
                w += getCharWidth(str[i]);
            }
            return w / (letterLengthDict["M"]);
        }
        function calcNumChars(str, Ms) {
            var w = 0;
            var c = 0;
            var mLen = letterLengthDict["M"] * Ms;
            for (var i = 0; i < str.length; i++) {
                var thisW = getCharWidth(str[i]);
                if (w + thisW > mLen) {
                    return c;
                }
                c += 1;
                w += thisW;
            }
            return c;
        }

        function shorten() {
            var numMs = +(max || 17);
            //console.log("inshort");
            var mv = value;
            if (typeof (mv) === "string" && calcMWidth(mv) > numMs) {
                var length = calcNumChars(mv, numMs);
                mv = mv.substring(0, length)

                //Remove space
                var lastspace = mv.lastIndexOf(' ');
                if (lastspace !== -1) {
                    //Also remove . and , so its gives a cleaner result.
                    if (value.charAt(lastspace - 1) === '.' || value.charAt(lastspace - 1) === ',') {
                        lastspace = lastspace - 1;
                    }
                    mv = mv.substr(0, lastspace);
                }

                mv = mv + "...";

                //console.log(" - shorten");
            }

            return mv;
        }

        //value = value.substr(0, max);
        //if (wordwise) {
        //    var lastspace = value.lastIndexOf(' ');
        //    if (lastspace !== -1) {
        //        //Also remove . and , so its gives a cleaner result.
        //        if (value.charAt(lastspace - 1) === '.' || value.charAt(lastspace - 1) === ',') {
        //            lastspace = lastspace - 1;
        //        }
        //        value = value.substr(0, lastspace);
        //    }
        //}

        return shorten();//value + (tail || ' …');
    };
});

acapp.directive('rolegroups', function () {
    var directive = {
        restrict: 'A',
        scope: {
            groups: '=groups',
            onUpdate: '&onUpdate',
            onDeleteRole: '&onDeleteRole',
        },
        controller: ["$scope", "$http", "$timeout", "$element", "$rootScope", function ($scope, $http, $timeout, $element, $rootScope) {
            $scope.addRoleToNode = function (group) {
                var attachType = group.AttachType;
                var attachId = group.AttachId;
                var _clientTimestamp = new Date().getTime();
                $http.post("/Accountability/AddRole/?aid=" + attachId + "&atype=" + attachType + "&_clientTimestamp=" + _clientTimestamp, {})
                    .error(function (data) {
                        showJsonAlert(data, true, true);
                    });
            };

            $scope.newRoleButton = function (group) {
                var origLength = group.Roles.length;
                $scope.addRoleToNode(group);
                depth = 0;
                var setFocus = function () {
                    try {
                        if (depth == 100)
                            return;
                        if (origLength < group.Roles.length) {
                            console.log("focusing");
                            var res = $($($element).find("[data-group=" + group.Id + "] input").last()).focus();
                        } else {
                            depth += 1;
                            $timeout(setFocus, 20);
                        }
                    } catch (e) {
                        console.error(e);
                    }
                }
                $timeout(setFocus, 20);
            }

            $scope.updating = function (r) {
                $scope.onUpdate()(r);
            };
            $scope.deleting = function (r) {
                $scope.onDeleteRole()(r);
            };
            $scope.focusing = function () {
                $rootScope.$emit("RoleFocused");
            }
            $scope.blurring = function () {
                $rootScope.$emit("RoleBlurred");
            }
            $scope.checkCreateRole = function (evt, r, group, index) {
                $timeout(function () {
                    var origLength = group.Roles.length;
                    var depth = 0;
                    if (evt.which === 13) {
                        //var isLast = index === origLength - 1
                        if (/*isLast &&*/ group.Editable != false) {
                            $scope.addRoleToNode(group);
                            depth = 0;
                            var setFocus = function () {
                                try {
                                    if (depth == 100)
                                        return;
                                    if (origLength < group.Roles.length) {
                                        console.log("focusing");
                                        $($($element).find("[data-group=" + group.Id + "] input")[index + 1]).focus();
                                    } else {
                                        depth += 1;
                                        $timeout(setFocus, 20);
                                    }
                                } catch (e) {
                                    console.error(e);
                                }
                            }
                            $timeout(setFocus, 20);
                        }
                    } else if (evt.which == 8) {
                        if (r.Name == "" || typeof (r.Name) === "undefined" || r.Name == null) {
                            $scope.deleting(r);
                            if (origLength != 1) {
                                var setFocus = function () {
                                    try {
                                        if (depth == 100)
                                            return;
                                        if (origLength > group.Roles.length) {
                                            console.log("focusing");

                                            $($($element).find("[data-group=" + group.Id + "] input")[index - 1]).focus();
                                            //$("[data-group=" + group.Id + "]").find("input:nth-child(" + (index) + ")").focus();
                                        } else {
                                            depth += 1;
                                            $timeout(setFocus, 20);
                                        }
                                    } catch (e) {
                                        console.error(e);
                                    }
                                }
                                $timeout(setFocus, 20);
                            } else {
                                $($element).find("[data-group=" + group.Id + "] .add-role-row").focus();
                            }
                        }
                    } else if (evt.which == 38) {
                        if (index > 0) {
                            var that = $($($element).find("[data-group=" + group.Id + "] input")[index - 1]);
                            $(that).focus();
                            $timeout(function () {
                                var len = $(that).val().length * 2;
                                $(that)[0].setSelectionRange(len, len);
                            }, 0);
                        }
                    } else if (evt.which == 40) {
                        if (index < origLength - 1) {
                            var that = $($($element).find("[data-group=" + group.Id + "] input")[index + 1]);
                            $(that).focus();
                            $timeout(function () {
                                var len = $(that).val().length * 2;
                                $(that)[0].setSelectionRange(len, len);
                            }, 0);
                        } else {
                            var id = $($($element).find("[data-group=" + group.Id + "] input")).closest("g.node").attr("data-id");
                            $scope.$emit("ExpandNode", id);
                        }
                    }
                }, 1);
            }
        }],

        template: "<div class='role-groups'>" +
        "<div ng-repeat='group in groups' class='role-group' ng-if='::group.Editable!=false || group.Roles.length>0' data-group='{{group.Id}}'>" +
        "<div class='role-group-title'><span ng-if='!(groups.length==1 && group.AttachType==\"Position\")'>{{::group.AttachName}} Roles </span>" +
        "<div ng-if='::group.Editable!=false' class='add-role-row' ng-class='{tinyRow:(groups.length==1 && group.AttachType==\"Position\")}' ng-click='newRoleButton(group)' style='opacity:0;'> <div class='circle' title='Add Role'>+</div> </div>" +
        "</div>" +
        "<ul>" +
        "<li ng-repeat='role in group.Roles'  class='role-row' >" +
        //"<tt-role tt-overflow='18'  ng-model='role'></tt-role>" +
        "<input tt-overflow='18' ng-model-options='{debounce:75}'  ng-focus='focusing()' ng-blur='blurring()'" + " ng-keydown='checkCreateRole($event,role,group,$index)'" + " title='{{role.Name}}' class='role' ng-if='::group.Editable!=false' ng-model=\"role.Name\" ng-change=\"updating(role)\">" +
        "<div title='{{role.Name}}' class='role' ng-show='::group.Editable==false'>{{role.Name | ttOverflowTxt:false:18 }}</div>" +
        "<span ng-if='::group.Editable!=false' class='delete-role-row' ng-click=\"deleting(role)\" title='Delete Role' tabindex='-1'></span>" +
        "</li>" +
        "</ul>" +
        "<div ng-if='group.Roles.length==0' class='gray no-roles-placeholder'>" +
        "No roles. <span ng-if='::group.Editable!=false'>Use the <span class='glyphicon glyphicon-plus-sign'></span> button to add some.</span>" +
        "</div>" +
        "</div>" +
        "</div>"
    };
    return directive;
});
acapp.directive('rolegroupsfallback', function () {
    var directive = {
        restrict: 'A',
        replace: true,
        templateNamespace: 'svg',
        scope: {
            groups: '=groups',
            onUpdate: '&onUpdate',
            onDeleteRole: '&onDeleteRole',
        },
        controller: ["$scope", "$http", "$timeout", "$element", "$rootScope", function ($scope, $http, $timeout, $element, $rootScope) {
            $scope.addRoleToNode = function (group) {
                var attachType = group.AttachType;
                var attachId = group.AttachId;
                var _clientTimestamp = new Date().getTime();
                $http.post("/Accountability/AddRole/?aid=" + attachId + "&atype=" + attachType + "&_clientTimestamp=" + _clientTimestamp, {})
                    .error(function (data) {
                        showJsonAlert(data, true, true);
                    });

            };

            $scope.newRoleButton = function (group) {
                var origLength = group.Roles.length;
                $scope.addRoleToNode(group);
                depth = 0;
                var setFocus = function () {
                    try {
                        if (depth == 100)
                            return;
                        if (origLength < group.Roles.length) {
                            console.log("focusing");
                            var res = $($($element).find("[data-group=" + group.Id + "] input").last()).focus();
                        } else {
                            depth += 1;
                            $timeout(setFocus, 20);
                        }
                    } catch (e) {
                        console.error(e);
                    }
                }
                $timeout(setFocus, 20);
            }


            $scope.updating = function (r) {
                $scope.onUpdate()(r);
            };
            $scope.deleting = function (r) {
                $scope.onDeleteRole()(r);
            };

            $scope.focusing = function () {
                $rootScope.$emit("RoleFocused");
            }
            $scope.blurring = function () {
                $rootScope.$emit("RoleBlurred");
            }
            $scope.checkCreateRole = function (evt, r, group, index) {
                $timeout(function () {
                    var origLength = group.Roles.length;
                    var depth = 0;
                    if (evt.which === 13) {
                        if (group.Editable != false) {
                            $scope.addRoleToNode(group);
                            depth = 0;
                            var setFocus = function () {
                                try {
                                    if (depth == 100)
                                        return;
                                    if (origLength < group.Roles.length) {
                                        console.log("focusing");
                                        $($($element).find("[data-group=" + group.Id + "] input")[index + 1]).focus();
                                    } else {
                                        depth += 1;
                                        $timeout(setFocus, 20);
                                    }
                                } catch (e) {
                                    console.error(e);
                                }
                            }
                            $timeout(setFocus, 20);
                        }
                    } else if (evt.which == 8) {
                        if (r.Name == "" || typeof (r.Name) === "undefined" || r.Name == null) {
                            $scope.deleting(r);
                            if (origLength != 1) {
                                var setFocus = function () {
                                    try {
                                        if (depth == 100)
                                            return;
                                        if (origLength > group.Roles.length) {
                                            console.log("focusing");
                                            $($($element).find("[data-group=" + group.Id + "] input")[index - 1]).focus();
                                        } else {
                                            depth += 1;
                                            $timeout(setFocus, 20);
                                        }
                                    } catch (e) {
                                        console.error(e);
                                    }
                                }
                                $timeout(setFocus, 20);
                            } else {
                                $($element).find("[data-group=" + group.Id + "] .add-role-row").focus();
                            }
                        }
                    } else if (evt.which == 38) {
                        if (index > 0) {
                            var that = $($($element).find("[data-group=" + group.Id + "] input")[index - 1]);
                            $(that).focus();
                            $timeout(function () {
                                var len = $(that).val().length * 2;
                                $(that)[0].setSelectionRange(len, len);
                            }, 0);
                        }
                    } else if (evt.which == 40) {
                        if (index < origLength - 1) {
                            var that = $($($element).find("[data-group=" + group.Id + "] input")[index + 1]);
                            $(that).focus();
                            $timeout(function () {
                                var len = $(that).val().length * 2;
                                $(that)[0].setSelectionRange(len, len);
                            }, 0);
                        } else {
                            var id = $($($element).find("[data-group=" + group.Id + "] input")).closest("g.node").attr("data-id");
                            $scope.$emit("ExpandNode", id);
                        }
                    }
                }, 1);
            }
        }],
        template: "<g class='role-groups'>" +
        "<g ng-repeat='group in groups' class='role-group' ng-if='::group.Editable!=false || group.Roles.length>0' data-group='{{group.Id}}'>" +
        "<g class='role-group-title' ><text ng-if='!(groups.length==1 && group.AttachType==\"Position\")'>{{::group.AttachName}} Roles </text>" +
        "<g ng-if='::group.Editable!=false' transform='translate(200,0)' class='add-role-row acc-fallback-ignore' ng-class='{tinyRow:(groups.length==1 && group.AttachType==\"Position\")}' ng-click='newRoleButton(group)' style='opacity:1;'> <circle class='acc-fallback-ignore' cx='3' cy='-4' fill='#ff0000' r='6'></circle><text>+</text> </g>" +
        "</g>" +
        "<g>" +
        "<g ng-repeat='role in group.Roles'  class='role-row' ng-init='rolesIndex=$index'>" +
        "<text title='{{role.Name}}' class='role' transform='translate(0,{{($index*16)}})'>{{role.Name}}</text>" +
        "<text ng-if='::group.Editable!=false' transform='translate(-20,{{($index*16)}})' class='delete-role-row' ng-click=\"deleting(role)\" title='Delete Role' tabindex='-1'>x</text>" +
        "</g>" +
        "</g>" +
        "<g ng-if='group.Roles.length==0' class='gray no-roles-placeholder'>" +
        "<text>No roles.</text><text dx='35' ng-if='::group.Editable!=false'>Use the + button to add some.</text>" +
        "</g>" +
        "</g>" +
        "</g>"
    };
    return directive;
});


acapp.controller('ACController', ['$scope', '$http', '$timeout', '$location', 'radial', 'orgId', 'chartId', 'dataUrl', "$compile", "$sce", "$q", "$window", "$rootScope",
    function ($scope, $http, $timeout, $location, radial, orgId, chartId, dataUrl, $compile, $sce, $q, $window, $rootScope) {
        $scope.orgId = orgId;
        $scope.chartId = chartId;
        $scope.model = $scope.model || {};
        $scope.model.height = $scope.model.height || 10000;
        $scope.model.width = $scope.model.width || 10000;

        $scope.suspendUpdate = false;

        function rejoin(connection, proxy, callback) {
            try {
                if (proxy) {
                    proxy.invoke("join", $scope.orgId, connection.id).done(function () {
                        console.log("rejoin");
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

        var r = radial($scope, 'organizationHub', rejoin);
        var tzoffset = Time.tzoffset();// r.updater.tzoffset;
        $scope.functions.reload = function (clearData) {
            if (typeof (clearData) === "undefined")
                clearData = false;
            Time.tzoffset();//tzoffset();
            console.log("reloading...");
            var url = dataUrl;
            if (dataUrl.indexOf("{0}") != -1) {
                url = url.replace("{0}", $scope.chartId);
            } else {
                url = url + $scope.chartId;
            }

            //var date = ((+new Date()) /*+ (window.tzoffset * 60 * 1000)*/);
            //if (dataUrl.indexOf("?") != -1) {
            //	url += "&_clientTimestamp=" + date;
            //} else {
            //	url += "?_clientTimestamp=" + date;
            //}
            url = Time.addTimestamp(url);

            $http({ method: 'get', url: url })
                .success(function (data, status) {
                    var ddr = undefined;

                    console.log("Data Loaded");
                    r.updater.convertDates(data);
                    if (clearData) {
                        r.updater.clearAndApply(data);
                        $scope.search.searchTerms = [];
                    } else {
                        r.updater.applyUpdate(data);
                    }
                    $rootScope.$emit("SelectNode", $scope.model.center || 0);

                }).error(function (data, status) {
                    console.log("Error");
                    console.error(data);
                });
        }
        $scope.functions.reload(true);

        //AJAX
        function fixNodeRecurse(self) {
            if (self && self.Type == "AngularAccountabilityNode") {
                delete self.children;
                delete self._children;
                delete self.parent;
            }
        }
        $scope.functions.sendUpdate = function (self) {
            var dat = angular.copy(self);
            var _clientTimestamp = new Date().getTime();

            fixNodeRecurse(dat);
            //r.updater.convertDatesForServer(dat, Time.tzoffset());
            console.warn("Dates were not converted, this needs to be confirmed");

            var url = Time.addTimestamp("/Accountability/Update" + self.Type + "?connectionId=" + $scope.connectionId);

            $http.post(url, dat).error(function (data) {
                showJsonAlert(data, true, true);
            });
        };


        var RemoveRow = Undo.Command.extend({
            constructor: function (data) {
                //this.id = data.id;
                //this.oldParent = data.oldParentId;
                //this.newParent = data.newParentId;

                //this.change = function (nodeId, newParent, revertId) {
                //	var _clientTimestamp = new Date().getTime();
                //	$http.post("/Accountability/Swap/" + nodeId + "?parent=" + newParent + "&connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, {})
                //		.then(function () { }, function (data) {
                //			showJsonAlert(data.data, true, true);
                //			$rootScope.$emit("SwapNode", nodeId, revertId);

                //		});
                //}
                this.self = data;
            },
            execute: function () {
                var _clientTimestamp = new Date().getTime();
                $http.post("/Accountability/Remove" + this.self.Type + "/" + this.self.Id + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, null)
                    .error(function (data) {
                        showJsonAlert(data, true, true);
                    });
            },
            undo: function () {
                var _clientTimestamp = new Date().getTime();
                $http.post("/Accountability/Unremove" + this.self.Type + "/" + this.self.Id + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, null)
                    .error(function (data) {
                        showJsonAlert(data, true, true);
                    });
            }
        });

        $scope.functions.removeRow = function (self) {
            undoStack.execute(new RemoveRow(self));
        };

        //SEARCH
        var self = this;
        $scope.search = {};
        $scope.search.querySearch = function (query) {
            function createFilterFor(query) {
                var lowercaseQuery = angular.lowercase(query);
                return function filterFn(x) {
                    if (x.Managing == false)
                        return false;

                    var index = (x.Name + "").toLowerCase().indexOf(lowercaseQuery);
                    var any = index != -1 && (index === 0 || x.Name[index - 1] == " ");
                    if (x.User && x.User.Name) {
                        var f = x.User.Name.toLowerCase().indexOf(lowercaseQuery);
                        any = any || (f != -1 && f == 0 || x.User.Name[f - 1] == " ");
                    }
                    return any;
                };
            }
            var possible = $scope.model.data.AllUsers;
            var results = possible.filter(createFilterFor(query));
            var tcQ = toTitleCase(query);
            results.push({
                Name: tcQ + " (Create User)",
                RealName: tcQ,
                Id: -2
            });

            return results;
        }
        $scope.search.queryPositions = function (query) {
            return $http({
                method: 'GET',
                url: '/dropdown/angularpositions?create=true&q=' + query
            }).then(function (results) {
                return results.data;
            });
        }
        $scope.search.findNode = function (query) {
            function createFilterFor(query) {
                var lowercaseQuery = angular.lowercase(query);
                return function filterFn(x) {
                    var any = (x.Name + "").toLowerCase().indexOf(lowercaseQuery) === 0;
                    if (x.User && x.User.Name) {
                        var f = x.User.Name.toLowerCase().indexOf(lowercaseQuery);
                        any = any || (f != -1 && f == 0 || x.User.Name[f - 1] == " ");
                    }
                    return any;
                };
            }

            var possible = [];
            for (var i in $scope.model.Lookup) {
                if (arrayHasOwnIndex($scope.model.Lookup, i)) {
                    var n = $scope.model.Lookup[i];
                    if (i.indexOf("AngularAccountabilityNode_") == 0 && n.User) {
                        possible.push(n);
                    }
                }
            }
            return possible.filter(createFilterFor(query));
        }
        $scope.search.searchTerms = {};

        //SELECT AUTOCOMPLETE
        //ULTRA HACK
        $rootScope.$on("BeginCallbackSignalR", function (event, count) {
            $scope.suspendUpdate = true;
            //console.log("suspend update (" + count + ")...");
        });
        $rootScope.$on("EndCallbackSignalR", function (event, count) {
            if (count <= 0) {
                $scope.suspendUpdate = false;
                //console.log("...resume update");
            } else {
                //console.log("...(" + count + ")...");
            }
            //console.log("------------------");
        });

        $scope.roleFocused = false;
        $rootScope.$on("RoleFocused", function (event) {
            $scope.roleFocused = true;
        });
        $rootScope.$on("RoleBlurred", function (event) {
            $scope.roleFocused = false;
        });
        //END ULTRA HACK
        $scope.functions.selectedItemChange = function (node) {
            d3.select(".selected").classed("selected", false).attr("filter", null);
            if (node) {
                $rootScope.$emit("SelectNode", node.Id);
            }
        };
        $scope.functions.selectedItemChange_UpdateNode = function (id) {
            console.log("selectedItemChange_UpdateNode");
            if (!$scope.suspendUpdate) {
                console.log("..sending update");
                $scope.functions.sendUpdate($scope.model.Lookup['AngularAccountabilityNode_' + id + '']);
            } else {
                console.log("..applying update");
                try {
                    $scope.$eval("search.searchPos_" + id + "=$scope.model.Lookup['AngularAccountabilityNode_" + id + "'].Group.Position.Name");
                } catch (e) {
                    console.error("Silly position update " + e)
                }
            }
        };
        $scope.functions.selectedItemChange_UpdateUser = function (id) {
            console.log("selectedItemChange_UpdateUser");
            if (!$scope.suspendUpdate) {
                console.log("..sending update");
                $scope.functions.sendUpdate($scope.model.Lookup['AngularAccountabilityNode_' + id + '']);
            } else {
                console.log("..applying update");
                try {
                    $scope.$eval("search.searchText_" + id + "=$scope.model.Lookup['AngularAccountabilityNode_" + id + "'].User.Name");
                } catch (e) {
                    console.error("Silly user update " + e)
                }
            }
        };

        //TREE EVENT FUNCTIONS
        $scope.nodeWatch = function (node) {
            var uname = null;
            var roles = null;
            var pos = false;
            if (node.User && node.User.Name)
                uname = node.User.Name;
            if (node.Group && node.Group.RoleGroups) {
                if ($scope.roleFocused) {
                    var copy = node.Group.RoleGroups.map(function (rg) {
                        var cp = angular.copy(rg);
                        cp.Roles = cp.Roles.map(function () { return null; });
                        return cp;
                    });
                    //    var rolesGroups = [];
                    //    for(var c in copy){
                    //        var rg = copy[c];
                    //        rg.Roles = null;
                    //        rolesGroups.push(rg);
                    //    }
                    roles = copy;
                }
                else
                    roles = node.Group.RoleGroups;
            }
            if (node.Group)
                pos = node.Group.Position;
            return {
                name: uname,
                roles: roles,
                pos: pos
            };
        }
        var SwapParent = Undo.Command.extend({
            constructor: function (data) {
                this.id = data.id;
                this.oldParent = data.oldParentId;
                this.newParent = data.newParentId;

                this.change = function (nodeId, newParent, revertId) {
                    var _clientTimestamp = new Date().getTime();
                    $http.post("/Accountability/Swap/" + nodeId + "?parent=" + newParent + "&connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, {})
                        .then(function () { }, function (data) {
                            showJsonAlert(data.data, true, true);
                            $rootScope.$emit("SwapNode", nodeId, revertId);

                        });
                }
            },
            execute: function () {
                $rootScope.$emit("SwapNode", this.id, this.newParent);
                this.change(this.id, this.newParent, this.oldParent);
            },
            undo: function () {
                $rootScope.$emit("SwapNode", this.id, this.oldParent);
                this.change(this.id, this.oldParent, this.newParent);
            }
        });

        $scope.dragStart = function (d) {
            var targ = undefined;
            if (d3.event.sourceEvent && d3.event.sourceEvent.srcElement)
                targ = d3.event.sourceEvent.srcElement;
            else
                targ = d3.event.sourceEvent.target;


            if (!d3.select(targ).classed("move-icon")) {
                throw "Incorrect selector";
            }
            if (d.Editable == false) {
                throw "Cannot edit";
            }
        }
        $scope.dragEnd = function (d, data) {
            if (data) {
                if (data.swap) {
                    var _clientTimestamp = new Date().getTime();
                    console.log(data.oldParentId + "->" + data.newParentId);
                    undoStack.execute(new SwapParent(data));
                }
            }
        }

        $scope.collapseExpand = function (d) {
            if ($(d3.event.srcElement || d3.event.target).closest(".minimize-icon").length != 1) {
                throw "Incorrect selector";
            }
        }

        $scope.clearIfNull = function (item, searchText, update) {
            if (!item) {
                $scope.$eval(searchText + "=null");
            }

            if (update) {
                var result = $scope.$eval(update);
                if (result) {
                    $scope.functions.sendUpdate(result);
                }
            }
        }

        $scope.functions.fixName = function (id, nameVar, item, self) {
            if (!self.focused) {
                if (item == null) {
                    $scope.$eval(nameVar + "=null");
                } else {
                }
            } else {
                if (item && item.Id == -2) {
                    $scope.$eval(nameVar + "=null");
                    var managerId = -3;
                    //try{
                    //	var node = $scope.model.Lookup["AngularAccountabilityNode_" + id];
                    //	if (node && node.parent && node.parent.Id) {
                    //		managerId = node.parent.Id;
                    //	}					
                    //} catch (e) {
                    //}

                    $scope.functions.showModal('Add user',
                        '/User/AddModal?managerNodeId=' + managerId + "&forceNoSend=true&forceManager=true&name=" + item.RealName + "&hideIsManager=true&hidePosition=true&nodeId=" + id
                        , '/nexus/AddManagedUserToOrganization'
                    );

                } else {
                    $scope.functions.selectedItemChange_UpdateNode(id);
                }
            }
        }
        self.dirtyList = {};
        $scope.functions.markDirty = function (id) {
            self.dirtyList[id] = true;
        }

        var fallback = false;
        if (msieversion()) {
            var v = msieversion();
            if (v >= 11) {
                fallback = true; //Edge
            } else {
                fallback = true; //IE
            }
        }

        var canReorder = function (d) {
            var res = d.Editable == true && $scope.model.data.CanReorganize;
            if (res)
                debugger;
            return res;
        }
        var standardNodeEnter = function (nodeEnter) {

            var rect = nodeEnter.append("rect").attr("class", "acc-rect")
                .attr("width", 0).attr("height", 0).attr("x", 0).attr("rx", 2).attr("ry", 2);

            var node = nodeEnter.append("foreignObject")
                .classed("foreignObject", true)
                .append("xhtml:div").classed("acc-node", true)
                .style("font", "14px 'Helvetica Neue'");

            var buttonsTop = node.append("xhtml:div")
                .classed("acc-buttons move-icon top-bar", true)
                .classed("move-icon", function (d) {
                    return d.Editable != false;
                }).text(function (d) {
                    return d.Name;
                });

            var contents = node.append("xhtml:div").classed("acc-contents", true);

            var position = contents.append("xhtml:div").classed("acc-position", true);

            var posAutoComplete = position.append("md-autocomplete")
                .attr("placeholder", "Function")
                .attr("tt-md-overflow", "14")
                .attr("tt-md-clear-no-match", "true")
                .attr("md-input-name", function (d) {
                    return "searchPosName_" + d.Id;
                }).attr("ng-disabled", function (d) {
                    if (d.Editable == false)
                        return "true";
                    return null;
                })
                .attr("md-blur", function (d) {
                    return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,\"search.searchPos_" + d.Id + "\",\"model.Lookup['AngularAccountabilityNode_" + d.Id + "']\")";
                })
                .attr("md-selected-item", function (d) {
                    return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
                }).attr("md-item-text", function (d) {
                    return "pitem.Name";
                }).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
                .attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
                .attr("ng-class", function (d) {
                    return "{'no-match':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position ==null && search.searchPos_" + d.Id + " }";
                }).attr("ng-attr-title", function (d) {
                    return "{{(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position ==null)?'Invalid: Position does not exist.':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position.Name}}";
                })
                .attr("md-selected-item-change", function (d) {
                    return "functions.fixName(" + d.Id + ",'search.searchPos_" + d.Id + "',model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,this)";
                }).attr("md-no-cache", "true");

            //Is this even used? vvv
            posAutoComplete.append("md-item-template").append("span").attr("md-highlight-text", function (d) { return "search.searchPos_" + d.Id; }).attr("md-highlight-flags", "^i").text("{{pitem.Name}}{{pitem.Create}}");


            var owner = contents.append("xhtml:div").classed("acc-owner", true);
            var autoComplete = owner.append("md-autocomplete")
                .attr("tt-md-overflow", "14")
                .attr("tt-md-clear-no-match", "true")
                .attr("md-blur", function (d) {
                    return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User,\"search.searchText_" + d.Id + "\",\"model.Lookup['AngularAccountabilityNode_" + d.Id + "']\")";
                }).attr("md-selected-item", function (d) {
                    return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User";
                }).attr("md-item-text", function (d) {
                    return "uitem.Name";
                }).attr("md-items", function (d) { return "uitem in search.querySearch(search.searchText_" + d.Id + ")"; })
                .attr("md-search-text", function (d) { return "search.searchText_" + d.Id; })
                .attr("placeholder", "Employee")
                .attr("ng-class", function (d) {
                    return "{'no-match':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User ==null && search.searchText_" + d.Id + " }";
                }).attr("ng-attr-title", function (d) {
                    return "{{(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User ==null)?'Invalid: Employee does not exist.':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User.Name}}";
                })
                .attr("ng-disabled", function (d) {
                    if (d.Editable == false)
                        return "true";
                    return null;
                }).attr("md-selected-item-change", function (d) {
                    return "functions.fixName(" + d.Id + ",'search.searchText_" + d.Id + "',model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User,this)";
                });
            autoComplete.append("md-item-template")
                .append("span")
                .attr("md-highlight-text", function (d) { return "search.searchText_" + d.Id; })
                .attr("md-highlight-flags", "^i")
                .text("{{uitem.Name||uitem.User.Name}}");
            autoComplete.append("md-not-found")
                .text(function (d) {
                    return "No matches were found.";
                });

            //ROLES

            contents.append("div").attr("rolegroups", "").attr("groups", function (d) {
                return "model.Lookup['" + d.Group.Key + "'].RoleGroups";
            }).attr("on-update", "functions.sendUpdate").attr("on-delete-role", "functions.removeRow");


            nodeEnter.on('mouseover', function (d) {
                d3.select(this).selectAll(".add-role-row").style("opacity", 1);
                d3.select(this).selectAll(".node-button").transition().style("opacity", 1);
            }).on('mouseout', function (d) {
                d3.select(this).selectAll(".add-role-row").style("opacity", 0);
                d3.select(this).selectAll(".node-button").transition().style("opacity", 0);
            });

            var buttonsBottom = node.append("xhtml:div")
                .classed("acc-buttons bottom-bar", true);
            var clickAddNode = function (d) {
                if (d.Id) {
                    addNode(d.Id);
                    d3.event.stopPropagation();
                } else {
                    throw "Add node requires Id"
                }
            };
            var clickRemoveNode = function (d) {
                if (d.Id) {
                    if ((d._children && d._children.length) || (d.children && d.children.length)) {
                        showModal({
                            title: "Cannot delete accountability node that has direct reports.",
                            noCancel: true,
                            icon: "warning",
                        });
                    } else {
                        var runDelete = function () {
                            $.ajax({ url: "/Accountability/Remove/" + id });
                        };

                        var fields = [];
                        if (d.User)
                            fields.push({ type: "h6", value: "*Deleting this node DOES NOT remove this user from the organization." });
                        var id = d.Id;
                        if (d.User || (d.Group && d.Group.Position)) {
                            showModal({
                                title: "Are you sure you want to delete this accountability node?",
                                icon: "warning",
                                fields: fields,
                                success: runDelete
                            });
                        } else {
                            //Cell is empty just delete it.
                            runDelete();
                        }
                    }
                    d3.event.stopPropagation();
                } else {
                    throw "Add node requires Id"
                }
            };

            var printNode = function (d) {

                genPdf(true, d.Id);

                //console.log(d);
                //if (d.Id) {
                //    var ajax = {
                //        url: "/pdf/ac?fit=" + false + "&pw=11&ph=8.5&selected=" + d.Id,
                //        method: "POST",
                //        dataType: 'native',
                //        xhrFields: {
                //            responseType: 'blob'
                //        },
                //        contentType: "application/json; charset=utf-8",
                //        //processData: true,
                //        success: function (blob) {
                //            console.log(blob.size);
                //            var link = document.createElement('a');
                //            link.href = window.URL.createObjectURL(blob);
                //            link.download = "Accountability Chart.pdf";
                //            link.click();
                //        }, error: function (D) {
                //            showAlert("An error occurred");
                //        }
                //    };
                //    $.ajax(ajax);
                //}
                //else {
                //    throw "node id requires"
                //}
            };

            function onClickKey(func) {
                debugger;
                if (d3.event && (d3.event.keyCode == 13 || d3.event.keyCode == 32))
                    return func;
                return function (d) { };
            }


            var expandNode = function (d) {
            };

            var childIndication = nodeEnter.append("g").classed("childIndication", true).attr("transform", "translate(99.75,75)")
            childIndication.append("circle").attr("r", function (d) {
                if (d.Id) {
                    if ((d._children && d._children.length) || (d.children && d.children.length)) {
                        return 1.2;
                    }
                }
                else {
                    return 0;
                }
            }).attr("stroke-width", 3).attr("fill", "#EF7622").attr("cx", 0).attr("cy", 10);
            childIndication.append("circle").attr("r", function (d) {
                if (d.Id) {
                    if ((d._children && d._children.length) || (d.children && d.children.length)) {
                        return 1.2;
                    }
                }
                else {
                    return 0;
                }
            }).attr("stroke-width", 3).attr("fill", "#EF7622").attr("cx", 0).attr("cy", 15);
            childIndication.append("circle").attr("r", function (d) {
                if (d.Id) {
                    if ((d._children && d._children.length) || (d.children && d.children.length)) {
                        return 1.2;
                    }
                }
                else {
                    return 0;
                }
            }).attr("stroke-width", 3).attr("fill", "#EF7622").attr("cx", 0).attr("cy", 20);

            nodeEnter.append("rect").classed("bounding-box", true);
            var minimizeNodeBtn = nodeEnter.append("g").classed("button minimize minimize-icon node-button", true).style("opacity", 0).attr("tabindex", 0)
                .on("click", expandNode)
                .on("keydown", function (d) {
                    if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                        expandNode(d);
                });
            minimizeNodeBtn.append("circle").attr("r", 10).attr("title", "Collapse node").on("click", expandNode);
            minimizeNodeBtn.append("title").text("Hide Direct Reports");
            minimizeNodeBtn.append("text").classed("glyphicon", true).attr("title", "Hide Direct Reports").on("click", expandNode);

            var printPdfBtn = nodeEnter.append("g").classed("button print node-button", true).style("opacity", 0).attr("tabindex", 0)
                .on("click", printNode)
                .on("keydown", function (d) {
                    if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                        printNode(d);
                });
            printPdfBtn.append("circle").attr("r", 10).attr("title", "Print")//.on("click", printNode);
            printPdfBtn.append("title").text("Print");
            printPdfBtn.append("text").classed("glyphicon glyphicon-glyphicon-print", true).attr("title", "Print").text("")//.on("click", printNode);


            var addNodeBtn = nodeEnter.append("g").classed("button add node-button", true).attr("tabindex", 0).style("opacity", 0)
                .on("click", clickAddNode)
                .on("keydown", function (d) {
                    if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                        clickAddNode(d);
                });
            addNodeBtn.append("circle").attr("r", 10).attr("title", "Add direct report").on("click", clickAddNode);
            addNodeBtn.append("title").text("Add direct report");
            addNodeBtn.append("text").text("+").attr("title", "Add direct report").on("click", clickAddNode);


            var deleteNodeBtn = nodeEnter.append("g").classed("button remove node-button", true).attr("tabindex", 0).style("opacity", 0).on("click", clickRemoveNode).on("keyup", function (d) {
                if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                    clickRemoveNode(d);
            });
            deleteNodeBtn.append("circle").attr("r", 10).attr("title", "Remove node").on("click", clickRemoveNode);
            deleteNodeBtn.append("title").text("Remove from Accountability Chart");
            deleteNodeBtn.append("text").classed("glyphicon ", true).attr("title", "Remove node").text("").on("click", clickRemoveNode);


            nodeEnter.call(function (d3Selection) {
                d3Selection.each(function (d, i) {
                    // this is the actual DOM element
                    var newScope = $scope.$new();
                    $compile(this)(newScope);
                });
            });
        };



        ///////////////////////////////FALLBACK START///////////////////////////////

        var fallbackNodeEnter = function (nodeEnter) {
            nodeEnter.classed("acc-fallback", true);

            var rect = nodeEnter.append("rect").attr("class", "acc-rect acc-fallback-ignore")
                .attr("width", 0).attr("height", 0).attr("x", 0).attr("rx", 2).attr("ry", 2);

            var node = nodeEnter.append("g")
                .classed("foreignObject", true)
                .append("g").classed("acc-node", true)
                .style("font", "14px 'Helvetica Neue'");

            var buttonsTop = node.append("g").classed("acc-buttons top-bar acc-fallback-ignore", true);

            buttonsTop.text(function (d) {
                return d.Name;
            });
            buttonsTop.append("rect").classed("move-icon", function (d) {
                return canReorder(d) != false;
            });
            ////

            var contents = node.append("g").classed("acc-contents", true);
            contents.append("rect").classed("acc-fallback-placeholder", true).attr("width", "175").attr("height", "20");

            var position = contents.append("g").classed("acc-position", true).attr("width", "200px");//.style("width", "100px").style("height", "100px");

            var posAutoComplete = position.append("text").classed("acc-fallback-hide", true).attr("dy", "23").attr("dx", "6").append("tspan").text(function (d) {
                return "{{model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position.Name}}";
            });
            //.attr("placeholder", "Function")
            //.attr("md-input-name", function (d) {
            //	return "searchPosName_" + d.Id;
            //}).attr("ng-disabled", function (d) {
            //	if (d.Editable == false)
            //		return "true";
            //	return null;
            //})
            //.attr("md-blur", function (d) {
            //	return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,\"search.searchPos_" + d.Id + "\",\"model.Lookup['AngularAccountabilityNode_" + d.Id + "']\")";
            //})
            //.attr("md-selected-item", function (d) {
            //	return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
            //}).attr("md-item-text", function (d) {
            //	return "pitem.Name";
            //}).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
            //.attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
            //.attr("ng-class", function (d) {
            //	return "{'no-match':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position ==null && search.searchPos_" + d.Id + " }";
            //}).attr("ng-attr-title", function (d) {
            //	return "{{(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position ==null)?'Invalid: Position does not exist.':model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position.Name}}";
            //})
            //.attr("md-selected-item-change", function (d) {
            //	return "functions.fixName(" + d.Id + ",'search.searchPos_" + d.Id + "',model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,this)";
            //}).attr("md-no-cache", "true");


            var owner = contents.append("g").classed("acc-owner", true).attr("transform", "translate(0,25)");
            var autoComplete = owner.append("text").classed("acc-fallback-hide", true).attr("dy", "18").attr("dx", "6").append("tspan").text(function (d) {
                return "{{model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User.Name}}";
            });

            //ROLES

            contents.append("g").attr("rolegroupsfallback", "").attr("transform", "translate(6,65)").attr("groups", function (d) {
                return "model.Lookup['" + d.Group.Key + "'].RoleGroups";
            }).attr("on-update", "functions.sendUpdate").attr("on-delete-role", "functions.removeRow");


            //nodeEnter.on('mouseover', function (d) {
            //	d3.select(this).selectAll(".add-role-row").style("opacity", 1);
            //	d3.select(this).selectAll(".node-button").transition().style("opacity", 1);
            //}).on('mouseout', function (d) {
            //	d3.select(this).selectAll(".add-role-row").style("opacity", 0);
            //	d3.select(this).selectAll(".node-button").transition().style("opacity", 0);
            //});

            var buttonsBottom = node.append("g")
                .classed("acc-buttons bottom-bar acc-fallback-ignore", true);
            var clickAddNode = function (d) {
                if (d.Id) {
                    addNode(d.Id);
                    d3.event.stopPropagation();
                } else {
                    throw "Add node requires Id"
                }
            };
            var clickRemoveNode = function (d) {
                if (d.Id) {
                    if ((d._children && d._children.length) || (d.children && d.children.length)) {
                        showModal({
                            title: "Cannot delete accountability node that has direct reports.",
                            noCancel: true,
                            icon: "warning",
                        });
                    } else {
                        var runDelete = function () {
                            $.ajax({ url: "/Accountability/Remove/" + id });
                        };

                        var fields = [];
                        if (d.User)
                            fields.push({ type: "h6", value: "*Deleting this node DOES NOT remove this user from the organization." });
                        var id = d.Id;

                        if (d.User || (d.Group && d.Group.Position)) {
                            showModal({
                                title: "Are you sure you want to delete this accountability node?",
                                icon: "warning",
                                fields: fields,
                                success: runDelete
                            });
                        } else {
                            //Cell is empty just delete it.
                            runDelete();
                        }
                    }
                    d3.event.stopPropagation();
                } else {
                    throw "Add node requires Id"
                }
            };

            function onClickKey(func) {
                debugger;
                if (d3.event && (d3.event.keyCode == 13 || d3.event.keyCode == 32))
                    return func;
                return function (d) { };
            }

            var expandNode = function (d) {
            };
            nodeEnter.append("rect").classed("bounding-box acc-fallback-ignore", true);
            var minimizeNodeBtn = nodeEnter.append("g").classed("button minimize minimize-icon node-button acc-fallback-ignore", true).style("opacity", 1).attr("tabindex", 0)
                .on("click", expandNode)
                .on("keydown", function (d) {
                    if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                        expandNode(d);
                });
            minimizeNodeBtn.append("circle").classed("acc-fallback-ignore", true).attr("r", 10).attr("title", "Collapse node").on("click", expandNode);
            minimizeNodeBtn.append("text").classed("glyphicon acc-fallback-ignore", true).attr("transform", "translate(-4.5,4.5)").attr("title", "Remove node").on("click", expandNode);
            var addNodeBtn = nodeEnter.append("g").classed("button add node-button acc-fallback-ignore", true).attr("tabindex", 0).style("opacity", 1)
                .on("click", clickAddNode)
                .on("keydown", function (d) {
                    if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                        clickAddNode(d);
                });
            var ci = addNodeBtn.append("circle").classed("acc-fallback-ignore", true).attr("r", 10).attr("title", "Add direct report").on("click", clickAddNode);
            debugger;
            addNodeBtn.append("title").text("Add direct report");
            addNodeBtn.append("text").attr("transform", "translate(-4.5,4.5)").classed("acc-fallback-ignore", true).text("+").attr("title", "Add direct report").on("click", clickAddNode);

            var deleteNodeBtn = nodeEnter.append("g").classed("button remove node-button acc-fallback-ignore", true).attr("tabindex", 0).style("opacity", 1).on("click", clickRemoveNode).on("keyup", function (d) {
                if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
                    clickRemoveNode(d);
            });
            deleteNodeBtn.append("circle").classed("acc-fallback-ignore", true).attr("r", 10).attr("title", "Remove node").on("click", clickRemoveNode);
            deleteNodeBtn.append("text").attr("transform", "translate(-4.5,4.5)").classed("glyphicon glyphicon-trash acc-fallback-ignore", true).attr("title", "Remove node").text("").on("click", clickRemoveNode);

            nodeEnter.call(function (d3Selection) {
                d3Selection.each(function (d, i) {
                    // this is the actual DOM element
                    var newScope = $scope.$new();
                    $compile(this)(newScope);
                });
                $timeout(function () {
                    d3.selectAll(".acc-fallback-hide").classed("acc-fallback-hide", false);
                }, 100);
            });
        }

        var standardNodeUpdate = function (nodeUpdate) {
            nodeUpdate.select(".acc-rect").attr("width", function (d) {
                //console.log("update called");
                return d.width;
            }).attr("height", function (d) {
                return (d.height || 20);
            });

            nodeUpdate.select(".button.print").attr("transform", function (d) {
                return "translate(" + (d.width / 2 - 60) + "," + (d.height + 15.5) + ")";
            }).attr("tabindex", function (d) {
                return canReorder(d) == false ? "-1" : "0";
            }).style("display", function (d) {
                debugger;
                return canReorder(d) == false ? "none" : null;
            });

            nodeUpdate.select(".button.add").attr("transform", function (d) {
                return "translate(" + (d.width / 2 - 30) + "," + (d.height + 15.5) + ")";
            }).attr("tabindex", function (d) {
                return canReorder(d) == false ? "-1" : "0";
            }).style("display", function (d) {
                debugger;
                return canReorder(d) == false ? "none" : null;
            });

            nodeUpdate.select(".button.remove").attr("transform", function (d) {
                return "translate(" + (d.width / 2 + 30) + "," + (d.height + 15.5) + ")";
            }).attr("tabindex", function (d) {
                return canReorder(d) == false ? "-1" : "0";
            }).style("display", function (d) {
                debugger;
                return canReorder(d) == false ? "none" : null;
            });

            nodeUpdate.select(".button.minimize").attr("transform", function (d) {
                return "translate(" + (d.width / 2 - .25) + "," + (d.height + 15.5) + ")";
            });

            nodeUpdate.select(".childIndication").attr("transform", function (d) {
                return "translate(" + (d.width / 2 - .25) + "," + (d.height - 4) + ")";
            }).style("display", function (d) {
                if (!d.parent)
                    return "none";
                else
                    return null;
            });

            nodeUpdate.select(".button.minimize")
                .classed("minimize-icon", function (d) {
                    return (d.children && d.children.length) || (d._children && d._children.length);
                }).attr("tabindex", function (d) {
                    return !((d.children && d.children.length) || (d._children && d._children.length)) ? "-1" : "0";
                }).style("display", function (d) {
                    return !((d.children && d.children.length) || (d._children && d._children.length)) ? "none" : null;
                })
                .attr("title", function (d) {
                    if (d._children && d._children.length)
                        return "Expand direct reports";
                    if (d.children && d.children.length)
                        return "Collapse direct reports";
                    return null;
                })
                .select("text")
                .text(function (d) {
                    if (d.children && d.children.length)
                        return "";//
                    if (d._children && d._children.length)
                        return "";
                    return "";
                });

            nodeUpdate.select(".bounding-box").attr("transform", function (d) {
                return "translate(" + (d.width / 2 - 12) + "," + (d.height) + ")";
            });

            nodeUpdate.select(".add-role-row").attr("transform", function (d) {
                return "translate(" + d.width + "," + (d.height - 11) + ")";
            });

            var isFirefox = typeof InstallTrigger !== 'undefined';
            var safari = isSafari();
            var fo = nodeUpdate.select(".foreignObject")
                .attr("width", function (d) {
                    if (isFirefox)
                        return d.width + 20;
                    return d.width;
                });
            if (isFirefox || safari) {
                fo.attr("height", function (d) {
                    return d.height;
                });
            }

        };

        var fallbackNodeUpdate = function (nodeUpdate) {
            standardNodeUpdate(nodeUpdate);

            nodeUpdate.select(".add-role-row").attr("transform", function (d) {
                return "translate(" + (d.width - 11) + ",0)";
            });

            nodeUpdate.select(".acc-buttons rect").attr("width", function (d) {
                return d.width;
            }).attr("height", function (d) {
                return (8);
            });
        };

        $scope.nodeExit = function (nodeExit) {
            nodeExit.each(function (d, i) {
                var s = angular.element(this).scope();
                if (s) {
                    console.log("ac-node: destroy scope");
                    s.$destroy();
                }
            });
        };
        if (fallback) {
            console.info("fallback used");
            $scope.nodeEnter = fallbackNodeEnter;
            $scope.nodeUpdate = fallbackNodeUpdate;
        } else {
            $scope.nodeEnter = standardNodeEnter;
            $scope.nodeUpdate = standardNodeUpdate;
        }


        var addNode = function (parentId) {
            var _clientTimestamp = new Date().getTime();
            $rootScope.$emit("ExpandNode", parentId);
            $http.post("/Accountability/AddNode/" + parentId + "?&_clientTimestamp=" + _clientTimestamp, {})
                .error(function (data) {
                    showJsonAlert(data, true, true);
                });
        }

    }]);

