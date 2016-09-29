////http://bl.ocks.org/robschmuecker/7880033




//function startAccChart(selector, orgId) {

//    var selectedNode = null;
//    var draggingNode = null;
//    var lockDrag = false;
//    treeJSON = d3.json("/Data/OrganizationHierarchy/" + orgId, function (error, treeData) {

//        // Calculate total nodes, max label length
//        var totalNodes = 0;
//        var maxLabelLengthLeft = {};
//        var maxLabelLengthRight = {};
//        // variables for drag/drop
//        // panning variables
//        var panSpeed = 200;
//        var panBoundary = 20; // Within 20px from edges will pan when dragging.
//        // Misc. variables
//        var i = 0;
//        var duration = 750;
//        var root;
//        var viewerWidth;
//        var viewerHeight;

//        // size of the diagram

//        var tree = d3.layout.tree();
//        function fixSize() {
//            viewerWidth = $(document).width();
//            viewerHeight = $(document).height() - 30;
//            tree.size([viewerHeight, viewerWidth]);
//        }

//        var rtime;
//        var timeout = false;
//        var delta = 200;
//        $(window).resize(function () {
//            rtime = new Date();
//            if (timeout === false) {
//                timeout = true;
//                setTimeout(resizeend, delta);
//            }
//        });

//        function resizeend() {
//            if (new Date() - rtime < delta) {
//                setTimeout(resizeend, delta);
//            } else {
//                timeout = false;
//                fixSize();
//            }
//        }

//        fixSize();


//        // define a d3 diagonal projection for use by the node paths later on.
//        var diagonal = d3.svg.diagonal()
//            .projection(function (d) {
//                return [d.x, d.y];
//            });

//        // A recursive helper function for performing some setup by walking through all nodes

//        function visit(parent, visitFn, childrenFn) {
//            if (!parent) return;

//            visitFn(parent);

//            var children = childrenFn(parent);
//            if (children) {
//                var count = children.length;
//                for (var i = 0; i < count; i++) {
//                    visit(children[i], visitFn, childrenFn);
//                }
//            }
//        }

//        // sort the tree according to the node names
//        function sortTree() {
//            tree.sort(function (a, b) {
//                return b.name.toLowerCase() < a.name.toLowerCase() ? 1 : -1;
//            });
//        }
//        // Sort the tree initially incase the JSON isn't in a sorted order.
//        sortTree();

//        // TODO: Pan function, can be better implemented.
//        var panTicks = 0;
//        function pan(domNode, direction) {
//            var speed = panSpeed;
//            if (panTimer && panTicks < 30) {
//                panTicks += 1;
//                clearTimeout(panTimer);
//                translateCoords = d3.transform(svgGroup.attr("transform"));
//                if (direction == 'left' || direction == 'right') {
//                    translateX = direction == 'left' ? translateCoords.translate[0] + speed : translateCoords.translate[0] - speed;
//                    translateY = translateCoords.translate[1];
//                } else if (direction == 'up' || direction == 'down') {
//                    translateX = translateCoords.translate[0];
//                    translateY = direction == 'up' ? translateCoords.translate[1] + speed : translateCoords.translate[1] - speed;
//                }
//                scaleX = translateCoords.scale[0];
//                scaleY = translateCoords.scale[1];
//                scale = zoomListener.scale();
//                svgGroup.transition().attr("transform", "translate(" + translateX + "," + translateY + ")scale(" + scale + ")");
//                d3.select(domNode).select('g.node').attr("transform", "translate(" + translateX + "," + translateY + ")");
//                zoomListener.scale(zoomListener.scale());
//                zoomListener.translate([translateX, translateY]);
//                panTimer = setTimeout(function () {
//                    pan(domNode, speed, direction);
//                }, 50);
//            } else {
//                panTicks = 0;
//                clearTimeout(panTimer);
//            }
//        }

//        // Define the zoom function for the zoomable tree

//        function zoom() {
//            svgGroup.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
//        }

//        function addConnection(parent, child) {
//            if (typeof parent.children !== 'undefined' || typeof parent._children !== 'undefined') {
//                if (typeof parent.children !== 'undefined') {
//                    parent.children.push(child);
//                } else {
//                    parent._children.push(child);
//                }
//            } else {
//                parent.children = [];
//                parent.children.push(child);
//            }
//            expand(parent);
//            sortTree();
//        }


