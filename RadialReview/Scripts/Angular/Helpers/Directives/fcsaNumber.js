/*! angular-fcsa-number (version 1.5.3) 2014-10-17 */
(function () {
	var fcsaNumberModule,
      __hasProp = {}.hasOwnProperty;

	fcsaNumberModule = angular.module('fcsa-number', []);

	fcsaNumberModule.directive('fcsaNumber', [
      'fcsaNumberConfig', function (fcsaNumberConfig) {
      	var addCommasToInteger, controlKeys, defaultOptions, getOptions, hasMultipleDecimals, isNotControlKey, isNotDigit, isNumber, makeIsValid, makeMaxDecimals, makeMaxDigits, makeMaxNumber, makeMinNumber;
      	defaultOptions = fcsaNumberConfig.defaultOptions;
      	getOptions = function (scope) {
      		var option, options, value, _ref;
      		options = angular.copy(defaultOptions);
      		if (scope.options != null) {
      			_ref = scope.$eval(scope.options);
      			for (option in _ref) {
      				if (!__hasProp.call(_ref, option)) continue;
      				value = _ref[option];
      				options[option] = value;
      			}
      		}
      		return options;
      	};
      	isNumber = function (val) {
      		return !isNaN(parseFloat(val)) && isFinite(val);
      	};
      	isNotDigit = function (which) {
      		return which < 44 || which > 57 || which === 47;
      	};
      	controlKeys = [0, 8, 13];
      	isNotControlKey = function (which) {
      		return controlKeys.indexOf(which) === -1;
      	};
      	hasMultipleDecimals = function (val) {
      		return (val != null) && val.toString().split('.').length > 2;
      	};
      	makeMaxDecimals = function (maxDecimals) {
      		var regexString, validRegex;
      		if (maxDecimals > 0) {
      			regexString = "^-?\\d*\\.?\\d{0," + maxDecimals + "}$";
      		} else {
      			regexString = "^-?\\d*$";
      		}
      		validRegex = new RegExp(regexString);
      		return function (val) {
      			return validRegex.test(val);
      		};
      	};
      	makeMaxNumber = function (maxNumber) {
      		return function (val, number) {
      			return number <= maxNumber;
      		};
      	};
      	makeMinNumber = function (minNumber) {
      		return function (val, number) {
      			return number >= minNumber;
      		};
      	};
      	makeMaxDigits = function (maxDigits) {
      		var validRegex;
      		validRegex = new RegExp("^-?\\d{0," + maxDigits + "}(\\.\\d*)?$");
      		return function (val) {
      			return validRegex.test(val);
      		};
      	};
       	makeIsValid = function (options) {
      		var validations;
      		validations = [];
      		if (options.maxDecimals != null) {
      			validations.push(makeMaxDecimals(options.maxDecimals));
      		}
      		if (options.max != null) {
      			validations.push(makeMaxNumber(options.max));
      		}
      		if (options.min != null) {
      			validations.push(makeMinNumber(options.min));
      		}
      		if (options.maxDigits != null) {
      			validations.push(makeMaxDigits(options.maxDigits));
      		}
      		return function (val) {
      			var i, number, _i, _ref;
      			if (!isNumber(val)) {
      				return false;
      			}
      			if (hasMultipleDecimals(val)) {
      				return false;
      			}
      			number = Number(val);
      			for (i = _i = 0, _ref = validations.length; 0 <= _ref ? _i < _ref : _i > _ref; i = 0 <= _ref ? ++_i : --_i) {
      				if (!validations[i](val, number)) {
      					return false;
      				}
      			}
      			return true;
      		};
      	};
      	resize = function (val,options) {
      		var absV = Math.abs(val);
      		var shorter = false;
      		if (options && (options.prepend || options.append || val < 0)) {
      			shorter = true;
      		}

      		if (absV >= 1000000000) {
      			return { val: Math.round(val / 10000000) / 100 + "B", skipComma: true, decimals: -1 };
      		} else if (absV >= 1000000) {
      			return { val: Math.round(val / 10000) / 100 + "M", skipComma: true, decimals: -1 };
      		} else if (shorter && absV >= 100000) {
      			return { val: Math.round(val / 100) / 10 + "k", skipComma: true, decimals: -1 };
      		}else if ((absV >= 10000) || (shorter && absV >= 1000)) {
      			return { val: Math.round(val), skipComma: false, decimals:-1 };
      		} 
      		return { val: val, skipComma: false };

      	}
      	addCommasToInteger = function (val, groupSep) {
      		var commas, decimals, wholeNumbers;
      		decimals = val.indexOf('.') == -1 ? '' : val.replace(/^-?\d+(?=\.)/, '');
      		wholeNumbers = val.replace(/(\.\d+)$/, '');
      		commas = wholeNumbers.replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1' + groupSep);

      		return "" + (commas + decimals).replace("..", ".");
      	};
      	return {
      		restrict: 'A',
      		require: 'ngModel',
      		scope: {
      			options: '@fcsaNumber'
      		},
      		link: function (scope, elem, attrs, ngModelCtrl) {
      			var isValid, options;
      			options = getOptions(scope);
      			isValid = makeIsValid(options);

      			ngModelCtrl.$parsers.unshift(function (viewVal) {
      				var noCommasVal,radixReplace;
      				var groupSep = ",";
      				var radixSep = ".";
      				if (options.localization) {
					  	if (options.localization.radix)
					  		radixSep = options.localization.radix;
      					if (options.localization.group)
      						groupSep = options.localization.group;
      					if (options.localization.radix=="," && (!options.localization.group || options.localization.group == ","))
      						options.localization.group = " ";
      				}
      				var groupFinder = new RegExp(groupSep.replace(".","\\."), "g");
      				noCommasVal = viewVal.replace(groupFinder, '');
      				var radixFinder = new RegExp(radixSep.replace(".", "\\."), "g");
      				radixReplace = noCommasVal.replace(radixFinder, '.');
					
      				//console.log("parser:" + viewVal + " -> " + radixReplace);

      				if (isValid(radixReplace) || !radixReplace) {
      					ngModelCtrl.$setValidity('fcsaNumber', true);
      					return radixReplace;
      				} else {
      					ngModelCtrl.$setValidity('fcsaNumber', false);
      					return viewVal;
      					//return void 0;
      				}
      			});
      			ngModelCtrl.$formatters.push(function (val) {
      				//console.log("$formatters:" + val);
      				if ((options.nullDisplay != null) && (!val || val === '')) {
      					return options.nullDisplay;
      				}
      				if ((val == null) || !isValid(val)) {
      					return val;
      				}
      				ngModelCtrl.$setValidity('fcsaNumber', true);
      				scope.realValue = val;
      				var valFormat = val;
      				var skipComma = false;
      				var decimals = -1;
      				if (options.prepend) {
      					decimals = 2;
      				}

      				if (options.resize) {
      					var r = resize(valFormat,options);
      					valFormat = r.val;
      					skipComma = r.skipComma;
      					decimals = r.decimals || decimals;
      				}

      				if (!skipComma && decimals >= 0 && !isNaN(+valFormat)) {
      					valFormat = (+valFormat).toFixed(decimals);
      				}

      				var groupSep = ",";
      				if (options.localization && options.localization.radix == ",") {
      					valFormat = valFormat.toString().replace(".", ",");
      					if (!options.localization.group || options.localization.group == ",")
      						options.localization.group = " ";
      				}
      				if (!skipComma) {
      					if (options.localization && options.localization.group)
      						groupSep = options.localization.group;
      					valFormat = addCommasToInteger(valFormat.toString(), groupSep);
      				}

      				if (options.prepend != null) {
      					var pre = "";
      					if (valFormat.indexOf("-") == 0) {
      						pre = "-";
      						valFormat = valFormat.substr(1);
      					}

      					valFormat = pre + "" + options.prepend + valFormat;
      				}
      				if (options.append != null) {
      					valFormat = "" + valFormat + options.append;
      				}
      				return valFormat;
      			});
      			elem.on('blur', function () {
      				var formatter, viewValue, _i, _len, _ref;
      				viewValue = ngModelCtrl.$modelValue ;
      				//console.log("fsca blur:" + ngModelCtrl.$modelValue);
      				scope.realValue = viewValue;
					if ((viewValue == null) || !isValid(viewValue)) {
      					return;
      				}

      				
      				_ref = ngModelCtrl.$formatters;
      				for (_i = 0, _len = _ref.length; _i < _len; _i++) {
      					formatter = _ref[_i];
      					viewValue = formatter(viewValue);
      				}
      				//if (options.localization != null && options.localization.radix == ",") {
      				//	viewValue = ("" + viewValue).replace(/\./g, ",");
      				//}
      				ngModelCtrl.$viewValue = viewValue;
      				return ngModelCtrl.$render();
      			});
      			elem.on('focus', function () {
      				var val;
      				//val = elem.val();
      				//if (options.prepend != null) {
      				//    val = val.replace(options.prepend, '');
      				//}
      				//if (options.append != null) {
      				//    val = val.replace(options.append, '');
      				//}
      				//elem.val(val.replace(/,/g, ''));
      				var vVal = scope.realValue || "";
      				if (options.localization && options.localization.radix) {
      					vVal = ("" + vVal).replace(".", options.localization.radix);
      				}


      				elem.val(vVal);
      				return elem[0].select();
      			});
      			if (options.preventInvalidInput === true) {
      				return elem.on('keypress', function (e) {
      					if (isNotDigit(e.which) && isNotControlKey(e.which)) {
      						return e.preventDefault();
      					}
      				});
      			}
      		}
      	};
      }
	]);

	fcsaNumberModule.provider('fcsaNumberConfig', function () {
		var _defaultOptions;
		_defaultOptions = {};
		this.setDefaultOptions = function (defaultOptions) {
			return _defaultOptions = defaultOptions;
		};
		this.$get = function () {
			return {
				defaultOptions: _defaultOptions
			};
		};
	});

}).call(this);