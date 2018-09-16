angular.module('signalRModule', []).factory('signalR', ['$rootScope', "$timeout", function ($rootScope, $timeout) {

	if (typeof (window.angularSharedSignalR) === 'undefined' || window.angularSharedSignalR === null) {
		angularSharedSignalR = {
			globalConnection: $.hubConnection(),
			proxies: {}
		};
	}

	/*function getConnection() {
		return $.hubConnection();//$.connection;//window.angularSharedSignalR.globalConnection;
	}*/

	function startHub(callback) {
		//var hubName = "RealTimeHub"
		//if (hubName in window.angularSharedSignalR.proxies)
		//	return window.angularSharedSignalR.proxies[hubName];
		//console.log("Starting Hub: "+hubName);
		//debugger;
		//var proxy = getConnection().createHubProxy(hubName);

		if (typeof (window.RealTime) === "undefined") {
			throw "RealTimeHub undefined. Initialize before calling.";
		}
		window.RealTime.start(function () {
			//console.log('Now connected. ConnectionId: ' + $.connection.hub.id);
			if (callback) {
				callback(window.RealTime);
			}
		});
		//window.RealTime.afterStart();
	}


	function signalRFactory(callback) {
		var disconnected = false;
		startHub(callback);

		var isUnloading = false;
		window.addEventListener("beforeunload", function () {
			isUnloading = true;
		});

		$.connection.hub.disconnected(function () {
			console.log("Hub disconnect. " + new Date());
			if (!isUnloading) {
				clearAlerts();
				setTimeout(function () {
					showAlert("Connection lost. Reconnecting.", 1000);
					disconnected = true;
					setTimeout(function () {
						startHub(function () {
							if (callback) {
								callback(window.RealTime);
							}
							clearAlerts();
							showAlert("Connected.", 1000);
						});
					}, 5000); // Restart connection after 5 seconds.
				}, 1000);
			}
		});

		var numberOutstanding = 0;

		var o = {
			hub: window.RealTime,
			disconnected: disconnected,
			on: function (eventName, callback) {
				RealTime.client[eventName] = function (result) {
					numberOutstanding += 1;
					$rootScope.$emit("BeginCallbackSignalR", numberOutstanding);
					$rootScope.$apply(function () {
						try {
							if (callback) {
								callback(result);
							}
						} finally {
							$timeout(function () {
								numberOutstanding -= 1;
								$rootScope.$emit("EndCallbackSignalR", numberOutstanding);
							}, 0);
						}
					});
				};
				/*
				proxy.on(eventName, function (result) {
					//convertDates(result);
					numberOutstanding += 1;
					$rootScope.$emit("BeginCallbackSignalR", numberOutstanding);
					$rootScope.$apply(function () {
						try {
							if (callback) {
								callback(result);
							}
						} finally {
							$timeout(function () {
								numberOutstanding -= 1;
								$rootScope.$emit("EndCallbackSignalR", numberOutstanding);
							}, 0);
						}
					});
				});*/
			},
			invoke: function (methodName, callback, args) {
				debugger;
				console.error("HEY THIS METHOD IS DEPRICATED");
				var ags = [];
				try {
					ags = Array.prototype.slice.call(arguments, 2);
				} catch (e) {
					console.error(e);
				}

				proxy.invoke(methodName, ags)
				.done(function (result) {
					$rootScope.$apply(function () {
						if (callback) {
							callback(result);
						}
					});
				});
			}
		};

		return o;
	};
	return signalRFactory;
}]);

