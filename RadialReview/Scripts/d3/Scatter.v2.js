//ScatterImage Constructor
function ScatterImage(id) {
	this.id = id;

	this.height = 500;//Dont change
	this.width = 500;//Dont change
	
	this.defaultPoints = function (points) {
		points.append("circle")
		.attr("r", function (d) { return d.radius; })
		.style("fill", function (d) { return d.color; });
	};

	this.nodeSize = 22;

	var nodeSize = this.nodeSize;

	this.imagePoints = function (points) {
		var g = points.append("g").classed("card", true).attr("transform", "translate(-" + (nodeSize / 2) + ",-" + (nodeSize / 2) + ")");
		g.append("rect").classed("border", true).attr("width", nodeSize).attr("height", nodeSize);
		var extra = g.append("g").classed("extra", true).attr("opacity", "0");
		extra.append("rect").classed("background", true).attr("transform", "translate(2,2)").attr("width", 100).attr("height", 100);
		extra.append("foreignObject").classed("title", true).attr("width", "160px").attr("height", "26px").attr("x", "110").attr("y", "3")
			.html(function (d) { return "<div title='" + d.title + "'>" + d.title + "</div>"; });
		extra.append("text").classed("subtitle", true).attr("x", "115").attr("y", "41").text(function (d) { return d.subtitle; });
		extra.append("text").classed("axisTitle", true).attr("x", "115").attr("y", "68").text(function(d) { return d.xAxis; });
		extra.append("text").classed("axisTitle", true).attr("x", "115").attr("y", "91").text(function (d) { return d.yAxis; });
		extra.append("text").classed("axisValue", true).attr("x", "268").attr("y", "68").attr("text-anchor", "end").text(function (d) { return Math.round(d.xValue); });
		extra.append("text").classed("axisValue", true).attr("x", "268").attr("y", "91").attr("text-anchor", "end").text(function (d) { return Math.round(d.yValue); });
		g.append("image").classed("image", true).attr("transform", "translate(2,2)").attr("width", nodeSize - 4).attr("height", nodeSize - 4).attr("xlink:href", function(d) { return d.imageUrl; });
		g.append("rect").classed("hoverable",true).attr("width", nodeSize).attr("height", nodeSize).attr("opacity", "0")
				.on("mouseover", function () {
					d3.select(this).transition().attr("width", "278").attr("height", "104");
					d3.select(this.parentNode).select(".border").transition().attr("width", "278").attr("height", "104");
					//d3.select(this.parentNode).selectAll(".extra").attr("opacity","1");
					d3.select(this.parentNode).select(".image").transition().attr("transform", "translate(6,6)").attr("width", "92").attr("height", "92");
					d3.select(this.parentNode).selectAll(".extra").transition().delay(200).duration(200).attr("opacity", "1");
				}).on("mouseleave", function () {

					d3.select(this).transition().attr("width", nodeSize).attr("height", nodeSize);
					d3.select(this.parentNode).transition().select(".border").attr("width", nodeSize).attr("height", nodeSize);

					d3.select(this.parentNode).select(".image").transition().attr("transform", "translate(2,2)").attr("width", nodeSize - 4).attr("height", nodeSize - 4);
					//d3.select(this.parentNode).select(".border").transition().attr("width",nodeSize).attr("height",nodeSize);

					d3.select(this.parentNode).selectAll(".extra").attr("opacity", "0");
					d3.select(this.parentNode).selectAll(".extra").transition().duration(100).attr("opacity", "0");
				});
	};




};

