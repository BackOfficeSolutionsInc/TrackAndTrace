
//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);

//tabbable.js
//https://github.com/marklagendijk/jquery.tabbable/blob/master/jquery.tabbable.min.js
!function (e) { "use strict"; function t(t) { var n = e(t), a = e(":focus"), r = 0; if (1 === a.length) { var i = n.index(a); i + 1 < n.length && (r = i + 1) } n.eq(r).focus() } function n(t) { var n = e(t), a = e(":focus"), r = n.length - 1; if (1 === a.length) { var i = n.index(a); i > 0 && (r = i - 1) } n.eq(r).focus() } function a(t) { function n(t) { return e.expr.filters.visible(t) && !e(t).parents().addBack().filter(function () { return "hidden" === e.css(this, "visibility") }).length } var a, r, i, u = t.nodeName.toLowerCase(), o = !isNaN(e.attr(t, "tabindex")); return "area" === u ? (a = t.parentNode, r = a.name, t.href && r && "map" === a.nodeName.toLowerCase() ? (i = e("img[usemap=#" + r + "]")[0], !!i && n(i)) : !1) : (/input|select|textarea|button|object/.test(u) ? !t.disabled : "a" === u ? t.href || o : o) && n(t) } e.focusNext = function () { t(":focusable") }, e.focusPrev = function () { n(":focusable") }, e.tabNext = function () { t(":tabbable") }, e.tabPrev = function () { n(":tabbable") }, e.extend(e.expr[":"], { data: e.expr.createPseudo ? e.expr.createPseudo(function (t) { return function (n) { return !!e.data(n, t) } }) : function (t, n, a) { return !!e.data(t, a[3]) }, focusable: function (t) { return a(t, !isNaN(e.attr(t, "tabindex"))) }, tabbable: function (t) { var n = e.attr(t, "tabindex"), r = isNaN(n); return (r || n >= 0) && a(t, !r) } }) }(jQuery);

