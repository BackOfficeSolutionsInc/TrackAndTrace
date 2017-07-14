/*
 * 
	Usage: DiagonalTable("table", -Math.PI / 4, 100);
 * 
 */
	/**
* Splits new lines of text into separate divs
*
* ### Options:
* - `width` string The width of the box. By default, it tries to use the
*	 element's width. If you don't define a width, there's no way to split it
*	 by lines!
*	- `tag` string The tag to wrap the lines in
*	- `keepHtml` boolean Whether or not to try and preserve the html within
*	 the element. Default is true
*
*	@@param options object The options object
*	@@license MIT License (http://www.opensource.org/licenses/mit-license.php)
*/
	(function ($) {

		/**
		 * Creates a temporary clone
		 *
		 * @@param element element The element to clone
		 */
		function _createTemp(element) {
			return element.clone().css({ position: 'absolute' });
		};

		/**
		 * Splits contents into words, keeping their original Html tag. Note that this
		 * tags *each* word with the tag it was found in, so when the wrapping begins
		 * the tags stay intact. This may have an effect on your styles (say, if you have
		 * margin, each word will inherit those styles).
		 *
		 * @@param node contents The contents
		 */
		function _splitHtmlWords(contents) {
			var words = [];
			var splitContent;
			for (var c = 0; c < contents.length; c++) {
				if (contents[c].nodeType == 3) {
					splitContent = _splitWords(contents[c].textContent || contents[c].toString());
				} else {
					var tag = $(contents[c]).clone();
					splitContent = _splitHtmlWords(tag.contents());
					for (var t = 0; t < splitContent.length; t++) {
						tag.empty();
						splitContent[t] = tag.html(splitContent[t]).wrap('<p></p>').parent().html();
					}
				}
				for (var w = 0; w < splitContent.length; w++) {
					words.push(splitContent[w]);
				}
			}
			return words;
		};

		/**
		 * Splits words by spaces
		 *
		 * @@param string text The text to split
		 */
		function _splitWords(text) {
			return text.split(/\s+/);
		}

		/**
		 * Formats html with tags and wrappers.
		 *
		 * @@param tag
		 * @@param html content wrapped by the tag
		 */
		function _markupContent(tag, html) {
			// wrap in a temp div so .html() gives us the tags we specify
			tag = '<div>' + tag;
			// find the deepest child, add html, then find the parent
			return $(tag)
				.find('*:not(:has("*"))')
				.html(html)
				.parentsUntil()
				.slice(-1)
				.html();
		}

		/**
		 * The jQuery plugin function. See the top of this file for information on the
		 * options
		 */
		$.fn.splitLines = function (options) {
			var settings = {
				width: 'auto',
				tag: '<div>',
				wrap: '',
				keepHtml: true
			};
			if (options) {
				$.extend(settings, options);
			}
			var newHtml = _createTemp(this);
			var contents = this.contents();
			var text = this.text();
			this.append(newHtml);
			newHtml.text('42');
			var maxHeight = newHtml.height() + 2;
			newHtml.empty();

			var tempLine = _createTemp(newHtml);
			if (settings.width !== 'auto') {
				tempLine.width(settings.width);
			}
			this.append(tempLine);
			var words = settings.keepHtml ? _splitHtmlWords(contents) : _splitWords(text);
			var prev;
			for (var w = 0; w < words.length; w++) {
				var html = tempLine.html();
				tempLine.html(html + words[w] + ' ');
				if (tempLine.html() == prev) {
					// repeating word, it will never fit so just use it instead of failing
					prev = '';
					newHtml.append(_markupContent(settings.tag, tempLine.html()));
					tempLine.html('');
					continue;
				}
				if (tempLine.height() > maxHeight) {
					prev = tempLine.html();
					tempLine.html(html);
					newHtml.append(_markupContent(settings.tag, tempLine.html()));
					tempLine.html('');
					w--;
				}
			}
			newHtml.append(_markupContent(settings.tag, tempLine.html()));

			this.html(newHtml.html());

		};
	})(jQuery);

	function DiagonalTable(tableSelector, angleRad, maxHeight,extraPad) {
		var angle = angleRad;
		var rotateStyle = "-ms-transform: rotate(" + angle + "rad);-webkit-transform: rotate(" + angle + "rad);transform: rotate(" + angle + "rad);";
		var skewStyle = "-ms-transform: skewX(" + angle + "rad);-webkit-transform: skewX(" + angle + "rad);transform: skewX(" + angle + "rad);transform-origin: 0% 100%;-webkit-transform-origin: 0% 100%;-ms-transform-origin: 0% 100%;";
		$(tableSelector).addClass("diagonal-table");
		var head = $(tableSelector).find("thead");
		if (head.length == 0) {
			console.error("Missing thead");
		} 
		

		head.find("td,th").css("white-space", "nowrap");

		var transform = function (item, maxTextWidth, A) {
			//var parent = $(item).parent();
			item.splitLines({ width: maxTextWidth, tag: "<span class='diagonal-item-row' style='" + rotateStyle + "'>" });
			item.append($("<div class='diagonal-width-marker'></div>"));
			item.append($("<div class='diagonal-bar-top diagonal-bar' style='" + rotateStyle + "'></div>"));
			item.append($("<div class='diagonal-bar-cap'></div>"));
			item.prepend($("<div class='diagonal-fill' style='" + skewStyle + "'></div>"));
			$(item).wrapInner("<div class='diagonal-item-container'></div>");
		};

		//var beta = Math.PI / 2.0 - angle;
		//console.log("angle", angle);
		var alpha = -angle;
		var beta = Math.PI / 2.0 - alpha;

		//alpha = alpha - Math.floor(alpha / (Math.PI / 2.0)) * (Math.PI / 2.0);
		console.log("alpha:", alpha);
		console.log("beta:", beta);

		var H = maxHeight / Math.cos(beta);


		var later = [];
		var maxA = 0;
		var maxB = 0;
		$(tableSelector).find("thead th,thead td").each(function () {
			var header = $(this);
			var lineWidth = header.width() ;
			var lineHeight = header.height();
			console.log([lineWidth, lineHeight]);

			B = lineHeight * Math.tan(beta);
			maxB = Math.max(maxB, B);
			var maxWidth = H - B;
			var A = lineHeight / Math.cos(beta);
			maxA = Math.max(maxA, A);
			console.log(maxWidth);
			later.push(function () {
				transform(header, maxWidth, A);
			});
		});
		head.find("td,th").css("white-space", "normal");
		for (var i = 0; i < later.length; i++) {
			later[i]();
		}

		var calcMaxW = 0;
		$(tableSelector).find(".diagonal-item-row").each(function () {
			calcMaxW = Math.max(calcMaxW, $(this).width());
		})

		var calcHyp = maxB + calcMaxW + extraPad;
		var calcHeight = Math.sin(alpha) * calcHyp;
		$(tableSelector).find(".diagonal-width-marker").css("height", Math.abs(calcHeight) );
		$(tableSelector).find(".diagonal-fill").css("height", Math.abs(calcHeight)-1 );
		var containers = $(tableSelector).find(".diagonal-item-container");
		containers.each(function (i) {
			var n = $(this).find(".diagonal-item-row").length;
			$(this).find(".diagonal-width-marker").css("width", maxA * n);
			var cellW = $(this).width();
			var additionalLeft = 0
			if (cellW > maxA * n) {
				additionalLeft = (cellW - maxA * n) / 2;
			}

			if (i == containers.length - 1) {
				var lastBar = $("<div class='diagonal-bar-bottom diagonal-bar' style='" + rotateStyle + "'></div>");
				lastBar.css("left", cellW +1);
				$(this).append(lastBar);
			}
			$(this).find(".diagonal-bar").each(function () {
				$(this).css("width", calcHyp);
			});
			$(this).find(".diagonal-bar-cap").each(function () {
				var x = calcHeight / Math.tan(beta);
				$(this).css("left", x);
			});


			$(this).find(".diagonal-item-row").each(function (i) {
				//debugger;
				$(this).css("left", maxA * (i + 1) + additionalLeft);
				//$(this).css("margin-left", -maxB);
				//$(this).css("padding-left", maxB);
				//$(this).css("width", calcHyp);
				//var D = maxB * Math.sin(alpha);
				//$(this).css("bottom", -D);


				if ($(this).text() == "") {
					$(this).html("&nbsp;");
				}
			});
		});


		//if (calcHeight > maxHeight) {
		//	DiagonalTable(tableSelector, angleRad, calcHeight)
		//}

		//splitLines = function (jq, maxLineWidth) {
		//	if (jq < maxLineWidth) {
		//		return jq;
		//	} else {
		//		var parts = jq.Split(' ');

		//	}
		//};


	}
