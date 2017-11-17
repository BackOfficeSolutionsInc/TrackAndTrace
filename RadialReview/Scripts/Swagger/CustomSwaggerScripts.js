﻿function CopyClipboard(element) {
	// creating new textarea element and giveing it id 't'
	let t = document.createElement('textarea')
	t.id = 't'
	// Optional step to make less noise in the page, if any!
	t.style.height = 0
	// You have to append it to your page somewhere, I chose <body>
	document.body.appendChild(t)
	// Copy whatever is in your div to our new textarea
	t.value = $(element)[0].innerText
	// Now copy whatever inside the textarea to clipboard
	let selector = document.querySelector('#t')
	selector.select()
	document.execCommand('copy')
	// Remove the textarea
	document.body.removeChild(t)
}

function addCopyButton(self) {
	if (self.find('.copy-button').length == 0 && self.closest('.copy-button').length == 0) {
		var copy = $("<span class='copy-button'></span>");
		copy.on("click", function () {
			debugger;
			$(this).closest('.copy-added').addClass("decollapse");
			CopyClipboard(self);
			$(this).closest('.copy-added').removeClass("decollapse");
		});

		self.prepend(copy);
		self.addClass("copy-added");
	}
}

setInterval(function () {
	$("div.block").each(function () {
		var self = $(this);

		addCopyButton(self);
	});

	$(".snippet_json code:not(.json-fixed), pre.json code:not(.json-fixed)").each(function () {
		var self = $(this);
		self.addClass('json-fixed');
		var text = $(this).text();
		$(this).html("");
		$(this).jsonView(text);
		
		var self = $(this);
		addCopyButton(self);
	});

}, 500);

(function () {
	var link = document.querySelector("link[rel*='icon']") || document.createElement('link');
	link.type = 'image/x-icon';
	link.rel = 'shortcut icon';
	link.href = '/content/favicon.ico?v=3';
	document.getElementsByTagName('head')[0].appendChild(link);
})();

$(".resource > .heading a").each(function () {
	var s = $(this).text();
	var n = s.indexOf('_');
	s = s.substring(0, n != -1 ? n : s.length);

	$(this).text(s);
});


/**
 * jquery.json-view - jQuery collapsible JSON plugin
 * @version v1.0.0
 * @link http://github.com/bazh/jquery.json-view
 * @license MIT
 */
!function (e) { "use strict"; var n = function (n) { var a = e("<span />", { "class": "collapser", on: { click: function () { var n = e(this); n.toggleClass("collapsed"); var a = n.parent().children(".block"), p = a.children("ul"); n.hasClass("collapsed") ? (p.hide(), a.children(".dots, .comments").show()) : (p.show(), a.children(".dots, .comments").hide()) } } }); return n && a.addClass("collapsed"), a }, a = function (a, p) { var t = e.extend({}, { nl2br: !0 }, p), r = function (e) { return e.toString() ? e.toString().replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;").replace(/>/g, "&gt;") : "" }, s = function (n, a) { return e("<span />", { "class": a, html: r(n) }) }, l = function (a, p) { switch (e.type(a)) { case "object": p || (p = 0); var c = e("<span />", { "class": "block" }), d = Object.keys(a).length; if (!d) return c.append(s("{", "b")).append(" ").append(s("}", "b")); c.append(s("{", "b")); var i = e("<ul />", { "class": "obj collapsible level" + p }); return e.each(a, function (a, t) { d--; var r = e("<li />").append(s('"', "q")).append(a).append(s('"', "q")).append(": ").append(l(t, p + 1)); -1 === ["object", "array"].indexOf(e.type(t)) || e.isEmptyObject(t) || r.prepend(n()), d > 0 && r.append(","), i.append(r) }), c.append(i), c.append(s("...", "dots")), c.append(s("}", "b")), c.append(1 === Object.keys(a).length ? s("// 1 item", "comments") : s("// " + Object.keys(a).length + " items", "comments")), c; case "array": p || (p = 0); var d = a.length, c = e("<span />", { "class": "block" }); if (!d) return c.append(s("[", "b")).append(" ").append(s("]", "b")); c.append(s("[", "b")); var i = e("<ul />", { "class": "obj collapsible level" + p }); return e.each(a, function (a, t) { d--; var r = e("<li />").append(l(t, p + 1)); -1 === ["object", "array"].indexOf(e.type(t)) || e.isEmptyObject(t) || r.prepend(n()), d > 0 && r.append(","), i.append(r) }), c.append(i), c.append(s("...", "dots")), c.append(s("]", "b")), c.append(1 === a.length ? s("// 1 item", "comments") : s("// " + a.length + " items", "comments")), c; case "string": if (a = r(a), /^(http|https|file):\/\/[^\s]+$/i.test(a)) return e("<span />").append(s('"', "q")).append(e("<a />", { href: a, text: a })).append(s('"', "q")); if (t.nl2br) { var o = /\n/g; o.test(a) && (a = (a + "").replace(o, "<br />")) } var u = e("<span />", { "class": "str" }).html(a); return e("<span />").append(s('"', "q")).append(u).append(s('"', "q")); case "number": return s(a.toString(), "num"); case "undefined": return s("undefined", "undef"); case "null": return s("null", "null"); case "boolean": return s(a ? "true" : "false", "bool") } }; return l(a) }; return e.fn.jsonView = function (n, p) { var t = e(this); if (p = e.extend({}, { nl2br: !0 }, p), "string" == typeof n) try { n = JSON.parse(n) } catch (r) { } return t.append(e("<div />", { "class": "json-view" }).append(a(n, p))), t } }(jQuery);