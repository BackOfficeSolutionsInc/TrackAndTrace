angular.module("panzoom", []).directive("panzoom", [function () {
    return {
        templateNamespace: 'svg',
        restrict: "A",
        scope: { graph: "=" },
        transclude: true,
        //replace:true,
        template: '<g class="pz-zoom"> <rect width="100%" height="100%" style="width:100%;height:100%;fill:transparent;"></rect>' +
                  '<g class="pz-pan" ng-transclude></g>' +
                  '</g>',
        link: function (scope, element, attr, ctrl, transclude) {

            var viewerWidth = 1000;
            var viewerHeight = 1000;

            //var panSpeed = 200;
            //var panBoundary = 20; // Within 20px from edges will pan when dragging.

            attr.$set("height", "100%");
            attr.$set("width", "100%");
            attr.$set("viewBox", "0 0 " + viewerWidth + " " + viewerHeight);

            var pzZoom = element.find(".pz-zoom");
            var pzPan = pzZoom.find(".pz-pan");

            function zoomed() {
                pzPan.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
            }

            scope.panzoom = d3.behavior.zoom().scaleExtent([.001, 10]).on("zoom", zoomed);

            scope.panzoom.zoomed = zoomed;

            d3.select(pzZoom[0]).call(scope.panzoom);

        }
    }
}]);