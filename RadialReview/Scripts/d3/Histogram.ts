module Charts {
	export class Margin {
		top: number;
		bottom: number;
		left: number;
		right: number;
		constructor(margins: number=30) {
			this.top = margins;
			this.bottom = margins;
			this.left = margins;
			this.right = margins;
		}
	}

	export class Dimension {
		constructor(public width: number, public height: number) {}
	}

	export class Base {
		constructor(public _id:string, private _dimension: Dimension =  new Dimension(960,500),private _margin = new Margin()){ }

		width(): number {
			return this._dimension.width - this._margin.left - this._margin.right;
		}
		height(): number {
			return this._dimension.height - this._margin.top - this._margin.bottom;
		}
	}

	export class HistogramBin {

	};

	export class Histogram extends Base {



		bins: HistogramBin[];
		data: number[];
		private initialized=false;

		constructor(id:string,dimension: Dimension = new Dimension(960, 500), margin = new Margin()) {
			super(id,dimension, margin);
		}

		Initialize(values:number[], binCount: number=20) {
			this.initialized = true;
			var max = Math.max.apply(Math, values);
			var min = Math.min.apply(Math, values);

			var x = d3.scale.linear().domain([min, max]).range([0, this.width()]);
			var data = d3.layout.histogram().bins(x.ticks(binCount))(values);
			var y = d3.scale.linear().domain([0, d3.max(data, d => d.y)]).range([this.height(), 0]);

			var xAxis = d3.svg.axis().scale(x).orient("bottom");

			var svg = d3.select(this._id).append("svg")
				.attr("width", width + margin.left + margin.right)
				.attr("height", height + margin.top + margin.bottom)
				.append("g")
				.attr("transform", "translate(" + margin.left + "," + margin.top + ")");

			var bar = svg.selectAll(".bar")
				.data(data)
				.enter().append("g")
				.attr("class", "bar")
				.attr("transform", function (d) { return "translate(" + x(d.x) + "," + y(d.y) + ")"; });

			bar.append("rect")
				.attr("x", 1)
				.attr("width", x(data[0].dx) - 1)
				.attr("height", function (d) { return height - y(d.y); });

			bar.append("text")
				.attr("dy", ".75em")
				.attr("y", 6)
				.attr("x", x(data[0].dx) / 2)
				.attr("text-anchor", "middle")
				.text(function (d) { return formatCount(d.y); });

			svg.append("g")
				.attr("class", "x axis")
				.attr("transform", "translate(0," + height + ")")
				.call(xAxis);


			d3.layout.histogram().bins();


		}
		
		SetData(data: number[]) {
			this.data = data;
		}

	}
}
