angular.module('radialModule', ["updaterModule"]).factory('radial', ['signalR', 'updater', '$http', function (signalR, updater, $http) {


	/*

        radial($scope,options);
        radial($scope,"hubName",rejoin); (DEPRECATED)
        
        options:{
			hubs: {... HubResources to connect to ...}
            sendUpdateUrl : func(objToUpdate)||string   (default: does not create $scope.functions.sendUpdate() )
            sendUpdateOptions : object                  (default: {})
            loadDataUrl : func()||string                (default: does not initialize)
            loadDataOptions : object                    (default: undefined)
            rejoin: func(connection, proxy, callback)   **Only use if you're missing the hubJoin options**
            

		  ===***DEPRICATED***===
			hubName: string,
            hubJoinMethod: string                       (default:"Join", require hub to have Join method. ConnectionId appended by default),
            hubJoinArgs: [string]                       (default: []),          

  
        };
    
        ===Plug-and-Play===
        
        radial($scope,hubName,[...],{sendUpdateUrl:sendUpdateUrl,loadDataUrl:loadDataUrl,hubJoinArgs:hubJoinArgs,hubJoinMethod:hubJoinMethod});


        RETURNS:
         {
            hub,
            updater,
            rejoin,
            loadData(url,options) {success,complete,error,override};

         }

        Modifies the $scope variable.

        ADDS:
         str     = $scope.dateFormat;
         bool    = $scope.disconnected;
         void    = $scope.functions.showModal;
         url     = $scope.functions.addTimestamp(url);
         void    = $scope.functions.sendUpdate(objToUpdate,pathParamsObj) [only created if sendUpdateUrl is specified]


    */
	function radialFactory($scope, options) {

		$scope.dateFormat = $scope.dateFormat || window.dateFormat || "MM-dd-yyyy";
		$scope.functions = $scope.functions || {};

		options.hubs = options.hubs || {};

		if (typeof (optionshubName) !== "undefined")
			throw "HubName deprecated";
		if (typeof (options.hubJoinMethod) !== "undefined")
			throw "hubJoinMethod deprecated";
		if (typeof (options.hubJoinArgs) !== "undefined")
			throw "hubJoinArgs deprecated";

		options.rejoin  = options.rejoin || function (rt, callback) {
			try {
				$scope.disconnected = false;
				if (rt) {
					options.hubs.connectionId = rt.id;
					rt.server["join"](options.hubs).done(function () {
						console.log("Rejoin completed.");
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
		};	

		options.sendUpdateUrl = options.sendUpdateUrl;
		options.loadDataUrl = options.loadDataUrl;
		options.loadDataOptions = options.loadDataOptions;
		var rejoin = options.rejoin;

		$scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
			showModal(title, pull, push, callback, validation, onSuccess);
		};
		$scope.functions.showAlert = function (data, arg2, arg3) {
			var d = data;
			if (typeof (data.data) !== "undefined") {
				showJsonAlert(data.data, arg2, arg3);
				return;
			}
			showAlert(d, arg2, arg3);
		};

		//Select List Builders
		$scope.loadedOptions = {};
		$scope.loadSelectOptions = function (url, forceReload) {
			var rtn = [];
			if (url in $scope.loadedOptions && !forceReload) {
				return $scope.loadedOptions[url];
			}
			$http.get(url).success(function (data) {
				$scope.loadedOptions[url] = data;
				for (var i in data) {
					if (arrayHasOwnIndex(data, i)) {
						rtn.push(data[i]);
					}
				}
			});
			return rtn;
		}

		var o = {};
		//Construct Hub
		var hub = signalR(function (rt) {
			$scope.connectionId = rt.id;
			rejoin(rt, function () {});
		});



		//Construct Updater
		var u = updater($scope, hub);
		var addTimestamp = Time.addTimestamp;
		$scope.functions.addTimestamp = addTimestamp;

		//Load Data


		var isReloading = false;
		var firstLoad = true;
		var loadData = function (url, opts) {
			url = url || options.loadDataUrl;
			opts = opts || angular.copy(options.loadDataOptions || {});
			opts.success = opts.success || function () { };
			opts.complete = opts.complete || function () { };
			opts.error = opts.error || showAngularError;
			opts.override = opts.override || false; //Force clearAndApply 

			if (isReloading) {
				console.error("Aborting. Already loading.");
				return;
			}
			var urlType = typeof (url);
			if (urlType === "undefined") {
				console.error("Aborting. Undefined url. Must be a function or string");
				return;
			}

			//Get url as string
			if (typeof (url) === "function") {
				url = url();
				if (typeof (url) !== "string") {
					console.error("Aborting. Url did not resolve to a string.");
					return;
				}
			}

			//Add time stamp
			url = addTimestamp(url);

			//Make the call
			isReloading = true;
			console.log("Loading data: " + url);
			$http({ method: 'get', url: url }).then(function (response) {
				var data = response.data;

				u.convertDates(data);
				if (typeof (opts.override) === "undefined")
					opts.override = false;

				if (firstLoad || opts.override) {
					u.clearAndApply(data);
				} else {
					u.applyUpdate(data);
				}
				firstLoad = false;
				isReloading = false;

				if (typeof (callback) === "function") {
					try {
						callback(data);
					} catch (e) {
						console.error(e);
					}
				}
				console.log("Loading complete.");
				opts.success(response.data);
				opts.complete(response.data);
			}, function (response) {
				isReloading = false;
				opts.error(response.data);
				opts.complete(response.data);
			});
		}

		//Rejoin Hub
		/*if (typeof (rejoin) === "undefined") {
			$scope.disconnected = false;
			var rejoinArr = options.hubJoinArgs.slice();
			rejoin = function (rt, callback) {
				try {
					if (rt) {
						var modRejoinArr = rejoinArr.slice();
						modRejoinArr.splice(0, 0, options.hubJoinMethod);
						modRejoinArr.push(rt.id);
						console.log("Rejoining.");
						//debugger;
						//proxy.invoke(modRejoinArr[0], modRejoinArr[1], modRejoinArr[2], modRejoinArr[3])
						//proxy.invoke.apply(proxy, [modRejoinArr]).done(function () {
						rt.server.invoke.apply(rt.server, modRejoinArr).done(function () {
							console.log("Rejoined.");
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
		}*/

		//Construct the sendUpdates function
		if (typeof (options.sendUpdateUrl) !== "undefined") {
			o.sendUpdate = u.constructSendUpdate(options.sendUpdateUrl, options.sendUpdateOptions);
			$scope.functions.sendUpdate = o.sendUpdate;
		}

		//Actually load some data
		if (typeof (options.loadDataUrl) !== "undefined") {
			loadData();
		}

		$scope.functions.loadData = loadData;

		// o.hub = hub;
		o.updater = u;
		o.rejoin = rejoin;//function (connection, proxy, callback) {}
		o.loadData = loadData;

		return o;
	}

	return radialFactory;
}]);