var acapp = angular.module('ACApp', ['helpers']);





//var acharts = angular.module('charts', [])
//    .controller('mainCtrl', function AppCtrl($scope) {
//        $scope.options = { width: 500, height: 300, 'bar': 'aaa' };
//        $scope.data = [1, 2, 3, 4];
//        $scope.hovered = function (d) {
//            $scope.barValue = d;
//            $scope.$apply();
//        };
//        $scope.barValue = 'None';
//    })
acapp.directive('barChart', function () {
    var chart = d3.custom.barChart();
    return {
        restrict: 'E',
        replace: true,
        template: '<div class="chart chart-bar"></div>',
        scope: {
            height: '=height',
            data: '=data',
            hovered: '&hovered'
        },
        link: function (scope, element, attrs) {
            var chartEl = d3.select(element[0]);
            chart.on('customHover', function (d, i) {
                scope.hovered({ args: d });
            });

            scope.$watch('data', function (newVal, oldVal) {
                chartEl.datum(newVal).call(chart);
            });

            scope.$watch('height', function (d, i) {
                chartEl.call(chart.height(scope.height));
            })
        }
    }
}).directive('tree', function () {
    var chart = d3.custom.barChart();
    return {
        restrict: 'E',
        replace: true,
        template: '<div class="chart chart-tree"></div>',
        scope: {
           // height: '=height',
            data: '=data',
            hovered: '&hovered'
        },
        link: function (scope, element, attrs) {
            var chartEl = d3.select(element[0]);
            chart.on('customHover', function (d, i) {
                scope.hovered({ args: d });
            });

            scope.$watch('data', function (newVal, oldVal) {
                chartEl.datum(newVal).call(chart);
            });

            scope.$watch('height', function (d, i) {
                chartEl.call(chart.height(scope.height));
            })
        }
    }
})
    .directive('chartForm', function () {
        return {
            restrict: 'E',
            replace: true,
            controller: function AppCtrl($scope) {
                $scope.update = function (d, i) { $scope.data = randomData(); };
                function randomData() {
                    return d3.range(~~(Math.random() * 50) + 1).map(function (d, i) { return ~~(Math.random() * 1000); });
                }
            },
            template: '<div class="form">' +
                    'Height: {{options.height}}<br />' +
                    '<input type="range" ng-model="options.height" min="100" max="800"/>' +
                    '<br /><button ng-click="update()">Update Data</button>' +
                    '<br />Hovered bar data: {{barValue}}</div>'
        }
    });


//acapp.directive("panzoom",["$compile", function ($compile) {
//    return {
//        templateNamespace: 'svg',
//        restrict: "A",
//        scope: { graph: "=" },
//        transclude: true,
//        //replace:true,
//        template: //'<svg ng-attr-height="{{graph.height}}" ng-attr-width="{{graph.width}}">  ' +
//                  '    <g class="pz-zoom">                                                  ' +
//                  '         <g class="pz-pan" ng-transclude></g>                            ' +
//                  '    </g>                                                                 ' ,
//                  //'</svg>                                                                   ',

//        link: function (scope,element,attr,ctrl,transclude) {

//            scope.getGraph = function () {
//                return scope.graph;
//            }
//            //debugger;
//            //var pzZoom = element.find(".pz-zoom");
//            //var pzPan = pzZoom.find(".pz-pan");

//            //function zoomed() {
//            //    pzPan.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
//            //}

//            //var zoom = d3.behavior.zoom().scaleExtent([1, 10]).on("zoom", zoomed);

//            ////var drag = d3.behavior.drag()
//            ////    .origin(function (d) { return d; })
//            ////    .on("dragstart", dragstarted)
//            ////    .on("drag", dragged)
//            ////    .on("dragend", dragended);

//            //d3.select(pzZoom[0]).call(zoom);


//            //var template = '<g class="pz-zoom"><g class="pz-pan"></g></g>';
//            //var templateEl = angular.element(template);

//            transclude(scope, function (clonedContent) {
//                d3.select(element[0]).select(".pz-pan").selectAll("*").remove();
//                d3.select(element[0]).select(".pz-pan").append(clonedContent);
//              //  ;
//               // element.find().replaceWith(clonedContent);
//            });

//            //transclude(scope, function (clonedContent) {
//            //    templateEl.find(".pz-pan").append(clonedContent);

//            //    $compile(templateEl)(scope, function (clonedTemplate) {
//            //        element.append(clonedTemplate);
//            //    });
//            //});
//        }
//    }
//}]);


//acapp.directive("tree", function () {
//    return {

//        templateNamespace: 'svg',
//       // replace:true,
//        restrict: "E",
//       // require: '^panzoom',
//       // scope: true,
//        template: '' +
//                    '    <circle ng-repeat="circle in graph.data.children"                   ' +
//                    '    cx="10"                                           ' +
//                    '    ng-attr-cy="{{circle.id}}"                                           ' +
//                    '    r= "2">                                          ' +
//                    '    </circle>                                                           ' +
//                    '                                                                        ',
//        link: function (scope, element, attrs, ctrl) {
//           // scope.graph = ctrl.getGraph();
//            //
//            //scope.graph = scope.graph || {};
//            //scope.graph.height = scope.graph.height || 100;
//            //scope.graph.width = scope.graph.width || 100;
//        }
//    };
//});

acapp.controller('ACController', ['$scope', '$http', '$timeout', '$location', 'radial', 'orgId', 'dataUrl', "$compile", "$sce", "$q", "$window",
function ($scope, $http, $timeout, $location, radial, orgId, dataUrl, $compile, $sce, $q, $window) {

    $scope.options = { width: 500, height: 300, 'bar': 'aaa' };
    $scope.data = [1, 2, 3, 4];
    $scope.hovered = function (d) {
        $scope.barValue = d;
        $scope.$apply();
    };
    $scope.barValue = 'None';



    $scope.orgId = orgId;
    $scope.graph = $scope.graph || {};
    $scope.graph.height = $scope.graph.height || 100;
    $scope.graph.width = $scope.graph.width || 100;

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
                        showAlert("Reconnected.", "alert-success", "Success");
                    }
                    $scope.disconnected = false;
                });
            }
        } catch (e) {
            console.error(e);
        }
    }

    var r = radial($scope, 'organizationHub', rejoin);
    var tzoffset = r.updater.tzoffset;
    $scope.functions.reload = function (clearData) {
        if (typeof (clearData) === "undefined")
            clearData = false;
        tzoffset();
        console.log("reloading...");
        var url = dataUrl;
        if (dataUrl.indexOf("{0}") != -1) {
            url = url.replace("{0}", $scope.orgId);
        } else {
            url = url + $scope.orgId;
        }

        var date = ((+new Date()) + (window.tzoffset * 60 * 1000));
        if (dataUrl.indexOf("?") != -1) {
            url += "&_clientTimestamp=" + date;
        } else {
            url += "?_clientTimestamp=" + date;
        }

        $http({ method: 'get', url: url })
        .success(function (data, status) {
            var ddr = undefined;
            //debugger;
            console.log("Got Data");
            r.updater.convertDates(data);
            if (clearData) {
                r.updater.clearAndApply(data);
            } else {
                r.updater.applyUpdate(data);
            }
        }).error(function (data, status) {
            console.log("Error");
            console.error(data);
        });
    }
    $scope.functions.reload(true);


}]);