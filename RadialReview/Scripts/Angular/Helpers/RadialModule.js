angular.module('radialModule', ["updaterModule"]).factory('radial', ['signalR','updater', function (signalR,updater) {

   

    function radialFactory($scope,hubName,rejoin) {
        $scope.functions = $scope.functions || {};

        $scope.functions.showModal = function (title, pull, push, callback, validation, onSuccess) {
            showModal(title, pull, push, callback, validation, onSuccess);
        };

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