//        // define the zoomListener which calls the zoom function on the "zoom" event constrained within the scaleExtents
//        var zoomListener = d3.behavior.zoom().scaleExtent([0.1, 5]).on("zoom", zoom);

//        function initiateDrag(d, domNode) {
//            closePlus(d3.select(domNode));
//            draggingNode = d;
//            d3.select(domNode).select('.ghostCircle').attr('pointer-events', 'none');
//            d3.selectAll('.ghostCircle').attr('class', 'ghostCircle show');
//            d3.select(domNode).attr('class', function (d) { return 'node ' + d.class + ' activeDrag'; });

//            svgGroup.selectAll("g.node").sort(function (a, b) { // select the parent and sort the path's
//                if (a.id != draggingNode.id) return 1; // a is not the hovered element, send "a" to the back
//                else return -1; // a is the hovered element, bring "a" to the front
//            });
//            // if nodes has children, remove the links and nodes
//            if (nodes.length > 1) {
//                // remove link paths
//                links = tree.links(nodes);
//                nodePaths = svgGroup.selectAll("path.link")
//                    .data(links, function (d) {
//                        return d.target.id;
//                    }).remove();
//                // remove child nodes
//                nodesExit = svgGroup.selectAll("g.node")
//                    .data(nodes, function (d) {
//                        return d.id;
//                    }).filter(function (d, i) {
//                        if (d.id == draggingNode.id) {
//                            return false;
//                        }
//                        return true;
//                    }).remove();
//            }

//            // remove parent link
//            parentLink = tree.links(tree.nodes(draggingNode.parent));
//            svgGroup.selectAll('path.link').filter(function (d, i) {
//                if (d.target.id == draggingNode.id) {
//                    return true;
//                }
//                return false;
//            }).remove();

//            dragStarted = null;
//        }

//        // define the baseSvg, attaching a class for styling and the zoomListener
//        var baseSvg = d3.select(selector).html("").append("svg")
//            .attr("height", "100%")
//            .attr("width", "100%")
//            .attr("viewBox", "0 0 " + viewerWidth + " " + viewerHeight)
//            .attr("class", "overlay")
//            .call(zoomListener);


//        // Define the drag listeners for drag/drop behaviour of nodes.
//        dragListener = d3.behavior.drag()
//            .on("dragstart", function (d) {
//                if (d.managing) {
//                    clearAlerts();
//                    if (d == root) {
//                        return;
//                    }
//                    dragStarted = true;
//                    nodes = tree.nodes(d);
//                    d3.event.sourceEvent.stopPropagation();
//                }
//                // it's important that we suppress the mouseover event on the node being dragged. Otherwise it will absorb the mouseover event and the underlying node will not detect it d3.select(this).attr('pointer-events', 'none');
//            })
//            .on("drag", function (d) {
//                if (d.managing) {
//                    if (d == root) {
//                        return;
//                    }
//                    if (dragStarted) {
//                        domNode = this;
//                        initiateDrag(d, domNode);
//                    }

//                    // get coords of mouseEvent relative to svg container to allow for panning
//                    relCoords = d3.mouse($('svg').get(0));
//                    if (relCoords[0] < panBoundary) {
//                        panTimer = true;
//                        pan(this, 'left');
//                    } else if (relCoords[0] > ($('svg').width() - panBoundary)) {
//                        panTimer = true;
//                        pan(this, 'right');
//                    } else if (relCoords[1] < panBoundary) {
//                        panTimer = true;
//                        pan(this, 'up');
//                    } else if (relCoords[1] > ($('svg').height() - panBoundary)) {
//                        panTimer = true;
//                        pan(this, 'down');
//                    } else {
//                        try {
//                            clearTimeout(panTimer);
//                        } catch (e) {

//                        }
//                    }

//                    d.x0 += d3.event.dx;
//                    d.y0 += d3.event.dy;
//                    var node = d3.select(this);
//                    node.attr("transform", "translate(" + d.x0 + "," + d.y0 + ")");
//                    updateTempConnector();
//                }
//            })
//            .on("dragend", function (d) {
//                if (d == root) {
//                    return;
//                }
//                domNode = this;
//                if (lockDrag == false) {


