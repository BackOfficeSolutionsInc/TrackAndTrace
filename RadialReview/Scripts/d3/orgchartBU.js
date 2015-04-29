/// <reference path="underscore.js" />
/// <reference path="d3.v3.min.js" />
//http://jsfiddle.net/VividD/LLLKP/

function createOrgChart(selector) {
    var ref = 1000;

    var chart = d3.select(selector).html("").append("svg")
                    .attr("height", "100%")
                    .attr("width", "100%")
                    .attr("viewBox", "0 0 " + ref + " " + ref)
                    .attr("preserveAspectRatio", "xMidYMid meet")
                    .attr("class", "orgchart")
                    .attr("pointer-events", "all")
                    .append("g")
                        .call(d3.behavior.zoom().scaleExtent([0.1, 10]).on("zoom", zoom))
                //d3.behavior.zoom().on("zoom", redraw))
                    .append('g');
 chart.append("rect")
        .attr('width', ref)
        .attr('height', ref)
        .attr('fill', 'white')
        .attr('fill-opacity', '0');
    chart.append("g").attr("class", "links");
    chart.append("g").attr("class", "nodes");
    chart.append("g").attr("class", "people");


   

    chart.scaleFactor = 1;
    chart.translation = [0, 0];

    function zoom() {
        chart.scaleFactor = d3.event.scale;
        chart.translation = d3.event.translate;
        tick(chart); //update positions
    }

    chart.masterForce = d3.layout.force()
                                    .charge(-20)
                                    .linkDistance(1)
                                    .linkStrength(1)
                                    .gravity(.02)
                                    .size([ref, ref]);
    /*chart.append('svg:rect')
        .attr('width', ref)
        .attr('height', ref)
        .attr('fill', 'yellow')
        .attr('fill-opacity', '0');*/
    // chart.append("g").attr("pointer-events", "all").attr("class", "links");
    // chart.append("g").attr("pointer-events", "all").attr("class", "nodes");
    // chart.append("g").attr("pointer-events", "all").attr("class", "people");

    chart.ref = ref;
    chart.nodes = [];
    chart.links = [];
    chart.ns = [];
    chart.ls = [];
    chart.chartNodes = [];
    chart.bilinks = [];
    chart.roots = [];
    chart.people = [];
    chart.uid = -1;
    chart.selector = selector;
    chart.lookup = {};

    return chart;
}



function nextId(chart) {
    var id = chart.uid;
    chart.uid--;
    return id;
}

function dblclick(d) {
    d3.event.stopPropagation();
    var self=d3.select(this);
    if (self.data()[0].depth != 0) {
        self.classed("fixed", d.fixed = false);
    } else {
    }
}

function dragstart(d) {
    d3.event.sourceEvent.stopPropagation();
    d3.select(this).classed("fixed", d.fixed = true);

}

function dragged(d,chart) {
    d3.event.sourceEvent.stopPropagation();
    
    if (d.depth != 0) {
        //if (d.fixed) return; //root is fixed

        //get mouse coordinates relative to the visualization
        //coordinate system:
        var mouse = d3.mouse(chart.node());
        d.x = rX(mouse[0], chart);
        d.y = rY(mouse[1], chart);

        tick(chart);//re-position this node and any links
    } else {
        return false;
    }
}

function mouseover(d) {
    d3.select(this).transition().attrTween("r", function (d, i, a) { return d3.interpolate(a, 20); });

}

function mouseleave(d) {
    d3.select(this).transition().attrTween("r", function (d, i, a) { return d3.interpolate(a, 10); })
}