ScatterImage.prototype.Pull = function Pull(url, data, callback) {
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

ScatterImage.prototype.PullPlot = function PullPlot(url, args, callback) {
	var that = this;
	function callback2(data) {
		this.Plot(data.Points, data);
		if (callback)
			callback.call(that, data);
	}
	this.Pull(url, args, callback2);
};


ScatterImage.prototype.Plot = function Plot(scatterData, options) {
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
	options.defaultFilter = options.defaultFilter;

	options.xAxis = options.xAxis || "x";
	options.yAxis = options.yAxis || "y";

	options.nodeSize = this.nodeSize;
	options.padding = options.padding || 0;//options.nodeSize/4;// separation between nodes
	options.chartPadding = options.chartPadding || 30;// separation between nodes

	options.axisRectSize = options.axisRectSize || options.chartPadding/2;
	options.drawPoints = options.drawPoints || this.imagePoints;

	options.height = options.height || this.height;
	options.width = options.width || this.width;


	options.xMin = options.xMin || -100;
	options.yMin = options.yMin || -100;
	options.xMax = options.xMax ||  100;
	options.yMax = options.yMax ||  100;

	var chartCenterX = (options.width / 2);// - options.chartPadding * 2) / 2;
	var chartCenterY = (options.height / 2);// - options.chartPadding * 2) / 2;

	var xMap = d3.scale.linear().domain([options.xMin, options.xMax]).range([options.chartPadding, options.width  - options.chartPadding]);
	var yMap = d3.scale.linear().domain([options.yMax, options.yMin]).range([options.chartPadding, options.height - options.chartPadding]);


	options.callback = options.callback || function () { };

	if (options.animate === undefined) {
		options.animate = true;
	}


	var force = d3.layout.force()
		.nodes(scatterData)
		.size([options.width, options.height])
		.gravity(0)
		.charge(0)
		.on("tick", tick)
		.on('end', function () {
			d3.selectAll(".beginHidden").classed("beginHidden", false);
		}).start();

	var svg = d3.select("#" + this.id).append("svg")
		.attr("viewBox", "0 0 " + options.width + " " + options.height)
		.attr("width", "100%")
		.attr("height", "100%")
		.classed("scatter", true);

	svg.append("marker")
		.attr("xmlns", "http://www.w3.org/2000/svg")
		.attr("id", "arrowId")
		.attr("viewBox", "0 0 10 10")
		.attr("refX",10)
		.attr("refY", 5)
		.attr("markerUnits", "strokeWidth")
		.attr("markerWidth", 6)
		.attr("markerHeight", 5).attr("orient", "auto")
		.append("path")
		.attr("d", "M 0 0 L 10 5 L 0 10 z");

	svg.append("rect").classed("axisRect axisRect-x", true)
		.attr("x", options.chartPadding).attr("width", options.width - options.chartPadding * 2)
		.attr("y", options.height - options.chartPadding - (options.axisRectSize / 2) + (options.chartPadding / 2)).attr("height", options.axisRectSize);
	svg.append("rect").classed("axisRect axisRect-y", true)
		.attr("x", -(options.axisRectSize / 2) + (options.chartPadding / 2)).attr("width", options.axisRectSize)
		.attr("y", options.chartPadding).attr("height", options.height - options.chartPadding * 2);

	svg.append("text").classed("axisTitle axisTitle-x", true).attr("x", chartCenterX).attr("y", options.height - options.chartPadding / 2 + options.axisRectSize / 3).attr("text-anchor", "middle").text(options.xAxis);
	svg.append("text").classed("axisTitle axisTitle-y", true).attr("transform", "translate(" + (options.chartPadding / 2 + options.axisRectSize / 3) + "," + chartCenterY + "),rotate(-90)").attr("text-anchor", "middle").text(options.yAxis);

	svg.append("line").classed("axis axis-y", true).attr("x1", chartCenterX).attr("x2", chartCenterX).attr("y1", options.chartPadding).attr("y2", options.height - options.chartPadding);
	svg.append("line").classed("axis axis-x", true).attr("y1", chartCenterY).attr("y2", chartCenterY).attr("x1", options.chartPadding).attr("x2", options.width - options.chartPadding);

	svg.append("text").classed("title", true).attr("x", chartCenterX).attr("y", options.chartPadding / 2 + options.axisRectSize / 3).attr("text-anchor", "middle").text(options.title);


	d3.selection.prototype.moveToFront = function () {
		return this.each(function () {
			this.parentNode.appendChild(this);
		});
	};


	$.each(scatterData, function (i, d) {
		d.xValue = d.cx;
		d.yValue = d.cy;

		d.cx = xMap(d.cx);
		d.cy = yMap(d.cy);
		d.radius = this.nodeSize;
	});



	var enter = svg.selectAll("g.points").data(scatterData).enter();

	var lines = enter.append("line")
					.classed("beginHidden", true)
					.attr("x1", function (d) { return d.x;  })
					.attr("x2", function (d) { return d.cx; })
					.attr("y1", function (d) { return d.y;  })
					.attr("y2", function (d) { return d.cy; })
					//.style("stroke",function(d){return d.color;})
					.call(force.drag);
	var gs = enter.append("g")
					.classed("beginHidden", true)
					.attr("x", function (d) { return d.x; })
					.attr("y", function (d) { return d.y; })
					.on("mouseover", function (d) {
						d3.select(this).moveToFront();
					}).call(force.drag);
	var points = options.drawPoints(gs);


	function tick(e) {
		gs.each(gravity(.2 * e.alpha))
			.each(collide(.5))
			.attr("transform", function (d) {
				return "translate(" + d.x + "," + d.y + ")";
			});
		//.attr("cx", function(d) { return d.x; })
		//.attr("cy", function(d) { return d.y; });

		lines.each(gravity(.2 * e.alpha))
			  .each(collide(.5))
			  .attr("x1", function (d) { return d.x; })
			  .attr("x2", function (d) { return d.cx; })
			  .attr("y1", function (d) { return d.y; })
			  .attr("y2", function (d) { return d.cy; })
			  .attr("marker-end", "url(#arrowId)");
	}

	// Move nodes toward cluster focus.
	function gravity(alpha) {
		return function (d) {
			d.y += (d.cy - d.y) * alpha;
			d.x += (d.cx - d.x) * alpha;
		};
	}

	// Resolve collisions between nodes.
	function collide(alpha) {
		var quadtree = d3.geom.quadtree(scatterData);
		return function (d) {
			var r = options.nodeSize + options.padding,
				nx1 = d.x - r,
				nx2 = d.x + r,
				ny1 = d.y - r,
				ny2 = d.y + r;
			quadtree.visit(function (quad, x1, y1, x2, y2) {
				if (quad.point && (quad.point !== d)) {
					var x = d.x - quad.point.x,
						y = d.y - quad.point.y,
						l = Math.sqrt(x * x + y * y),
						r = options.nodeSize*1.35 + options.padding;
					if (l < r) {
						l = (l - r) / l * alpha;
						d.x -= x *= l;
						d.y -= y *= l;
						quad.point.x += x;
						quad.point.y += y;
					}
				}
				if (quad.point && (quad.point !== d)) {
					var x = d.x - quad.point.cx,
						y = d.y - quad.point.cy,
						l = Math.sqrt(x * x + y * y),
						r = options.nodeSize*2 + /*(d.color !== quad.point.color) */ options.padding;
					if (l < r) {
						l = (l - r) / l * alpha;
						d.x -= x *= l;
						d.y -= y *= l;
						//quad.point.x += x;
						//quad.point.y += y;
					}
				}


				return x1 > nx2 || x2 < nx1 || y1 > ny2 || y2 < ny1;
			});

			var m = options.height - options.chartPadding - options.nodeSize/2;
			if (d.y > m) {
				d.y -= (d.y - m) * alpha / 2;
			}
			m = options.chartPadding + options.nodeSize / 2;
			if (d.y < m) {
				d.y -= (d.y - m) * alpha / 2;
			}


			m = options.width - options.chartPadding - options.nodeSize / 2;
			if (d.x > m) {
				d.x -= (d.x - m) * alpha/2;
			}
			m = options.chartPadding + options.nodeSize / 2;
			if (d.x < m) {
				d.x -= (d.x - m) * alpha / 2;
			}
		};
	}

};