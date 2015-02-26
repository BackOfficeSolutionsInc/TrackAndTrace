var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var Charts;
(function (Charts) {
    var Margin = (function () {
        function Margin(margins) {
            if (margins === void 0) { margins = 30; }
            this.top = margins;
            this.bottom = margins;
            this.left = margins;
            this.right = margins;
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
        }
        Base.prototype.width = function () {
            return this._dimension.width - this._margin.left - this._margin.right;
        };
        Base.prototype.height = function () {
            return this._dimension.height - this._margin.top - this._margin.bottom;
        };
        return Base;
    })();
    Charts.Base = Base;
    var Histogram = (function (_super) {
        __extends(Histogram, _super);
        function Histogram(selector, dimension, margin) {
            if (dimension === void 0) { dimension = new Dimension(960, 500); }
            if (margin === void 0) { margin = new Margin(); }
            _super.call(this, selector, dimension, margin);
            this.initialized = false;
        }
        Histogram.prototype.InitializeWidth = function (values, binWidth) {
            if (binWidth === void 0) { binWidth = 10; }
            var max = Math.max.apply(Math, values);
            var min = Math.min.apply(Math, values);
            var count = Math.ceil((max - min) / binWidth);
            return this.InitializeCount(values, count);
        };
        Histogram.prototype.InitializeCount = function (values, binCount) {
            var _this = this;
            if (binCount === void 0) { binCount = 20; }
            this.values = values;
            this.initialized = true;
            var max = Math.max.apply(Math, values);
            var min = Math.min.apply(Math, values);
            var x = d3.scale.linear().domain([min, max]).range([0, this.width()]);
            var data = d3.layout.histogram().bins(x.ticks(binCount))(values);
            var y = d3.scale.linear().domain([0, d3.max(data, function (d) { return d.y; })]).range([this.height(), 0]);
            var xAxis = d3.svg.axis().scale(x).orient("bottom");
            var formatCount = d3.format(",.0f");
            var svg = d3.select(this._selector).append("svg").attr("viewBox", "0 0 100 100").attr("width", "100%").attr("height", "100%").append("g").attr("transform", "translate(" + this._margin.left + "," + this._margin.top + ")");
            var bar = svg.selectAll(".bar").data(data).enter().append("g").attr("class", "bar").attr("transform", function (d) { return "translate(" + x(d.x) + "," + y(d.y) + ")"; });
            bar.append("rect").attr("x", 1).attr("width", x(data[0].dx) - 1).attr("height", function (d) { return _this.height() - y(d.y); });
            bar.append("text").attr("dy", ".75em").attr("y", 6).attr("x", x(data[0].dx) / 2).attr("text-anchor", "middle").text(function (d) { return formatCount(d.y); });
            svg.append("g").attr("class", "x axis").attr("transform", "translate(0," + this.height() + ")").call(xAxis);
            //d3.layout.histogram().bins();
        };
        return Histogram;
    })(Base);
    Charts.Histogram = Histogram;
})(Charts || (Charts = {}));
//@ sourceMappingURL=pubsub.js.map 
//# sourceMappingURL=Charts.js.map