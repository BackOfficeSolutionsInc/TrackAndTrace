﻿


var width = 600, // width of the graph
    height = 500, // height of the graph
    xPadding = 100,
    padding = 50,
    xRange = d3.scale.linear().range([xPadding, width - xPadding]), // x range function
    yRange = d3.scale.linear().range([height - padding, padding]), // y range function
    rRange = d3.scale.linear().range([5, 15]), // radius range function
    drawingData, // data we want to display
    xAxis = d3.svg.axis().scale(xRange).tickSize(5).tickSubdivide(true), // x axis function
    yAxis = d3.svg.axis().scale(yRange).tickSize(5).orient("left").tickSubdivide(true), // y axis function
    xAxisLabel,
    yAxisLabel,
    xAxisText,
    yAxisText,
    legend,
    vis; // visualisation selection
var axisSelect = AxisSelect;
var axisLabels = AxisLabel;


// runs once when the visualisation loads
function init() {

    vis = d3.select("#visualisation");

    vis.append("svg:g")
        .append("svg:line")
        .attr("style", "stroke: #000; opacity: 0.4;")
        .attr("x1", xPadding)
        .attr("x2", width - xPadding)
        .attr("y1", height / 2)
        .attr("y2", height / 2);

    vis.append("svg:g")
        .append("svg:line")
        .attr("style", "stroke: #000; opacity: 0.4;")
        .attr("y1", padding)
        .attr("y2", height - padding)
        .attr("x1", width / 2)
        .attr("x2", width / 2);


    // add in the x axis
    vis.append("svg:g")
        .attr("class", "x axis")
        .attr("transform", "translate(0," + (height - padding) + ")")
        .call(xAxis);

    // add in the y axis
    vis.append("svg:g")
        .attr("class", "y axis")
        .attr("transform", "translate(" + xPadding + ",0)")
        .call(yAxis);

    // add in axis labels
    vis.append("svg:text")
       .attr("class", "x label")
         .attr("text-anchor", "middle")
         .attr("x", width / 2)
         .attr("y", height - 5)
         .text("");

    vis.append("svg:text")
       .attr("class", "y label")
         .attr("text-anchor", "middle")
         //.attr("x", 50)
         //.attr("y", height / 2)
         .text("")
         .attr("transform", function (d) { return "translate(50," + height / 2 + ")rotate(-90)"; });

    // add in legend
    vis.append("text")
        .attr("class", "legendTitle")
        .attr("x", width - xPadding)
        .attr("y", 35)
        .text("Legend:")
    /*
    vis.append("text")
        .attr("class", "hover")
        .attr("x", 95)
            .attr("y", 151)
            .text("*Hover for HRR Name");*/

    var legend = vis.selectAll("g.legend")
      .data(LegendData)
    .enter().append("svg:g")
      .attr("transform", function (d, i) { return "translate(" + (width - xPadding + 5) + "," + (i * 14 + 50) + ")"; });

    legend.append("svg:ellipse")
        .attr("class", function (d) { return d; })
              .style("opacity", 0.9)
              .attr("rx", 5)
              .attr("ry", 5);

    legend.append("svg:text")
          .attr("class", "legend")
        .attr("x", 12)
        .attr("dy", ".31em")
        .text(function (d) { return d; });

    // load data and draw it
    update();
} // end init()

// this redraws the graph based on the data in the drawingData variable
function redraw() {
    var dataset = vis.selectAll("circle").data(drawingData, function (d) { return d.id; }), // select the data points and set their data
        axes = getAxes(); // object containing the axes we'd like to use

    // add new points if needed
    dataset.enter()
        .append("svg:circle")
            .attr("cx", function (d) {
                return xRange(d[axes.xAxis]);
            })
            .attr("cy", function (d) {
                return yRange(d[axes.yAxis]);
            })
            .attr("class", "Self")/* function (d) {
                        return "Self";
                        return d.about;
                    })*/

            .append("svg:title")
            .text(function (d) { return d.about; });

    // the data domains or desired axes might have changed, so update them all
    xRange.domain([
        /*d3.min(drawingData, function (d) { return +d[axes.xAxis]; }),
        d3.max(drawingData, function (d) { return +d[axes.xAxis]; })*/
        -100, 100
    ]);
    yRange.domain([
        /*d3.min(drawingData, function (d) { return +d[axes.yAxis]; }),
        d3.max(drawingData, function (d) { return +d[axes.yAxis]; })*/
        -100, 100
    ]);
    rRange.domain([
        d3.min(drawingData, function (d) { return +d[axes.radiusAxis]; }),
        d3.max(drawingData, function (d) { return +d[axes.radiusAxis]; })
    ]);

    // transition the axes
    var t = vis.transition().duration(1500).ease("exp-in-out");
    t.select(".x.axis").call(xAxis);
    t.select(".y.axis").call(yAxis);

    // transition the points
    dataset.transition().duration(500).ease("exp-in-out")
      .attr("class", function (d) { return "datapoint " + d.about; })
        .attr("r", function (d) {
            //return rRange(d[axes.radiusAxis]);
            return 7;
        })
        .attr("cx", function (d) { return xRange(d[axes.xAxis]); })
        .attr("cy", function (d) { return yRange(d[axes.yAxis]); });

    // remove points not needed
    dataset.exit()
        .transition().duration(500).ease("exp-in-out")
        .attr("cx", function (d) { return xRange(d[axes.xAxis]); })
        .attr("cy", function (d) { return yRange(d[axes.yAxis]); })
            .style("opacity", 0)
            .attr("r", 0)
                .remove();

    // update axis labels
    d3.selectAll(".x.label").text(axes.xAxisLabel);
    d3.selectAll(".y.label").text(axes.yAxisLabel);
}

// start it up
init();

// return an object containing the currently selected axis choices
function getAxes() {
    var a = document.getElementById("xAxis"),
          b = document.getElementById("yAxis");
    //c = document.getElementById("r-axis");
    var x = a.options[a.selectedIndex].value,
            y = b.options[b.selectedIndex].value;
    //r = c.options[c.selectedIndex].value;
    var xindex = axisSelect.indexOf(x),
        yindex = axisSelect.indexOf(y);
    var xlabel = axisLabels[xindex]
    ylabel = axisLabels[yindex];
    return {
        xAxis: x,
        yAxis: y,
        //radiusAxis: r,
        xAxisLabel: xlabel,
        yAxisLabel: ylabel
    };
}

// called every time a form field has changed
function update() {
    if ($("#xAxis").val() == $("#yAxis").val()) {
        $(".axisTitle").addClass("red");
    } else {
        $(".axisTitle").removeClass("red");
    }


    d3.csv("/Data/ReviewData/"+ForUserId+"?reviewsId="+ReviewId, function (data) {
        // store data in the appropriate variables
        drawingData = data;
        redraw();
    });
}

function updateSearch(a) {
    d3.selectAll("circle").each(function (d) {
        var element = d3.select(this);
        var compareTerm = d.name.toLowerCase();
        if (compareTerm == a) {
            element.style("stroke-width", 2.5).style("stroke", "#333");
        }
        else { element.style("stroke-width", 0); }
    });
}


// listen to the form fields changing

document.getElementById("controls").addEventListener("click", update, false);
document.getElementById("controls").addEventListener("keyup", update, false);
document.getElementById("xAxis").addEventListener("change", update, false);
document.getElementById("yAxis").addEventListener("change", update, false);
//document.getElementById("r-axis").addEventListener("change", update, false);
