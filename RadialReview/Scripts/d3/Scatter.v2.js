//ScatterImage Constructor
function ScatterImage(id) {
	this.id = id;
	
	this.height = 500;//Dont change
	this.width = 500;//Dont change

	//Start Shapes
	var a = 12, b = 4;
	var builder = "";
	builder += (-b / 2) + "," + (-b / 2) + " ";
	builder += (-b / 2) + "," + (-a / 2) + " ";
	builder += (b / 2) + "," + (-a / 2) + " ";
	builder += (b / 2) + "," + (-b / 2) + " ";
	builder += (a / 2) + "," + (-b / 2) + " ";
	builder += (a / 2) + "," + (b / 2) + " ";
	builder += (b / 2) + "," + (b / 2) + " ";
	builder += (b / 2) + "," + (a / 2) + " ";
	builder += (-b / 2) + "," + (a / 2) + " ";
	builder += (-b / 2) + "," + (b / 2) + " ";
	builder += (-a / 2) + "," + (b / 2) + " ";
	builder += (-a / 2) + "," + (-b / 2);
	this.cross = builder;

	a = 9;
	var t = -0.433012702;
	this.triangle = "0," + (a * t) + " " + (a * .5) + "," + (-a * t) + " " + (a * -0.5) + "," + (-a * t);
	////End Shapes

	this.defaultPoints = function (points) {
		points.append("circle")
		.attr("r", function (d) { return d.radius; })
		.style("fill", function (d) { return d.color; });
	};

	this.nodeSize = 22;
	this.arrowSize = 4;
	var nodeSize = this.nodeSize;
	this.imagePoints = function (points) {
		var g = points.append("g").attr("class", function (d) {
			var o = d.class;
			return o;
		}).classed("card", true).attr("transform", "translate(-" + (nodeSize / 2) + ",-" + (nodeSize / 2) + ")");
		g.append("rect").classed("border", true).attr("width", nodeSize).attr("height", nodeSize);
		var extra = g.append("g").classed("extra", true).attr("opacity", "0");
		extra.append("rect").classed("background", true).attr("transform", "translate(2,2)").attr("width", 100).attr("height", 100);
		extra.append("foreignObject").classed("title", true).attr("width", "160px").attr("height", "26px").attr("x", "110").attr("y", "3")
			.html(function (d) {
				var loc = "";
				if (d.link) {
					return "<a class='' title='" + d.title + "' xlink:href='" + d.link + "'><text class=''>" + d.title + "</text></a>";
				}
				return "<div title='" + d.title + "'>" + d.title + "</div>";
			});
		extra.append("foreignObject").classed("subtitle", true).attr("width", "160px").attr("height", "26px").attr("x", "110").attr("y", "26")
			.html(function (d) {
				return "<div title='" + d.subtitle + "'>" + d.subtitle + "</a>";
			});
		extra.append("text").classed("axisTitle", true).attr("x", "115").attr("y", "68").text(function (d) { return d.xAxis; });
		extra.append("text").classed("axisTitle", true).attr("x", "115").attr("y", "91").text(function (d) { return d.yAxis; });
		extra.append("text").classed("axisValue", true).attr("x", "268").attr("y", "68").attr("text-anchor", "end").text(function (d) { return Math.round(d.xValue); });
		extra.append("text").classed("axisValue", true).attr("x", "268").attr("y", "91").attr("text-anchor", "end").text(function (d) { return Math.round(d.yValue); });
		g.append("image").classed("image", true).attr("transform", "translate(2,2)").attr("width", nodeSize - 4).attr("height", nodeSize - 4).attr("xlink:href", function (d) { return d.imageUrl; });
		//g.append("image").classed("image", true).attr("transform", "translate(2,2)").attr("width", nodeSize - 4).attr("height", nodeSize - 4).attr("data-src", function (d) { return d.imageUrl; });
		g.append("rect").classed("pointerEvents", true).classed("hoverable", function (d) { return d.link !== undefined; }).attr("width", nodeSize).attr("height", nodeSize).attr("opacity", "0")
				.on("mouseover", function (d) {
					d3.select(this).transition().delay(100).attr("width", "278").attr("height", "104");
					d3.select(this.parentNode).select(".border").transition().delay(100).attr("width", "278").attr("height", "104");
					//d3.select(this.parentNode).selectAll(".extra").attr("opacity","1");
					d3.select(this.parentNode).select(".image").transition().delay(100).attr("transform", "translate(6,6)").attr("width", "92").attr("height", "92");
					d3.select(this.parentNode).selectAll(".extra").transition().delay(300).duration(200).attr("opacity", "1");
					var dd = d;
					setTimeout(function () {
						d.CanClick = true;
					},400);

				}).on("mouseleave", function (d) {

					d3.select(this).transition().attr("width", nodeSize).attr("height", nodeSize);
					d3.select(this.parentNode).transition().select(".border").attr("width", nodeSize).attr("height", nodeSize);

					d3.select(this.parentNode).select(".image").transition().attr("transform", "translate(2,2)").attr("width", nodeSize - 4).attr("height", nodeSize - 4);
					//d3.select(this.parentNode).select(".border").transition().attr("width",nodeSize).attr("height",nodeSize);

					d3.select(this.parentNode).selectAll(".extra").attr("opacity", "0");
					d3.select(this.parentNode).selectAll(".extra").transition().duration(100).attr("opacity", "0");
					d.CanClick = false;
				}).on("click", function (d) {
					console.log(d);
					if (d.link && d.CanClick) {
						window.location.href = d.link;
						d3.event.stopPropagation();
					}
				});
		/*$.getScript("/Scripts/jquery/jquery.unveil.js", function () {
			debugger;
			$("#" + id + " image").unveil();
		});*/
	};

	this.shapePoints = function (points) {
		var g = points.append("g").attr("class", function (d) {
			return d.class + " pointerEvents scatterPoint";
		}).each(function (d) {
			if (d.class.indexOf("shape-square") != -1) {
				d3.select(this).append("rect").attr("x", -4).attr("y", -4).attr("width", 8).attr("height", 8).classed("pointerEvents", true);
			} else if (d.class.indexOf("shape-triangle") != -1) {
				d3.select(this).append("polygon").attr("points", chart.triangle).classed("pointerEvents", true);
			} else if (d.class.indexOf("shape-x") != -1) {
				d3.select(this).append("polygon").attr("points", chart.cross).attr("transform", "rotate(45)").classed("pointerEvents", true);
			} else if (d.class.indexOf("shape-diamond") != -1) {
				d3.select(this).append("rect").attr("x", -3).attr("y", -3).attr("width", 7).attr("height", 7).attr("transform", "rotate(45)").classed("pointerEvents", true);
			} else if (d.class.indexOf("shape-plus") != -1) {
				d3.select(this).append("polygon").attr("points", chart.cross).classed("pointerEvents", true);
			} else {
				d3.select(this).append("circle").attr("cx", 0).attr("cy", 0).attr("r", 5).classed("pointerEvents", true);
			}

		});
	};

	this.shapeLegend = function (legendId, legend) {
		if (legendId && legend) {
			d3.select("#" + legendId).html("");
			var svg = d3.select("#" + legendId).append("svg")
				.attr("viewBox", "0 0 100 200")
				.attr("width", "100%")
				.attr("height", "100%")
				.classed("scatterLegend", true);

			svg.append("marker")
				.attr("xmlns", "http://www.w3.org/2000/svg")
				.attr("id", "arrowId")
				.attr("viewBox", "0 0 10 10")
				.attr("refX", 10)
				.attr("refY", 5)
				.attr("markerUnits", "strokeWidth")
				.attr("markerWidth", 4)
				.attr("markerHeight",3.25).attr("orient", "auto")
				.append("path")
				.attr("d", "M 0 0 L 10 5 L 0 10 z");

			var i = 1;
			var pts = svg.selectAll("g.scatterPoint").data(legend).enter()
				.append("g")
				.attr("transform", function (d) { return "translate(8," + (i++) * 16 + ")"; });
			pts.append("g")
				.classed("title", true)
				.attr("transform", "translate(8,3)")
				.append("text")
				.text(function (d) { return d.title; });
			this.shapePoints(pts);
		}
	};
};

