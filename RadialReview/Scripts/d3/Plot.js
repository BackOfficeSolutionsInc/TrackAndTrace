//ScatterChart Constructor
function ScatterChart(id) {
    this.id = id;
    var padding = 50;

    this.height = 1000;//Dont change
    this.width = 1000;//Dont change

    this.leftWidth = 0;
    this.rightWidth = 0;
    this.titleHeight = 10;
    this.footerHeight = 5;

    this.tickSize = 10;

    this.pointRadius = 10;

    this.legendWidth = 200;
    this.legendHeight = 400;
    this.chartPadding = padding;
    this.legendPadding = padding;

    this.xScale = d3.scale.linear();
    this.yScale = d3.scale.linear();

    this.legend = "none";//top,right,bottom

    this.square = true;

    this.chartHeight = function (first) {
        if (first !== undefined || !this.square) {
            var additional = 0;
            if (this.legend.toLowerCase() == "bottom" || this.legend.toLowerCase() == "top")
                additional = this.legendPadding * 2 + this.legendHeight;
            return this.height - (this.titleHeight + this.chartPadding * 2 + this.footerHeight + additional)
        }
        return Math.min(this.chartWidth(true), this.chartHeight(true));
    };

    this.chartWidth = function (first) {

        if (first !== undefined || !this.square) {
            var additional = 0;
            if (this.legend.toLowerCase() == "left" || this.legend.toLowerCase() == "right")
                additional = this.legendPadding * 2 + this.legendWidth;
            return this.width - (this.leftWidth + this.chartPadding * 2 + this.rightWidth + additional)
        }
        return Math.min(this.chartWidth(true), this.chartHeight(true));
    };

    this.legendTopLeft = function () {
        var x, y;
        if (this.legend.toLowerCase() == "left") {
            x = this.leftWidth + this.legendPadding;
            y = this.titleHeight + this.legendPadding;
        } else if (this.legend.toLowerCase() == "right") {
            x = this.leftWidth + this.chartWidth() + this.chartPadding * 2 + this.legendPadding;
            y = this.titleHeight + this.legendPadding;
        } else if (this.legend.toLowerCase() == "top") {
            x = this.leftWidth + this.legendPadding;
            y = this.titleHeight + this.legendPadding;
        } else if (this.legend.toLowerCase() == "bottom") {
            x = this.leftWidth + this.legendPadding;
            y = this.titleHeight + this.chartPadding * 2 + this.chartHeight() + this.legendPadding;
        } else {
            x = this.width;
            y = this.height;
        }

        return { x: x, y: y };
    }

    this.chartTopLeft = function () {
        var additionalLeft = 0;
        var additionalTop = 0;
        if (this.legend.toLowerCase() == "left")
            additionalLeft = this.legendPadding * 2 + this.legendWidth;
        if (this.legend.toLowerCase() == "top")
            additionalTop = this.legendPadding * 2 + this.legendHeight;

        var x = this.leftWidth + this.chartPadding + additionalLeft;
        var y = this.titleHeight + this.chartPadding + additionalTop;
        return { x: x, y: y };
    };
};

ScatterChart.prototype.Pull = function Pull(url, data, callback) {
    var that = this;

    function onComplete(d) {
        this.Data = d;
        if (callback)
            callback.call(that, d);
    }

    $.ajax({
        url: url,
        method: "GET",
        data: data,
        success: function (data) {
            onComplete(data.Object);
        }
    });
};


ScatterChart.prototype.GetDate = function getDate(jsonDate) {
    return new Date(parseInt(jsonDate.substr(6)));
}