//                    if (selectedNode != null && d.managing && selectedNode.managing && selectedNode.manager) {
//                        // now remove the element from the parent, and insert it into the new elements children

//                        var index = draggingNode.parent.children.indexOf(draggingNode);
//                        if (index > -1) {
//                            //Make the call to detach from the parent

//                            if (selectedNode != null && draggingNode != null && draggingNode.parent != null && draggingNode.parent.id != selectedNode.id) {
//                                lockDrag = true;
//                                var selectedLock = selectedNode;
//                                var draggingLock = draggingNode;
//                                $.ajax({
//                                    url: "/User/SwapManager/",
//                                    method: "POST",
//                                    data: {
//                                        oldManagerId: draggingLock.parent.id,
//                                        newManagerId: selectedLock.id,
//                                        userId: draggingLock.id,
//                                    },
//                                    success: function (data) {
//                                        if (!data.Error) {
//                                            draggingLock.parent.children.splice(index, 1);
//                                            addConnection(selectedLock, draggingLock);
//                                            lockDrag = false;
//                                            endDrag();
//                                        } else {
//                                            lockDrag = false;
//                                            StoreJsonAlert(data);
//                                            //refresh();
//                                        }
//                                    }
//                                });
//                            } else {
//                                endDrag();
//                            }
//                        } else {
//                            alert("err");
//                            // NowAdd(this);
//                        }
//                    } else {
//                        if (selectedNode != null) {
//                            if (!selectedNode.manager) {
//                                var alert = "You cannot add a subordinate to someone that isn't a manager.";
//                                if (selectedNode.managing)
//                                    alert += " You can promote this user by hovering over their circle and clicking the up arrow.";
//                                showAlert(alert, "alert-danger", "Error");
//                            }
//                        }
//                        endDrag();
//                    }
//                }
//            });

//        function endDrag() {
//            if (lockDrag == false) {
//                selectedNode = null;
//                d3.selectAll('.ghostCircle').attr('class', 'ghostCircle');
//                d3.select(domNode).attr('class', function (d) { return 'node ' + d.class; });
//                // now restore the mouseover event or we won't be able to drag a 2nd time
//                d3.select(domNode).select('.ghostCircle').attr('pointer-events', '');
//                updateTempConnector();
//                if (draggingNode !== null) {
//                    update(root);
//                    centerNode(draggingNode);
//                    draggingNode = null;
//                }
//            }
//        }

//        // Helper functions for collapsing and expanding nodes.

//        function collapse(d) {
//            if (d.children) {
//                d._children = d.children;
//                d._children.forEach(collapse);
//                d.children = null;
//            }
//        }

//        function expand(d) {
//            if (d._children) {
//                d.children = d._children;
//                d.children.forEach(expand);
//                d._children = null;
//            }
//        }

//        var overCircle = function (d) {
//            if (d.managing && lockDrag == false) {
//                selectedNode = d;
//                updateTempConnector();
//            }
//        };
//        var outCircle = function (d) {
//            if (lockDrag == false) {
//                selectedNode = null;
//                updateTempConnector();
//            }
//        };

//        // Function to update the temporary connector indicating dragging affiliation
//        var updateTempConnector = function () {
//            var data = [];
//            if (draggingNode !== null && selectedNode !== null) {
//                // have to flip the source coordinates since we did this for the existing connectors on the original tree
//                data = [{
//                    source: {
//                        x: selectedNode.y0,
//                        y: selectedNode.x0
//                    },
//                    target: {
//                        x: draggingNode.y0,
//                        y: draggingNode.x0
//                    }
//                }];
//            }
//            var link = svgGroup.selectAll(".templink").data(data);

//            link.enter().append("path")
//                .attr("class", "templink")
//                .attr("d", d3.svg.diagonal())
//                .attr('pointer-events', 'none');

//            link.attr("d", d3.svg.diagonal());

//            link.exit().remove();
//        };

//        // Function to center node when clicked/dropped so node doesn't get lost when collapsing/moving with large amount of children.

//        function centerNode(source) {
//            scale = zoomListener.scale();
//            x = -source.y0;
//            y = -source.x0;
//            x = x * scale + viewerWidth / 2;
//            y = y * scale + viewerHeight / 2;
//            d3.select('g').transition()
//                .duration(duration)
//                .attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
//            zoomListener.scale(scale);
//            zoomListener.translate([x, y]);
//        }

