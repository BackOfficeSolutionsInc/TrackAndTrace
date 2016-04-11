angular.module('VtoApp').controller('VtoController', ['$scope', '$http', '$timeout', 'radial', 'signalR', 'vtoDataUrlBase', 'vtoId', "vtoCallback",
function ($scope, $http, $timeout,radial, signalR, vtoDataUrlBase, vtoId, vtoCallback) {
	if (vtoId == null)
		throw Error("VtoId was empty");
	$scope.disconnected = false;
	$scope.vtoId = vtoId;

	function rejoin(connection, proxy, callback) {
		try {
			if (proxy) {
				proxy.invoke("join", $scope.vtoId, connection.id).done(function () {
					console.log("rejoin");
					$(".rt").prop("disabled", false);
					if (callback) {
						callback();
					}
					if ($scope.disconnected) {
						clearAlerts();
						showAlert("Reconnected.", "alert-success", "Success");
					}
					$scope.disconnected = false;
				});
			}
		} catch (e) {
			console.error(e);
		}
	}
	//function removeDeleted(model) {
	//	for (var key in model) {
	//		if (model[key] == "`delete`")
	//			model[key] = null;
	//		if (typeof (model[key]) == 'object')
	//			removeDeleted(model[key]);
	//	}
	//}

	//function baseExtend(dst, objs, deep) {
	//	var h = dst.$$hashKey;

	//	for (var i = 0, ii = objs.length; i < ii; ++i) {
	//		var obj = objs[i];
	//		if (!angular.isObject(obj) && !angular.isFunction(obj)) continue;
	//		var keys = Object.keys(obj);
	//		for (var j = 0, jj = keys.length; j < jj; j++) {
	//			var key = keys[j];
	//			var src = obj[key];
	//			if (deep && angular.isObject(src)) {
	//				if (src.AngularList) {
	//					//Special AngularList Object
	//					if (src.UpdateMethod == "Add") {
	//						dst[key] = dst[key].concat(src.AngularList);
	//					} else if (src.UpdateMethod == "ReplaceAll") {
	//						dst[key] = src.AngularList;
	//					}
	//				} else {
	//					if (!angular.isObject(dst[key]))
	//						dst[key] = angular.isArray(src) ? [] : {};

	//					if (angular.isArray(dst[key])) {
	//						dst[key] = dst[key].concat(src);
	//					} else {
	//						if (dst[key].Key == src.Key)
	//							baseExtend(dst[key], [src], true);
	//						else
	//							dst[key] = src;
	//					}
	//				}
	//			} else {
	//				dst[key] = src;
	//			}
	//		}
	//	}
	//	if (h) {
	//		dst.$$hashKey = h;
	//	} else {
	//		delete dst.$$hashKey;
	//	}
	//	return dst;
	//}

	//function convertDates(obj) {
	//	var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
	//	//var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;
	//	var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
	//	var dateRegex3 = /\S{3} \S{3} \d{2} \d{4} \d{2}:\d{2}:\d{2}/;
	//	for (var key in obj) {
	//		var value = obj[key];
	//		var type = typeof (value);
	//		if (obj[key] == null) {
	//			//Do nothing
	//		} else if (type == 'string' && dateRegex1.test(value)) {
	//			obj[key] = new Date(parseInt(value.substr(6)));
	//		} else if (type == 'string' && dateRegex2.test(value)) {
	//			obj[key] = new Date(obj[key]);
	//		}else if (type == 'string' && dateRegex3.test(value)) {
	//			obj[key] = new Date(dateRegex3.exec(obj[key]));
	//		} else if (obj[key].getDate !== undefined) {
	//			obj[key] = new Date(obj[key].getTime() /*- obj[key].getTimezoneOffset() * 60000*/);
	//		} else if (type == 'object') {
	//			convertDates(value);
	//		}
	//	}
	//};

	//function update(data, status) {

	//	console.log("update:");
	//	console.log(data);
	//	//angular.merge($scope.model, data);
	//	baseExtend($scope.model, [data], true);


	//	convertDates($scope.model);
	//	removeDeleted($scope.model);
	//}

	//var meetingHub = signalR('vtoHub', function (connection, proxy) {
	//	console.log('trying to connect to service');
	//	$scope.connectionId = connection.id;
	//	rejoin(connection, proxy, function () {
	//		console.log("Logged in: " + connection.id);
	//	});
	//});

    //meetingHub.on('update', update);


	var r = radial($scope, 'vtoHub', rejoin);

	$http({ method: 'get', url: vtoDataUrlBase + $scope.vtoId })
	.success(function (data, status) {
		//console.log(data);
		//convertDates(data);
	    //$scope.model = data;
	    r.updater.clearAndApply(data);

		if (vtoCallback) {
			setTimeout(function () {
				vtoCallback();
			}, 1);
		}

		//update(data, status);
	}).error(function (data, status) {
		//$scope.model = {};
		console.log("Error");
		console.error(data);
	});
	$scope.functions = {};
	$scope.filters = {};

	$scope.functions.subtractDays = function (date, days) {
		var d = new Date(date);
		d.setDate(d.getDate() - days);
		return d;
	};

	$scope.proxyLookup = {};

	$scope.functions.sendUpdate = function (self) {
		var dat = angular.copy(self);
		var _clientTimestamp = new Date().getTime();
	    //debugger;
        console.log(self)
		$http.post("/VTO/Update" + self.Type + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, dat).error(function (data) {
			showJsonAlert(data, true, true);
		});
	};

	$scope.functions.AddRow = function(url, self) {
		$scope.functions.Get(url);

		debugger;
	};
	$scope.functions.Get = function (url, dat) {
		var _clientTimestamp = new Date().getTime();
		url += (url.indexOf("?") != -1) ? "&" : "?";
		url += "connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp;
		$http.get(url).error(function (data) {
			showJsonAlert(data, false, true);
		});
	};

	//$scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
	//	showModal(title, pull, push, callback, validation, onSuccess);
	//};
	//$scope.$watch('date', function (newDate) {
	//	console.log('New date set: ', newDate);
	//}, false);

	//$scope.$watch('Complete', function (newDate) {
	//	console.log('Complete: ', newDate);
	//}, false);
}]).directive('blurToCurrency', function($filter) {
	return {
		scope: {
			amount: '='
		},
		link: function(scope, el, attrs) {
			el.val($filter('currency')(scope.amount));

			el.bind('focus', function() {
				el.val(scope.amount);
			});

			el.bind('input', function() {
				scope.amount = el.val();
				scope.$apply();
			});

			el.bind('blur', function() {
				el.val($filter('currency')(scope.amount));
			});
		}
	};
}).directive('textareaResize',function(){
    return {
        link: function (scope, elem) {
            if (elem.attr('isResized') !== "true") {
                scope.resize =$(elem).autoResize();
                elem.attr('isResized', 'true');
            }

            scope.$on(
                "$destroy",
                function handleDestroyEvent() {
                    scope.resize.destroy();
                }
            );
        }
    };
});
