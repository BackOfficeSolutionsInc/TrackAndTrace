module Charts {

	interface NumToStringCallback {
		(n: number): string;
	}

	interface DataPoint {
		x: number;
		y?: number;
		color?:string;
		group?: string;
	}

	export class Util {

		static pad(num: number, size: number): string {
			var s = num + "";
			while (s.length < size) s = "0" + s;
			return s;
		}

	}

	export class Margin {
		top: number;
		bottom: number;
		left: number;
		right: number;

		constructor() {
			this.top = 10;
			this.bottom = 30;
			this.left = 30;
			this.right = 30;
		}

	}

	export class Dimension {

		constructor(public width: number, public height: number) {}

	}

	export class Base {

		constructor(public _selector: string, public _dimension: Dimension = new Dimension(960, 500), public _margin = new Margin()) {}

		width(): number {
			return this._dimension.width - this._margin.left - this._margin.right;
		}

		height(): number {
			return this._dimension.height - this._margin.top - this._margin.bottom;
		}

		LabelFunction = d=> "" + d;
		_WasInitialized = false;
		_AfterInitialized: any[]= [];

		SetLabelFunction(f: NumToStringCallback): any {
			this.LabelFunction = f;
			return this;
		}

		RotateXTitle(): any {
			this.RunAfterInitialized(this, function() {
				d3.select(this._selector + " svg .x.axis").selectAll("text")
					.attr("y", 0)
					.attr("x", 9)
					.attr("dy", ".35em")
					.attr("transform", "rotate(90)")
					.style("text-anchor", "start");
			});
			return this;
		}

		RunAfterInitialized(self, f: Function) {
			if (this._WasInitialized) {
				f.call(self);
			} else {
				this._AfterInitialized.push([self, f]);
			}
		}

		SetInitialized(): void {
			this._WasInitialized = true;
			for (var i in this._AfterInitialized) {
				var a = this._AfterInitialized[i];
				a[1].call(a[0]);
			}
		}

	}

	export class Pie extends Base {

		DefaultColors: string[];
		TextColor: string;

		constructor(selector: string, dimension: Dimension = new Dimension(960, 500), margin = new Margin()) {
			super(selector, dimension, margin);
			margin.bottom = Math.max(110, margin.bottom);
			margin.left = Math.max(40, margin.left);
			this.DefaultColors = ["#98abc5", "#8a89a6", "#7b6888", "#6b486b", "#a05d56", "#d0743c", "#ff8c00"];
			this.TextColor = "#F4F4F4";
		}

		Initialize(data: DataPoint[], yTitle = "") {
			
			//var parseDate = d3.time.format("%d-%b-%y").parse;

			var values: any[] = [];

			for (var i = 0; i < data.length; i++) {
				var v : any = data[i];
				v.i = i;
				values.push(data[i]);
			}

			//var color = d3.scale.ordinal().range(colors);
			var radius = Math.min(this.width(), this.height()) / 2;
			var arc = d3.svg.arc().outerRadius(radius - 10).innerRadius(0);

			var pie = d3.layout.pie().sort(null).value(d=> d.y);

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
				.style("fill", d=> {
					if (d.data.color)
						return d.data.color;
					return this.DefaultColors[d.data.i % this.DefaultColors.length];
				});

			g.append("text")
				.attr("transform", d=> {
				var shift = [0, 0];
				if (values.length != 1)
					shift = arc.centroid(d);
				shift[1] -= 10;
				return "translate(" + shift + ")";
			})
				.attr("dy", ".35em")
				.style({ "text-anchor": "middle", "font-size": "1.75em", "fill": this.TextColor })
				.text(d=> d.data.group);

			g.append("text")
				.attr("transform", d=> {
					var shift = [0, 0];
					if (values.length != 1)
						shift = arc.centroid(d);
					shift[1] += 10;
					return "translate(" + shift + ")";
				})
				.attr("dy", ".55em")
				.style({ "text-anchor": "middle", "font-size": "1em", "fill": this.TextColor })
				.text(d=> d.data.y);

			this.SetInitialized();
			return this;
		}
	}

	export class Line extends Base {

		constructor(selector: string, dimension: Dimension = new Dimension(960, 500), margin = new Margin()) {
			super(selector, dimension, margin);
			margin.bottom = Math.max(110, margin.bottom);
			margin.left = Math.max(40, margin.left);
		}

		Initialize(data: DataPoint[], yTitle = "") {
			//var parseDate = d3.time.format("%d-%b-%y").parse;

			var x = d3.time.scale().range([0, this.width()]);
			var y = d3.scale.linear().range([this.height(), 0]);

			var xAxis = d3.svg.axis().scale(x).orient("bottom").tickFormat(d=> this.LabelFunction(d));
			var yAxis = d3.svg.axis().scale(y).ticks(10).tickValues([1,2,3,4,5,6,7,8,9,10]).orient("left");
			var line = d3.svg.line().x(d=> x(d.x)).y(d=> y(d.y));
		
			var svg = d3.select(this._selector).append("svg")
				.classed("charts line-chart", true)
				.attr("viewBox", "0 0 " + this._dimension.width + " " + this._dimension.height)
				.attr("width", "100%")
				.attr("height", "100%")
				.append("g")
				.attr("transform", "translate(" + this._margin.left + "," + this._margin.top + ")");


			x.domain(d3.extent(data, d=> d.x));
			y.domain([1,10]/* d3.extent(data, d=> d.y)*/);

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
				.attr("class", d=>"dot "+d.classed)
				.attr("cx", line.x())
				.attr("cy", line.y())
				.attr("r", 3.5);
		
			this.SetInitialized();
		return this;
		}
	
	}

	export class Histogram extends Base {

		values: DataPoint[];
		bins : number[];

		private initialized = false;

		constructor(selector: string, dimension: Dimension = new Dimension(960, 500), margin = new Margin()) {
			super(selector, dimension, margin);
			margin.bottom = Math.max(110, margin.bottom);
			margin.top = Math.max(25, margin.top);
			this.bins = [];
		}

		private _CreateSvg() {
			return d3.select(this._selector).append("svg")
				.classed("charts histogram", true)
				.attr("viewBox", "0 0 " + this._dimension.width + " " + this._dimension.height)
				.attr("width", "100%")
				.attr("height", "100%")
				.append("g")
				.attr("transform", "translate(" + this._margin.left + "," + this._margin.top + ")");
		}

		private _Initialize(svg, data: DataPoint[], x, y, xAxis, classed) {
		
			var formatCount = d3.format(",.0f");
			var bar = svg.selectAll(".bar ." + classed)
				.data(data)
				.enter().append("g")
				.attr("class", s=> "bar " + classed)
				.attr("transform", d=> "translate(" + x(d.x) + "," + y(d.y) + ")");

			bar.append("rect")
				.attr("x", 1)
				.attr("width", x(data[1].x) - x(data[0].x))
				.attr("height", d=> this.height() - y(d.y));

			bar.append("text")
				.classed("value",true)
				.attr("dy", ".75em")
				.attr("y", -21)
				.attr("x",(x(data[1].x) - x(data[0].x)) / 2)
				.attr("text-anchor", "middle")
				.text(d=> {
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
		}

		InitializeWidth(values2: DataPoint[], binWidth: number = 10) {
			var min = Number.MAX_VALUE;
			var max = Number.MIN_VALUE;
			//var values: any[]=[];
			var groups = {};
			for (var i = 0; i < values2.length; i++) {
				max = Math.max(max, values2[i].x);
				min = Math.min(min, values2[i].x);
				groups[values2[i].group] = true;
				//values.push({xx:values2[i].x});
			}

			min = Math.floor(min / binWidth) * binWidth;
			max = Math.ceil(max / binWidth) * binWidth;

			var count = Math.ceil((max - min) / binWidth);

			var binsHalf: number[] = [];
			var constructBins = !this.bins || this.bins.length == 0;

			var even = true;
			var low: number = Number.MAX_VALUE;
			var high: number = Number.MIN_VALUE;
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
			} else {
				
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
				var values: any[] = [];
				for (var i = 0; i < values2.length; i++) {
					if (values2[i].group + "" == j) {
						values.push(values2[i].x);
					}
				}
				var data = d3.layout.histogram().bins(this.bins)(values);
				maxFreq = Math.max(maxFreq, d3.max(data, d=> d.y));
			}
			var first = true;
			for (var j in groups) {
				var values: any[] = [];
				for (var i = 0; i < values2.length; i++) {
					if (values2[i].group+"" == j) {
						values.push(values2[i].x);
					}
				}
				var data = d3.layout.histogram().bins(this.bins)(values);
				var x = d3.scale.linear().domain([low, high]).range([0, this.width()]);
				var y = d3.scale.linear().domain([0, maxFreq]).range([this.height(), 0]);
				var xAxis = d3.svg.axis().scale(x).orient("bottom").ticks(0).tickFormat(d=> this.LabelFunction(d));
				if (first)
					xAxis = d3.svg.axis().scale(x).orient("bottom").tickValues(useBins).tickFormat(d=> this.LabelFunction(d));
				first = false;
				this._Initialize(svg, data, x, y, xAxis, j);
			}
			this.SetInitialized();
		}
	
		/*
		InitializeCount(values: DataPoint[], binCount: number = 20) {
			this.values = values;
			this.initialized = true;
			var max = Math.max.apply(Math, values);
			var min = Math.min.apply(Math, values);

			var x = d3.scale.linear().domain([min, max]).range([0, this.width()]);
			var data = d3.layout.histogram().bins(x.ticks(binCount))(values);
			var y = d3.scale.linear().domain([0, d3.max(data, d => d.y)]).range([this.height(), 0]);

			var xAxis = d3.svg.axis().scale(x).orient("bottom");
			return this._Initialize(data, x, y, xAxis);
			//d3.layout.histogram().bins();
		}*/
	}
}