//        // Toggle children function

//        function toggleChildren(d) {
//            if (d.children) {
//                d._children = d.children;
//                d.children = null;
//            } else if (d._children) {
//                d.children = d._children;
//                d._children = null;
//            }
//            return d;
//        }

//        // Toggle children on click.

//        function click(d) {
//            if (d3.event.defaultPrevented)
//                return; // click suppressed
//            d = toggleChildren(d);
//            update(d);
//            centerNode(d);
//        }

//        function addedUser(data, d) {
//            showJsonAlert(data, true);
//            if (!data.Error) {
//                //d.children.push(data.Object);
//                addConnection(d, data.Object);
//                update(root);
//            }
//        }

//        function addUser(d) {
//            showModal(
//                "Add managed user to " + d.name, "/User/AddModal/?managerId=" + d.id,
//                "/nexus/AddManagedUserToOrganization", null, null, function (data) {
//                    return addedUser(data, d);
//                });
//        }

//        function setManager(d) {
//            $.ajax({
//                url: "/User/SetManager/" + d.id,
//                method: "POST",
//                data: { manager: true },
//                success: function (data) {
//                    if (!data.Error) {
//                        d.manager = true;
//                        d.class += " manager";
//                        update(root);
//                        sortTree();
//                    } else {
//                        showJsonAlert(data);
//                    }
//                }
//            });
//        }

//        function width(text) {
//            var o = $("<g class='.node'><text>" + text + "</text></g>").css({
//                'position': 'absolute',
//                'float': 'left',
//                'white-space': 'nowrap',
//                'visibility': 'hidden',
//                "font-size": "10px",
//                "font-family": "sans-serif",
//            });
//            o.appendTo($("body"));
//            var w = o.width();
//            o.remove();
//            return w;
//        }

//        function closePlus(parent) {
//            parent.selectAll(".icon").transition().duration(300).attr("dy", -2).attr("dx", 5).style("opacity", 0);
//        }

//        function update(source) {
//            // Compute the new height, function counts total children of root node and sets tree height accordingly.
//            // This prevents the layout looking squashed when new nodes are made visible or looking sparse when nodes are removed
//            // This makes the layout more consistent.
//            var levelWidth = [1];
//            var childCount = function (level, n) {

//                if (n.children && n.children.length > 0) {
//                    if (levelWidth.length <= level + 1) levelWidth.push(0);

//                    levelWidth[level + 1] += n.children.length;
//                    n.children.forEach(function (d) {
//                        childCount(level + 1, d);
//                    });
//                }
//            };

//            childCount(0, root);
//            var newHeight = d3.max(levelWidth) * 25; // 25 pixels per line  
//            tree = tree.size([newHeight, viewerWidth]);

//            // Compute the new tree layout.
//            var nodes = tree.nodes(root).reverse(),
//                links = tree.links(nodes);

//            // Update the nodes…
//            node = svgGroup.selectAll("g.node")
//                .data(nodes, function (d) {
//                    return d.id + "_" + d.depth;// || (d.id = ++i);
//                });

//            maxLabelLengthLeft = { 0: 0 };
//            maxLabelLengthRight = { 0: 0 };
//            var maxD = 0;
//            nodes.forEach(function (d, i) {
//                if (maxLabelLengthLeft[d.depth] === undefined)
//                    maxLabelLengthLeft[d.depth] = 0;
//                if (maxLabelLengthRight[d.depth + 1] === undefined)
//                    maxLabelLengthRight[d.depth + 1] = 0;

//                maxD = Math.max(maxD, d.depth + 1);

//                var len = 0;
//                if (d.name) {
//                    len = width(d.name);
//                }
//                if (d.subtext) {
//                    len = Math.max(d.subtext.length / 2, len);
//                }
//                if ((d._children && d._children.length > 0) || (d.children && d.children.length > 0)) {
//                    maxLabelLengthLeft[d.depth] = Math.max(len, maxLabelLengthLeft[d.depth]);
//                } else {
//                    maxLabelLengthRight[d.depth + 1] = Math.max(len, maxLabelLengthRight[d.depth + 1]);
//                }
//            });

//            // Call visit function to establish maxLabelLength
//            visit(treeData, function (d) {
//                totalNodes++;
//            }, function (d) {
//                return d.children && d.children.length > 0 ? d.children : null;
//            });

