/*!
* TableSorter 2.15.11 min - Client-side table sorting with ease!
* Copyright (c) 2007 Christian Bach
* http://mottie.github.io/tablesorter/docs/index.html
*/
!function (g) { g.extend({ tablesorter: new function () { function d() { var a = arguments[0], b = 1 < arguments.length ? Array.prototype.slice.call(arguments) : a; if ("undefined" !== typeof console && "undefined" !== typeof console.log) console[/error/i.test(a) ? "error" : /warn/i.test(a) ? "warn" : "log"](b); else alert(b) } function t(a, b) { d(a + " (" + ((new Date).getTime() - b.getTime()) + "ms)") } function m(a) { for (var b in a) return !1; return !0 } function q(a, b, c) { if (!b) return ""; var h = a.config, e = h.textExtraction, f = "", f = "simple" === e ? h.supportsTextContent ? b.textContent : g(b).text() : "function" === typeof e ? e(b, a, c) : "object" === typeof e && e.hasOwnProperty(c) ? e[c](b, a, c) : h.supportsTextContent ? b.textContent : g(b).text(); return g.trim(f) } function s(a) { var b = a.config, c = b.$tbodies = b.$table.children("tbody:not(." + b.cssInfoBlock + ")"), h, e, w, k, n, g, l, z = ""; if (0 === c.length) return b.debug ? d("Warning: *Empty table!* Not building a parser cache") : ""; b.debug && (l = new Date, d("Detecting parsers for each column")); c = c[0].rows; if (c[0]) for (h = [], e = c[0].cells.length, w = 0; w < e; w++) { k = b.$headers.filter(":not([colspan])"); k = k.add(b.$headers.filter('[colspan="1"]')).filter('[data-column="' + w + '"]:last'); n = b.headers[w]; g = f.getParserById(f.getData(k, n, "sorter")); b.empties[w] = f.getData(k, n, "empty") || b.emptyTo || (b.emptyToBottom ? "bottom" : "top"); b.strings[w] = f.getData(k, n, "string") || b.stringTo || "max"; if (!g) a: { k = a; n = c; g = -1; for (var m = w, y = void 0, x = f.parsers.length, r = !1, s = "", y = !0; "" === s && y;) g++, n[g] ? (r = n[g].cells[m], s = q(k, r, m), k.config.debug && d("Checking if value was empty on row " + g + ", column: " + m + ': "' + s + '"')) : y = !1; for (; 0 <= --x;) if ((y = f.parsers[x]) && "text" !== y.id && y.is && y.is(s, k, r)) { g = y; break a } g = f.getParserById("text") } b.debug && (z += "column:" + w + "; parser:" + g.id + "; string:" + b.strings[w] + "; empty: " + b.empties[w] + "\n"); h.push(g) } b.debug && (d(z), t("Completed detecting parsers", l)); b.parsers = h } function v(a) { var b = a.tBodies, c = a.config, h, e, w = c.parsers, k, n, p, l, z, m, y, x = []; c.cache = {}; if (!w) return c.debug ? d("Warning: *Empty table!* Not building a cache") : ""; c.debug && (y = new Date); c.showProcessing && f.isProcessing(a, !0); for (l = 0; l < b.length; l++) if (c.cache[l] = { row: [], normalized: [] }, !g(b[l]).hasClass(c.cssInfoBlock)) { h = b[l] && b[l].rows.length || 0; e = b[l].rows[0] && b[l].rows[0].cells.length || 0; for (n = 0; n < h; ++n) if (z = g(b[l].rows[n]), m = [], z.hasClass(c.cssChildRow)) c.cache[l].row[c.cache[l].row.length - 1] = c.cache[l].row[c.cache[l].row.length - 1].add(z); else { c.cache[l].row.push(z); for (p = 0; p < e; ++p) "undefined" === typeof w[p] ? c.debug && d("No parser found for cell:", z[0].cells[p], "does it have a header?") : (k = q(a, z[0].cells[p], p), k = w[p].format(k, a, z[0].cells[p], p), m.push(k), "numeric" === (w[p].type || "").toLowerCase() && (x[p] = Math.max(Math.abs(k) || 0, x[p] || 0))); m.push(c.cache[l].normalized.length); c.cache[l].normalized.push(m) } c.cache[l].colMax = x } c.showProcessing && f.isProcessing(a); c.debug && t("Building cache for " + h + " rows", y) } function A(a, b) { var c = a.config, h = c.widgetOptions, e = a.tBodies, w = [], k = c.cache, d, p, l, z, q, y, x, r, s, u, v; if (m(k)) return c.appender ? c.appender(a, w) : a.isUpdating ? c.$table.trigger("updateComplete", a) : ""; c.debug && (v = new Date); for (r = 0; r < e.length; r++) if (d = g(e[r]), d.length && !d.hasClass(c.cssInfoBlock)) { q = f.processTbody(a, d, !0); d = k[r].row; p = k[r].normalized; z = (l = p.length) ? p[0].length - 1 : 0; for (y = 0; y < l; y++) if (u = p[y][z], w.push(d[u]), !c.appender || c.pager && !(c.pager.removeRows && h.pager_removeRows || c.pager.ajax)) for (s = d[u].length, x = 0; x < s; x++) q.append(d[u][x]); f.processTbody(a, q, !1) } c.appender && c.appender(a, w); c.debug && t("Rebuilt table", v); b || c.appender || f.applyWidget(a); a.isUpdating && c.$table.trigger("updateComplete", a) } function D(a) { var b = [], c = {}, h = 0, e = g(a).children("thead, tfoot").children("tr"), f, d, n, p, l, m, t, q, s, r; for (f = 0; f < e.length; f++) for (l = e[f].cells, d = 0; d < l.length; d++) { p = l[d]; m = p.parentNode.rowIndex; t = m + "-" + p.cellIndex; q = p.rowSpan || 1; s = p.colSpan || 1; "undefined" === typeof b[m] && (b[m] = []); for (n = 0; n < b[m].length + 1; n++) if ("undefined" === typeof b[m][n]) { r = n; break } c[t] = r; h = Math.max(r, h); g(p).attr({ "data-column": r }); for (n = m; n < m + q; n++) for ("undefined" === typeof b[n] && (b[n] = []), t = b[n], p = r; p < r + s; p++) t[p] = "x" } a.config.columns = h + 1; return c } function C(a) { return /^d/i.test(a) || 1 === a } function E(a) { var b = D(a), c, h, e, w, k, n, p, l = a.config; l.headerList = []; l.headerContent = []; l.debug && (p = new Date); w = l.cssIcon ? '<i class="' + (l.cssIcon === f.css.icon ? f.css.icon : l.cssIcon + " " + f.css.icon) + '"></i>' : ""; l.$headers = g(a).find(l.selectorHeaders).each(function (a) { h = g(this); c = l.headers[a]; l.headerContent[a] = g(this).html(); k = l.headerTemplate.replace(/\{content\}/g, g(this).html()).replace(/\{icon\}/g, w); l.onRenderTemplate && (e = l.onRenderTemplate.apply(h, [a, k])) && "string" === typeof e && (k = e); g(this).html('<div class="' + f.css.headerIn + '">' + k + "</div>"); l.onRenderHeader && l.onRenderHeader.apply(h, [a]); this.column = b[this.parentNode.rowIndex + "-" + this.cellIndex]; this.order = C(f.getData(h, c, "sortInitialOrder") || l.sortInitialOrder) ? [1, 0, 2] : [0, 1, 2]; this.count = -1; this.lockedOrder = !1; n = f.getData(h, c, "lockedOrder") || !1; "undefined" !== typeof n && !1 !== n && (this.order = this.lockedOrder = C(n) ? [1, 1, 1] : [0, 0, 0]); h.addClass(f.css.header + " " + l.cssHeader); l.headerList[a] = this; h.parent().addClass(f.css.headerRow + " " + l.cssHeaderRow).attr("role", "row"); l.tabIndex && h.attr("tabindex", 0) }).attr({ scope: "col", role: "columnheader" }); G(a); l.debug && (t("Built headers:", p), d(l.$headers)) } function B(a, b, c) { var h = a.config; h.$table.find(h.selectorRemove).remove(); s(a); v(a); H(h.$table, b, c) } function G(a) { var b, c, h = a.config; h.$headers.each(function (e, d) { c = g(d); b = "false" === f.getData(d, h.headers[e], "sorter"); d.sortDisabled = b; c[b ? "addClass" : "removeClass"]("sorter-false").attr("aria-disabled", "" + b); a.id && (b ? c.removeAttr("aria-controls") : c.attr("aria-controls", a.id)) }) } function F(a) { var b, c, h, e = a.config, d = e.sortList, k = f.css.sortNone + " " + e.cssNone, n = [f.css.sortAsc + " " + e.cssAsc, f.css.sortDesc + " " + e.cssDesc], p = ["ascending", "descending"], l = g(a).find("tfoot tr").children().removeClass(n.join(" ")); e.$headers.removeClass(n.join(" ")).addClass(k).attr("aria-sort", "none"); h = d.length; for (b = 0; b < h; b++) if (2 !== d[b][1] && (a = e.$headers.not(".sorter-false").filter('[data-column="' + d[b][0] + '"]' + (1 === h ? ":last" : "")), a.length)) for (c = 0; c < a.length; c++) a[c].sortDisabled || (a.eq(c).removeClass(k).addClass(n[d[b][1]]).attr("aria-sort", p[d[b][1]]), l.length && l.filter('[data-column="' + d[b][0] + '"]').eq(c).addClass(n[d[b][1]])); e.$headers.not(".sorter-false").each(function () { var a = g(this), b = this.order[(this.count + 1) % (e.sortReset ? 3 : 2)], b = a.text() + ": " + f.language[a.hasClass(f.css.sortAsc) ? "sortAsc" : a.hasClass(f.css.sortDesc) ? "sortDesc" : "sortNone"] + f.language[0 === b ? "nextAsc" : 1 === b ? "nextDesc" : "nextNone"]; a.attr("aria-label", b) }) } function L(a) { if (a.config.widthFixed && 0 === g(a).find("colgroup").length) { var b = g("<colgroup>"), c = g(a).width(); g(a.tBodies[0]).find("tr:first").children("td:visible").each(function () { b.append(g("<col>").css("width", parseInt(g(this).width() / c * 1E3, 10) / 10 + "%")) }); g(a).prepend(b) } } function M(a, b) { var c, h, e, d = a.config, f = b || d.sortList; d.sortList = []; g.each(f, function (a, b) { c = [parseInt(b[0], 10), parseInt(b[1], 10)]; if (e = d.$headers[c[0]]) d.sortList.push(c), h = g.inArray(c[1], e.order), e.count = 0 <= h ? h : c[1] % (d.sortReset ? 3 : 2) }) } function N(a, b) { return a && a[b] ? a[b].type || "" : "" } function O(a, b, c) { var h, e, d, k = a.config, n = !c[k.sortMultiSortKey], p = k.$table; p.trigger("sortStart", a); b.count = c[k.sortResetKey] ? 2 : (b.count + 1) % (k.sortReset ? 3 : 2); k.sortRestart && (e = b, k.$headers.each(function () { this === e || !n && g(this).is("." + f.css.sortDesc + ",." + f.css.sortAsc) || (this.count = -1) })); e = b.column; if (n) { k.sortList = []; if (null !== k.sortForce) for (h = k.sortForce, c = 0; c < h.length; c++) h[c][0] !== e && k.sortList.push(h[c]); h = b.order[b.count]; if (2 > h && (k.sortList.push([e, h]), 1 < b.colSpan)) for (c = 1; c < b.colSpan; c++) k.sortList.push([e + c, h]) } else { if (k.sortAppend && 1 < k.sortList.length) for (c = 0; c < k.sortAppend.length; c++) d = f.isValueInArray(k.sortAppend[c][0], k.sortList), 0 <= d && k.sortList.splice(d, 1); if (0 <= f.isValueInArray(e, k.sortList)) for (c = 0; c < k.sortList.length; c++) d = k.sortList[c], h = k.$headers[d[0]], d[0] === e && (d[1] = h.order[b.count], 2 === d[1] && (k.sortList.splice(c, 1), h.count = -1)); else if (h = b.order[b.count], 2 > h && (k.sortList.push([e, h]), 1 < b.colSpan)) for (c = 1; c < b.colSpan; c++) k.sortList.push([e + c, h]) } if (null !== k.sortAppend) for (h = k.sortAppend, c = 0; c < h.length; c++) h[c][0] !== e && k.sortList.push(h[c]); p.trigger("sortBegin", a); setTimeout(function () { F(a); I(a); A(a); p.trigger("sortEnd", a) }, 1) } function I(a) { var b, c, h, e, d, k, g, p, l, q, s, u, x = 0, r = a.config, v = r.textSorter || "", A = r.sortList, B = A.length, C = a.tBodies.length; if (!r.serverSideSorting && !m(r.cache)) { r.debug && (l = new Date); for (c = 0; c < C; c++) d = r.cache[c].colMax, p = (k = r.cache[c].normalized) && k[0] ? k[0].length - 1 : 0, k.sort(function (c, k) { for (b = 0; b < B; b++) { e = A[b][0]; g = A[b][1]; x = 0 === g; if (r.sortStable && c[e] === k[e] && 1 === B) break; (h = /n/i.test(N(r.parsers, e))) && r.strings[e] ? (h = "boolean" === typeof r.string[r.strings[e]] ? (x ? 1 : -1) * (r.string[r.strings[e]] ? -1 : 1) : r.strings[e] ? r.string[r.strings[e]] || 0 : 0, q = r.numberSorter ? r.numberSorter(c[e], k[e], x, d[e], a) : f["sortNumeric" + (x ? "Asc" : "Desc")](c[e], k[e], h, d[e], e, a)) : (s = x ? c : k, u = x ? k : c, q = "function" === typeof v ? v(s[e], u[e], x, e, a) : "object" === typeof v && v.hasOwnProperty(e) ? v[e](s[e], u[e], x, e, a) : f["sortNatural" + (x ? "Asc" : "Desc")](c[e], k[e], e, a, r)); if (q) return q } return c[p] - k[p] }); r.debug && t("Sorting on " + A.toString() + " and dir " + g + " time", l) } } function J(a, b) { a[0].isUpdating && a.trigger("updateComplete"); "function" === typeof b && b(a[0]) } function H(a, b, c) { var h = a[0].config.sortList; !1 !== b && !a[0].isProcessing && h.length ? a.trigger("sorton", [h, function () { J(a, c) }, !0]) : J(a, c) } function K(a) { var b = a.config, c = b.$table; c.unbind("sortReset update updateRows updateCell updateAll addRows updateComplete sorton appendCache updateCache applyWidgetId applyWidgets refreshWidgets destroy mouseup mouseleave ".split(" ").join(b.namespace + " ")).bind("sortReset" + b.namespace, function (c) { c.stopPropagation(); b.sortList = []; F(a); I(a); A(a) }).bind("updateAll" + b.namespace, function (c, e, d) { c.stopPropagation(); a.isUpdating = !0; f.refreshWidgets(a, !0, !0); f.restoreHeaders(a); E(a); f.bindEvents(a, b.$headers); K(a); B(a, e, d) }).bind("update" + b.namespace + " updateRows" + b.namespace, function (b, c, d) { b.stopPropagation(); a.isUpdating = !0; G(a); B(a, c, d) }).bind("updateCell" + b.namespace, function (h, e, d, f) { h.stopPropagation(); a.isUpdating = !0; c.find(b.selectorRemove).remove(); var n, p, l; n = c.find("tbody"); h = n.index(g(e).parents("tbody").filter(":first")); var m = g(e).parents("tr").filter(":first"); e = g(e)[0]; n.length && 0 <= h && (p = n.eq(h).find("tr").index(m), l = e.cellIndex, n = b.cache[h].normalized[p].length - 1, b.cache[h].row[a.config.cache[h].normalized[p][n]] = m, b.cache[h].normalized[p][l] = b.parsers[l].format(q(a, e, l), a, e, l), H(c, d, f)) }).bind("addRows" + b.namespace, function (h, e, d, f) { h.stopPropagation(); a.isUpdating = !0; if (m(b.cache)) G(a), B(a, d, f); else { var g, p = e.filter("tr").length, l = [], t = e[0].cells.length, v = c.find("tbody").index(e.parents("tbody").filter(":first")); b.parsers || s(a); for (h = 0; h < p; h++) { for (g = 0; g < t; g++) l[g] = b.parsers[g].format(q(a, e[h].cells[g], g), a, e[h].cells[g], g); l.push(b.cache[v].row.length); b.cache[v].row.push([e[h]]); b.cache[v].normalized.push(l); l = [] } H(c, d, f) } }).bind("updateComplete" + b.namespace, function () { a.isUpdating = !1 }).bind("sorton" + b.namespace, function (b, e, d, f) { var g = a.config; b.stopPropagation(); c.trigger("sortStart", this); M(a, e); F(a); g.delayInit && m(g.cache) && v(a); c.trigger("sortBegin", this); I(a); A(a, f); c.trigger("sortEnd", this); "function" === typeof d && d(a) }).bind("appendCache" + b.namespace, function (b, c, d) { b.stopPropagation(); A(a, d); "function" === typeof c && c(a) }).bind("updateCache" + b.namespace, function (c, e) { b.parsers || s(a); v(a); "function" === typeof e && e(a) }).bind("applyWidgetId" + b.namespace, function (c, e) { c.stopPropagation(); f.getWidgetById(e).format(a, b, b.widgetOptions) }).bind("applyWidgets" + b.namespace, function (b, c) { b.stopPropagation(); f.applyWidget(a, c) }).bind("refreshWidgets" + b.namespace, function (b, c, d) { b.stopPropagation(); f.refreshWidgets(a, c, d) }).bind("destroy" + b.namespace, function (b, c, d) { b.stopPropagation(); f.destroy(a, c, d) }) } var f = this; f.version = "2.15.11"; f.parsers = []; f.widgets = []; f.defaults = { theme: "default", widthFixed: !1, showProcessing: !1, headerTemplate: "{content}", onRenderTemplate: null, onRenderHeader: null, cancelSelection: !0, tabIndex: !0, dateFormat: "mmddyyyy", sortMultiSortKey: "shiftKey", sortResetKey: "ctrlKey", usNumberFormat: !0, delayInit: !1, serverSideSorting: !1, headers: {}, ignoreCase: !0, sortForce: null, sortList: [], sortAppend: null, sortStable: !1, sortInitialOrder: "asc", sortLocaleCompare: !1, sortReset: !1, sortRestart: !1, emptyTo: "bottom", stringTo: "max", textExtraction: "simple", textSorter: null, numberSorter: null, widgets: [], widgetOptions: { zebra: ["even", "odd"] }, initWidgets: !0, initialized: null, tableClass: "", cssAsc: "", cssDesc: "", cssNone: "", cssHeader: "", cssHeaderRow: "", cssProcessing: "", cssChildRow: "tablesorter-childRow", cssIcon: "tablesorter-icon", cssInfoBlock: "tablesorter-infoOnly", selectorHeaders: "> thead th, > thead td", selectorSort: "th, td", selectorRemove: ".remove-me", debug: !1, headerList: [], empties: {}, strings: {}, parsers: [] }; f.css = { table: "tablesorter", childRow: "tablesorter-childRow", header: "tablesorter-header", headerRow: "tablesorter-headerRow", headerIn: "tablesorter-header-inner", icon: "tablesorter-icon", info: "tablesorter-infoOnly", processing: "tablesorter-processing", sortAsc: "tablesorter-headerAsc", sortDesc: "tablesorter-headerDesc", sortNone: "tablesorter-headerUnSorted" }; f.language = { sortAsc: "Ascending sort applied, ", sortDesc: "Descending sort applied, ", sortNone: "No sort applied, ", nextAsc: "activate to apply an ascending sort", nextDesc: "activate to apply a descending sort", nextNone: "activate to remove the sort" }; f.log = d; f.benchmark = t; f.construct = function (a) { return this.each(function () { var b = g.extend(!0, {}, f.defaults, a); !this.hasInitialized && f.buildTable && "TABLE" !== this.tagName ? f.buildTable(this, b) : f.setup(this, b) }) }; f.setup = function (a, b) { if (!a || !a.tHead || 0 === a.tBodies.length || !0 === a.hasInitialized) return b.debug ? d("ERROR: stopping initialization! No table, thead, tbody or tablesorter has already been initialized") : ""; var c = "", h = g(a), e = g.metadata; a.hasInitialized = !1; a.isProcessing = !0; a.config = b; g.data(a, "tablesorter", b); b.debug && g.data(a, "startoveralltimer", new Date); b.supportsTextContent = "x" === g("<span>x</span>")[0].textContent; b.supportsDataObject = function (a) { a[0] = parseInt(a[0], 10); return 1 < a[0] || 1 === a[0] && 4 <= parseInt(a[1], 10) }(g.fn.jquery.split(".")); b.string = { max: 1, min: -1, "max+": 1, "max-": -1, zero: 0, none: 0, "null": 0, top: !0, bottom: !1 }; /tablesorter\-/.test(h.attr("class")) || (c = "" !== b.theme ? " tablesorter-" + b.theme : ""); b.$table = h.addClass(f.css.table + " " + b.tableClass + c).attr({ role: "grid" }); b.namespace = b.namespace ? "." + b.namespace.replace(/\W/g, "") : ".tablesorter" + Math.random().toString(16).slice(2); b.$tbodies = h.children("tbody:not(." + b.cssInfoBlock + ")").attr({ "aria-live": "polite", "aria-relevant": "all" }); b.$table.find("caption").length && b.$table.attr("aria-labelledby", "theCaption"); b.widgetInit = {}; E(a); L(a); s(a); b.delayInit || v(a); f.bindEvents(a, b.$headers); K(a); b.supportsDataObject && "undefined" !== typeof h.data().sortlist ? b.sortList = h.data().sortlist : e && h.metadata() && h.metadata().sortlist && (b.sortList = h.metadata().sortlist); f.applyWidget(a, !0); 0 < b.sortList.length ? h.trigger("sorton", [b.sortList, {}, !b.initWidgets, !0]) : (F(a), b.initWidgets && f.applyWidget(a)); b.showProcessing && h.unbind("sortBegin" + b.namespace + " sortEnd" + b.namespace).bind("sortBegin" + b.namespace + " sortEnd" + b.namespace, function (b) { f.isProcessing(a, "sortBegin" === b.type) }); a.hasInitialized = !0; a.isProcessing = !1; b.debug && f.benchmark("Overall initialization time", g.data(a, "startoveralltimer")); h.trigger("tablesorter-initialized", a); "function" === typeof b.initialized && b.initialized(a) }; f.isProcessing = function (a, b, c) { a = g(a); var h = a[0].config; a = c || a.find("." + f.css.header); b ? ("undefined" !== typeof c && 0 < h.sortList.length && (a = a.filter(function () { return this.sortDisabled ? !1 : 0 <= f.isValueInArray(parseFloat(g(this).attr("data-column")), h.sortList) })), a.addClass(f.css.processing + " " + h.cssProcessing)) : a.removeClass(f.css.processing + " " + h.cssProcessing) }; f.processTbody = function (a, b, c) { a = g(a)[0]; if (c) return a.isProcessing = !0, b.before('<span class="tablesorter-savemyplace"/>'), c = g.fn.detach ? b.detach() : b.remove(); c = g(a).find("span.tablesorter-savemyplace"); b.insertAfter(c); c.remove(); a.isProcessing = !1 }; f.clearTableBody = function (a) { g(a)[0].config.$tbodies.empty() }; f.bindEvents = function (a, b) { a = g(a)[0]; var c, h = a.config; b.find(h.selectorSort).add(b.filter(h.selectorSort)).unbind(["mousedown", "mouseup", "sort", "keyup", ""].join(h.namespace + " ")).bind(["mousedown", "mouseup", "sort", "keyup", ""].join(h.namespace + " "), function (e, d) { var f; f = e.type; if (!(1 !== (e.which || e.button) && !/sort|keyup/.test(f) || "keyup" === f && 13 !== e.which || "mouseup" === f && !0 !== d && 250 < (new Date).getTime() - c)) { if ("mousedown" === f) return c = (new Date).getTime(), "INPUT" === e.target.tagName ? "" : !h.cancelSelection; h.delayInit && m(h.cache) && v(a); f = /TH|TD/.test(this.tagName) ? this : g(this).parents("th, td")[0]; f = h.$headers[b.index(f)]; f.sortDisabled || O(a, f, e) } }); h.cancelSelection && b.attr("unselectable", "on").bind("selectstart", !1).css({ "user-select": "none", MozUserSelect: "none" }) }; f.restoreHeaders = function (a) { var b = g(a)[0].config; b.$table.find(b.selectorHeaders).each(function (a) { g(this).find("." + f.css.headerIn).length && g(this).html(b.headerContent[a]) }) }; f.destroy = function (a, b, c) { a = g(a)[0]; if (a.hasInitialized) { f.refreshWidgets(a, !0, !0); var h = g(a), e = a.config, d = h.find("thead:first"), k = d.find("tr." + f.css.headerRow).removeClass(f.css.headerRow + " " + e.cssHeaderRow), n = h.find("tfoot:first > tr").children("th, td"); d.find("tr").not(k).remove(); h.removeData("tablesorter").unbind("sortReset update updateAll updateRows updateCell addRows updateComplete sorton appendCache updateCache applyWidgetId applyWidgets refreshWidgets destroy mouseup mouseleave keypress sortBegin sortEnd ".split(" ").join(e.namespace + " ")); e.$headers.add(n).removeClass([f.css.header, e.cssHeader, e.cssAsc, e.cssDesc, f.css.sortAsc, f.css.sortDesc, f.css.sortNone].join(" ")).removeAttr("data-column"); k.find(e.selectorSort).unbind(["mousedown", "mouseup", "keypress", ""].join(e.namespace + " ")); f.restoreHeaders(a); !1 !== b && h.removeClass(f.css.table + " " + e.tableClass + " tablesorter-" + e.theme); a.hasInitialized = !1; "function" === typeof c && c(a) } }; f.regex = { chunk: /(^([+\-]?(?:0|[1-9]\d*)(?:\.\d*)?(?:[eE][+\-]?\d+)?)?$|^0x[0-9a-f]+$|\d+)/gi, chunks: /(^\\0|\\0$)/, hex: /^0x[0-9a-f]+$/i }; f.sortNatural = function (a, b) { if (a === b) return 0; var c, d, e, g, k, n; d = f.regex; if (d.hex.test(b)) { c = parseInt(a.match(d.hex), 16); e = parseInt(b.match(d.hex), 16); if (c < e) return -1; if (c > e) return 1 } c = a.replace(d.chunk, "\\0$1\\0").replace(d.chunks, "").split("\\0"); d = b.replace(d.chunk, "\\0$1\\0").replace(d.chunks, "").split("\\0"); n = Math.max(c.length, d.length); for (k = 0; k < n; k++) { e = isNaN(c[k]) ? c[k] || 0 : parseFloat(c[k]) || 0; g = isNaN(d[k]) ? d[k] || 0 : parseFloat(d[k]) || 0; if (isNaN(e) !== isNaN(g)) return isNaN(e) ? 1 : -1; typeof e !== typeof g && (e += "", g += ""); if (e < g) return -1; if (e > g) return 1 } return 0 }; f.sortNaturalAsc = function (a, b, c, d, e) { if (a === b) return 0; c = e.string[e.empties[c] || e.emptyTo]; return "" === a && 0 !== c ? "boolean" === typeof c ? c ? -1 : 1 : -c || -1 : "" === b && 0 !== c ? "boolean" === typeof c ? c ? 1 : -1 : c || 1 : f.sortNatural(a, b) }; f.sortNaturalDesc = function (a, b, c, d, e) { if (a === b) return 0; c = e.string[e.empties[c] || e.emptyTo]; return "" === a && 0 !== c ? "boolean" === typeof c ? c ? -1 : 1 : c || 1 : "" === b && 0 !== c ? "boolean" === typeof c ? c ? 1 : -1 : -c || -1 : f.sortNatural(b, a) }; f.sortText = function (a, b) { return a > b ? 1 : a < b ? -1 : 0 }; f.getTextValue = function (a, b, c) { if (c) { var d = a ? a.length : 0, e = c + b; for (c = 0; c < d; c++) e += a.charCodeAt(c); return b * e } return 0 }; f.sortNumericAsc = function (a, b, c, d, e, g) { if (a === b) return 0; g = g.config; e = g.string[g.empties[e] || g.emptyTo]; if ("" === a && 0 !== e) return "boolean" === typeof e ? e ? -1 : 1 : -e || -1; if ("" === b && 0 !== e) return "boolean" === typeof e ? e ? 1 : -1 : e || 1; isNaN(a) && (a = f.getTextValue(a, c, d)); isNaN(b) && (b = f.getTextValue(b, c, d)); return a - b }; f.sortNumericDesc = function (a, b, c, d, e, g) { if (a === b) return 0; g = g.config; e = g.string[g.empties[e] || g.emptyTo]; if ("" === a && 0 !== e) return "boolean" === typeof e ? e ? -1 : 1 : e || 1; if ("" === b && 0 !== e) return "boolean" === typeof e ? e ? 1 : -1 : -e || -1; isNaN(a) && (a = f.getTextValue(a, c, d)); isNaN(b) && (b = f.getTextValue(b, c, d)); return b - a }; f.sortNumeric = function (a, b) { return a - b }; f.characterEquivalents = { a: "\u00e1\u00e0\u00e2\u00e3\u00e4\u0105\u00e5", A: "\u00c1\u00c0\u00c2\u00c3\u00c4\u0104\u00c5", c: "\u00e7\u0107\u010d", C: "\u00c7\u0106\u010c", e: "\u00e9\u00e8\u00ea\u00eb\u011b\u0119", E: "\u00c9\u00c8\u00ca\u00cb\u011a\u0118", i: "\u00ed\u00ec\u0130\u00ee\u00ef\u0131", I: "\u00cd\u00cc\u0130\u00ce\u00cf", o: "\u00f3\u00f2\u00f4\u00f5\u00f6", O: "\u00d3\u00d2\u00d4\u00d5\u00d6", ss: "\u00df", SS: "\u1e9e", u: "\u00fa\u00f9\u00fb\u00fc\u016f", U: "\u00da\u00d9\u00db\u00dc\u016e" }; f.replaceAccents = function (a) { var b, c = "[", d = f.characterEquivalents; if (!f.characterRegex) { f.characterRegexArray = {}; for (b in d) "string" === typeof b && (c += d[b], f.characterRegexArray[b] = RegExp("[" + d[b] + "]", "g")); f.characterRegex = RegExp(c + "]") } if (f.characterRegex.test(a)) for (b in d) "string" === typeof b && (a = a.replace(f.characterRegexArray[b], b)); return a }; f.isValueInArray = function (a, b) { var c, d = b.length; for (c = 0; c < d; c++) if (b[c][0] === a) return c; return -1 }; f.addParser = function (a) { var b, c = f.parsers.length, d = !0; for (b = 0; b < c; b++) f.parsers[b].id.toLowerCase() === a.id.toLowerCase() && (d = !1); d && f.parsers.push(a) }; f.getParserById = function (a) { var b, c = f.parsers.length; for (b = 0; b < c; b++) if (f.parsers[b].id.toLowerCase() === a.toString().toLowerCase()) return f.parsers[b]; return !1 }; f.addWidget = function (a) { f.widgets.push(a) }; f.getWidgetById = function (a) { var b, c, d = f.widgets.length; for (b = 0; b < d; b++) if ((c = f.widgets[b]) && c.hasOwnProperty("id") && c.id.toLowerCase() === a.toLowerCase()) return c }; f.applyWidget = function (a, b) { a = g(a)[0]; var c = a.config, d = c.widgetOptions, e = [], m, k, n; c.debug && (m = new Date); c.widgets.length && (c.widgets = g.grep(c.widgets, function (a, b) { return g.inArray(a, c.widgets) === b }), g.each(c.widgets || [], function (a, b) { (n = f.getWidgetById(b)) && n.id && (n.priority || (n.priority = 10), e[a] = n) }), e.sort(function (a, b) { return a.priority < b.priority ? -1 : a.priority === b.priority ? 0 : 1 }), g.each(e, function (e, f) { if (f) { if (b || !c.widgetInit[f.id]) f.hasOwnProperty("options") && (d = a.config.widgetOptions = g.extend(!0, {}, f.options, d)), f.hasOwnProperty("init") && f.init(a, f, c, d), c.widgetInit[f.id] = !0; !b && f.hasOwnProperty("format") && f.format(a, c, d, !1) } })); c.debug && (k = c.widgets.length, t("Completed " + (!0 === b ? "initializing " : "applying ") + k + " widget" + (1 !== k ? "s" : ""), m)) }; f.refreshWidgets = function (a, b, c) { a = g(a)[0]; var h, e = a.config, m = e.widgets, k = f.widgets, n = k.length; for (h = 0; h < n; h++) k[h] && k[h].id && (b || 0 > g.inArray(k[h].id, m)) && (e.debug && d('Refeshing widgets: Removing "' + k[h].id + '"'), k[h].hasOwnProperty("remove") && e.widgetInit[k[h].id] && (k[h].remove(a, e, e.widgetOptions), e.widgetInit[k[h].id] = !1)); !0 !== c && f.applyWidget(a, b) }; f.getData = function (a, b, c) { var d = ""; a = g(a); var e, f; if (!a.length) return ""; e = g.metadata ? a.metadata() : !1; f = " " + (a.attr("class") || ""); "undefined" !== typeof a.data(c) || "undefined" !== typeof a.data(c.toLowerCase()) ? d += a.data(c) || a.data(c.toLowerCase()) : e && "undefined" !== typeof e[c] ? d += e[c] : b && "undefined" !== typeof b[c] ? d += b[c] : " " !== f && f.match(" " + c + "-") && (d = f.match(RegExp("\\s" + c + "-([\\w-]+)"))[1] || ""); return g.trim(d) }; f.formatFloat = function (a, b) { if ("string" !== typeof a || "" === a) return a; var c; a = (b && b.config ? !1 !== b.config.usNumberFormat : "undefined" !== typeof b ? b : 1) ? a.replace(/,/g, "") : a.replace(/[\s|\.]/g, "").replace(/,/g, "."); /^\s*\([.\d]+\)/.test(a) && (a = a.replace(/^\s*\(([.\d]+)\)/, "-$1")); c = parseFloat(a); return isNaN(c) ? g.trim(a) : c }; f.isDigit = function (a) { return isNaN(a) ? /^[\-+(]?\d+[)]?$/.test(a.toString().replace(/[,.'"\s]/g, "")) : !0 } } }); var q = g.tablesorter; g.fn.extend({ tablesorter: q.construct }); q.addParser({ id: "text", is: function () { return !0 }, format: function (d, t) { var m = t.config; d && (d = g.trim(m.ignoreCase ? d.toLocaleLowerCase() : d), d = m.sortLocaleCompare ? q.replaceAccents(d) : d); return d }, type: "text" }); q.addParser({ id: "digit", is: function (d) { return q.isDigit(d) }, format: function (d, t) { var m = q.formatFloat((d || "").replace(/[^\w,. \-()]/g, ""), t); return d && "number" === typeof m ? m : d ? g.trim(d && t.config.ignoreCase ? d.toLocaleLowerCase() : d) : d }, type: "numeric" }); q.addParser({ id: "currency", is: function (d) { return /^\(?\d+[\u00a3$\u20ac\u00a4\u00a5\u00a2?.]|[\u00a3$\u20ac\u00a4\u00a5\u00a2?.]\d+\)?$/.test((d || "").replace(/[+\-,. ]/g, "")) }, format: function (d, t) { var m = q.formatFloat((d || "").replace(/[^\w,. \-()]/g, ""), t); return d && "number" === typeof m ? m : d ? g.trim(d && t.config.ignoreCase ? d.toLocaleLowerCase() : d) : d }, type: "numeric" }); q.addParser({ id: "ipAddress", is: function (d) { return /^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$/.test(d) }, format: function (d, g) { var m, u = d ? d.split(".") : "", s = "", v = u.length; for (m = 0; m < v; m++) s += ("00" + u[m]).slice(-3); return d ? q.formatFloat(s, g) : d }, type: "numeric" }); q.addParser({ id: "url", is: function (d) { return /^(https?|ftp|file):\/\//.test(d) }, format: function (d) { return d ? g.trim(d.replace(/(https?|ftp|file):\/\//, "")) : d }, type: "text" }); q.addParser({ id: "isoDate", is: function (d) { return /^\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2}/.test(d) }, format: function (d, g) { return d ? q.formatFloat("" !== d ? (new Date(d.replace(/-/g, "/"))).getTime() || d : "", g) : d }, type: "numeric" }); q.addParser({ id: "percent", is: function (d) { return /(\d\s*?%|%\s*?\d)/.test(d) && 15 > d.length }, format: function (d, g) { return d ? q.formatFloat(d.replace(/%/g, ""), g) : d }, type: "numeric" }); q.addParser({ id: "usLongDate", is: function (d) { return /^[A-Z]{3,10}\.?\s+\d{1,2},?\s+(\d{4})(\s+\d{1,2}:\d{2}(:\d{2})?(\s+[AP]M)?)?$/i.test(d) || /^\d{1,2}\s+[A-Z]{3,10}\s+\d{4}/i.test(d) }, format: function (d, g) { return d ? q.formatFloat((new Date(d.replace(/(\S)([AP]M)$/i, "$1 $2"))).getTime() || d, g) : d }, type: "numeric" }); q.addParser({ id: "shortDate", is: function (d) { return /(^\d{1,2}[\/\s]\d{1,2}[\/\s]\d{4})|(^\d{4}[\/\s]\d{1,2}[\/\s]\d{1,2})/.test((d || "").replace(/\s+/g, " ").replace(/[\-.,]/g, "/")) }, format: function (d, g, m, u) { if (d) { m = g.config; var s = m.$headers.filter("[data-column=" + u + "]:last"); u = s.length && s[0].dateFormat || q.getData(s, m.headers[u], "dateFormat") || m.dateFormat; d = d.replace(/\s+/g, " ").replace(/[\-.,]/g, "/"); "mmddyyyy" === u ? d = d.replace(/(\d{1,2})[\/\s](\d{1,2})[\/\s](\d{4})/, "$3/$1/$2") : "ddmmyyyy" === u ? d = d.replace(/(\d{1,2})[\/\s](\d{1,2})[\/\s](\d{4})/, "$3/$2/$1") : "yyyymmdd" === u && (d = d.replace(/(\d{4})[\/\s](\d{1,2})[\/\s](\d{1,2})/, "$1/$2/$3")) } return d ? q.formatFloat((new Date(d)).getTime() || d, g) : d }, type: "numeric" }); q.addParser({ id: "time", is: function (d) { return /^(([0-2]?\d:[0-5]\d)|([0-1]?\d:[0-5]\d\s?([AP]M)))$/i.test(d) }, format: function (d, g) { return d ? q.formatFloat((new Date("2000/01/01 " + d.replace(/(\S)([AP]M)$/i, "$1 $2"))).getTime() || d, g) : d }, type: "numeric" }); q.addParser({ id: "metadata", is: function () { return !1 }, format: function (d, q, m) { d = q.config; d = d.parserMetadataName ? d.parserMetadataName : "sortValue"; return g(m).metadata()[d] }, type: "numeric" }); q.addWidget({ id: "zebra", priority: 90, format: function (d, t, m) { var u, s, v, A, D, C, E = RegExp(t.cssChildRow, "i"), B = t.$tbodies; t.debug && (D = new Date); for (d = 0; d < B.length; d++) u = B.eq(d), C = u.children("tr").length, 1 < C && (v = 0, u = u.children("tr:visible").not(t.selectorRemove), u.each(function () { s = g(this); E.test(this.className) || v++; A = 0 === v % 2; s.removeClass(m.zebra[A ? 1 : 0]).addClass(m.zebra[A ? 0 : 1]) })); t.debug && q.benchmark("Applying Zebra widget", D) }, remove: function (d, q, m) { var u; q = q.$tbodies; var s = (m.zebra || ["even", "odd"]).join(" "); for (m = 0; m < q.length; m++) u = g.tablesorter.processTbody(d, q.eq(m), !0), u.children().removeClass(s), g.tablesorter.processTbody(d, u, !1) } }) }(jQuery);


$(function () {

	$.tablesorter.addParser({
		// set a unique id 
		id: 'attr',
		is: function (s) {
			// return false so this parser is not auto detected 
			return false;
		},
		format: function (s, table, cell, cellIndex) {
			// get data attributes from $(cell).attr('data-something');
			// check specific column using cellIndex
			return $(cell).find("[data-sort]").attr('data-sort');
		},
		// set type, either numeric or text 
		type: 'numeric'
	});
	/*
	$.tablesorter.addParser({
		// set a unique id 
		id: 'cbox',
		is: function (s) {
			// return false so this parser is not auto detected 
			return false;
		},
		format: function (s, table, cell, cellIndex) {
			// get data attributes from $(cell).attr('data-something');
			// check specific column using cellIndex
			return $(cell).find("input[type=checkbox]").prop('checked');
		},
		// set type, either numeric or text 
		type: 'numeric'
	});*/


});