//jquery.autoResize.js
//https://github.com/alexbardas/jQuery.fn.autoResize/blob/master/jquery.autoresize.js
/*(function (n) {
	n.fn.autoResize = function (t) {
		var i = n.extend({ onResize: function () { }, animate: !1, animateDuration: 150, animateCallback: function () { }, extraSpace: 0, limit: 1e3, useOriginalHeight: !1 }, t), u, r; return this.destroyList = [], u = this, r = null, this.filter("textarea").each(function () {
			var t = n(this).css({ resize: "none", "overflow-y": "hidden" }), c = i.useOriginalHeight ? t.height() : 0, e = function () { var f = {}, i; 
			return n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), i = t.clone().removeAttr("id").removeAttr("name").css({ position: "absolute", top: 0, left: -9999 }).css(f).attr("tabIndex", "-1").insertBefore(t), r != null && n(r).remove(), r = i, u.destroyList.push(i), i }(), o = null, f = function () {
				var f = {}, r, u; if (n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), e.css(f), e.height(0).val(n(this).val()).scrollTop(1e4), r = Math.max(e.scrollTop(), c) + i.extraSpace, u = n(this).add(e), o !== r) {
					if (o = r, r >= i.limit) { n(this).css("overflow-y", ""); return } i.onResize.call(this);
					r = Math.max(16, r);; i.animate && t.css("display") === "block" ? u.stop().animate({ height: r }, i.animateDuration, i.animateCallback) : u.height(r)
				}
			}, s, h; t.unbind(".dynSiz").bind("keyup.dynSiz", f).bind("keydown.dynSiz", f).bind("change.dynSiz", f); s = function () { f.call(t) }; n(window).bind("resize.dynSiz", function () { clearTimeout(h); h = setTimeout(s, 100) }); setTimeout(function () { f.call(t) }, 1)
		}), this.destroy = function () { for (var t = 0; t < this.destroyList.length; t++) n(this.destroyList[t]).remove(); this.destroyList = [] }, this
	}
})(jQuery);*/
/*
 * jQuery.fn.autoResize 1.1
 * --
 * https://github.com/jamespadolsey/jQuery.fn.autoResize
 * --
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

(function ($) {
	var defaults = autoResize.defaults = {
		onResize: function () { },
		animate: {
			duration: 200,
			complete: function () { }
		},
		extraSpace: 0,
		minHeight: 'original',
		maxHeight: 500,
		minWidth: 'original',
		maxWidth: 500
	};
	autoResize.cloneCSSProperties = [
		'lineHeight', 'textDecoration', 'letterSpacing',
		'fontSize', 'fontFamily', 'fontStyle', 'fontWeight',
		'textTransform', 'textAlign', 'direction', 'wordSpacing', 'fontSizeAdjust',
		'width','padding'
	];
	autoResize.cloneCSSValues = {
		position: 'absolute',
		top: -9999,
		left: -9999,
		opacity: 0,
		overflow: 'hidden'
	};
	autoResize.resizableFilterSelector = 'textarea,input:not(input[type]),input[type=text],input[type=password]';
	autoResize.AutoResizer = AutoResizer;
	$.fn.autoResize = autoResize;
	function autoResize(config) {
		this.filter(autoResize.resizableFilterSelector).each(function () {
			new AutoResizer($(this), config);
		});
		return this;
	}
	function AutoResizer(el, config) {
		config = this.config = $.extend(autoResize.defaults, config);
		this.el = el;
		this.nodeName = el[0].nodeName.toLowerCase();
		this.originalHeight = el.height();
		this.previousScrollTop = null;
		this.value = el.val();
		if (config.maxWidth === 'original') config.maxWidth = el.width();
		if (config.minWidth === 'original') config.minWidth = el.width();
		if (config.maxHeight === 'original') config.maxHeight = el.height();
		if (config.minHeight === 'original') config.minHeight = el.height();
		if (this.nodeName === 'textarea') {
			el.css({
				resize: 'none',
				overflowY: 'hidden'
			});
		}
		el.data('AutoResizer', this);
		this.createClone();
		this.injectClone();
		this.bind();
	}
	AutoResizer.prototype = {
		bind: function () {
			var check = $.proxy(function () {
				this.check();
				return true;
			}, this);
			var focus = $.proxy(function () {
				this.focus();
				return true;
			}, this);
			var blur = $.proxy(function () {
				this.blur();
				return true;
			}, this);
			this.unbind();
			this.el.bind('keyup.autoResize', check)
				   //.bind('keydown.autoResize', check)
				   .bind('change.autoResize', check)
				   .bind('focus.autoResize', focus)
				   .bind('blur.autoResize', blur);
			var self = this;
			self.check(null, true);
		},
		focus: function () {
			var self = this;
			this.focused = true;
			try{
				var rescroll = function () {
					try {
						if (self.el[0].scrollTop != 0) {
							self.el[0].scrollTop = 0;
						}
						if (self.focused) {
							window.requestAnimationFrame(rescroll);
						}
					} catch (e) {
					}
				}
				window.requestAnimationFrame(rescroll);
			}catch(e){
			}
		},
		blur:function(){
			this.focused = false;
			//clearInterval(this.focusInterval);
		},
		unbind: function () {
			this.el.unbind('.autoResize');
		},
		createClone: function () {
			var el = this.el, clone;
			if (this.nodeName === 'textarea') {
				clone = el.clone().height('auto');
			} else {
				clone = $('<span/>').width('auto').css({whiteSpace: 'nowrap'});
			}
			this.clone = clone;
			$.each(autoResize.cloneCSSProperties, function (i, p) {
				clone[0].style[p] = el.css(p);
			});

			if (typeof (this.config.style) === "object") {
				var styles = this.config.style;
				$.each(styles, function (i, p) {
					clone[0].style[i] = p;
				});
			}
			clone.removeAttr('name').removeAttr('id').attr('tabIndex', -1).css(autoResize.cloneCSSValues);
			//$("body").append(clone);
		},
		check: function (e, immediate) {
			var self = this;
			var checkFunc = function () {
				var config = self.config, clone = self.clone, el = self.el, value = el.val();
				el.scrollTop = 0;
				if (self.nodeName === 'input') {
					clone.text(value);
					// Calculate new width + whether to change
					var cloneWidth = clone.width(),
						newWidth = (cloneWidth + config.extraSpace) >= config.minWidth ?
							cloneWidth + config.extraSpace : config.minWidth,
						currentWidth = el.width();
					newWidth = Math.min(newWidth, config.maxWidth);
					if ((newWidth < currentWidth && newWidth >= config.minWidth) || (newWidth >= config.minWidth && newWidth <= config.maxWidth)) {
						config.onResize.call(el);
						el.scrollLeft(0);
						if (config.animate && !immediate) {
							//debugger;
							el.stop(1, 1).animate({ width: newWidth }, config.animate);
						} else {
							el.width(newWidth);
						}
					}
					return;
				}
				// TEXTAREA
				clone.height(0).val(value).scrollTop(10000);
				clone.css({ 'width': $(el).outerWidth() });
				var scrollTop = clone[0].scrollTop + config.extraSpace;
				// Don't do anything if scrollTop hasen't changed:
				if (self.previousScrollTop === scrollTop) {
					return;
				}
				var forceImmediate = false;
				if (self.previousScrollTop <= scrollTop) {
					forceImmediate = true;
				}
				self.previousScrollTop = scrollTop;
				if (scrollTop >= config.maxHeight) {
					el.css('overflowY', '');
					return;
				}
				el.css('overflowY', 'hidden');
				if (scrollTop < config.minHeight) {
					scrollTop = config.minHeight;
				}
				config.onResize.call(el);
				// Either animate or directly apply height:

				if (config.animate && !immediate && !forceImmediate) {
					//debugger;
					//setTimeout(function () {
					//	debugger;
					//	el[0].scrollTop = 0;
					//}, 100);
					el[0].scrollTop = 0;
					el.stop(1, 1).animate({ height: scrollTop }, config.animate);
				} else {
					el.height(scrollTop);
				}
			};
			try {
				window.requestAnimationFrame(checkFunc);
			} catch (e) {
				checkFunc();
			}

		},
		destroy: function () {
			this.unbind();
			this.el.removeData('AutoResizer');
			this.clone.remove();
			delete this.el;
			delete this.clone;
		},
		injectClone: function () {
			(autoResize.cloneContainer || (autoResize.cloneContainer = $('<arclones/>').appendTo('body'))).append(this.clone);
		}
	};

})(jQuery);



// $().waitUntilExist
; (function ($, window) {

	var intervals = {};
	var removeListener = function (selector) {

		if (intervals[selector]) {

			window.clearInterval(intervals[selector]);
			intervals[selector] = null;
		}
	};
	var found = 'waitUntilExists.found';

	/**
     * @function
     * @property {object} jQuery plugin which runs handler function once specified
     *           element is inserted into the DOM
     * @param {function|string} handler 
     *            A function to execute at the time when the element is inserted or 
     *            string "remove" to remove the listener from the given selector
     * @param {bool} shouldRunHandlerOnce 
     *            Optional: if true, handler is unbound after its first invocation
     * @example jQuery(selector).waitUntilExists(function);
     */

	$.fn.waitUntilExists = function (handler, shouldRunHandlerOnce, isChild) {

		var selector = this.selector;
		var $this = $(selector);
		var $elements = $this.not(function () { return $(this).data(found); });

		if (handler === 'remove') {

			// Hijack and remove interval immediately if the code requests
			removeListener(selector);
		}
		else {

			// Run the handler on all found elements and mark as found
			$elements.each(handler).data(found, true);

			if (shouldRunHandlerOnce && $this.length) {

				// Element was found, implying the handler already ran for all 
				// matched elements
				removeListener(selector);
			}
			else if (!isChild) {

				// If this is a recurring search or if the target has not yet been 
				// found, create an interval to continue searching for the target
				intervals[selector] = window.setInterval(function () {

					$this.waitUntilExists(handler, shouldRunHandlerOnce, true);
				}, 500);
			}
		}

		return $this;
	};

}(jQuery, window));



////Fix Submenus
//$('ul.dropdown-menu [data-toggle=dropdown]').on('click', function (event) {
//	event.preventDefault();
//	event.stopPropagation();
//	$('ul.dropdown-menu [data-toggle=dropdown]').parent().removeClass('open');
//	$(this).parent().addClass('open');
//});