//            // Set widths between levels based on maxLabelLength.
//            nodes.forEach(function (d) {
//                //maxLabelLength[d.depth]
//                var dist = 0;
//                for (var i = 0; i <= d.depth; i++) {
//                    dist += ((maxLabelLengthLeft[i] || 0) + (maxLabelLengthRight[i] || 0)) + 30;
//                }
//                d.y = dist;
//            });


//            // Enter any new nodes at the parent's previous position.
//            var nodeEnter = node.enter().append("g")
//                .call(dragListener)
//                .attr("class", function (d) {
//                    return "node " + d.class;
//                })
//                .attr("transform", function (d) {
//                    return "translate(" + source.x0 + "," + source.y0 + ")";
//                });

//            var nodeHoverEnter = nodeEnter.append("g").attr("class", "hoverStuff")
//                .on("mouseover", function (d) {
//                    d3.select(this.parentNode).selectAll(".icon").transition().duration(300).attr("dy", -3.5).attr("dx", 8).style("opacity", 1);
//                })
//                .on("mouseleave", function (d) {
//                    closePlus(d3.select(this.parentNode));
//                });

//            //Hover Icon
//            nodeHoverEnter.append("circle")
//                .attr('class', "hoverIcon")
//                .attr("r", 8);

//            nodeHoverEnter.append("rect")
//                .attr('class','acc-box')
//                .attr("width", 100)
//                .attr("height", 117).attr("x", -50).attr("y", -25);

//            //Actual Circle thing
//            nodeHoverEnter.append("circle")
//                .attr('class', function (d) { return 'nodeCircle' + (d._children ? " collapsed" : ""); })
//                .attr("r", 0)
//                .on('click', click);

//            //Add User
//            nodeHoverEnter.append("text")
//                .attr('class', 'addUser icon')
//                .attr("dy", -2)
//                .attr("dx", 5)
//                .text("+")
//                .style("opacity", "0")
//                /*.on("mouseover", function (d) {
//                    d3.select(this).transition().duration(0).duration(300).attr("dy", -3.5).attr("dx", 8).style("opacity", 1);
//                }).on("mouseleave", function (d) {
//                    //var nodeCircle = d3.select(this);
//                    //nodeCircle.transition().duration(0).duration(200).attr("r", "2").style("opacity", .5);
//                })*/
//                .attr("text-anchor", "middle")
//                .on("click", addUser);

//            //Set manager
//            nodeHoverEnter.append("text")
//                .attr('class', 'setManager icon')
//                .attr("text-anchor", "middle")
//                .text("▴")
//                .attr("dy", -2)
//                .attr("dx", 5)
//                .style("opacity", "0")
//                /*.on("mouseover", function (d) {
//                    d3.select(this).transition().duration(0).duration(300).attr("dy", -3.5).attr("dx", 8).style("opacity", 1);
//                })/*.on("mouseleave", function (d) {
//                    var nodeCircle = d3.select(this);
//                    nodeCircle.transition().duration(0).duration(200).attr("r", "2").style("opacity", .5);
//                })*/
//                .on("click", setManager);

//            //Show name
//            var nodeLinksEnter = nodeEnter.append("a")
//                .attr("href", function (d) { return "/User/Details/" + d.id; })
//                .on("click", function (d) {
//                    window.location.href = "/User/Details/" + d.id;
//                })
//                .append("g");
//            nodeLinksEnter.append("text")
//                .attr("x", function (d) {
//                    return d.children || d._children ? -10 : 10;
//                })
//                .attr("dy", ".35em")
//                .attr('class', 'nodeText')
//                .attr("text-anchor", function (d) {
//                    return d.children || d._children ? "end" : "start";
//                })
//                .text(function (d) {
//                    return d.name;//d.name;
//                })
//                .style("fill-opacity", 0);

//            //Show subtext
//            nodeLinksEnter.append("text")
//                .attr("x", function (d) {
//                    return d.children || d._children ? -10 : 10;
//                })
//                .attr("dy", "1.60em")
//                .attr('class', 'nodeSubtext')
//                .style("font-size", "40%")
//                .style("fill", "gray")
//                .attr("text-anchor", function (d) {
//                    return d.children || d._children ? "end" : "start";
//                })
//                .text(function (d) {
//                    return d.subtext;//d.name;
//                })
//                .style("fill-opacity", 0);

