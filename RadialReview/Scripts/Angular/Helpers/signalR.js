angular.module('signalRModule', []).factory('signalR', ['$rootScope', function ($rootScope) {

	function signalRFactory(hubName, callback) {
		var connection = $.hubConnection();
		var proxy = connection.createHubProxy(hubName);
		var disconnected = false;
		connection.start().done(function () {
				console.log('Now connected, connection ID=' + connection.id);
				if (callback) {
					callback(connection, proxy);
				}
			})
			.fail(function () { alert("Connection failed"); });

		var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
		var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;
		var convertDates = function (obj) {
			for (var key in obj) {
				var value = obj[key];
				var type = typeof (value);
				if (type == 'string' && dateRegex1.test(value)) {
					obj[key] = new Date(parseInt(value.substr(6)));
				} else if (type == 'string' && dateRegex2.test(value)) {
					obj[key] = new Date(obj[key]);
				} else if (type == 'object') {
					convertDates(value);
				}
			}
		};

		return {
			proxy: proxy,
			connection: connection,
			disconnected:disconnected,
			on: function (eventName, callback) {
				proxy.on(eventName, function (result) {
					//convertDates(result);
					$rootScope.$apply(function () {
						if (callback) {
							callback(result);
						}
					});
				});
			},
			invoke: function (methodName, callback) {
				proxy.invoke(methodName)
				.done(function (result) {
					//convertDates(result);
					$rootScope.$apply(function () {
						if (callback) {
							callback(result);
						}
					});
				});
			}
		};
	};
	return signalRFactory;
}]);

