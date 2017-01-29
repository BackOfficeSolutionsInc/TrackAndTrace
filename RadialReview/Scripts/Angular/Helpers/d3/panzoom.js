angular.module("panzoom", []).directive("panzoom", [function () {
    return {
        templateNamespace: 'svg',
        restrict: "A",
        scope: { graph: "=" },
       // transclude: true,
       // replace:true,
        //template: '<g class="pz-zoom"> <rect class="pz-rect" width="100%" height="100%" style="width:100%;height:100%;fill:transparent;"></rect>' +
        //          '<g class="pz-pan" ng-transclude></g>' +
        //          '</g>',
        link: function (scope, element, attr, ctrl, transclude) {

            //var viewerWidth = 1000;
            //var viewerHeight = 1000;

            ////var panSpeed = 200;
            ////var panBoundary = 20; // Within 20px from edges will pan when dragging.

            //attr.$set("height", "100%");
            //attr.$set("width", "100%");
            //attr.$set("viewBox", "0 0 " + viewerWidth + " " + viewerHeight);

            //var pzZoom = element.find(".pz-zoom");
            //var pzZoomRect = element.find(".pz-zoom rect");
            //var pzPan = pzZoom.find(".pz-pan");


            //var canZoom = true;

            //function zoomed() {
            //	if (canZoom) {
            //		pzPan.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
            //	}
            //}

            ////function zoomStart() {
            ////	var canZoom = true;

            ////	var isTranslate = false;
            ////	if (scope.oldZoom != null && d3.event.scale == scope.oldZoom) {
            ////		console.log("translating");
            ////		isTranslate = true;
            ////	} else {
            ////		console.log("zooming");
            ////	}

            ////	if (d3.event.sourceEvent && !d3.select(d3.event.sourceEvent.target).classed("pz-rect") && isTranslate) {
            ////		canZoom=false;
            ////	}
            ////	scope.oldTranslate = d3.event.translate;
            ////	scope.oldZoom = d3.event.scale;
            ////}

            ////function zoomEnd() {
            ////	if (!canZoom) {
            ////		scope.panzoom.scale(scope.oldZoom);
            ////		scope.panzoom.translate(scope.oldTranslate);

            ////	}

            ////}

            //scope.panzoom = d3.behavior.zoom().scaleExtent([.001, 10]).on("zoom", zoomed);//.on("zoomstart", zoomStart).on("zoomend", zoomEnd);

            ////scope.oldTranslate = scope.panzoom.translate();
            ////scope.oldZoom = scope.panzoom.scale();

            //scope.panzoom.zoomed = zoomed;

            //d3.select(pzZoom[0]).call(scope.panzoom);

        }
    }
}]);