//            //Phantom node to give us mouseover in a radius around it
//            nodeEnter.append("circle")
//                .attr('class', 'ghostCircle')
//                .attr("r", 30)
//                .attr("opacity", 0.2) // change this to zero to hide the target area
//                .style("fill", "red")
//                    .attr('pointer-events', 'mouseover')
//                    .on("mouseover", function (node) {
//                        overCircle(node);
//                    })
//                    .on("mouseout", function (node) {
//                        outCircle(node);
//                    });


//            svgGroup.selectAll(".setManager").classed("hidden", function (d) { return d.manager || !d.managing; });
//            svgGroup.selectAll(".addUser").classed("hidden", function (d) { return !d.manager || !d.managing; });
//            svgGroup.selectAll(".ghostCircle").classed("hidden", function (d) {
//                return !(d.manager && d.managing);
//            });
//            svgGroup.selectAll(".node").classed("manager", function (d) { return d.manager; });

//            // Update the text to reflect whether node has children or not.
//            node.select('.nodeText')
//                .attr("x", function (d) {
//                    return d.children || d._children ? -10 : 10;
//                })
//                .attr("text-anchor", function (d) {
//                    return d.children || d._children ? "end" : "start";
//                })
//                .text(function (d) {
//                    return d.name;
//                });

//            node.select('.nodeSubtext')
//                .attr("x", function (d) {
//                    return d.children || d._children ? -10 : 10;
//                })
//                .attr("text-anchor", function (d) {
//                    return d.children || d._children ? "end" : "start";
//                })
//                .text(function (d) {
//                    return d.subtext;
//                });


//            // Change the circle fill depending on whether it has children and is collapsed
//            node.select("circle.nodeCircle")
//                .attr("r", 4.5)
//                .attr('class', function (d) {
//                    return 'nodeCircle' + (d._children ? " collapsed" : "");
//                });

//            // Transition nodes to their new position.
//            var nodeUpdate = node.transition()
//                .duration(duration)
//                .attr("transform", function (d) {
//                    return "translate(" + d.x + "," + d.y + ")";
//                });

//            // Fade the text in
//            nodeUpdate.selectAll("text")
//                .style("fill-opacity", 1);

//            // Transition exiting nodes to the parent's new position.
//            var nodeExit = node.exit().transition()
//                .duration(duration)
//                .style("opacity", 0)
//                .attr("transform", function (d) {
//                    return "translate(" + source.x + "," + source.y + ")";
//                })
//                .remove();

//            nodeExit.select("circle")
//                .attr("r", 0);

//            /*nodeExit//.select("text")
//                .style("opacity", 0);*/

//            // Update the links…
//            var link = svgGroup.selectAll("path.link")
//                .data(links, function (d) {
//                    return d.target.id + "_" + d.target.depth;
//                });

//            // Enter any new links at the parent's previous position.
//            link.enter().insert("path", "g")
//                .attr("class", "link")
//                .attr("d", function (d) {
//                    var o = {
//                        x: source.x0,
//                        y: source.y0
//                    };
//                    return diagonal({
//                        source: o,
//                        target: o
//                    });
//                });

//            // Transition links to their new position.
//            link.transition()
//                .duration(duration)
//                .attr("d", diagonal);

//            // Transition exiting nodes to the parent's new position.
//            link.exit().transition()
//                .duration(duration)
//                .attr("d", function (d) {
//                    var o = {
//                        x: source.x,
//                        y: source.y
//                    };
//                    return diagonal({
//                        source: o,
//                        target: o
//                    });
//                })
//                .remove();

//            // Stash the old positions for transition.
//            nodes.forEach(function (d) {
//                d.x0 = d.x;
//                d.y0 = d.y;
//            });
//        }

//        // Append a group which holds all nodes and which the zoom Listener can act upon.
//        var svgGroup = baseSvg.append("g");

//        // Define the root
//        root = treeData;
//        root.x0 = viewerHeight / 2;
//        root.y0 = 0;

//        // Layout the tree initially and center on the root node.
//        update(root);
//        try {
//            centerNode(d3.select(".you").data()[0]);
//        } catch (e) {
//            centerNode(root);
//        }
//    });
//}