ScatterChart.prototype.Plot = function Plot(scatterData, options) {
    /// <param name="scatterData" type="RadialReview.Models.Json.ScatterData">
    ///     ScatterPlot data
    /// </param>
    //Data quality control

    var that = this;

    var first = false;
    if (!this.once) {
        this.once = true;
        first = true;
    }

    if (options === undefined)
        options = new Object();

    options.xAxis = options.xAxis || "x";
    options.yAxis = options.yAxis || "y";
    options.title = options.title || "";

    options.extraClasses = options.extraClasses || "";

    options.xDimensionId = options.xDimensionId || scatterData.InitialXDimension;
    options.yDimensionId = options.yDimensionId || scatterData.InitialYDimension;

    options.mouseover = options.mouseover || function () { return null; };
    options.mouseout = options.mouseout || function () { return null; };
    options.startTime = options.startTime || new Date(0);
    options.endTime = options.endTime || Date.now;
    options.time = options.time || Date.now;
    this.legend = (options.legend || this.legend || "right").toLowerCase();

    options.legendFunc = options.legendFunc || function (legendData,chart) { };

    options.filters = options.filters || [];

    if (options.groups == "")
        options.groups = [[]];

    options.groups = options.groups || [["*"]];//[["class1","class2"],["class3"]]
    if (options.groups.length == 0)
        options.groups = [["*"]];

    if (options.animate === undefined) {
        options.animate = true;
    }
    if (options.reset === undefined) {
        options.reset = options.reset || false;
    }
    options.reset = options.reset || first;

    var xDimension = scatterData.Dimensions[options.xDimensionId];
    var yDimension = scatterData.Dimensions[options.yDimensionId];

    //Functions
    var topLeft = this.chartTopLeft();
    var width = this.chartWidth();
    var height = this.chartHeight();

    var dataIdFunction = function (d) {
        return d.Id;
    };

    this.xScale = this.xScale.range([0, width]); // value -> display
    //setup x
    var xValue = function (scatterDataPoint) {
        try {
            return scatterDataPoint.Dimensions[options.xDimensionId].Value / scatterDataPoint.Dimensions[options.xDimensionId].Denominator;
        } catch (e) {
            console.log(e);
            return 0;
        }
    }, // data -> value
        xMap = function (scatterDataPoint) {
            return that.xScale(xValue(scatterDataPoint)) + topLeft.x + that.pointRadius/2;
        }, // data -> display
        xAxis = d3.svg.axis().scale(this.xScale).orient("bottom").tickSize(this.tickSize).tickSubdivide(true);

    this.yScale = this.yScale.range([height, 0]); // value -> display
    //setup y
    var yValue = function (scatterDataPoint) {
        try{
            return scatterDataPoint.Dimensions[options.yDimensionId].Value / scatterDataPoint.Dimensions[options.yDimensionId].Denominator;
        } catch (e) {
            console.log(e);
            return 0;
        }
    }, // data -> value
        yMap = function (scatterDataPoint) {
            return that.yScale(yValue(scatterDataPoint)) + topLeft.y + that.pointRadius / 2;
        }, // data -> display
        yAxis = d3.svg.axis().scale(this.yScale).orient("left").tickSize(this.tickSize).tickSubdivide(true);

    if (options.reset && options.animate) {
        this.xScale.domain([-1, 1]);
        this.yScale.domain([-1, 1]);
    }
    
    var svgContainerOuter = d3.select("#" + this.id);
    var svgContainer = d3.select("#" + this.id + " svg");
    var container = d3.select("#" + this.id + " svg g.middle");
    var underContainer = d3.select("#" + this.id + " svg g.bottom");
    var overContainer = d3.select("#" + this.id + " svg g.top");
    var legend = d3.select("#" + this.id + " svg g.legend");
    var xAxisTitle = d3.select("#" + this.id + " svg g.xAxisTitle text");
    var yAxisTitle = d3.select("#" + this.id + " svg g.yAxisTitle text");
    var title = d3.select("#" + this.id + " svg g.title text");

    options.legendFunc(scatterData.Legend, this);

    if (options.reset) {
        if (d3.select(".scatter-tooltip")[0][0]==null) {
            d3.select("body").append("div")
                .attr("class", "scatter-tooltip")
                .style("position", "absolute")
                .style("z-index", "10")
                .style("visibility", "hidden")
                .style("background-color", "white")
                .style("padding", "10px")
                .style("background-color", "white")
                .style("border", "1px solid #808080")
                .style("border-radius","3px");
        }


        svgContainer = svgContainerOuter.append("svg").attr("xmlns", "http://www.w3.org/2000/svg");
        svgContainer.html('<marker xmlns="http://www.w3.org/2000/svg" id="triangle" viewBox="0 0 10 10" refX="18" refY="5" markerUnits="strokeWidth" markerWidth="10" markerHeight="10" orient="auto"><path d="M 0 0 L 10 5 L 0 10 z"/></marker>');

        underContainer = svgContainer.append("g").attr("class", "bottom");
        container = svgContainer.append("g").attr("class", "middle");
        overContainer = svgContainer.append("g").attr("class", "top");

        svgContainer.attr("viewBox", "0 0 " + this.width + " " + this.height)
            .attr("class", scatterData.Class)
            .attr("height", "100%")
            .attr("width", "100%");

        //x-axis
        container.append("g")
                    .attr("class", "x axis")
                    .attr("transform", function (d) { return "translate(" + (topLeft.x) + "," + (topLeft.y + height) + ")"; })
                    .call(xAxis);
        container.append("line")
                    .attr("class", "x axis origin")
                    .attr("x1", topLeft.x)
                    .attr("x2", topLeft.x + width)
                    .attr("y1", topLeft.y + height / 2)
                    .attr("y2", topLeft.y + height / 2);
        xAxisTitle = container.append("g")
                        .attr("transform", function (d) { return "translate(" + (topLeft.x + width *.10) + "," + (topLeft.y + height/2 - 2) + ")"; })
                        .attr("class", "xAxisTitle axisTitle")
                        .append("text")
                        .attr("text-anchor", "middle");

        //y-axis
        container.append("g")
                    .attr("class", "y axis")
                    .attr("transform", function (d) { return "translate(" + (topLeft.x) + "," + (topLeft.y) + ")"; })
                    .call(yAxis);

        yAxisTitle = container.append("g")
                        .attr("transform", function (d) { return "translate(" + (topLeft.x +width/2 -2) + "," + (topLeft.y + height *.90) + ")"; })
                        .attr("class", "yAxisTitle axisTitle")
                        .append("text")
                        .attr("text-anchor", "middle")
                        .attr("transform", function (d) { return "rotate(-90)"; });

        container.append("line")
                    .attr("class", "y axis origin")
                    .attr("x1", topLeft.x + width / 2)
                    .attr("x2", topLeft.x + width / 2)
                    .attr("y1", topLeft.y)
                    .attr("y2", topLeft.y + height);

        title = container.append("g")
                    .attr("transform", function (d) { return "translate(" + (topLeft.x + width/2) + "," + (topLeft.y -10) + ")"; })
                    .attr("class", "title")
                    .append("text")
                    .attr("text-anchor", "middle");



        legend = container.append("g").attr("class", "legend");
        var legendPos = this.legendTopLeft();
        legend.append("rect")
            .attr("x", legendPos.x)
            .attr("y", legendPos.y)
            .attr("width", this.legendWidth)
            .attr("height", this.legendHeight);

    }

    svgContainer.attr("class", options.extraClasses.join(" "));

    xAxisTitle.text(options.xAxis);
    yAxisTitle.text(options.yAxis);
    title.text(options.title);

    //Moving points around
    var transition = svgContainer;

    if (options.animate) {
        transition = transition.transition().duration(500).ease("exp-in-out");
    } else {
        transition = transition;
    }

    //Group points together
    var scatterDataGrouped = [];

    var dataPoints = [];

    function containsAll(list, required) {
        var comparableWildcards = [];
        for (var r = 0; r < required.length; r++) {
            var found = false;
            var regexStr = "^"+required[r].replace("*", "[a-zA-Z0-9_\\-]+")+"$";
            var re = new RegExp(regexStr, "g")
            for (var i = 0; i < list.length; i++) {
                if (re.test(list[i])) {
                    found = true;
                    /*if(required[r]!=list[i])
                    {
                        debugger;
                        comparableWildcards.push(list[i]);
                    }*/
                }
            }
            if (found == false)
                return false;
        }
        return true;
    }

    function containsAny(list, required) {
        var comparableWildcards = [];
        for (var r = 0; r < required.length; r++) {
            var found = false;
            var regexStr = "^"+required[r].replace("*", "[a-zA-Z0-9_\\-]+")+"$";
            var re = new RegExp(regexStr, "g")
            for (var i = 0; i < list.length; i++) {
                if (re.test(list[i])) {
                    return true;
                }
            }
        }
        return false;
    }


    function intersection(array1, array2) {
        return array1.filter(function (n) {
            return array2.indexOf(n) != -1
        });
    }

    function jqueryIntersection(array1, array2) {
        var a2 = $.map(array2, function (d) { return d.outerHTML; });
        return $.map(array1,function (d) { return d.outerHTML; }).filter(function (e) {
            return a2.indexOf(e) != -1
        });
    }

    function splitBySpace(classStr) {
        var trimmed = classStr.replace(/^\s+|\s+$/g, '');
        return trimmed.split(/\s+/g);
    }

    function getClasses(classStr) {
        return splitBySpace(classStr);
    }

    function getTitles(titles)
    {
        var titles = $("<div>" + titles + "</div>").find(".title");
        return titles;/*
        var trimmed = classStr.replace(/^\s+|\s+$/g, '');
        return trimmed.split(/\s+/g);*/
    }

    

    function normalizedMatchingClasses(classes, requiredClasses) {
        var matches = [];
        var required = requiredClasses;
        for (var r = 0; r < required.length; r++) {
            var found = false;
            var regexStr = required[r].replace("*", "[a-zA-Z0-9_\\-]+");
            var re = new RegExp(regexStr, "");
            for (var i = 0; i < classes.length; i++) {
                if (re.test(classes[i])) {
                    found = true;
                    matches.push(classes[i]);
                }
            }
            if (found == false)
                return null;
        }
        matches.sort();
        return matches;
    }

    function findMatches(points, requiredClasses, requireAll)//RequiredClasses=["c1","c2"..]
    {
        var matches = [];
        for (var p = 0; p < points.length; p++) {
            var point = points[p];

            var availableDims = {};

            for (var d in point.Dimensions) {
                var dim = point.Dimensions[d];
                var classes = point.Class + " " + dim.Class;
                var classList = getClasses(classes);

                //var comparableWildcards=containsAll(classList, requiredClasses);

                if (
                    (requireAll && containsAll(classList, requiredClasses)) ||
                    (!requireAll && containsAny(classList, requiredClasses))
                   ) {
                    availableDims[dim.DimensionId] = dim;
                }

            }

            if (Object.keys(availableDims).length > 0) {
                var newPoint = jQuery.extend(true, {}, point);
                newPoint.Dimensions = availableDims;
                matches.push(newPoint);
            }
        }
        return matches;
    }


    //groupClasses=["class1","class2"]
    function mergePoints(points, groupClasses) {
        var merged = {};

        for (var i = 0; i < points.length; i++) {
            var point = points[i];
            var sliceId = point.SliceId;
            var normClasses = normalizedMatchingClasses(getClasses(point.Class), groupClasses);
            var groupId = normClasses.join(",");
            var key = normClasses;

            key.splice(0, 0, sliceId);

            if (!(key in merged)) {
                merged[key] = {
                    SliceId: point.SliceId,
                    Id: point.Id,
                    Date: point.Date,
                    Dimensions: {},
                    Class: point.Class,
                    GroupId: groupId,
                    Title: point.Title,
                    Subtext: point.Subtext,
                };
            }

            merged[key].Class = intersection(getClasses(merged[key].Class), getClasses(point.Class)).join(" ");
            merged[key].Title = jqueryIntersection(getTitles(merged[key].Title), getTitles(point.Title)).join(" ");

            for (var d in point.Dimensions) {
                var dim = point.Dimensions[d];
                if (!(d in merged[key].Dimensions)) {
                    merged[key].Dimensions[d] = {
                        DimensionId: dim.DimensionId,
                        Value: 0,
                        Denominator: 0,
                        Class: dim.Class,
                    };
                }

                merged[key].Dimensions[d].Value += dim.Value;
                merged[key].Dimensions[d].Denominator += dim.Denominator;
                merged[key].Dimensions[d].Class = intersection(getClasses(merged[key].Dimensions[d].Class), getClasses(dim.Class)).join(" ");
            }
        }

        var output = [];
        for (var m in merged) {
            //merged[m].groupId = m.;
            output.push(merged[m]);
        }
        return output;
    }

    function separateByGroups(points, groups) {
        /*if (groups.length == 0)
            return points;*/
        if (groups == "none") {
            for (var key in points)
            {
                points[key].GroupId = points[key].OtherData.GroupId;
            }

            return points;
        }

        var separated = [];
        for (var g = 0; g < groups.length; g++) {
            var groupPoints = mergePoints(findMatches(points, groups[g], true), groups[g]);
            for (var p = 0; g < groupPoints.length; g++) {
                separated.push(groupPoints[g]);
            }
        }

        return separated;
    }

    function getPrevious(point, points) {
        var found = points.filter(function (d) {
            return d.GroupId == point.GroupId;
        });
        found.sort(function (a, b) { return b.SliceId - a.SliceId; });

        var index = found.indexOf(point);
        if (index > 0)
            return found[index - 1];
        return null;
    }

    function getNext(point, points) {
        var found = points.filter(function (d) {
            return d.GroupId == point.GroupId;
        });
        found.sort(function (a, b) { return b.SliceId - a.SliceId; });

        var index = found.indexOf(point);
        if (index < found.length - 1)
            return found[index + 1];
        return null;
    }



    var filtered;

    if (options.filters.length != 0) {
        filtered = findMatches(scatterData.Points, options.filters, false);
    } else {
        filtered = scatterData.Points;
    }

    var dataPoints = separateByGroups(filtered, options.groups);

    //On first call:
    var dataset = container.selectAll(".scatter-point").data(dataPoints, dataIdFunction);
    //dataset.enter().append("circle").attr("cx", xMap).attr("cy", yMap);
    dataset.enter().append("g")
        .attr("transform", function(d) {
            var x = xMap(d);
            var y = yMap(d);
            return "translate(" + x + "," + y + ")";
        }).append("circle").attr("cx", 0).attr("cy", 0).attr("r",10).classed("point",true);

    var onExit = dataset.exit().transition().duration(200);
    onExit.remove();
    onExit.select(".point")
        .attr("r", 0)
        .style("opacity", "0");

    dataset.attr("class", function (d) {
        try {
            return "scatter-point " + d.Class + " " + d.Dimensions[options.xDimensionId].Class + " " + d.Dimensions[options.yDimensionId].Class;
        } catch (e) {
            console.log(e);
            return "scatter-point";
        }
    });/*.on("mouseover", function () {
        debugger;
        return tooltip.style("visibility", "visible");
    }).on("mousemove", function () {
	    return tooltip.style("top", (event.pageY - 10) + "px").style("left", (event.pageX + 10) + "px");
	}).on("mouseout", function () {
	    debugger;
	    return tooltip.style("visibility", "hidden");
	}).text(function(d){
        return d.Title;
    });*/

    var lineSet = underContainer.selectAll(".scatter-link").data(dataPoints.filter(function (d) { return getPrevious(d, dataPoints) || false; }), dataIdFunction);
    lineSet.enter()
        .append("line")
        .style("opacity","0")
        .attr("marker-end", "url(#triangle)")
        .attr("x1", xMap)
        .attr("x2", function (d) {
            return xMap(getPrevious(d, dataPoints));
        })
        .attr("y1", yMap)
        .attr("y2", function (d) {
            return yMap(getPrevious(d, dataPoints));
        });

    //Resize domain
    this.xScale.domain([xDimension.Min, xDimension.Max]);
    this.yScale.domain([yDimension.Min, yDimension.Max]);

    //Shift axis
    var verticalShift = Math.max(0, Math.min(1, yDimension.Max / (yDimension.Max - yDimension.Min))) * height;
    var horizontalShift = Math.max(0, Math.min(1, xDimension.Min / (xDimension.Min - xDimension.Max))) * width;
    transition.select(".x.axis.origin")
            .attr("y1", topLeft.y + verticalShift)
            .attr("y2", topLeft.y + verticalShift);
    transition.select(".y.axis.origin")
            .attr("x1", topLeft.x + horizontalShift)
            .attr("x2", topLeft.x + horizontalShift);
    transition.select(".x.axis").call(xAxis);
    transition.select(".y.axis").call(yAxis);

    
    var tooltip = d3.select(".scatter-tooltip");
    //Update point positions
    var pointSet = dataset
        .on("mouseover", function (d) {
            options.mouseover(d, that);
            tooltip.style("visibility", "visible");
            tooltip.html(d.Title);
        }).on("mousemove", function (d) {
            console.log(event);
            tooltip.style("top", (event.pageY - 10) + "px").style("left", (event.pageX + 10) + "px");
        }).on("mouseout", function (d) {
            options.mouseout(d, that);
            tooltip.style("visibility", "hidden");
        }).classed("prior", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date < options.startTime;
        }).classed("posterior", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date > options.endTime;
        }).classed("inclusive", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date <= options.endTime && date >= options.startTime;
        }).classed("exclusive", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date > options.endTime || date < options.startTime;
        }).classed("nearest", function (d) {


            var nearestId = null;
            var nearestDelta = new Date(100000, 0, 1);
            var cur = d;
            var alreadyHit = []

            while (cur && alreadyHit.indexOf(cur.Id) == -1) {
                var date = new Date(parseInt(cur.Date.substr(6)));
                var delta = Math.abs(date - options.time);
                if (delta < nearestDelta) {
                    nearestDelta = delta;
                    nearestId = cur.Id;
                }
                alreadyHit.push(cur.Id);
                cur = getPrevious(cur, dataPoints);

            }
            var cur = d;
            while (cur && alreadyHit.indexOf(cur.Id) == -1) {
                var date = new Date(parseInt(cur.Date.substr(6)));
                var delta = Math.abs(date - options.time);
                if (delta < nearestDelta) {
                    nearestDelta = delta;
                    nearestId = cur.Id;
                }
                alreadyHit.push(cur.Id);
                cur = getNext(cur, dataPoints);
            }
            return nearestId == d.Id;
        })
    if (options.animate) {
        pointSet = pointSet.transition()
        .delay(function (d) { return 0; })
        .duration(500).ease("exp-in-out");
    }
    //pointSet.attr("cx", xMap).attr("cy", yMap).attr("r", function (d) {return that.pointRadius;});
    
    pointSet.attr("transform", function (d) {
        var x = xMap(d);
        var y = yMap(d);
        return "translate(" + x + "," + y + ")";
    })

    lineSet.attr("class", function (d) {
        return "scatter-link " + d.Class + " " + d.Dimensions[options.xDimensionId].Class + " " + d.Dimensions[options.yDimensionId].Class;
    })
        .classed("prior", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date < options.startTime;
        }).classed("posterior", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date > options.endTime;
        }).classed("inclusive", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date <= options.endTime && date >= options.startTime;
        }).classed("exclusive", function (d) {
            var date = new Date(parseInt(d.Date.substr(6)));
            return date > options.endTime || date < options.startTime;
        })
        .transition()
        .duration(500)
        .ease("exp-in-out")
        .attr("x1", xMap)
        .attr("x2", function (d) {
            return xMap(getPrevious(d, dataPoints));
        })
        .attr("y1", yMap)
        .attr("y2", function (d) {
            return yMap(getPrevious(d, dataPoints));
        })
        .call(function (d) {
            d.style("opacity", null);
        });
        

    lineSet.exit().remove();
        //.remove();
    //dataset.exit().transition().style("opacity", "0").duration(100).remove();

};