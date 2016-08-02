angular.module("tree", []).directive("tree", [function () {
    return {
        templateNamespace: 'svg',
        restrict: "E",
        scope: { graph: "=", ttEnter: "=?", ttUpdate: "=?", ttExit: "=?", ttWatch: "=?" },
        transclude: true,
        replace: true,
        template: '<g class="tt-tree"></g>',
        controller: ["$element", "$scope", "$rootScope", function ($element, $scope, $rootScope) {

            var svg = $element.closest("svg");
            $scope.duration = 250;

            $scope.expand = function (d) {
                if (d._children) {
                    d.children = d._children;
                    d.children.forEach($scope.expand);
                    d._children = null;
                }
            };

            $scope.centerNode = function (source) {
                try {
                    var scale = 1;
                    var pz3;
                    var pz = $element.closest('.pz-pan');
                    if (pz) {
                        var panzoom = pz.scope().panzoom;
                        if (panzoom)
                            scale = panzoom.scale();
                    }
                    if (typeof (source) === "number") {
                        source = d3.select("[data-id='" + source + "']").datum();
                    }
                    if (source) {
                        console.log("CenterNode called " + source.Id);
                        x = -source.x0;
                        y = -source.y0;
                        x = x * scale + svg.width() / 2;
                        y = y * scale + svg.height() / 3;
                        scale = Math.max(1, scale);
                        d3.select(pz[0]).transition()
                            .duration($scope.duration)
                            .ease("quad-in-out")
                            .attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
                        if (panzoom) {
                            panzoom.scale(scale);
                            panzoom.translate([x, y]);
                        }
                    }
                } catch (e) {
                    console.info("Could not center:", e);
                }
            }

            $scope.showNode = function (id) {
                console.log("ShowNode called " + id);
                //http://bl.ocks.org/jjzieve/a743242f46321491a950
                function searchTree(obj, id, path) {
                    if (obj.Id === id) { //if search is found return, add the object to the path and return it
                        path.push(obj);
                        return path;
                    }
                    else if (obj.children || obj._children) { //if children are collapsed d3 object will have them instantiated as _children
                        var children = (obj.children) ? obj.children : obj._children;
                        for (var i = 0; i < children.length; i++) {
                            path.push(obj);// we assume this path is the right one
                            var found = searchTree(children[i], id, path);
                            if (found) {// we were right, this should return the bubbled-up path from the first if statement
                                return found;
                            }
                            else {//we were wrong, remove this parent from the path and continue iterating
                                path.pop();
                            }
                        }
                    }
                    else {//not the right object, return false so it will continue to iterate in the loop
                        return false;
                    }
                }
                function openPaths(paths) {
                    for (var i = 0; i < paths.length; i++) {
                        if (paths[i].Id !== "1") {//i.e. not root
                            paths[i].class = 'found';
                            if (paths[i]._children) { //if children are hidden: open them, otherwise: don't do anything
                                paths[i].children = paths[i]._children;
                                paths[i]._children = null;
                            }
                            $scope.updater(paths[i]);
                        }
                    }
                }


                var paths = searchTree($scope.root, id, []);
                if (typeof (paths) !== "undefined") {
                    openPaths(paths);
                }
            }

            $scope.selectNode = function (id) {
                console.log("SelectNode called " + id);
                $scope.showNode(id)
                setTimeout(function () {
                    d3.select(".selected").classed("selected", false);
                    d3.selectAll("[data-id='" + id + "']").classed("selected", true);
                    setTimeout(function () {
                        $scope.centerNode(id);
                    }, $scope.duration || 250);
                }, 1);
            }

            $rootScope.$on("CenterNode", function (event, source) {
                setTimeout(function () {
                    $scope.centerNode(source);
                }, 1);
            });
            $rootScope.$on("ShowNode", function (event, id) {
                $scope.showNode(id);
            });
            $rootScope.$on("SelectNode", function (event, id) {
                $scope.selectNode(id);
            });




        }],
        link: function (scope, element, attr, ctrl, transclude) {

            var minHeight = 20;
            var minWidth = 20;

            var i = 0,
                duration = scope.duration;
            //scope.root;
            scope.root = scope.root || {};

            var vSeparation = 25;
            var hSeparation = 25;
            var height = scope.graph.height;
            var width = scope.graph.width;

            var baseWidth = 250;
            var baseHeight = 50;

            var panBoundary = 20;

            var tree = d3.layout.tree()
                .nodeSize([baseWidth, baseHeight]);

            scope.tree = tree;

            var diagonal = function (d) {
                var sx = d.source.x, sy = d.source.y;
                var tx = d.target.x, ty = d.target.y;
                return "M" + sx + "," + sy
                 + "L" + (sx) + "," + (ty - vSeparation / 2)
                 + " " + (tx) + "," + (ty - vSeparation / 2)
                 + " " + (tx) + "," + ty;
                + " " + (tx) + "," + ty;
            };

            var svg = element.closest("svg");
            var self = d3.select(element[0]);

            scope.updater = function (source) {
                // Compute the new tree layout.
                var nodes = tree.nodes(scope.root),//.reverse(),
                    links = tree.links(nodes);

                // Normalize for fixed-depth.
                var maxHeightRow = {};
                maxHeightRow[-1] = 0;

                var maxWidth = baseWidth;

                var rowWidth = {};

                var maxDepth = 0;
                nodes.forEach(function (d) {
                    maxHeightRow[d.depth] = Math.max((d.height || minHeight), maxHeightRow[d.depth] || 0);
                    maxDepth = Math.max(maxDepth, d.depth);
                    rowWidth[d.depth] = rowWidth[d.depth] || [];
                    rowWidth[d.depth].push({
                        ox: d.x,
                        w: d.width
                    });
                    maxWidth = Math.max(maxWidth, d.width);
                });
                for (var i = 1; i <= maxDepth; i++) {
                    maxHeightRow[i] = maxHeightRow[i] + maxHeightRow[i - 1];
                    rowWidth[i].sort(function (a, b) {
                        return a.ox - b.ox;
                    });
                }
                nodes.forEach(function (d) {
                    d.y = d.depth * vSeparation + maxHeightRow[d.depth - 1];
                    var shift = 0;
                    for (var i = 0; i < rowWidth[d.depth].length; i++) {
                        var ww = (rowWidth[d.depth][i].w - baseWidth + hSeparation);
                        // shift+=ww;
                        if (rowWidth[d.depth][i].ox == d.x) {
                            break;
                        }
                        shift += ww;
                    }
                    d.ddx = shift;
                });

                //

                // Update the nodes�
                var node = self.selectAll("g.node")
                    .data(nodes, function (d) {
                        return (d.Id);//|| (d.Id = ++i));
                    });


                // Enter any new nodes at the parent's previous position.

                var nodeEnter = node.enter().append("g")
                    .call(scope.dragListener || function () { })
                    .call(dragListener)
                    .attr("class", "node")
                    .style("opacity", -10)
                    .attr("transform", function (d) {
                        return "translate(" + source.x0 + "," + source.y0 + ")";
                    })
                    .on("click", click);

                nodeEnter.append("circle")
                    .attr('class', 'ghost')
                    .attr("opacity", 0.2) // change this to zero to hide the target area
                .style("fill", "lightbluesteel")
                    .attr('pointer-events', 'mouseover')
                    .on("mouseover", function (node) {
                        overCircle(node);
                    })
                    .on("mouseout", function (node) {
                        outCircle(node);
                    });

                if (scope.ttEnter) {
                    scope.ttEnter(nodeEnter);
                } else {
                    nodeEnter.append("rect").attr("width", 20).attr("height", 20).attr("x", -10).attr("y", 0);
                }

                // Transition nodes to their new position.
                var nodeUpdate = node
                    .attr("data-id", function (d) { return d.Id })
                    .attr("data-height", function (d) {
                        var maxH = 0; var maxW = 0;
                        var dive = function (parent) {
                            if ($(parent).css("display") == "none")
                                return;

                            var oh =  $(parent).outerHeight();
                            var ow = $(parent).outerWidth();
                            if (ow>0)
                                maxH = Math.max(maxH,oh);
                            if (oh>0)
                                maxW = Math.max(maxW, ow);
                            $(parent).children().each(function () {
                                dive(this);
                            });
                        }
                        dive(this);
                        d.height = maxH;
                        d.width = maxW;
                        return d.height;
                    })
                    .attr("data-width", function (d) {
                        return d.width;
                    })
                    .classed("collapsed", function (d) {
                        return d._children && d._children.length;
                    })
                    .transition()
                    .duration(duration)
                    .style("opacity", 1)
                    .attr("transform", function (d) { return "translate(" + (d.x - d.width / 2) + "," + d.y + ")"; });

                nodeUpdate.select(".ghost")
                    .attr("r", vSeparation/2)
                    //.attr("height", function (d) { return d.height + 16; })
                    .attr("transform", function (d) { return "translate(" + (d.width / 2) + "," + (d.height + vSeparation/2) + ")"; });

                if (scope.ttUpdate)
                    scope.ttUpdate(nodeUpdate);



                // Transition exiting nodes to the parent's new position.
                var nodeExit = node.exit().transition()
                    .duration(duration)
                    .attr("transform", function (d) { return "translate(" + source.x + "," + source.y + ")"; })
                    .style("opacity", -10)
                    .remove();

                if (scope.ttExit)
                    scope.ttExit(nodeExit);


                // Update the links�
                var link = self.selectAll("path.link")
                    .data(links, function (d) { return d.target.Id; });

                // Enter any new links at the parent's previous position.
                link.enter().insert("path", "g")
                    .attr("class", "link")
                    .style("opacity", -20)
                    .attr("d", function (d) {
                        var o = { x: source.x0, y: source.y0 };
                        return diagonal({ source: o, target: o });
                    });

                // Transition links to their new position.
                link.transition()
                    .duration(duration)
                    .style("opacity", 1)
                    .attr("d", function (d) {
                        var s = d.source;
                        var t = d.target;
                        var o = { source: { x: s.x, y: s.y + (s.height || minHeight) }, target: { x: t.x, y: t.y } };
                        return diagonal(o);
                    });

                // Transition exiting nodes to the parent's new position.
                link.exit().transition()
                    .duration(duration)
                    .attr("d", function (d) {
                        var o = { x: source.x, y: source.y };
                        return diagonal({ source: o, target: o });
                    })
                    .style("opacity", -20)
                    .remove();

                // Stash the old positions for transition.
                nodes.forEach(function (d) {
                    d.x0 = d.x;
                    d.y0 = d.y;
                });

                var maxWidth = baseWidth;
                nodes.forEach(function (d) {
                    maxWidth = Math.max(maxWidth, d.width);
                });

                tree.nodeSize([maxWidth + hSeparation, baseHeight]);
            }

            var nestWatch = function (node) {
                if (node) {
                    var children = null;
                    if (node.children || node._children) {
                        children = (node.children || node._children).map(function (x) { return nestWatch(x) });
                    }
                    var nr = {};
                    if (scope.ttWatch)
                        nr = scope.ttWatch(node);
                    nr.children = children;
                    return nr;

                } else {
                    return undefined;
                }
            };

            scope.$watch(function () {
                if (scope.graph) {
                    var r = null;
                    var cn = null;
                    if (scope.graph.data) {
                        r = scope.graph.data.Root;
                        cn = scope.graph.data.CenterNode;
                    }
                    return {
                        nest: nestWatch(r),
                        center: cn
                    };
                }
                return undefined;
            }, function (newVal, oldVal) {
                console.log(newVal);
                if (scope.graph && scope.graph.data && scope.graph.data.Root) {// Array.isArray(scope.graph.data) && scope.graph.data.length) {
                    scope.root = scope.graph.data.Root;
                    scope.root.x = svg.width() / 2;
                    scope.root.y = 0;
                    scope.root.x0 = svg.width() / 2;
                    scope.root.y0 = 0;
                    scope.updater(scope.root);

                    if (newVal && (!oldVal || newVal.center !== oldVal.center))
                        scope.centerNode(newVal.center);

                    //////if (oldVal===undefined)
                    //var center = scope.root;
                    //if (scope.graph && scope.graph.center)
                    //    center =  scope.graph.center;
                    //scope.centerNode(center);
                    //setTimeout(function () {
                    //    scope.centerNode(center);
                    //}, duration);

                }
            }, true);
            // Toggle children on click.
            function click(d) {
                if (d.children) {
                    d._children = d.children;
                    d.children = null;
                } else {
                    d.children = d._children;
                    d._children = null;
                }
                scope.updater(d);
                scope.centerNode(d);
            }

            var svgGroup = self;
            var root = scope.root;
            var selectedNode = null;
            var panTimer = true;
            var dragStarted = false;
            var domNode = null;
            var panSpeed = 200;
            var panBoundary = 20; // Within 20px from edges will pan when dragging.
            var draggingNode = null;

            var updateTempConnector = function () {
                var data = [];
                if (draggingNode !== null && selectedNode !== null) {
                    // have to flip the source coordinates since we did this for the existing connectors on the original tree
                    //debugger;
                    data = [{
                        source: {
                            x: selectedNode.x0,
                            y: selectedNode.y0 + selectedNode.height
                        },
                        target: {
                            x: draggingNode.x0,
                            y: draggingNode.y0 + 50
                        }
                    }];
                }
                var link = svgGroup.selectAll(".templink").data(data);

                link.enter().append("path")
                    .attr("class", "templink")
                    .attr("d", d3.svg.diagonal())
                    .attr('pointer-events', 'none');

                link.attr("d", d3.svg.diagonal());

                link.exit().remove();
            };
            function pan(domNode, direction) {
                var speed = panSpeed;
                var pz = element.closest('.pz-pan');
                var svgGroup = d3.select(pz[0]);
                var zoomListener = pz.scope().panzoom;
                if (panTimer) {
                    clearTimeout(panTimer);
                    translateCoords = d3.transform(svgGroup.attr("transform"));
                    if (direction == 'left' || direction == 'right') {
                        translateX = direction == 'left' ? translateCoords.translate[0] + speed : translateCoords.translate[0] - speed;
                        translateY = translateCoords.translate[1];
                    } else if (direction == 'up' || direction == 'down') {
                        translateX = translateCoords.translate[0];
                        translateY = direction == 'up' ? translateCoords.translate[1] + speed : translateCoords.translate[1] - speed;
                    }
                    scaleX = translateCoords.scale[0];
                    scaleY = translateCoords.scale[1];
                    scale = zoomListener.scale();
                    svgGroup.transition().attr("transform", "translate(" + translateX + "," + translateY + ")scale(" + scale + ")");
                    d3.select(domNode).select('g.node').attr("transform", "translate(" + translateX + "," + translateY + ")");
                    zoomListener.scale(zoomListener.scale());
                    zoomListener.translate([translateX, translateY]);
                    panTimer = setTimeout(function () {
                        pan(domNode, speed, direction);
                    }, 50);
                }
            }

            var overCircle = function (d) {
                selectedNode = d;
                updateTempConnector();
            };
            var outCircle = function (d) {
                selectedNode = null;
                updateTempConnector();
            };


            function initiateDrag(d, domNode) {
                draggingNode = d;
                d3.select(domNode).select('.ghost').attr('pointer-events', 'none');
                d3.selectAll('.ghost').attr('class', 'ghost showing');
                d3.select(domNode).attr('class', 'node activeDrag');

                svgGroup.selectAll("g.node").sort(function (a, b) { // select the parent and sort the path's
                    if (a.Id != draggingNode.Id) return 1; // a is not the hovered element, send "a" to the back
                    else return -1; // a is the hovered element, bring "a" to the front
                });
                // if nodes has children, remove the links and nodes
                if (nodes.length > 1) {
                    // remove link paths
                    links = tree.links(nodes);
                    nodePaths = svgGroup.selectAll("path.link")
                        .data(links, function (d) {
                            return d.target.Id;
                        }).remove();
                    // remove child nodes
                    nodesExit = svgGroup.selectAll("g.node")
                        .data(nodes, function (d) {
                            return d.Id;
                        }).filter(function (d, i) {
                            if (d.Id == draggingNode.Id) {
                                return false;
                            }
                            return true;
                        }).remove();
                }

                // remove parent link
                parentLink = tree.links(tree.nodes(draggingNode.parent));
                svgGroup.selectAll('path.link').filter(function (d, i) {
                    if (d.target.Id == draggingNode.Id) {
                        return true;
                    }
                    return false;
                }).remove();

                dragStarted = null;
            };
            // Define the drag listeners for drag/drop behaviour of nodes.

            var ox, oy;
            dragListener = d3.behavior.drag()
                .on("dragstart", function (d) {
                    if (d == scope.root) {
                        return;
                    }
                    var pz = element.closest('.pz-pan');
                    dragStarted = true;
                    nodes = tree.nodes(d);
                    ox = d.width / 2; //d3.event.sourceEvent.offsetX + d.width / 2;/// pz.scope().panzoom.scale();
                    oy = -50//d3.event.sourceEvent.offsetY;/// pz.scope().panzoom.scale();

                    console.log(ox, oy);
                    d3.event.sourceEvent.stopPropagation();
                    // it's important that we suppress the mouseover event on the node being dragged. Otherwise it will absorb the mouseover event and the underlying node will not detect it d3.select(this).attr('pointer-events', 'none');
                })
                .on("drag", function (d) {
                    if (d == scope.root) {
                        return;
                    }
                    if (dragStarted) {
                        domNode = this;
                        initiateDrag(d, domNode);
                    }
                    var pz = element.closest('.pz-pan');

                    // get coords of mouseEvent relative to svg container to allow for panning
                    relCoords = d3.mouse(svg.get(0));
                    if (relCoords[0] < panBoundary) {
                        panTimer = true;
                        pan(this, 'left');
                    } else if (relCoords[0] > (svg.width() - panBoundary)) {
                        panTimer = true;
                        pan(this, 'right');
                    } else if (relCoords[1] < panBoundary) {
                        panTimer = true;
                        pan(this, 'up');
                    } else if (relCoords[1] > (svg.height() - panBoundary)) {
                        panTimer = true;
                        pan(this, 'down');
                    } else {
                        try {
                            clearTimeout(panTimer);
                        } catch (e) {

                        }
                    }

                    d.x0 += d3.event.dx;
                    d.y0 += d3.event.dy;
                    var node = d3.select(this);
                    node.attr("transform", "translate(" + (d.x0 - ox) + "," + (d.y0 - oy) + ")");
                    updateTempConnector();
                })
                .on("dragend", function (d) {
                    panTimer = null;
                    if (d == scope.root) {
                        return;
                    }
                    domNode = this;
                    if (selectedNode) {
                        // now remove the element from the parent, and insert it into the new elements children
                        var index = draggingNode.parent.children.indexOf(draggingNode);
                        if (index > -1) {
                            draggingNode.parent.children.splice(index, 1);
                        }
                        if (Array.isArray(selectedNode.children) || Array.isArray(selectedNode._children)) {
                            if (Array.isArray(selectedNode.children)) {
                                selectedNode.children.push(draggingNode);
                            } else {
                                selectedNode._children.push(draggingNode);
                            }
                        } else {
                            selectedNode.children = [];
                            selectedNode.children.push(draggingNode);
                        }
                        // Make sure that the node being added to is expanded so user can see added node is correctly moved
                        scope.expand(selectedNode);
                        //sortTree();
                        endDrag();
                    } else {
                        endDrag();
                    }
                });

            function endDrag() {
                selectedNode = null;
                d3.selectAll('.ghost').attr('class', 'ghost');
                d3.select(domNode).attr('class', 'node');
                // now restore the mouseover event or we won't be able to drag a 2nd time
                d3.select(domNode).select('.ghost').attr('pointer-events', '');
                updateTempConnector();
                if (draggingNode !== null) {
                    scope.updater(scope.root);
                    scope.centerNode(draggingNode);
                    draggingNode = null;
                }
            }

        }
    }
}]);