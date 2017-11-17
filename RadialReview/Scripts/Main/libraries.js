
//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);

//tabbable.js
//https://github.com/marklagendijk/jquery.tabbable/blob/master/jquery.tabbable.min.js
!function (e) { "use strict"; function t(t) { var n = e(t), a = e(":focus"), r = 0; if (1 === a.length) { var i = n.index(a); i + 1 < n.length && (r = i + 1) } n.eq(r).focus() } function n(t) { var n = e(t), a = e(":focus"), r = n.length - 1; if (1 === a.length) { var i = n.index(a); i > 0 && (r = i - 1) } n.eq(r).focus() } function a(t) { function n(t) { return e.expr.filters.visible(t) && !e(t).parents().addBack().filter(function () { return "hidden" === e.css(this, "visibility") }).length } var a, r, i, u = t.nodeName.toLowerCase(), o = !isNaN(e.attr(t, "tabindex")); return "area" === u ? (a = t.parentNode, r = a.name, t.href && r && "map" === a.nodeName.toLowerCase() ? (i = e("img[usemap=#" + r + "]")[0], !!i && n(i)) : !1) : (/input|select|textarea|button|object/.test(u) ? !t.disabled : "a" === u ? t.href || o : o) && n(t) } e.focusNext = function () { t(":focusable") }, e.focusPrev = function () { n(":focusable") }, e.tabNext = function () { t(":tabbable") }, e.tabPrev = function () { n(":tabbable") }, e.extend(e.expr[":"], { data: e.expr.createPseudo ? e.expr.createPseudo(function (t) { return function (n) { return !!e.data(n, t) } }) : function (t, n, a) { return !!e.data(t, a[3]) }, focusable: function (t) { return a(t, !isNaN(e.attr(t, "tabindex"))) }, tabbable: function (t) { var n = e.attr(t, "tabindex"), r = isNaN(n); return (r || n >= 0) && a(t, !r) } }) }(jQuery);

//jquery.autoResize.js
(function (n) { n.fn.autoResize = function (t) { var i = n.extend({ onResize: function () { }, animate: !1, animateDuration: 150, animateCallback: function () { }, extraSpace: 0, limit: 1e3, useOriginalHeight: !1 }, t), u, r; return this.destroyList = [], u = this, r = null, this.filter("textarea").each(function () { var t = n(this).css({ resize: "none", "overflow-y": "hidden" }), c = i.useOriginalHeight ? t.height() : 0, e = function () { var f = {}, i; return n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), i = t.clone().removeAttr("id").removeAttr("name").css({ position: "absolute", top: 0, left: -9999 }).css(f).attr("tabIndex", "-1").insertBefore(t), r != null && n(r).remove(), r = i, u.destroyList.push(i), i }(), o = null, f = function () { var f = {}, r, u; if (n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), e.css(f), e.height(0).val(n(this).val()).scrollTop(1e4), r = Math.max(e.scrollTop(), c) + i.extraSpace, u = n(this).add(e), o !== r) { if (o = r, r >= i.limit) { n(this).css("overflow-y", ""); return } i.onResize.call(this); r = Math.max(20, r); i.animate && t.css("display") === "block" ? u.stop().animate({ height: r }, i.animateDuration, i.animateCallback) : u.height(r) } }, s, h; t.unbind(".dynSiz").bind("keyup.dynSiz", f).bind("keydown.dynSiz", f).bind("change.dynSiz", f); s = function () { f.call(t) }; n(window).bind("resize.dynSiz", function () { clearTimeout(h); h = setTimeout(s, 100) }); setTimeout(function () { f.call(t) }, 1) }), this.destroy = function () { for (var t = 0; t < this.destroyList.length; t++) n(this.destroyList[t]).remove(); this.destroyList = [] }, this } })(jQuery);

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