jQuery.fn.d3MouseOver = function () {
	this.each(function (i, e) {
		var evt = document.createEvent("MouseEvents");
		evt.initMouseEvent("mouseover", true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
		e.dispatchEvent(evt);
	});
};
jQuery.fn.d3MouseLeave = function () {
	this.each(function (i, e) {
		var evt = document.createEvent("MouseEvents");
		evt.initMouseEvent("mouseleave", true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
		e.dispatchEvent(evt);
	});
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

ScatterImage.prototype.PullPlot = function PullPlot(url, args, callback, options) {
	var that = this;
	var opts = options;

	function callback2(data) {
		if (opts !== undefined) {
			for (var attrname in opts) {
				data[attrname] = opts[attrname];
			}
		}
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

	options.axisRectSize = options.axisRectSize || options.chartPadding / 2;
	options.drawPoints = options.drawPoints || this.imagePoints;

	options.height = options.height || this.height;
	options.width = options.width || this.width;

	if (options.useForce === undefined)
		options.useForce = true;

	options.legendFunc = options.legendFunc || function (d) {
		if (options.legendId) {
			d3.select("#" + options.legendId).html("");
		}
	};


	options.xMin = options.xMin || -100;
	options.yMin = options.yMin || -100;
	options.xMax = options.xMax || 100;
	options.yMax = options.yMax || 100;

	var chartCenterX = (options.width / 2);// - options.chartPadding * 2) / 2;
	var chartCenterY = (options.height / 2);// - options.chartPadding * 2) / 2;

	var xMap = d3.scale.linear().domain([options.xMin, options.xMax]).range([options.chartPadding, options.width - options.chartPadding]);
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
			if (options.rest) {
				options.rest();
			}
		}).start();

	this.force = force;

	d3.select("#" + this.id).html("");
	var svg = d3.select("#" + this.id).append("svg")
		.attr("viewBox", "0 0 " + options.width + " " + options.height)
		.attr("width", "100%")
		.attr("height", "100%")
		.classed("scatter", true);

	svg.append("marker")
		.attr("xmlns", "http://www.w3.org/2000/svg")
		.attr("id", "arrowId")
		.attr("viewBox", "0 0 10 10")
		.attr("refX", 10)
		.attr("refY", 5)
		.attr("markerUnits", "strokeWidth")
		.attr("markerWidth", 4)
		.attr("markerHeight", 4).attr("orient", "auto")
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

	svg.append("rect").classed("border", true).attr("x", options.chartPadding).attr("y", options.chartPadding)
		.attr("width", options.width - 2 * options.chartPadding).attr("height", options.height - 2 * options.chartPadding);

	svg.append("text").classed("title", true).attr("x", chartCenterX).attr("y", options.chartPadding / 2 + options.axisRectSize / 3).attr("text-anchor", "middle").text(options.title);

	options.legendFunc.call(that, options.legendId, options.Legend);


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

	if (options.useForce) {
		var lines = enter.append("line")
			.attr("class", function (d) {
				return d.class;
			}).classed("beginHidden", true)
			.attr("x1", function (d) { return d.x; })
			.attr("x2", function (d) { return d.cx; })
			.attr("y1", function (d) { return d.y; })
			.attr("y2", function (d) { return d.cy; })
			//.style("stroke",function(d){return d.color;})
			.call(force.drag);
	}
	var gs = enter.append("g")
					.classed("beginHidden", true)
					.attr("x", function (d) { return d.x; })
					.attr("y", function (d) { return d.y; })
					.on("mouseover", function (d) {
						d3.select(this).moveToFront();
					}).call(force.drag);
	var points = options.drawPoints(gs);


	function tick(e) {
		if (options.useForce) {
			gs.each(gravity(.15 * e.alpha))
				.each(collide(.05))
				.attr("transform", function (d) {
					return "translate(" + d.x + "," + d.y + ")";
				});
			//.attr("cx", function(d) { return d.x; })
			//.attr("cy", function(d) { return d.y; });

			lines.each(gravity(.15 * e.alpha))
				.each(collide(.05))
				.attr("x1", function (d) { return d.x; })
				.attr("x2", function (d) { return d.cx; })
				.attr("y1", function (d) { return d.y; })
				.attr("y2", function (d) { return d.cy; })
				.attr("marker-end", "url(#arrowId)");
		} else {
			gs.attr("transform", function (d) {
				return "translate(" + d.cx + "," + d.cy + ")";
			});
			
			/*lines.attr("x1", function(d) { return d.cx; });
			lines.attr("x2", function(d) { return d.cx; });
			lines.attr("y1", function(d) { return d.cy; });
			lines.attr("y2", function(d) { return d.cy; }).attr("marker-end", "url(#arrowId)");*/
		}
	}

	// Move nodes toward cluster focus.
	function gravity(alpha) {
		return function (d) {
			if ($(this).find(".hidden").length == 0) {
				d.y += (d.cy - d.y) * alpha;
				d.x += (d.cx - d.x) * alpha;
			}
		};
	}

	// Resolve collisions between nodes.
	function collide(alpha) {
		var quadtree = d3.geom.quadtree(scatterData);
		return function (d) {
			if ($(this).find(".hidden").length==0) {
				var r = options.nodeSize + options.padding,
					nx1 = d.x - r,
					nx2 = d.x + r,
					ny1 = d.y - r,
					ny2 = d.y + r;
				quadtree.visit(function(quad, x1, y1, x2, y2) {
					if (quad.point && (quad.point !== d)) {
						if (!d3.select(quad.point)[0][0].hidden && !d.hidden) {
							var x = d.x - quad.point.x,
								y = d.y - quad.point.y,
								l = Math.sqrt(x * x + y * y),
								r = options.nodeSize * .75 /*1.35*/ + options.padding;
							if (l < r) {
								l = (l - r) / l * alpha;
								d.x -= x *= l;
								d.y -= y *= l;
								quad.point.x += x;
								quad.point.y += y;
							}
						}
					}
					if (quad.point && (quad.point !== d)) {
						if (!d3.select(quad.point)[0][0].hidden) {
							var x = d.x - quad.point.cx,
								y = d.y - quad.point.cy,
								l = Math.sqrt(x * x + y * y),
								r = options.nodeSize * .00 /*2*/ + /*(d.color !== quad.point.color) */ options.padding;
							if (l < r) {
								l = (l - r) / l * alpha;
								d.x -= x *= l;
								d.y -= y *= l;
								//quad.point.x += x;
								//quad.point.y += y;
							}
						}
					}


					return x1 > nx2 || x2 < nx1 || y1 > ny2 || y2 < ny1;
				});

				var m = options.height - options.chartPadding - options.nodeSize / 2;
				if (d.y > m) {
					d.y -= (d.y - m) * alpha / 2;
				}
				m = options.chartPadding + options.nodeSize / 2;
				if (d.y < m) {
					d.y -= (d.y - m) * alpha / 2;
				}


				m = options.width - options.chartPadding - options.nodeSize / 2;
				if (d.x > m) {
					d.x -= (d.x - m) * alpha / 2;
				}
				m = options.chartPadding + options.nodeSize / 2;
				if (d.x < m) {
					d.x -= (d.x - m) * alpha / 2;
				}
				/*
				if (d.hidden) {
					d.x = d.cx;
					d.y = d.cy;
				}*/
			}
		};
	}

};