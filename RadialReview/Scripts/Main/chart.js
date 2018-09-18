jQuery.fn.appendGraph = function (options) {
	var id = ("chart-" + Math.random());
	var chart = $("<div id=" + id + ">");
	chart.addClass("metric-graphic");
	var o = $(this[0]);
	o.append(chart);



	if ($("[href='/Content/Chart/metric-graphics.css']").length == 0) {
		$('<link>').appendTo('head').attr({ type: 'text/css', rel: 'stylesheet', href: '/Content/Chart/metric-graphics.css' });
	}
	function _conditionOptions(obj) {
		for (var k in obj) {
			if (obj.hasOwnProperty(k)) {
				var x = obj[k];
				if (typeof (x) === "undefined" || x === null) {
					delete obj[k];
				}
			}
		}
		function dive(a) {
			return a.map(function (y) {
				if (Array.isArray(y)) {
					return dive(y);
				} else {
					y.date = Time.parseJsonDate(y.date);
					return y;
				}
			});
		}
		if (typeof (obj.data) !== "undefined") {
			obj.data = dive(obj.data);
		}
	}

	function _createChart() {
		var defaultOptions = {
			title: "",
			data: [],
			target: chart[0],
			interpolate: d3.curveLinear,
			center_title_full_width: true,
			right: 40
		};
		options = $.extend({}, defaultOptions, options);
		if (typeof (options.width) === "undefined" && typeof (options.full_width) === "undefined")
			options.full_width = true;
		if (typeof (options.height) === "undefined" && typeof (options.full_height) === "undefined")
			options.full_width = true;
		if (typeof (options.url) !== "undefined") {
			$.ajax({
				url: options.url,
				success: function (obj) {
					options = $.extend({}, options, obj);
					_setChart(options);
				}
			});
		} else {
			_setChart(options);
		}
	}

	function _setChart(o) {
		//debugger;
		_conditionOptions(o);
		MG.data_graphic(o);
		chart._options = o;
	}

	function _updateChart(options) {
		var o = $.extend({}, chart._options, options);
		_setChart(o);
	}

	var loading2 = false;
	var loading3 = false;

	function ensureLoaded(func) {
		function run2() {
			function run3() { func(); }
			if (typeof (MG) === "undefined") {
				if (loading3) {
					setTimeout(function () { run2() }, 50);
					console.info("already loading MG");
					return;
				}
				loading3 = true;
				$.getScript("/Scripts/Charts/metrics-graphics.js").done(run3);
			} else { run3(); }
		}
		if (typeof (d3) === "undefined") {
			if (loading2) {
				setTimeout(function () { ensureLoaded(func) }, 50);
				console.info("already loading d3");
				return;
			}
			loading2 = true;
			$.getScript("/Scripts/d3/d3.v4.12.2.js").done(run2);
		} else { run2(); }
	}

	chart.update = function (options) {
		ensureLoaded(function () {
			_updateChart(options);
		});
	};

	ensureLoaded(function () {
		MG.add_hook('line.after_each_series', function (this_data, existing_line, args) {
			//debugger;
			//args.enhanced_rollover = true;
		});
	});

	ensureLoaded(_createChart);

	return chart; // This is needed so others can keep chaining off of this
};