function updateOrgChart(chart) {

    var force = chart.masterForce;

   /* if (chart.chartNodes.length > 0)
        flatten();*/
    /*var root=chart.roots[0];
    
    var nodes2 = flatten(root);
    var links2 = d3.layout.tree().links(nodes2);*/
    chart.chartNodes.forEach(function (d, i) {
        if (!d.fixed) {
            d.x = chart.ref / 2 + i;
            d.y = 100 * d.depth + 100;

            if (d.depth == 0) {
                d.fixed = true;
                d.x = chart.ref / 2;
                d.y = 0;
            }
        }
    });

    chart.nodes.forEach(function (d, i) {
        if (d.depth == 0) {
            d.root = true;
        }
    });


    /*
    chart.chartNodes.forEach(function (d, i) {
        d.x = chart.ref / 2 + i;
        d.y = 100 * d.depth + 100;
    });*/

    force.nodes(chart.chartNodes)
        .links(chart.links)
        .start();

    var nodes = chart.select("g.nodes").selectAll("circle.node").data(chart.nodes);
    var links = chart.select("g.links").selectAll("path.link").data(chart.bilinks);
    //var people = chart.selectAll(".people").selectAll("g").data(chart.people);

    chart.ns = nodes;
    chart.ls = links

    var drag = force.drag().on("dragstart", dragstart).on("drag", function (d) { return dragged(d, chart); });

    nodes.enter()
            .append("circle")
            .attr("fill", "green")
            .attr("class", function (d) {
                return "node f" + d.depth+" id_"+d.id;
            })
            .attr("r", 10)
            .on("dblclick", dblclick)
            .on("mouseover", mouseover)
            .on("mouseleave", mouseleave)
            .call(drag);

    links.enter()
        .append("path")
        .attr("class", "link");

    force.on("tick", function (e) { tick(chart,e); });

    nodes.exit().remove();
}


/*
function flatten(root) {
    var nodes = [];
    function recurse(node, depth) {
        if (node.children) {
            node.children.forEach(function (child) {
                recurse(child, depth + 1);
            });
        }
        node.depth = depth;
        nodes.push(node);
    }
    recurse(root, 1);
    return nodes;
}*/


function rX(x, chart) {
    return chart.translation[0] + chart.scaleFactor * x;
}
function rY(y, chart) {
    return chart.translation[1] + chart.scaleFactor * y;
}
function rXI(x, chart) {
    var val=chart.translation[0] + 1.0 / chart.scaleFactor * x
    return val;
}
function rYI(y, chart) {
    return chart.translation[1] + 1.0/chart.scaleFactor * y;
}


function tick(chart, e) {
    var ky = chart.masterForce.alpha();
    if (e !== undefined)
        ky = e.alpha;

    chart.links.forEach(function (d, i) {
        if (!d.target.fixed) {
            d.target.y += (d.target.depth * 100 - d.target.y) * 5 * ky;
        }
    });
    chart.ls.attr("d", function (d) {
        return "M" + rX(d[0].x,chart) + "," + rY(d[0].y,chart)
             + "S" + rX(d[1].x,chart) + "," + rY(d[1].y,chart)
             + " " + rX(d[2].x,chart) + "," + rY(d[2].y,chart);
    });
    chart.ns.attr("cx", function (d) { return rX(d.x,chart); })
        .attr("cy", function (d) { return rY(d.y,chart); });
}

function addLink(chart, node1, node2) {
    var s = chart.lookup["n_" + node1],
        t = chart.lookup["n_" + node2],
        i = {depth:Math.min(s.depth,t.depth)}; // intermediate node

    if (s !== undefined && t !== undefined) {
        chart.chartNodes.push(i);
        chart.lookup["n_" + node1 + "_" + node2] = i;
        var link1 = { source: s, target: i };
        var link2 = { source: i, target: t };

        chart.lookup["l1_" + node1 + "_" + node2] = link1;
        chart.lookup["l2_" + node1 + "_" + node2] = link2;
        chart.links.push(link1, link2);
        var bilink = [s, i, t];
        chart.lookup["bl_" + node1 + "_" + node2] = bilink;
        chart.bilinks.push(bilink);
    } else {
    }
}

function removeLink(chart, node1, node2) {

}

function addNode(chart,id ,depth) {
    if (id === undefined)
        id = nextId(chart);
    var node = { id: id,depth:depth };
    chart.nodes.push(node);
    chart.chartNodes.push(node);
    chart.lookup["n_" + id] = node;
    //
}
/*
function addRoot(chart, id) {
    if (id === undefined)
        id = nextId(chart);
    var node = { id: id ,root:true};
    chart.roots.push(node);
    chart.chartNodes.push(node);
    chart.lookup["r_" + id] = node;
    //
}
*/
