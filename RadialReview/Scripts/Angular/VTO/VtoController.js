angular.module('VtoApp').controller('VtoController', ['$scope', '$http', '$timeout', 'radial', 'signalR', 'vtoDataUrlBase', 'vtoId', "vtoCallback",
function ($scope, $http, $timeout, radial, signalR, vtoDataUrlBase, vtoId, vtoCallback) {
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
						showAlert("Reconnected.", "alert-success", "Success", 1000);
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
		$(".target [textarea-resize]").autoResize();
		$(".target-market [textarea-resize]").autoResize();
		$(".purpose [textarea-resize]").autoResize();
		$(".niche [textarea-resize]").autoResize();
		
		//setTimeout(function () {
		//	$("[textarea-resize]").each(function () {
		//		$(this).autoResize();
		//	});
		//}, 1);
	}).error(showAngularError);
	$scope.functions = {};
	$scope.filters = {};

	$scope.functions.subtractDays = function (date, days) {
		var d = new Date(date);
		d.setDate(d.getDate() - days);
		return d;
	};

	$scope.proxyLookup = {};
	var tzoffset = r.updater.tzoffset;

	$scope.functions.sendUpdate = function (self) {
		var dat = angular.copy(self);
		var _clientTimestamp = new Date().getTime();
		r.updater.convertDatesForServer(dat, tzoffset());
		console.log(self)
		$http.post("/VTO/Update" + self.Type + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, dat)
		.then(function () { }, showAngularError);
	};

	$scope.functions.AddRow = function (url, self) {
		$scope.functions.Get(url);
	};

	$scope.functions.shouldCreateRow = function ($event, self, url, collection) {
		if ($event.keyCode == 9 && self.$last) {
			$scope.functions.AddRow(url, self);
			$event.preventDefault();
			var that = $event.currentTarget;
			var watcher = $scope.$watchCollection(
                    collection,
                    function (newValue, oldValue) {
                    	if (newValue.length > oldValue.length) {
                    		angular.element(that)
                                .closest("[ng-repeat]")
                                .siblings()
                                .filter(function () {
                                	return $(this).find("textarea").length;
                                }).last()
                                .find("textarea").last()
                                .focus()
                    		watcher();
                    	}

                    }
                );
		}
	}

	$scope.functions.Get = function (url, dat) {
		var _clientTimestamp = new Date().getTime();
		url += (url.indexOf("?") != -1) ? "&" : "?";
		url += "connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp;
		$http.get(url).then(function () { }, showAngularError)
	};

}]).directive('blurToCurrency', function ($filter) {
	return {
		scope: {
			amount: '='
		},
		link: function (scope, el, attrs) {
			el.val($filter('currency')(scope.amount));

			el.bind('focus', function () {
				el.val(scope.amount);
			});

			el.bind('input', function () {
				scope.amount = el.val();
				scope.$apply();
			});

			el.bind('blur', function () {
				el.val($filter('currency')(scope.amount));
			});
		}
	};
}).directive('textareaResize', function () {
	return {
		link: function (scope, elem) {
			if (elem.attr('isResized') !== "true") {
				scope.resize = $(elem).autoResize();
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
