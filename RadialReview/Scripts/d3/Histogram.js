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
            if (typeof margins === "undefined") { margins = 30; }
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
        function Base(_id, _dimension, _margin) {
            if (typeof _dimension === "undefined") { _dimension = new Dimension(960, 500); }
            if (typeof _margin === "undefined") { _margin = new Margin(); }
            this._id = _id;
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
        function Histogram(id, dimension, margin) {
            if (typeof dimension === "undefined") { dimension = new Dimension(960, 500); }
            if (typeof margin === "undefined") { margin = new Margin(); }
            _super.call(this, id, dimension, margin);
            this.initialized = false;
        }
        Histogram.prototype.Initialize = function (values, binCount) {
            if (typeof binCount === "undefined") { binCount = 20; }
            this.initialized = true;
            var max = Math.max.apply(Math, values);
            var min = Math.min.apply(Math, values);

            var x = d3.scale.linear().domain([min, max]).range([0, this.width()]);
            var data = d3.layout.histogram().bins(x.ticks(binCount))(values);
            var y = d3.scale.linear().domain([0, d3.max(data, function (d) {
                    return d.y;
                })]).range([this.height(), 0]);

            var xAxis = d3.svg.axis().scale(x).orient("bottom");

            var svg = d3.select(this._id).append("svg").attr("width", width + margin.left + margin.right).attr("height", height + margin.top + margin.bottom).append("g").attr("transform", "translate(" + margin.left + "," + margin.top + ")");

            var bar = svg.selectAll(".bar").data(data).enter().append("g").attr("class", "bar").attr("transform", function (d) {
                return "translate(" + x(d.x) + "," + y(d.y) + ")";
            });

            bar.append("rect").attr("x", 1).attr("width", x(data[0].dx) - 1).attr("height", function (d) {
                return height - y(d.y);
            });

            bar.append("text").attr("dy", ".75em").attr("y", 6).attr("x", x(data[0].dx) / 2).attr("text-anchor", "middle").text(function (d) {
                return formatCount(d.y);
            });

            svg.append("g").attr("class", "x axis").attr("transform", "translate(0," + height + ")").call(xAxis);

            d3.layout.histogram().bins();
        };

        Histogram.prototype.SetData = function (data) {
            this.data = data;
        };
        return Histogram;
    })(Base);
    Charts.Histogram = Histogram;
})(Charts || (Charts = {}));
//# sourceMappingURL=Histogram.js.map
