var Grid = {
	currentSize: 7,
	container: null,
	callback: null,
	first: true,
	ratio:1.23,
	createTile: function (item) {
		var x, y, w, h, id, url;
		if (typeof (item.X) !== "undefined") {
			x = item.X;
			y = item.Y;
			w = item.Width;
			h = item.Height;
			id = item.Id;
			url = item.DataUrl;
		} else {
			x = item.x;
			y = item.y;
			w = item.w;
			h = item.h;
			id = item.id;
			url = item.dataUrl;
		}
		var resizers = "";

		for (var i = 1; i <= Grid.currentSize; i++) {
			resizers += "<div class='resize-row'>";
			for (var j = Grid.currentSize; j > 0; j--) {
				var selected = (w >= j && h >= i) ? " selected" : "";
				resizers += '<span class="resize' + selected + '" data-w="' + j + '" data-h="' + i + '"></span>';
			}
			resizers += "</div>";
		}

		var classes = "";

		if (w == 1)
			classes += "width_1";
		var $item = $(
				'<li class="' + classes + '">' +
					'<div class="inner">' +
						'<div class="settings-container">' +
							'<span class="glyphicon glyphicon-fullscreen icon settings resize-control"></span>' +
							'<span class="glyphicon glyphicon-trash icon settings trash" onclick="Grid.removeTile(' + id + ')"></span>' +
							'<div class="controls" style="width:'+(Grid.currentSize*10+6)+'px;">' +
								resizers +
							'</div>' +
						'</div>' +
						//['+id+']
						'<div class="content" id="' + id + '_tile" ></div>' +
					'</div>' +
				'</li>'
			);
		$item.attr({
			'data-w': w,
			'data-h': h,
			'data-x': x,
			'data-y': y,
			'data-id': id
		});
		$item.find(".content").addClass("transparent");
		console.log("requesting tile");
		$.ajax({
			url: url,
			success: function (data) {
				console.log("received tile");
				console.log(data);
				try {
					var dom = $item.find(".content");
					var ctrl = $("[ng-controller=L10Controller]");
					//dom.html(data);
					var scope = angular.element(ctrl).scope();
					scope.functions.setHtml(dom, data);
					/*if (typeof(scope.ajaxData) === "undefined")
					scope.ajaxData = {};
				scope.ajaxData=data;*/
					//angular.bootstrap(dom.find(".app"), ['L10App']);
					setTimeout(function() {
						dom.removeClass("transparent");
						console.log("appearing tile");
					}, 500);
				} catch (e) {
					console.log("Error with tile");
					console.error(e);
				}
			},
			error: function (data) {
				var dom = $item.find(".content");
				dom.addClass("error");
				dom.html("<div class='gray error-message'>An error has occurred loading this tile.</div>");
				setTimeout(function () {
					dom.removeClass("transparent");
					console.log("appearing tile (error)");
				}, 500);
			}
		});

		return $item;
	},
	buildElements: function ($gridContainer, items, callback) {

		if (typeof (callback) === "function")
			Grid.callback = callback;



		Grid.container = $gridContainer;
		var i;
		for (i = 0; i < items.length; i++) {
			var $item = Grid.createTile(items[i]);
			$gridContainer.append($item);
		}
		Grid.refreshGrid(true);
		/*
		$($gridContainer).gridList({
			lanes: Grid.currentSize,
			direction: "vertical",
			minWidth: 200,
			minHeight: 200,
			widthHeightRatio: Grid.ratio, //264 / 294,
			//heightToFontSizeRatio: .1,
			onChange: function (changedItems) {
				Grid.callback(changedItems);
				//Grid.flashItems(changedItems);
				console.log(changedItems);
			}
		});*/
		$gridContainer.on("click",'li .resize',function (e) {
			e.preventDefault();
			var itemElement = $(e.currentTarget).closest('li'),
				itemWidth = $(e.currentTarget).data('w'),
				itemHeight = $(e.currentTarget).data('h');

			itemElement.removeClass("width_1");
			if (itemWidth == 1)
				itemElement.addClass("width_1");

			$(this).closest("li").find(".selected").removeClass("selected");
			$(this).closest("li").find(".resize").filter(function () {
				return $(this).attr("data-h") <= itemHeight && $(this).attr("data-w") <= itemWidth;
			}).addClass("selected");

			$gridContainer.gridList('resizeItem', itemElement, {
				w: itemWidth,
				h: itemHeight
			});
		});

		if (Grid.first) {
			$(window).resize(function () {
				Grid.container.gridList('reflow');
			});

			$("body").on("mouseover", ".resize", function () {
				var hh = $(this).attr("data-h");
				var ww = $(this).attr("data-w");
				$(".resize").filter(function () {
					return $(this).attr("data-h") <= hh && $(this).attr("data-w") <= ww;
				}).addClass("highlight");
			});
			$("body").on("mouseleave", ".resize", function () {
				$(".resize").removeClass("highlight");
			});
			Grid.first = false;
		}
	},
	resize: function (size) {
		if (size) {
			this.currentSize = size;
		}
		Grid.container.gridList('resize', this.currentSize);
	},
	refreshGrid:function(first) {
		Grid.container.gridList({
			lanes: Grid.currentSize,
			direction: "vertical",
			minWidth: 200,
			minHeight: 200,
			widthHeightRatio: Grid.ratio,
			onChange: Grid.callback
		},{handle:".heading"});
		if (!first) {
			Grid.container.gridList('resize', Grid.currentSize);
		}
	},
	removeTile: function (id) {
		//var instance = Grid.container.data('_gridList');
		//var items = [];

		//for (var i = 0; i < instance.items.length; i++) {
		//	if (instance.items[i].id == id)
		//		continue;
		//	items.push(instance.items[i]);
		//}
		//Grid.container.html("");
		//Grid.buildElements(Grid.container, items, Grid.callback);
		//Grid.container.data('_gridList').gridList.resizeGrid(Grid.currentSize);

		$.ajax({
			url: "/Dashboard/Tile/" + id + "?hidden=True",
			success: function (data) {
				if (showJsonAlert(data, false, true)) {
					$("[data-id='" + id + "']").remove();
					Grid.refreshGrid(false);
				}

			}
		});

	},
	addTile: function (tile) {
		var t = Grid.createTile(tile);
		Grid.container.append(t);
		Grid.refreshGrid(false);
		
		//Grid.container.gridList('reflow');
		//Grid.container.data('_gridList').gridList.resizeGrid(Grid.currentSize);


		/*
		var instance = Grid.container.data('_gridList');
		var items = instance.items.slice();
		items.push(tile);
		Grid.container.html("");
		//Grid.buildElements(Grid.container, items, Grid.callback);
		var item = Grid.container.data('_gridList').grid[0][0];
		Grid.container.data('_gridList').gridList.moveItemToPosition(item, [0, 0]);*/
		//Grid.container.gridList('moveItemToPosition', t);
	},
	flashItems: function (items) {
		// Hack to flash changed items visually
		/*for (var i = 0; i < items.length; i++) {
			(function ($element) {
				$element.addClass('changed');
				setTimeout(function () {
					$element.removeClass('changed');
				}, 0);
			})(items[i].$element);
		}*/
	}
};
