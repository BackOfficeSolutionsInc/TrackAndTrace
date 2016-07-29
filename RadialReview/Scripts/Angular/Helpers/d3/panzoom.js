angular.module("panzoom", []).directive("panzoom", [function () {
    return {
        templateNamespace: 'svg',
        restrict: "A",
        scope: { graph: "=" },
        transclude: true,
        //replace:true,
        template: '<g class="pz-zoom"> <rect style="width:100%;height:100%;fill:transparent;"></rect>' +
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
            //var panTimer = true;
            //var dragStarted = false;
            //var domNode = null;
            
            //scope.dragListener = d3.behavior.drag()
            //    .on("dragstart", function (d) {                  
            //        dragStarted = true;
            //        //d3.event.sourceEvent.stopPropagation();
            //    })
            //    .on("drag", function (d) {                   
            //        if (dragStarted) {
            //            domNode = this;
            //        }

            //        // get coords of mouseEvent relative to svg container to allow for panning
            //        relCoords = d3.mouse(pzZoom.get(0));
            //        if (relCoords[0] < panBoundary) {
            //            panTimer = true;
            //            pan(this, 'left');
            //        } else if (relCoords[0] > (pzZoom.width() - panBoundary)) {
            //            panTimer = true;
            //            pan(this, 'right');
            //        } else if (relCoords[1] < panBoundary) {
            //            panTimer = true;
            //            pan(this, 'up');
            //        } else if (relCoords[1] > (pzZoom.height() - panBoundary)) {
            //            panTimer = true;
            //            pan(this, 'down');
            //        } else {
            //            try {
            //                clearTimeout(panTimer);
            //            } catch (e) {

            //            }
            //        }

            //        d.x0 += d3.event.dy;
            //        d.y0 += d3.event.dx;
            //        //var node = d3.select(this);
            //        //node.attr("transform", "translate(" + d.y0 + "," + d.x0 + ")");
            //        //updateTempConnector();
            //    })

            // var pan = function(domNode, direction) {
            //    var speed = panSpeed;
            //    if (!scope.panTimer) {
            //        scope.panTimer = true;
            //    }
            //    clearTimeout(scope.panTimer);
            //    translateCoords = d3.transform(pzPan.attr("transform"));
            //    if (direction == 'left' || direction == 'right') {
            //        translateX = direction == 'left' ? translateCoords.translate[0] + speed : translateCoords.translate[0] - speed;
            //        translateY = translateCoords.translate[1];
            //    } else if (direction == 'up' || direction == 'down') {
            //        translateX = translateCoords.translate[0];
            //        translateY = direction == 'up' ? translateCoords.translate[1] + speed : translateCoords.translate[1] - speed;
            //    }
            //    scaleX = translateCoords.scale[0];
            //    scaleY = translateCoords.scale[1];
            //    scale = scope.panzoom.scale();
            //    svgGroup.transition().attr("transform", "translate(" + translateX + "," + translateY + ")scale(" + scale + ")");
            //    d3.select(domNode).select('g.node').attr("transform", "translate(" + translateX + "," + translateY + ")");
            //    scope.panzoom.scale(scope.panzoom.scale());
            //    scope.panzoom.translate([translateX, translateY]);
            //    scope.panTimer = setTimeout(function () {
            //        scope.pan(domNode, speed, direction);
            //    }, 50);
            //};


            scope.panzoom.zoomed = zoomed;

            d3.select(pzZoom[0]).call(scope.panzoom);

            //var drag = d3.behavior.drag()
            //    .origin(function (d) { return d; })
            //    .on("dragstart", dragstarted)
            //    .on("drag", dragged)
            //    .on("dragend", dragended);

        }
    }
}]);