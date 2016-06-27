var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var Charts;
(function (Charts) {
    var Util = (function () {
        function Util() {
        }
        Util.pad = function (num, size) {
            var s = num + "";
            while (s.length < size)
                s = "0" + s;
            return s;
        };
        return Util;
    })();
    Charts.Util = Util;
    var Margin = (function () {
        function Margin() {
            this.top = 10;
            this.bottom = 30;
            this.left = 30;
            this.right = 30;
        }
        return Margin;
    })();
    Charts.Margin = Margin;
    var Dimension = (function () {
        function Dimension(width, height) {
            this.width = width;
            this.height = height;
        }
        return Dimension;
    })();
    Charts.Dimension = Dimension;
    var Base = (function () {
        function Base(_selector, _dimension, _margin) {
            if (_dimension === void 0) { _dimension = new Dimension(960, 500); }
            if (_margin === void 0) { _margin = new Margin(); }
            this._selector = _selector;
            this._dimension = _dimension;
            this._margin = _margin;
            this.LabelFunction = function (d) { return "" + d; };
            this._WasInitialized = false;
            this._AfterInitialized = [];
        }
        Base.prototype.width = function () {
            return this._dimension.width - this._margin.left - this._margin.right;
        };
        Base.prototype.height = function () {
            return this._dimension.height - this._margin.top - this._margin.bottom;
        };
        Base.prototype.SetLabelFunction = function (f) {
            this.LabelFunction = f;
            return this;
        };
        Base.prototype.RotateXTitle = function () {
            this.RunAfterInitialized(this, function () {
                d3.select(this._selector + " svg .x.axis").selectAll("text")
                    .attr("y", 0)
                    .attr("x", 9)
                    .attr("dy", ".35em")
                    .attr("transform", "rotate(90)")
                    .style("text-anchor", "start");
            });
            return this;
        };
        Base.prototype.RunAfterInitialized = function (self, f) {
            if (this._WasInitialized) {
                f.call(self);
            }
            else {
                this._AfterInitialized.push([self, f]);
            }
        };
        Base.prototype.SetInitialized = function () {
            this._WasInitialized = true;
            for (var i in this._AfterInitialized) {
                var a = this._AfterInitialized[i];
                a[1].call(a[0]);
            }
        };
        return Base;
    })();
    Charts.Base = Base;
    var Pie = (function (_super) {
        __extends(Pie, _super);
        function Pie(selector, dimension, margin) {
            if (dimension === void 0) { dimension = new Dimension(960, 500); }
            if (margin === void 0) { margin = new Margin(); }
            _super.call(this, selector, dimension, margin);
            margin.bottom = Math.max(110, margin.bottom);
            margin.left = Math.max(40, margin.left);
            this.DefaultColors = ["#98abc5", "#8a89a6", "#7b6888", "#6b486b", "#a05d56", "#d0743c", "#ff8c00"];
            this.TextColor = "#F4F4F4";
        }
        Pie.prototype.Initialize = function (data, yTitle) {
            //var parseDate = d3.time.format("%d-%b-%y").parse;
            var _this = this;
            if (yTitle === void 0) { yTitle = ""; }
            var values = [];
            for (var i = 0; i < data.length; i++) {
                var v = data[i];
                v.i = i;
                values.push(data[i]);
            }
            //var color = d3.scale.ordinal().range(colors);
            var radius = Math.min(this.width(), this.height()) / 2;
            var arc = d3.svg.arc().outerRadius(radius - 10).innerRadius(0);
            var pie = d3.layout.pie().sort(null).value(function (d) { return d.y; });
            var svg = d3.select(this._selector).append("svg")
                .attr("width", this.width())
                .attr("height", this.height())
                .append("g")
                .attr("transform", "translate(" + this.width() / 2 + "," + this.height() / 2 + ")");
            var g = svg.selectAll(".arc")
                .data(pie(values))
                .enter().append("g")
                .attr("class", "arc");
            g.append("path")
                .attr("d", arc)
                .style("fill", function (d) {
                if (d.data.color)
                    return d.data.color;
                return _this.DefaultColors[d.data.i % _this.DefaultColors.length];
            });
            g.append("text")
                .attr("transform", function (d) {
                var shift = [0, 0];
                if (values.length != 1)
                    shift = arc.centroid(d);
                shift[1] -= 10;
                return "translate(" + shift + ")";
            })
                .attr("dy", ".35em")
                .style({ "text-anchor": "middle", "font-size": "1.75em", "fill": this.TextColor })
                .text(function (d) { return d.data.group; });
            g.append("text")
                .attr("transform", function (d) {
                var shift = [0, 0];
                if (values.length != 1)
                    shift = arc.centroid(d);
                shift[1] += 10;
                return "translate(" + shift + ")";
            })
                .attr("dy", ".55em")
                .style({ "text-anchor": "middle", "font-size": "1em", "fill": this.TextColor })
                .text(function (d) { return d.data.y; });
            this.SetInitialized();
            return this;
        };
        return Pie;
    })(Base);
    Charts.Pie = Pie;
    var Line = (function (_super) {
        __extends(Line, _super);
        function Line(selector, dimension, margin) {
            if (dimension === void 0) { dimension = new Dimension(960, 500); }
            if (margin === void 0) { margin = new Margin(); }
            _super.call(this, selector, dimension, margin);
            margin.bottom = Math.max(110, margin.bottom);
            margin.left = Math.max(40, margin.left);
        }
        Line.prototype.Initialize = function (data, yTitle) {
            //var parseDate = d3.time.format("%d-%b-%y").parse;
            var _this = this;
            if (yTitle === void 0) { yTitle = ""; }
            var x = d3.time.scale().range([0, this.width()]);
            var y = d3.scale.linear().range([this.height(), 0]);
            var xAxis = d3.svg.axis().scale(x).orient("bottom").tickFormat(function (d) { return _this.LabelFunction(d); });
            var yAxis = d3.svg.axis().scale(y).ticks(10).tickValues([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]).orient("left");
            var line = d3.svg.line().x(function (d) { return x(d.x); }).y(function (d) { return y(d.y); });
            var svg = d3.select(this._selector).append("svg")
                .classed("charts line-chart", true)
                .attr("viewBox", "0 0 " + this._dimension.width + " " + this._dimension.height)
                .attr("width", "100%")
                .attr("height", "100%")
                .append("g")
                .attr("transform", "translate(" + this._margin.left + "," + this._margin.top + ")");
            x.domain(d3.extent(data, function (d) { return d.x; }));
            y.domain([1, 10] /* d3.extent(data, d=> d.y)*/);
            svg.append("g")
                .attr("class", "x axis")
                .attr("transform", "translate(0," + this.height() + ")")
                .call(xAxis);
            svg.append("g")
                .attr("class", "y axis")
                .call(yAxis)
                .append("text")
                .attr("transform", "rotate(-90)")
                .attr("y", 6)
                .attr("dy", ".71em")
                .style("text-anchor", "end")
                .text(yTitle);
            svg.append("path")
                .datum(data)
                .attr("class", "line")
                .attr("d", line);
            svg.selectAll(".dot")
                .data(data)
                .enter().append("circle")
                .attr("class", function (d) { return "dot " + d.classed; })
                .attr("cx", line.x())
                .attr("cy", line.y())
                .attr("r", 3.5);
            this.SetInitialized();
            return this;
        };
        return Line;
    })(Base);
    Charts.Line = Line;
    var Histogram = (function (_super) {
        __extends(Histogram, _super);
        function Histogram(selector, dimension, margin) {
            if (dimension === void 0) { dimension = new Dimension(960, 500); }
            if (margin === void 0) { margin = new Margin(); }
            _super.call(this, selector, dimension, margin);
            this.initialized = false;
            margin.bottom = Math.max(110, margin.bottom);
            margin.top = Math.max(25, margin.top);
            this.bins = [];
        }
        Histogram.prototype._CreateSvg = function () {
            return d3.select(this._selector).append("svg")
                .classed("charts histogram", true)
                .attr("viewBox", "0 0 " + this._dimension.width + " " + this._dimension.height)
                .attr("width", "100%")
                .attr("height", "100%")
                .append("g")
                .attr("transform", "translate(" + this._margin.left + "," + this._margin.top + ")");
        };
        Histogram.prototype._Initialize = function (svg, data, x, y, xAxis, classed) {
            var _this = this;
            var formatCount = d3.format(",.0f");
            var bar = svg.selectAll(".bar ." + classed)
                .data(data)
                .enter().append("g")
                .attr("class", function (s) { return "bar " + classed; })
                .attr("transform", function (d) { return "translate(" + x(d.x) + "," + y(d.y) + ")"; });
            bar.append("rect")
                .attr("x", 1)
                .attr("width", x(data[1].x) - x(data[0].x))
                .attr("height", function (d) { return _this.height() - y(d.y); });
            bar.append("text")
                .classed("value", true)
                .attr("dy", ".75em")
                .attr("y", -21)
                .attr("x", (x(data[1].x) - x(data[0].x)) / 2)
                .attr("text-anchor", "middle")
                .text(function (d) {
                var f = formatCount(d.y);
                if (f == "0")
                    return "";
                return f;
            });
            svg.append("g")
                .attr("class", "x axis")
                .attr("transform", "translate(0," + this.height() + ")")
                .call(xAxis);
            return this;
        };
        Histogram.prototype.InitializeWidth = function (values2, binWidth) {
            var _this = this;
            if (binWidth === void 0) { binWidth = 10; }
            var min = Number.MAX_VALUE;
            var max = Number.MIN_VALUE;
            //var values: any[]=[];
            var groups = {};
            for (var i = 0; i < values2.length; i++) {
                max = Math.max(max, values2[i].x);
                min = Math.min(min, values2[i].x);
                groups[values2[i].group] = true;
            }
            min = Math.floor(min / binWidth) * binWidth;
            max = Math.ceil(max / binWidth) * binWidth;
            var count = Math.ceil((max - min) / binWidth);
            var binsHalf = [];
            var constructBins = !this.bins || this.bins.length == 0;
            var even = true;
            var low = Number.MAX_VALUE;
            var high = Number.MIN_VALUE;
            if (constructBins) {
                this.bins = [];
                var pad = 1;
                if (count < 8)
                    pad = 4;
                low = min - binWidth * pad;
                high = max + binWidth * pad;
                i = low;
                while (i <= high) {
                    this.bins.push(i);
                    if (even)
                        binsHalf.push(i);
                    i += binWidth;
                    even = !even;
                }
            }
            else {
                for (var j in this.bins) {
                    if (even)
                        binsHalf.push(this.bins[j]);
                    even = !even;
                    low = Math.min(low, this.bins[j]);
                    high = Math.max(high, this.bins[j]);
                }
            }
            var useBins = this.bins;
            if (this.bins.length > 20)
                useBins = binsHalf;
            var svg = this._CreateSvg();
            var maxFreq = 0;
            for (var j in groups) {
                var values = [];
                for (var i = 0; i < values2.length; i++) {
                    if (values2[i].group + "" == j) {
                        values.push(values2[i].x);
                    }
                }
                var data = d3.layout.histogram().bins(this.bins)(values);
                maxFreq = Math.max(maxFreq, d3.max(data, function (d) { return d.y; }));
            }
            var first = true;
            for (var j in groups) {
                var values = [];
                for (var i = 0; i < values2.length; i++) {
                    if (values2[i].group + "" == j) {
                        values.push(values2[i].x);
                    }
                }
                var data = d3.layout.histogram().bins(this.bins)(values);
                var x = d3.scale.linear().domain([low, high]).range([0, this.width()]);
                var y = d3.scale.linear().domain([0, maxFreq]).range([this.height(), 0]);
                var xAxis = d3.svg.axis().scale(x).orient("bottom").ticks(0).tickFormat(function (d) { return _this.LabelFunction(d); });
                if (first)
                    xAxis = d3.svg.axis().scale(x).orient("bottom").tickValues(useBins).tickFormat(function (d) { return _this.LabelFunction(d); });
                first = false;
                this._Initialize(svg, data, x, y, xAxis, j);
            }
            this.SetInitialized();
        };
        return Histogram;
    })(Base);
    Charts.Histogram = Histogram;
})(Charts || (Charts = {}));
//# sourceMappingURL=Charts.js.map