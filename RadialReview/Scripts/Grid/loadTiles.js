var Grid = {
    currentSize: 7,
    container: null,
    callback: null,
    first: true,
    ratio: 1.23,
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
        classes += "width_" + w;

        var $item = $(
				'<li class="grid-tile ' + classes + '">' +
					'<div class="inner">' +
						'<div class="settings-container">' +
							'<span class="glyphicon glyphicon-fullscreen icon settings resize-control"></span>' +
							'<span class="glyphicon glyphicon-trash icon settings trash" onclick="Grid.removeTile(' + id + ')"></span>' +
							'<div class="controls" style="width:' + (Grid.currentSize * 10 + 6) + 'px;">' +
								resizers +
							'</div>' +
						'</div>' +
						'<div class="content" id="' + id + '_tile" ></div>' +
                        '<div class="resize-bottom resizer resize-vertical"></div>' +
                        '<div class="resize-right resizer resize-horizontal"></div>' +
                        '<div class="resize-bottom-right resizer resize-nwse"></div>' +
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
                try {
                    var dom = $item.find(".content");
                    var ctrl = $("[ng-controller=L10Controller]");
                    var scope = angular.element(ctrl).scope();
                    scope.functions.setHtml(dom, data);

                    var style = $(dom).find(".heading").data("style");
                    $item.addClass("loaded");
                    //console.log("style:" + style);
                    //debugger;
                    //$(dom).closest(".grid-tile").find(".inner").attr("style", style);

                    setTimeout(function () {
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

        function runResize(item, w, h) {
            var li = $(item).closest("li")
            for (var i = 1; i <= Grid.currentSize; i++) {
                li.removeClass("width_" + i);
            }

            li.addClass("width_" + w);

            li.find(".selected").removeClass("selected");
            li.find(".resize").filter(function () {
                return li.attr("data-h") <= h && li.attr("data-w") <= w;
            }).addClass("selected");

            $gridContainer.gridList('resizeItem', li, {
                w: w,
                h: h
            });
        }


        $gridContainer.on("click", 'li .resize', function (e) {
            e.preventDefault();
            var itemElement = $(e.currentTarget).closest('li'),
				itemWidth = $(e.currentTarget).data('w'),
				itemHeight = $(e.currentTarget).data('h');

            runResize(itemElement, itemWidth, itemHeight);

        });

        if (Grid.first) {
            $(window).resize(function () {
                Grid.container.gridList('reflow');
            });
            var resizeing = false;
            var resizeDir = "vertical";
            var resizeSide = "bottom";
            var resizeCell = null;

            var resizerFunc = function (self,dir, side, unlocks) {
                if (resizeing == false) {
                    $("body").addClass("resizing");
                    $("body").addClass("resize-"+dir);
                    resizeing = true;
                    resizeDir = dir;
                    resizeSide = side;
                    resizeCell = $(self).closest(".grid-tile");
                    var originalH = $(resizeCell).data("h");
                    var originalW = $(resizeCell).data("w");
                    var height = $(resizeCell).height();
                    var width = $(resizeCell).width();
                    var originalX = null;
                    var originalY = null;
                    $(window).on("mousemove.gridresize", function (e) {
                        if (originalX == null)originalX = e.clientX;
                        if (originalY == null)originalY = e.clientY;
                        var diffX = (e.clientX - originalX);
                        var diffY = (e.clientY - originalY);
                        var curH = $(resizeCell).data("h");
                        var curW = $(resizeCell).data("w");
                        var newH = curH;
                        var newW = curW;
                        if (unlocks.indexOf("vertical") != -1) {
                            var shift=-.2
                            if (diffY < 0)
                                shift = -.8;                            
                            var newH = Math.max(1, Math.ceil(originalH + diffY / (height / originalH) + shift));
                        }
                        if (unlocks.indexOf("horizontal") != -1) {
                            var shift=-.2
                            if (diffX < 0)
                                shift = -.8;
                            var newW = Math.max(1, Math.ceil(originalW + diffX / (width / originalW) +shift));
                        }
                        if (curH != newH || curW != newW) {
                            runResize(resizeCell, newW, newH);
                        }
                    });
                }
            }

            $("body").on("mousedown", ".resize-bottom", function () { resizerFunc(this, "vertical", "bottom", "vertical"); });
            $("body").on("mousedown", ".resize-top", function () { resizerFunc(this, "vertical", "top", "vertical"); });
            $("body").on("mousedown", ".resize-left", function () { resizerFunc(this, "horizontal", "left", "horizontal"); });
            $("body").on("mousedown", ".resize-right", function () { resizerFunc(this, "horizontal", "right", "horizontal"); });
            $("body").on("mousedown", ".resize-bottom-right", function () { resizerFunc(this, "nwse", "bottom-right", "vertical horizontal"); });

            $(window).on("mouseup", function () {
                resizeing = false;
                $(window).off(".gridresize");
                $("body").removeClass("resizing").removeClass("resize-vertical").removeClass("resize-horizontal").removeClass("resize-nwse");
                var originalX = null;
                var originalY = null;
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
    refreshGrid: function (first) {
        Grid.container.gridList({
            lanes: Grid.currentSize,
            direction: "vertical",
            minWidth: 200,
            minHeight: 200,
            widthHeightRatio: Grid.ratio,
            onChange: Grid.callback
        }, { handle: ".heading" });
        if (!first) {
            Grid.container.gridList('resize', Grid.currentSize);
        }
    },
    removeTile: function (id) {
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
