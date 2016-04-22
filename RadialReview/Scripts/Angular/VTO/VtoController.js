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
	


	var r = radial($scope, 'vtoHub', rejoin);

	$http({ method: 'get', url: vtoDataUrlBase + $scope.vtoId })
	.success(function (data, status) {
	    r.updater.clearAndApply(data);

		if (vtoCallback) {
			setTimeout(function () {
				vtoCallback();
			}, 1);
		}
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
