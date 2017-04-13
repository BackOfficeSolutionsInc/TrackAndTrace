angular.module('radialModule', ["updaterModule"]).factory('radial', ['signalR','updater','$http', function (signalR,updater,$http) {

   

    function radialFactory($scope, hubName, rejoin) {
    

        $scope.dateFormat = $scope.dateFormat || window.dateFormat || "MM-dd-yyyy";

        $scope.functions = $scope.functions || {};

        $scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
            showModal(title, pull, push, callback, validation, onSuccess);
        };



        //$scope.loadPossibleDirections = function () {        };


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

        //$scope.functions.xEditable = function () {
        //    var type, name, pk, url, mode
        //}

        var o = {};

        var hub = signalR(hubName, function (connection, proxy) {
            console.log('trying to connect to service');
            $scope.connectionId = connection.id;
            rejoin(connection, proxy, function () {
                console.log("Logged in: " + connection.id);
            });
        });

        var u = updater($scope, hub);

        o.hub=hub;
        o.updater = u;
        o.rejoin =  rejoin;//function (connection, proxy, callback) {}
         

        return o;
    }

    return radialFactory;
}]);