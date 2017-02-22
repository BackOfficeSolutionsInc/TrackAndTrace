

$(window).bind('beforeunload', function (event) {
	if ($(".unsaved").length > 0)
		return "There are items you have not saved.";
	return;
});

$(".modifiable").change(modify);
$(".modifiable").bind('input', modify);

function modify() {
	$(this).addClass("unsaved");
	$(".saveButton").addClass("unsaved");
}

function clearUnsaved() {
	$(".unsaved").removeClass("unsaved");
}

function UrlEncodingFix(str) {
	str = replaceAll("%26%2339%3B", "%27", str);
	str = replaceAll("%26%2334%3B", "%22", str);
	str = replaceAll("%26%2313%3B", "%0A", str);
	str = replaceAll("%26%2310%3B", "%0D", str);
	return str;
}

function escapeString(str) {
	if (typeof (str) !== "string")
		return str;
	str = str.replace(/"/g, "&quot;");
	str = str.replace(/'/g, "&#39;");
	return str;
}
//Index of for IE
if (!Array.prototype.indexOf) {
	Array.prototype.indexOf = function (elt /*, from*/) {
		var len = this.length >>> 0;

		var from = Number(arguments[1]) || 0;
		from = (from < 0) ? Math.ceil(from) : Math.floor(from);
		if (from < 0)
			from += len;

		for (; from < len; from++) {
			if (from in this && this[from] === elt)
				return from;
		}
		return -1;
	};
}
function toTitleCase(str) {
	return str.replace(/\w\S*/g, function (txt) { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); });
}


(function ($) {
	$.fn.valList = function () {
		return $.map(this, function (elem) {
			return elem.value || "";
		}).join(",");
	};
	$.fn.nameList = function () {
		return $.map(this, function (elem) {
			return elem.name || "";
		}).join(",");
	};
	$.fn.asString = function () {
		if (Object.prototype.toString.call(this) === '[object Array]') {
			return $.map(this, function (elem) {
				return elem || "";
			}).join(",");
		}
		return this;
	};

	$.fn.serializeObject = function () {
		var o = {};
		var a = this.serializeArray();
		$.each(a, function () {
			if (o[this.name] !== undefined) {
				if (!o[this.name].push) {
					o[this.name] = [o[this.name]];
				}
				o[this.name].push(this.value || '');
			} else {
				o[this.name] = this.value || '';
			}
		});
		return o;
	};

})(jQuery);

function qtip() {
	$('[title]').qtip({
		position: {
			my: 'bottom left',  // Position my top left...
			at: 'top center', // at the bottom right of...
			target: 'mouse'
		}
	});
}

function save(key, value) {
	if ('localStorage' in window && window['localStorage'] !== null) {
		window.localStorage[key] = value;
	} else {
		console.log("Could not save");
	}
}

function load(key) {
	if ('localStorage' in window && window['localStorage'] !== null) {
		return window.localStorage[key];
	} else {
		console.log("Could not load");
	}
}

function ForceUnhide() {
	var speed = 40;
	$(".startHiddenGroup").each(function (i, e) {
		$(e).find(".startHidden").each(function (i, e2) {
			setTimeout(function () {
				$(e2).addClass("unhide");
			}, speed * i);
		});
	});
}

function ForceHide() { $(".startHidden").removeClass("startHidden").removeClass("unhide").addClass("startHidden"); }

function refresh() { location.reload(); }

/*
if callback returns text or bool, there is an error
*/
function showTextAreaModal(title, callback, defaultText) {
	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	if (typeof defaultText === "undefined")
		defaultText = "";

	$('#modalBody').html("<div class='error' style='color:red;font-weight:bold'></div><textarea class='form-control verticalOnly' rows=5>" + defaultText + "</textarea>");
	$("#modalTitle").html(title);
	$("#modalForm").unbind('submit');
	$("#modal").modal("show");

	$("#modalForm").submit(function (e) {
		e.preventDefault();
		var result = callback($('#modalBody').find("textarea").val());
		if (result) {
			if (typeof result !== "string") {
				result = "An error has occurred. Please check your input.";
			}
			$("#modalBody .error").html(result);
		} else {
			$("#modal").modal("hide");
		}
	});
}
function generateGuid() {
	var result, i, j;
	result = '';
	for (j = 0; j < 32; j++) {
		if (j == 8 || j == 12 || j == 16 || j == 20)
			result = result + '-';
		i = Math.floor(Math.random() * 16).toString(16).toUpperCase();
		result = result + i;
	}
	return result;
}

//function getFormattedDate(date) {

//	if (typeof (date) === "undefined") {
//		date = new Date();
//	} else if (typeof (date) === "string") {
//		console.error("Could not determine format from date string: " + date)
//	}

//	var _d = date.getDate(),
//                dd = _d > 9 ? _d : '0' + _d,
//                _m = date.getMonth() + 1,
//                mm = _m > 9 ? _m : '0' + _m,
//                yyyy = date.getFullYear(),
//                formatted = mm + '-' + dd + '-' + (yyyy);

//	var _userFormat = window.dateFormat
//        .replace(/mm/gi, mm).replace(/m/gi, _m)
//        .replace(/dd/gi, dd).replace(/d/gi, _d)
//        .replace(/yyyy/gi, yyyy).replace(/yy/gi, (yyyy - 2000));
//	return _userFormat;
//}

function generateDatepickerLocalize(selector, date, name, id, options) {
	var offset = new Date().getTimezoneOffset();
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (typeof (date) === "string") {
		console.error("Could not determine format from date string: " + date)
	}

	var newDate = new Date(date.getTime() + offset * 60000);
	generateDatepicker(selector, newDate, name, id, options, offset);
}

function generateDatepicker(selector, date, name, id, options, offsetMinutes) {
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (typeof (date) === "string") {
		console.error("Could not determine format from date string: " + date)
	}

	if (typeof (name) === "undefined")
		name = "Date";
	if (typeof (id) === "undefined")
		id = name;
	if (typeof (offsetMinutes) === "undefined")
		offsetMinutes = 0;


	date = new Date(date.getTime() - offsetMinutes * 60000);

	function formatDate(edate) {
		var _d = edate.getDate(),
		_m = edate.getMonth() + 1,
		_mm = edate.getMinutes(),
		_h = edate.getHours(),
		_s = edate.getSeconds(),
		d = _d > 9 ? _d : '0' + _d,
		m = _m > 9 ? _m : '0' + _m,
		h = _h > 9 ? _h : '0' + _h,
		mm = _mm > 9 ? _mm : '0' + _mm,
		s = _s > 9 ? _s : '0' + _s;
		return m + '-' + d + '-' + (edate.getFullYear()) +" "+h+":"+mm+":"+s;
	}

	var _d = date.getDate(),
        dd = _d > 9 ? _d : '0' + _d,
        _m = date.getMonth() + 1,
        mm = _m > 9 ? _m : '0' + _m,
        yyyy = date.getFullYear();
        //formatted = mm + '-' + dd + '-' + (yyyy);

	var formatted = formatDate(date);


	var _userFormat = window.dateFormat
        .replace(/mm/gi, mm).replace(/m/gi, _m)
        .replace(/dd/gi, dd).replace(/d/gi, _d)
        .replace(/yyyy/gi, yyyy).replace(/yy/gi, (yyyy - 2000));

	var guid = generateGuid();
	var builder = '<div class="input-append date ' + guid + '">';
	builder += '<input class="form-control client-date" data-val="true"' +
               ' data-val-date="The field Model must be a date." type="text" ' +
               'value="' + _userFormat + '">';
	builder += '<span class="add-on"><i class="icon-th"></i></span>';
	builder += '<input type="hidden" class="server-date" id="' + id + '" name="' + name + '" value="' + formatted + '" />';
	builder += '</div>';
	var dp = $(builder);
	$(selector).append(dp);
	var dpOptions = {
		format: window.dateFormat.toLowerCase(),
	};
	if (options) {
		for (var k in options) {
			dpOptions[k] = options[k];
		}
	}
	var _offsetMin = offsetMinutes;
	$('.' + guid + ' .client-date').datepickerX(dpOptions).on('changeDate', function (e) {
		var edate = new Date(e.date.getTime() + _offsetMin * 60000);
		var formatted = formatDate(edate);
		$('.' + guid + ' .server-date').val(formatted);
		$("[name='" + name + "']").trigger("change")
	});
}

function metGoal(direction, goal, measured, alternate) {

	if (!$.trim(measured)) {
		return undefined;
	} else if ($.isNumeric(measured)) {
		var m = +((measured+ "").replace(/,/gi, "."));
		var g = +((goal+ "").replace(/,/gi, "."));
		if (direction == "GreaterThan" || direction == 1) {
			return m >= g;
		} else if (direction == "LessThan" || direction == -1) {
			return m < g;
		} else if (direction == "LessThanOrEqual" || direction == -2) {
			return m <= g;
		} else if (direction == "GreaterThanNotEqual" || direction == 2) {
			return m > g;
		} else if (direction == "EqualTo" || direction == 0) {
			return m == g;
		} else if (direction == "Between" || direction == -3) {
			var ag = +((alternate + "").replace(/,/gi, "."));
			return g <= m && m <= ag;
		} else {
			console.log("Error: goal met could not be calculated. Unhandled direction: " + direction);
			return undefined;
		}
	} else {
		return undefined;
	}
}

Date.prototype.addDays = function (pDays) {
	var mDate = new Date(this.valueOf());
	mDate.setDate(mDate.getDate() + pDays);
	return mDate;
};
Date.prototype.startOfWeek = function (pStartOfWeek) {
	var mDifference = this.getDay() - pStartOfWeek;

	if (mDifference < 0) {
		mDifference += 7;
	}

	return new Date(this.addDays(mDifference * -1));
}

function getWeekSinceEpoch(day) {
	var oneDay = 24 * 60 * 60 * 1000;
	var span = day.startOfWeek(0);
	return Math.floor((span.getTime() / oneDay) / 7);
}

/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  settings ={                                                                                                                ///
///      container:<element_id>,		                                                                                        ///
///      id: (optional, default: guid),                                                                                         ///
///      data:[{},{},...],                                                                                                      ///
///		 title:(optional, default: ""),																							///
///		 nodataText:(optional, default: "No data available."),																	///
///																																///
///		 clickAdd:<url,function(row,settings)>(optional, default: null),														///
///		 clickEdit:<url,function(row,settings)>(optional, default: null),														///
///		 clickRemove:<url,function(row,settings)>(optional, default: null),														///
///		 clickReorder:<url,function(row,oldIndex,newIndex,settings)>(optional, default: null),									///
///			{0} = row.Id																										///
///			{1} = oldIndex																										///
///			{2} = newIndex																										///
///																																///
///      panel:{	(optional)																									///
///			id:(optional, default: "panel-{id}"),																				///
///			classes:(optional, default: "panel panel-primary"),																	///
///			element:(optional, default: "<div/>"),																				///
///			header:{																											///
///				id:(optional, defaults: null),																					///
///				classes:(optional, default: "panel-heading"),																	///
///				element:(optional, default: "<div/>"),																			///
///				title:{																											///
///					id:(optional, defaults: null),																				///
///					classes:(optional, default: "panel-title"),																	///
///					element:(optional, default: "<h2/>"),																		///
///				}																												///
///			},																													///
///			nodata:{																											///
///				id:(optional, defaults: "panel-nodata-{id}"),																	///
///				classes:(optional, default: "panel-body"),																		///
///				element:(optional, default: "<div/>"),																			///
///			},																													///
///		 },																														///
///      addButton:(optional)<false,{																							///
///			id:(optional, default: "add-{id}"),																					///
///			element:(optional, default: "<div/>"),																				///
///			classes:(optional, default: "btn btn-primary btn-invert"),															///
///			text:(optional, default: "New {title}"),																			///
///		 }>,																													///
///      table:{	(optional)																									///
///			id:(optional, default: "table-{id}"),																				///
///			element:(optional, default: "<table/>"),																			///
///			classes:(optional, default: "table table-hover"),																	///
///			rows:{ (optional)																									///
///				id:<function(row,settings)>(optional, defaults: "row row-{id}-{row.Id}"),										///
///				classes:(optional, default: ""),																				///
///				element:(optional, default: "<tr/>"),																			///
///			},																													///
///			cells:{ (optional)																									///
///				id:<function(row,settings)>(optional, default: null),															///
///				classes:<function(row,settings)>(optional, default: ""),														///
///				element:(optional, default: "<td/>"),																			///
///			}																													///
///			editText:(optional, default: "Edit"),																				///
///			removeText:(optional, default: "Delete"),																			///
///		 },																														///
///      cells: [{                                                                                                              ///
///          id:(optional, overrides above),																					///
///          classes:(optional, overrides above),																				///
///          contents:<string,function(row,i)>(optional),																		///
///          edit:<bool,function(settings)>(optional, false),																	///
///          remove:<bool,function(settings)>(optional, false),																	///
///          reorder:<bool,function(settings)>(optional, false),																///
///		 },...,																													///
///			...function(row,i) ,...																								///
///      },...],																												///
///      nodata: <string,element>,																								///
///  }                                                                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/
var DataTable = function (settings) {

	//Helpers
	var resolve = function (strFunc, args) {
		if (typeof (strFunc) === "string")
			return strFunc;
		else if (typeof (strFunc) === "function")
			return strFunc.apply(settings, [].splice.call(arguments, 1));
		else if (typeof (strFunc) === "boolean")
			return strFunc;
		return null;
	}

	if (!settings.container) {
		console.warn("Container not set for data-table.");
	}

	settings.id = settings.id || generateGuid();
	var id = settings.id;

	if (typeof (data) === "undefined" || data === null || data === false)
		data = [];

	settings.title = resolve(settings.title, settings) || "";
	settings.nodataText = resolve(settings.nodataText, settings) || "No data available.";

	//AddButton
	if (settings.addButton != false) {
		if (!settings.clickAdd) {
			console.warn("Cannot generate AddButton if clickAdd is null. To disable this warning set 'addButton: false'");
			settings.addButton = false;
		} else {
			settings.addButton = settings.addButton || {};
			settings.addButton.id = settings.addButton.id || "add-" + settings.id;
			settings.addButton.element = settings.addButton.element || $("<div/>");
			settings.addButton.classes = settings.addButton.classes || "btn btn-primary btn-invert";
			settings.addButton.text = settings.addButton.text || "New " + settings.title;
		}
	}

	//Panel
	settings.panel = settings.panel || {};
	settings.panel.id = settings.panel.id || "panel-" + settings.id;
	settings.panel.element = settings.panel.element || $("<div/>");
	settings.panel.classes = settings.panel.classes || "panel panel-primary";

	//Panel - Header
	if (settings.panel.header != false) {
		settings.panel.header = settings.panel.header || {};
		settings.panel.header.id = settings.panel.header.id || null;
		settings.panel.header.classes = settings.panel.header.classes || "panel-heading";
		settings.panel.header.element = settings.panel.header.element || $("<div/>");

		settings.panel.header.title = settings.panel.header.title || {};
		settings.panel.header.title.classes = settings.panel.header.title.classes || "panel-title";
		settings.panel.header.title.element = settings.panel.header.title.element || $("<h2/>");
	}

	//Panel - No Data
	settings.panel.nodata = settings.panel.nodata || {};
	settings.panel.nodata.id = settings.panel.nodata.id || function (_settings) { return "panel-nodata-" + _settings.id; };
	settings.panel.nodata.classes = settings.panel.nodata.classes || "panel-body gray";
	settings.panel.nodata.element = settings.panel.nodata.element || $("<div/>");

	//Table
	settings.table = settings.table || {};
	settings.table.id = settings.table.id || "table-" + settings.id;
	settings.table.element = settings.table.element || $("<table/>");
	settings.table.classes = settings.table.classes || "table table-hover";

	//Table - Rows
	settings.table.rows = settings.table.rows || {};
	settings.table.rows.id = settings.table.rows.id || function (row, _settings) { return "row-" + _settings.id + "-" + row.Id; };
	settings.table.rows.element = settings.table.rows.element || $("<tr/>");
	settings.table.rows.classes = settings.table.rows.classes || "row";

	//Table - Cells
	settings.table.cells = settings.table.cells || {};
	settings.table.cells.id = settings.table.cells.id || function (row, _settings) { return null; };
	settings.table.cells.element = settings.table.cells.element || $("<td/>");
	settings.table.cells.classes = settings.table.cells.classes || "";

	settings.table.editText = settings.table.editText || "Edit";
	settings.table.removeText = settings.table.removeText || "Delete";

	//Cell Selectors
	settings.cells = settings.cells || [];

	//Internal
	settings._ = settings._ || {};
	settings._.olddata = settings._.olddata || [];

	//Variables
	var container, addButton, panel, panelTitle, panelHeader, table, nodata;

	//Complex Updates
	if (typeof (settings.clickEdit) === "string") {
		settings._.onEditUrl = settings.clickEdit;
		settings.clickEdit = function (row, settings) {
			var title = settings.clickEditTitle || function (settings) { return "Edit " + resolve(settings.title, settings); };
			showModal(resolve(title, settings), settings._.onEditUrl.replace("{0}", row.Id), settings._.onEditUrl.replace("{0}", ""), null, null, function (d) {
				try {
					var ids = getIds(settings.data);
					var index = ids.indexOf(d.Object.Id);
					settings.data[index] = d.Object;
				} catch (e) {
					console.error(e);
				}
				editRow(d.Object);
			});
		};
	}
	if (typeof (settings.clickAdd) === "string") {
		settings._.onAddUrl = settings.clickAdd;
		settings.clickAdd = function (settings) {
			var title = settings.clickAddTitle || function (settings) { return "Add " + resolve(settings.title, settings); }
			showModal(resolve(title, settings), settings._.onAddUrl, settings._.onAddUrl, null, null, function (d) {
				addRow(d.Object);
			});
		};
	}
	if (typeof (settings.clickReorder) === "string") {
		settings._.clickReorderUrl = settings.clickReorder;
		settings.clickReorder = function (row, oldIndex, newIndex, settings) {
			$.ajax({
				url: settings._.clickReorderUrl.replace("{0}", row.Id).replace("{1}", oldIndex).replace("{2}", newIndex),
				error: function (e) {
					if (oldIndex > newIndex)
						oldIndex -= 1;
					var item = settings.data.splice(newIndex, 1);
					settings.data.splice(oldIndex, 0, item);
					update();
					refreshRowNum();
				}
			})
		};
	}
	if (typeof (settings.clickRemove) === "string") {
		settings._.onRemoveUrl = settings.clickRemove;
		settings.clickRemove = function (row, settings) {
			var title = settings.clickRemoveTitle || function (settings) { return "Are you sure you want to remove " + (resolve(settings.title, settings) || "").toLowerCase(); };
			showModal({
				icon: "warning",
				title: resolve(title, settings),
				success: function (d) {
					$.ajax({
						url: settings._.onRemoveUrl.replace("{0}", row.Id),
						success: function () {
							removeRow(row);
						}
					});
				}
			});
		};
	}

	//Generator Functions
	var generateContainer = function () {
		container = $("<div/>");
		panel = $(settings.panel.element).clone();
		if (settings.panel.header != false) {
			panelHeader = $(settings.panel.header.element).clone();
			panelTitle = $(settings.panel.header.title.element).clone();
			panelHeader.append(panelTitle);
			panel.append(panelHeader);
		}
		table = $(settings.table.element).clone();
		$(panel).append(table);

		nodata = $(settings.panel.nodata.element).clone();
		$(panel).append(nodata);

		if (settings.addButton != false) {
			var btnHolder = $("<div style='text-align: right;margin-bottom: 3px;'/>");
			addButton = $(settings.addButton.element).clone();
			btnHolder.append(addButton);
			container.append(btnHolder);
			$(addButton).on("click", function () { resolve(settings.clickAdd, settings); });
		}
		container.append(panel);

		var anyReorder = false;
		for (var c in settings.cells) {
			if (resolve(settings.cells[c].reorder, settings) == true) {
				anyReorder = true;
				break;
			}
		}
		if (anyReorder) {
			if (!settings.clickReorder) {
				console.warn("Cannot use cell.reorder if clickReorder is not defined. To disable this warning set 'addButton: false'");
				settings.addButton = false;
			} else {
				try {
					$.getScript("/Scripts/jquery/jquery.ui.sortable.js").done(function () {
						$("#" + settings.table.id + " tbody").xsortable({
							items: ">.row",
							handle: ".reorder-handle",
							start: function (e, ui) {
								$(this).attr('data-previndex', ui.item.index());
							},
							update: function (e, ui) {
								var newIndex = ui.item.index();
								var oldIndex = +$(this).attr('data-previndex');
								$(this).removeAttr('data-previndex');
								var row = settings.data[oldIndex];
								resolve(settings.clickReorder, row, oldIndex, newIndex, settings);
								refreshRowNum();
							}
						}).disableSelection();
					});
				} catch (e) {
					console.warn("xsortable not loaded.");
				}
			}
		}
	}
	var generateRow = function (rowData) {
		var row = $(settings.table.rows.element).clone();
		$(row).attr("id", resolve(settings.table.rows.id, rowData, settings));
		$(row).attr("class", resolve(settings.table.rows.classes, rowData, settings));
		row.append(generateRowCells(rowData));
		return row;
	};
	var generateRowCells = function (row) {
		var i = 0;
		var results = [];
		for (var s in settings.cells) {
			var cellSelector = settings.cells[s];
			var cell = $(settings.table.cells.element).clone();

			var contents = null;
			var cellSelectorId = settings.table.cells.id;
			var cellSelectorClasses = settings.table.cells.classes;

			if (typeof (cellSelector) === "object") {
				cellSelectorId = cellSelector.id || cellSelectorId;
				cellSelectorClasses = cellSelector.classes || cellSelectorId;
				contents = cellSelector.contents;
			} else if (typeof (cellSelector) === "function") {
				contents = cellSelector;
			}

			cell.attr("id", resolve(cellSelectorId, row, settings));
			cell.attr("class", resolve(cellSelectorClasses, row, settings));

			//Is edit button?
			if (resolve(cellSelector.edit, settings) == true) {
				cell.on("click", function () { resolve(settings.clickEdit, row, settings); });
				if (!contents)
					contents = settings.table.editText;
				cell.addClass("clickable");
			}

			//Is remove button?
			if (resolve(cellSelector.remove, settings) == true) {
				cell.on("click", function () { resolve(settings.clickRemove, row, settings); });
				if (!contents)
					contents = settings.table.removeText;
				cell.addClass("clickable");
			}

			//Is row number?
			if (resolve(cellSelector.rowNum, settings) == true) {
				var oldContents = contents;
				contents = function (row, i, settings) {
					return "<span class='rowNum'>" + (i + 1) + ". </span>" + (resolve(oldContents, row, i, settings) || "");
				};
			}

			//Is draggable?
			if (resolve(cellSelector.reorder, settings) == true) {
				contents = function (row, i, settings) {
					return "<span class='reorder-handle icon fontastic-icon-three-bars icon-rotate gray' style='margin-left: -5px;margin-right: -5px;cursor:move;'></span>";
				};
				
			}

			var html = resolve(contents, row, i, settings);

			if (contents == null)
				console.warn("Contents null for " + s);
			if (typeof(html)==="undefined")
				console.warn("Cell was undefined for " + s +" (Did you forget to 'return'?)");

			cell.html(html);

			results.push(cell);
			i++;
		}
		return results;
	};

	//Update Function
	var getIds = function (data) {
		var res = [];
		for (var d in data)
			res.push(data[d].Id);
		return res;
	};
	var diffIds = function (a, b) {
		return a.filter(function (i) { return b.indexOf(i) < 0; });
	};
	var getRowById = function (data, id) {
		for (var r in data) {
			if (data[r].Id == id)
				return data[r];
		}
		return null;
	};
	var insertAt = function (self, index, element) {
		var lastIndex = self.children().size()
		if (index < 0) {
			index = Math.max(0, lastIndex + 1 + index)
		}
		self.append(element)
		if (index < lastIndex) {
			self.children().eq(index).before(self.children().last())
		}
		return self;
	}

	var refreshRowNum = function () {
		$(".rowNum").each(function (i, x) {
			$(this).html("" + (i + 1) + ". ");
		});
	};
	var updateRowsUI = function (settings) {
		settings.data = settings.data || [];

		var dataIds = getIds(settings.data);
		var oldIds = getIds(settings._.olddata);

		var added = diffIds(dataIds, oldIds);
		var removed = diffIds(oldIds, dataIds);
		var checkEdit = diffIds(dataIds, added);

		for (var a in added) {
			var row = getRowById(settings.data, added[a]);
			var tableId = resolve(settings.table.id, settings);
			var tableElement = $("#" + tableId);
			insertAt(tableElement, dataIds.indexOf(added[a]), generateRow(row));
		}

		for (var a in removed) {
			var row = getRowById(settings._.olddata, removed[a])
			var rowId = settings.table.rows.id(row, settings);
			var rowElement = $("#" + rowId);
			rowElement.children().off();
			rowElement.off();
			rowElement.remove();
		}

		for (var a in checkEdit) {
			var newRow = getRowById(settings.data, checkEdit[a]);
			var oldRow = getRowById(settings._.olddata, checkEdit[a]);
			if (JSON.stringify(newRow) != JSON.stringify(oldRow)) {
				console.log("edit row " + checkEdit[a]);
				var rowId = settings.table.rows.id(newRow, settings);
				var rowElement = $("#" + rowId);
				rowElement.children().off();
				rowElement.children().remove();
				rowElement.append(generateRowCells(newRow));
			}
		}

		if (!settings.data || !settings.data.length) {
			$(table).hide();
			$(nodata).show();
		} else {
			$(table).show();
			$(nodata).hide();
		}
		refreshRowNum();
		settings._.olddata = JSON.parse(JSON.stringify(settings.data));
	}
	var updateProperties = function (settings) {
		$(panel).attr("id", resolve(settings.panel.id, settings));
		$(panel).attr("class", resolve(settings.panel.classes, settings));

		if (settings.panel.header != false) {
			$(panelHeader).attr("id", resolve(settings.panel.header.id, settings));
			$(panelHeader).attr("class", resolve(settings.panel.header.classes, settings));
			$(panelTitle).attr("id", resolve(settings.panel.header.title.id, settings));
			$(panelTitle).attr("class", resolve(settings.panel.header.title.classes, settings));
			$(panelTitle).html(resolve(settings.title, settings));
		}

		$(table).attr("id", resolve(settings.table.id, settings));
		$(table).attr("class", resolve(settings.table.classes, settings));

		$(nodata).attr("id", resolve(settings.panel.nodata.id, settings));
		$(nodata).attr("class", resolve(settings.panel.nodata.classes, settings));
		$(nodata).html(resolve(settings.nodataText, settings));

		if (settings.addButton != false) {
			$(addButton).attr("id", resolve(settings.addButton.id, settings));
			$(addButton).attr("class", resolve(settings.addButton.classes, settings));
			$(addButton).html(resolve(settings.addButton.text, settings));
		}
	};

	var addRow = function (row) {
		console.info("add row");
		if (row) {
			settings.data.push(row);
			update();
		} else {
			showAlert("Row could not be added.");
			console.warn("row was " + row);
		}
	};
	var editRow = function (row) {
		console.info("edit row");
		if (row) {
			update();
		} else {
			showAlert("Row could not be edited.");
			console.warn("row was " + row);
		}
	};
	var removeRow = function (row, skipUpdate) {
		console.info("remove row");
		if (row) {
			for (var i = settings.data.length - 1; i >= 0; i--) {
				if (settings.data[i].Id == row.Id)
					settings.data.splice(i, 1);
			}
			update();
		} else {
			showAlert("Row could not be removed.");
			console.warn("row was " + row);
		}
	};

	var update = function () {
		updateProperties(settings);
		updateRowsUI(settings);
	};

	generateContainer();

	if (settings.container) {
		$(settings.container).append(container);
		update();
	} else {
		console.warn("container was not specified.")
	}

	return {
		update: update,
		settings: settings,

		setData: function (data) {
			settings.data = data;
			update();
		},
		addRow: addRow,
		editRow: editRow,
		removeRow: removeRow,

		container: container,

		//data: settings.data,
	};
}


/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  obj ={                                                                                                                     ///
///      title:,                                                                                                                ///
///      icon : <success,warning,danger,info,primary,default> or {icon:"css icon name",title:"Title Text!",color:"Hex-Color"}   ///
///      fields: [{                                                                                                             ///
///          name:(optional)                                                                                                    ///
///          text:(optional)                                                                                                    ///
///          type: <text,textarea,checkbox,radio,span,div,header,h1,h2,h3,h4,h5,h6,number,date,time,file,yesno,label>(optional) ///
///          value: (optional)                                                                                                  ///
///          placeholder: (optional)                                                                                            ///
///          classes: (optional)																								///
///      },...],																												///
///		 contents: jquery object (optional, overrides fields)																	///
///      pushUrl:"",                                                                                                            ///
///      success:function,                                                                                                      ///
///      complete:function,                                                                                                     ///
///      cancel:function,                                                                                                       ///  
///      reformat: function,																									///
///      noCancel: bool																											///
///  }                                                                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/
function showModal(title, pullUrl, pushUrl, callback, validation, onSuccess, onCancel) {
	$("#modal").modal("hide");
	$("#modal-icon").attr("class", "");
	$("#modal #class-container").attr("class", "");
	$("#modalCancel").removeClass("hidden");

	if (typeof (title) === "object") {
		var obj = title;
		var push = pullUrl;
		var cback = pushUrl;
		return showModalObject(obj, push, cback);
	}

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	$.ajax({
		url: pullUrl,
		type: "GET",
		//Couldnt retrieve modal partial view
		error: function (jqxhr, status, error) {
			$('#modal').modal('hide');
			$("#modal").removeClass("loading");
			$("#modalForm").unbind('submit');
			if (status == "timeout")
				showAlert("The request has timed out. If the problem persists, please contact us.");
			else
				showAlert("Something went wrong. If the problem persists, please contact us.");
		},
		//Retrieved Partial Modal
		success: function (modal) {
			if (!modal) {
				$('#modal').modal('hide');
				$("#modal").removeClass("loading");
				showAlert("Something went wrong. If the problem persists, please contact us.");
				return;
			}
			_bindModal(modal, title, callback, validation, function (formData) {
				_submitModal(formData, pushUrl, onSuccess, null, false);
			});
		}
	});
}
function showModalObject(obj, pushUrl, onSuccess, onCancel) {
	$("#modal").modal("hide");
	$("#modalCancel").toggleClass("hidden", obj.noCancel || false);
	if (typeof (pushUrl) === "undefined")
		pushUrl = obj["push"] || obj["pushUrl"];
	if (typeof (onSuccess) === "undefined")
		onSuccess = obj["success"];
	if (typeof (onSuccess) !== "undefined" && typeof (pushUrl) !== "undefined") {
		var oldSuccess = onSuccess;
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, oldSuccess, obj.complete, true, contentType); };
	}
	if (typeof (onSuccess) === "undefined" && typeof (pushUrl) !== "undefined")
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, null, obj.complete, true, contentType); };

	var onClose = obj.close;

	if (typeof (onCancel) === "undefined")
		onCancel = obj["cancel"];

	if (!obj.fields && obj.pullUrl && obj.title && pushUrl)
		return showModal(obj.title, obj.pullUrl, pushUrl, onSuccess, obj.validation, obj.success);

	if (typeof (obj.title) === "undefined") {
		obj.title = "";
		console.warn("No title supplied");
	}

	obj.modalClass = obj.modalClass || "";

	var reformat = obj.reformat;

	var iconType = typeof (obj.icon);
	if (iconType !== "undefined") {
		obj.modalClass += " modal-icon";
		$("#modal-icon").attr("class", "modal-icon");
		if (iconType === "string") {
			obj.modalClass += " modal-icon-" + obj.icon;
			//obj.title = iconType.toLowerCase() + "!";
		} else if (iconType === "object") {
			var time = +new Date();
			var custom = "modal-icon-custom" + time;
			obj.modalClass += " " + custom;
			if (!obj.icon.icon)
				obj.modalClass += " modal-icon-info";

			var icon = (obj.icon.icon || ("icon-" + custom)).replace(".", "");
			var title = escapeString(obj.icon.title || "Hey!");
			var color = escapeString(obj.icon.color || "#5bc0de");
			$("#modal-icon").addClass(icon);
			icon = icon.replace(" ", ".")
			try {
				document.styleSheets[0].insertRule("." + custom + " ." + icon + ":after{content: '" + title + "' !important;}", 0);
				document.styleSheets[0].insertRule("." + custom + " ." + icon + ":before{ background-color: " + color + ";}", 0);
				document.styleSheets[0].insertRule("." + custom + " #modalOk{ background-color: " + color + ";}", 0);
			} catch (e) {
				console.error(e);
			}
		}

	}

	$("#modal #class-container").attr("class", obj.modalClass);

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	var allowed = ["text", "hidden", "textarea", "checkbox", "radio", "number", "date", "time", "header", "span", "div", "h1", "h2", "h3", "h4", "h5", "h6", "file", "yesno", "label", "img"];
	var addLabel = ["text", "textarea", "checkbox", "radio", "number", "date", "time", "file"];
	var tags = ["span", "h1", "h2", "h3", "h4", "h5", "h6", "label", "div"];
	var anyFields = ""

	if (typeof (obj.field) !== "undefined") {
		if (typeof (obj.fields) !== "undefined") {
			throw "A 'field' and a 'fields' property exists";
		} else {
			obj.fields = obj.field;
		}
	}

	if (typeof (obj.fields) === "object") {
		var allDeep = true;
		for (var f in obj.fields) {
			if (typeof (obj.fields[f]) !== "object") {
				allDeep = false;
				break;
			}
		}
		if (!allDeep) {
			obj.fields = [obj.fields];
		}
	}

	var fieldsTypeIsArray = Array.isArray(obj.fields);//typeof (obj.fields);

	var contentType = null;

	var builder = '<div class="form-horizontal modal-builder">';
	var runAfter = [];
	var genInput = function (type, name, eid, placeholder, value, others, classes) {
		others = others || "";
		if (type == "number")
			others += " step=\"any\"";

		if (type == "checkbox" && ((typeof (value) === "string" && (value.toLowerCase() === 'true')) || (typeof (value) === "boolean" && value)))
			others += "checked";

		return '<input type="' + escapeString(type) + '" class="form-control blend ' + classes + '"' +
                      ' name="' + escapeString(name) + '" id="' + eid + '" ' +
                      placeholder + ' value="' + escapeString(value) + '" ' + others + '/>';
	}

	var defaultLabelColumnClass = obj.labelColumnClass || "col-sm-2";
	var defaultValueColumnClass = obj.valueColumnClass || "col-sm-10";


	if (!obj.contents) {
		for (var f in obj.fields) {
			try {
				var field = obj.fields[f];
				var name = field.name || f;
				var label = typeof (field.text) !== "undefined" || !fieldsTypeIsArray;
				var text = field.text || name;
				var originalValue = field.value;
				var value = field.value || "";
				var placeholder = field.placeholder;
				var type = (field.type || "text").toLowerCase();
				var classes = field.classes || "";
				var onchange = field.onchange;
				var eid = escapeString(name);

				var labelColumnClass = field.labelColumnClass || defaultLabelColumnClass;
				var valueColumnClass = field.valueColumnClass || defaultValueColumnClass;

				if (typeof (classes) === "string" && (classes.indexOf('\'') != -1 || classes.indexOf('\"') != -1))
					throw "Classes cannot contain a quote character.";


				if (type == "header")
					type = "h4";

				if (typeof (placeholder) !== "undefined")
					placeholder = "placeholder='" + escapeString(placeholder) + "'";
				else
					placeholder = "";
				var input = "";
				var inputIndex = allowed.indexOf(type);
				if (inputIndex == -1) {
					console.warn("Input type not allowed:" + type);
					continue;
				}
				if (Object.prototype.toString.call(value) === '[object Date]' && type == "date") {
					value = value.toISOString().substring(0, 10);
				}

				if (type == "file")
					contentType = 'enctype="multipart/form-data"';

				if (tags.indexOf(type) != -1) {
					var txt = value || text;
					input = "<" + type + " name=" + escapeString(name) + '" id="' + eid + '" class="' + classes + '">' + txt + '</' + type + '>';
				} else if (type == "textarea") {
					input = '<textarea class="form-control blend verticalOnly ' + classes + '" rows=5 name="' + escapeString(name) + '" id="' + eid + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
				} else if (type == "date") {
					var guid = generateGuid();
					var curName = name;
					var curVal = originalValue;
					input = '<div class="date-container date-' + guid + ' ' + classes + '" id="'+ eid+'"></div>';
					runAfter.push(function () {
						generateDatepicker('.date-' + guid, curVal, curName, eid);
					});
				} else if (type == "yesno") {
					var selectedYes = (value == true) ? 'checked="checked"' : "";
					var selectedNo = (value == true) ? "" : 'checked="checked"';
					input = '<div class="form-group input-yesno ' + classes + '">' +
								'<label for="true" class="col-xs-4 control-label"> Yes </label>' +
								'<div class="col-xs-2">' + genInput("radio", name,eid, placeholder, "true", selectedYes) + '</div>' +
								'<label for="false" class="col-xs-1 control-label"> No </label>' +
								'<div class="col-xs-2">' + genInput("radio", name,eid, placeholder, "false", selectedNo) + '</div>' +
							'</div>';
				} else if (type == "img") {
					input = "<img src='" + field.src + "' class='" + classes + "'/>";
				} else {
					input = genInput(type, name,eid, placeholder, value, null, classes);
				}

				if (addLabel.indexOf(type) != -1 && label) {
					builder += '<div class="form-group"><label for="' + name + '" class="' + labelColumnClass + ' control-label">' + text + '</label><div class="' + valueColumnClass + '">' + input + '</div></div>';
				} else {
					builder += input;
				}

				if (onchange) {
					if (typeof (onchange) === "function") {
						var ocf = onchange;
						var mname = name;
						runAfter.push(function () {
							$("[name=" + mname).on("change", ocf);
						});
					} else {
						console.warn("Unhandled onchange type:"+typeof(onchange) + " for "+eid );
					}
				}

			} catch (e) {
				console.error(e);
			}
		}
		builder += "</div>";
	} else {
		builder = $(obj.contents);
	}
	_bindModal(builder, obj.title, undefined, undefined, onSuccess, onCancel, reformat, onClose, contentType);
	setTimeout(function () {
		for (var i = 0; i < runAfter.length; i++) {
			runAfter[i]();
		}
	}, 1);
}

function _bindModal(html, title, callback, validation, onSuccess, onCancel, reformat, onClose, contentType) {
	$('#modalBody').html("");
	setTimeout(function () {
		$('#modalBody').append(html);
	}, 0);

	$("#modalTitle").html(title);
	$("#modal").removeClass("loading");
	//Reregister submit button
	$("#modalForm").unbind('submit');

	var onCloseArg = onClose;
	var onCancelArg = onCancel;
	var onSuccessArg = onSuccess;
	var contentTypeArg = contentType;
	var validationArg = validation;
	var reformatArg = reformat;
	var callbackArg = callback;

	var dur = 1;
	if ($("#modalBody :focusable").first().is("select"))
		dur = 360;

	setTimeout(function () { $("#modalBody :focusable").first().focus(); }, dur);
	//$("#modalForm input:visible,#modalForm textarea:visible,#modalForm button:not(.close):visible").first().focus();

	$("#modalForm").submit(function (ev) {
		ev.preventDefault();

		var formData = $("#modalForm").serializeObject();
		$("#modalForm").find("input:checkbox").each(function () {
			formData[$(this).prop("name")] = $(this).is(":checked") ? "True" : "False";
		});
		$("#modalForm").find(".input-yesno").each(function () {
			var name = $(this).find("input").attr("name");
			var v = $(this).find("[name=" + name + "]:checked").val();
			formData[name] = v=="true" ? "True" : "False";
		});

		if (typeof (reformatArg) === "function") {
			var o = reformatArg(formData);
			if (typeof (o) !== "undefined" && o != null)
				formData = o;//Data was returned, otherwise formdata was manipulated
		}

		if (validationArg) {
			var message = undefined;
			if (typeof (validationArg) === "string") {
				message = eval(validationArg + '()');
			} else if (typeof (validationArg) === "function") {
				message = validationArg();
			}
			if (message !== undefined && message != true) {
				if (message == false) {
					$("#modalMessage").html("Error");
				}
				else {
					$("#modalMessage").html(message);
				}
				$("#modalMessage").removeClass("hidden");
				return;
			}
		}
		$("#modal").modal("hide");
		$("#modal").removeClass("loading");
		//onSuccess(formData);

		if (onSuccessArg) {
			if (typeof onSuccessArg === "string") {
				eval(onSuccessArg + "(formData," + contentTypeArg + ")");
			} else if (typeof onSuccessArg === "function") {
				onSuccessArg(formData, contentTypeArg);
			}
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				eval(onCloseArg + "()");
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal button[data-dismiss='modal']").unbind('click.radialModal');


	$("#modal button[data-dismiss='modal']").on("click.radialModal", function () {
		if (typeof onCancelArg === "string") {
			eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (typeof onCancelArg === "string") {
			eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				eval(onCloseArg + "()");
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal").removeClass("loading");
	$('#modal').modal('show');
	var count = 0;
	setTimeout(function () {
		if (callbackArg) {
			if (typeof (callbackArg) === "string")
				eval(callbackArg + '()');
			else if (typeof (callbackArg) === "function")
				callbackArg();
		} else {
			//$('#modal input:not([type=hidden]):not(.disable):first').focus();
		}
	}, 50);
}

function _submitModal(formData, pushUrl, onSuccess, onComplete, useJson, contentType) {
	///FORM DATA IS NOT USED
	///TODO use form data;
	var serialized
	//var serialized = $.param(formData);
	//var contentType = null;

	if (typeof (contentType) === "undefined")
		contentType = null;
	var processData = null;
	if (useJson && contentType == null) {
		serialized = JSON.stringify(formData);
		contentType = "application/json; charset=utf-8";
	} else if (contentType == 'enctype="multipart/form-data"') {
		serialized = new FormData($('#modalForm')[0]);
		processData = false;
		contentType = false;
	} else {
		serialized = $("#modalForm").serialize();
		contentType = contentType || "application/x-www-form-urlencoded";
	}
	var onSuccessArg = onSuccess;
	var onCompleteArg = onComplete;

	$.ajax({
		url: pushUrl,
		type: "POST",
		contentType: contentType,
		data: serialized,// JSON.stringify(formData),
		processData: processData,
		success: function (data, status, jqxhr) {
			if (!data) {
				$("#modal").modal("hide");
				$("#modal").removeClass("loading");
				showAlert("Something went wrong. If the problem persists, please contact us.");
			} else {
				if (onSuccessArg) {
					if (typeof onSuccessArg === "string") {
						eval(onSuccessArg + "(data,formData)");
					} else if (typeof onSuccessArg === "function") {
						onSuccessArg(data, formData);
					}
				} else {
				}
			}
		},
		complete: function (dd) {
			if (dd) {
				var data = dd.responseJSON;
				if (data) {
					if (onCompleteArg) {
						if (typeof onCompleteArg === "string") {
							eval(onCompleteArg + "(data,formData)");
						} else if (typeof onCompleteArg === "function") {
							onCompleteArg(data, formData);
						}
					}
				}
			}
		},
		error: function (jqxhr, status, error) {
			if (error == "timeout") {
				showAlert("The request has timed out. If the problem persists, please contact us.");
			} else {
				showAlert("Something went wrong. If the problem persists, please contact us.");
			}
			$("#modal").modal("hide");
			$("#modal").removeClass("loading");
		}
	});
}
function UnstoreJsonAlert() {
	var data = localStorage.getItem("Alert");
	localStorage.setItem("Alert", null);

	var alert = JSON.parse(data);
	if (alert !== undefined && alert != null && alert != "null") {
		clearAlerts();
		var type = alert.type;
		var title = alert.title;
		var message = alert.message;
		if (type === undefined) type = "alert-success";
		if (title === undefined) title = "Success!";
		if (message === undefined) message = "";
		showAlert(message, type, title);
	}
}

function StoreJsonAlert(json) {
	var alert = new Object();
	alert.message = json.Message;
	if (!json.MessageType)
		json.MessageType = "danger";
	alert.type = "alert-" + json.MessageType.toLowerCase();
	alert.title = json.Heading;
	localStorage.setItem("Alert", JSON.stringify(alert));
}

function showHtmlErrorAlert(html, defaultMessage) {
	var message = defaultMessage;
	debugger;
	if (typeof (html) === "object" && typeof (html.responseText) === "string") {
		var ele = $($(html.responseText)[1]);
		if (ele.is("title")) {
			message = ele.text();
		}
	} else if (typeof (html) === "string") {
		message = $(html).text();
	}

	if (typeof (message) === "undefined" || message == null || message == "") {
		message = "An error occurred.";
	}
	showAlert(message);
}

function showAlert(message, alertType, preface, duration) {
	if (typeof (alertType) === "number" && typeof (preface) === "undefined" && typeof (duration) === "undefined")
		duration = alertType;
	else if (typeof (preface) === "number" && typeof (duration) === "undefined")
		duration = preface;


	if (alertType === undefined)
		alertType = "alert-danger";
	if (preface === undefined)
		preface = "Warning!";
	if (Object.prototype.toString.call(message) === '[object Array]') {
		if (message.length > 1) {
			var msg = "<ul style='margin-bottom:0px;'>";
			for (var i in message) {
				msg += "<li>" + message[i] + "</li>"
			}
			message = msg + "</ul>"
		} else {
			message = message.join("");
		}
	}

	var alert = $("<div class=\"alert " + alertType + " alert-dismissable start\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + preface + "</strong> <span class=\"message\">" + message + "</span></div>");
	$("#alerts").prepend(alert);
	setTimeout(function () { alert.removeClass("start"); }, 1);

	if (typeof (duration) !== "number") {
		duration = 3000;
	}
	setTimeout(function () {
		$(alert).remove();
	}, duration);
}

var alertsTimer = null;
function clearAlerts() {
	var found = $("#alerts .alert").remove();
	/*found.css({ height: "0px", opacity: 0.0, padding: "0px", border: "0px", margin: "0px" });
    if (alertsTimer) {
        clearTimeout(alertsTimer);
    }
    alertsTimer = setTimeout(function () {
        found.remove();
    }, 1000);*/
}

function showAngularError(d, status, headers, config, statusTxt) {
	if (typeof (d) === "undefined") {
		showJsonAlert();
		return;
	}
	if (typeof (d.Message) !== "undefined" && d.Message != null) {
		showJsonAlert(d);
	} else if (typeof (d.data) !== "undefined" && d.data != null) {
		showJsonAlert(d.data);
	} else {
		if (typeof (d.statusText) !== "undefined" && d.statusText !== "") {
			showAlert(d.statusText);
		} else {
			showJsonAlert();
		}
	}
}

function showJsonAlert(data, showSuccess, clearOthers) {
	try {
		if (clearOthers) {
			clearAlerts();
		}
		var stdError = "Something went wrong.";

		if (!data) {
			showAlert(stdError);
		} else if (typeof (data) === "string") {
			if (data.trim().length < 300)
				showAlert(data.trim(), "alert-danger", "Error");
			else
				showAlert(stdError);
		} else {
			var showDetails = typeof (data.NoErrorReport) === "undefined" || !data.NoErrorReport;
			var message = data.Message;
			if (message === undefined)
				message = "";
			if (data.Trace && showDetails) {
				console.error(data.TraceMessage);
				console.error(data.Trace);
			}
			console.log(data.Message);
			if (!data.Silent && (data.MessageType !== undefined && data.MessageType != "Success" || showSuccess)) {
				var mType = data.MessageType || "danger";
				showAlert(message, "alert-" + mType.toLowerCase(), data.Heading);
			}
			if (data.Error) {
				if (showDetails) {
					debugger;
					sendErrorReport();
				}
			}

		}
	} catch (e) {
		console.error(e);
	}
	if (!data)
		return false;
	return !data.Error;
}

function getKeySelector(selector, prefix) {
	prefix = prefix || "";
	var output = { selector: selector, key: false };

	if ($(selector).data("key")) {
		output.key = prefix + $(selector).data("key");
	} else if ($(selector).attr("name")) {
		output.key = prefix + $(selector).attr("name");
		output.selector = "[name=" + $(selector).attr("name") + "]";
		/*if ($(selector).is("[type='radio']")) {
            output.selector += ":checked";
        }*/
	} else if ($(selector).attr("id")) {
		output.key = prefix + $(selector).attr("id");
		output.selector = "#" + $(selector).attr("id");
	}

	return output;
}

function getVal(selector) {
	var self = $(selector);
	if (self.is("[type='checkbox']")) {
		return self.is(':checked');
	}
	if (self.is("[type='radio']")) {
		return self.filter(":checked").val();
	}
	else if (self.hasClass("panel-collapse")) {
		return self.hasClass("in");
	} else {
		return self.val();
	}
}

function setVal(selector, val) {
	var self = $(selector);
	if (self.is("[type='checkbox']")) {
		self.prop('checked', val == "true");
	} else if (self.is("[type='radio']")) {
		self.prop('checked', function () {
			return $(this).attr("value") == val;
		});
	} else if (self.hasClass("panel-collapse")) {
		self.collapse(val == "true" ? "show" : "hide");
	} else {
		self.val(val);
	}
	self.change();
}

function getInitials(name, initials) {
	if (typeof (name) === "undefined" || name == null) {
		name = "";
	}

	if (typeof (initials) === "undefined") {
		var m = name.match(/\b\w/g) || [];
		var arr = [];
		if (m.length > 0)
			arr.push(m[0]);
		if (m.length > 1)
			arr.push(m[1]);
		initials = arr.join(' ');
	}
	return initials;
}

function profilePicture(url, name, initials) {
	var picture = "";
	var hash = 0;
	if (typeof (name) !== "string") {
		name = "";
	}
	if (name.length != 0) {
		for (var i = 0; i < name.length; i++) {
			{
				var chr = name.charCodeAt(i);
				hash = ((hash << 5) - hash) + chr;
				hash |= 0; // Convert to 32bit integer
			}
		}
		//console.log(name + ": " + hash + " = " + Math.abs(hash) % 360);
		hash = Math.abs(hash) % 360;
	}
	if (url !== "/i/userplaceholder" && url !== null) {
		picture = "<span class='picture' style='background: url(" + url + ") no-repeat center center;'></span>";
	} else {
		if (name == "")
			name = "n/a";

		initials = getInitials(name, initials).toUpperCase();
		picture = "<span class='picture' style='background-color:hsla(" + hash + ", 36%, 49%, 1);color:hsla(" + hash + ", 36%, 72%, 1)'><span class='initials'>" + initials + "</span></span>";
	}

	return "<span class='profile-picture'>" +
		      "<span class='picture-container' title='" + escapeString(name) + "'>" +
					picture +
			  "</span>" +
		   "</span>";
}



(function ($) {
	$.fn.setCursorToTextEnd = function () {
		var $initialVal = this.val();
		this.val($initialVal);
	};

	$(".panel-collapse").collapse({
		toggle: false
	});

	$(".autoheight").each(function (index) {
		var maxHeight = 0;
		$(this).children().each(function () {
			maxHeight = Math.max(maxHeight, $(this).height());
		});
		$(this).height(maxHeight);
	});

	var scrollTopModal = 0;

	$("#modalForm").on("show.bs.modal", function () {
		scrollTopModal = $("body").scrollTop();
	}).on("hidden.bs.modal", function () {
		setTimeout(function () { $("body").scrollTop(scrollTopModal); }, 1);
	}).on("shown.bs.modal", function () {
		setTimeout(function () { $("body").scrollTop(scrollTopModal); }, 1000);
	});


})(jQuery);

$.fn.flash = function (ms, backgroundColor, borderColor, color) {
	ms = ms || 1000;
	color = color || '#3C763D';
	borderColor = borderColor || '#D6E9C6';
	backgroundColor = backgroundColor || '#DFF0D8';


	var originalBackgroundColor = this.css('background-color');
	var originalBorderColor = this.css('border-color');
	var originalBoxColor = this.css('boxShadow');
	var originalColor = this.css('color');

	this.css({ 'border-color': borderColor, 'background-color': backgroundColor, "boxShadow": "0px 0px 5px 3px " + borderColor, "color": color })
    .animate({ 'border-color': originalBorderColor, 'background-color': originalBackgroundColor, "boxShadow": "0px 0px 0px 0px " + borderColor, "color": originalColor }, ms, function () {
    	$(this).css("boxShadow", originalBoxColor);
    	$(this).css("background-color", "");
    	$(this).css("border-color", "");
    	$(this).css("color", "");
    	$(this).css("boxShadow", "");
    });


};
/////////////////////////////////////////////////////////////////
//Ajax Interceptors

var interceptAjax = function (event, request, settings) {
	//console.log(event);
	//console.log(settings);
	try {
		var result = $.parseJSON(request.responseText);
		try {
			if (result.Refresh) {
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					StoreJsonAlert(result);
				}
				location.reload();
			} else if (result.Redirect) {
				var url = result.Redirect;
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					result.Redirect = false;
					StoreJsonAlert(result);
				}
				window.location.href = url;
			} else {
				if (result.Silent !== undefined && !result.Silent) {
					showJsonAlert(result, true, true);
				}
			}
		} catch (e) {
			console.log(e);
		}
	} catch (e) {
	}
};

$(document).ajaxSuccess(interceptAjax);
$(document).ajaxError(interceptAjax);



$(document).ajaxSend(function (event, jqX, ajaxOptions) {
	if (ajaxOptions.url == null) {
		ajaxOptions.url = "";
	}



	if (typeof (ajaxOptions.data) === "string" && ajaxOptions.data.indexOf("_clientTimestamp") != -1) {
		return;
		/*var start = ajaxOptions.data.indexOf("_clientTimestamp")+17;
        debugger;
        date = ajaxOptions.data.substr(start).split("&")[0];*/
	}

	//var date = (new Date().getTime());


	if (ajaxOptions.url.indexOf("_clientTimestamp") == -1) {
		if (!window.tzoffset) {
			var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
			window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
                         (jul.getTime() % 24 * 60 * 60 * 1000)
                         ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
		}
		if (ajaxOptions.url.indexOf("?") == -1)
			ajaxOptions.url += "?";
		else
			ajaxOptions.url += "&";

		ajaxOptions.url += "_clientTimestamp=" + ((+new Date()) + (window.tzoffset * 60 * 1000));
	}
	console.info(ajaxOptions.type + " " + ajaxOptions.url);
	if (typeof (ajaxOptions.type) === "string" && ajaxOptions.type.toUpperCase() == "POST" && !(ajaxOptions.url.indexOf("/support/email") == 0)) {
		//debugger;
		console.info(ajaxOptions.data);
	}
});
/////////////////////////////////////////////////////////////////

(function ($) {
	$.fn.focusTextToEnd = function () {
		this.focus();
		var $thisVal = this.val();
		this.val('').val($thisVal);
		return this;
	};
	$.fn.insertAt = function (elements, index) {
		var children = this.children();
		if (index >= children.size()) {
			this.append(elements);
			return this;
		}
		var before = children.eq(index);
		$(elements).insertBefore(before);
		return this;
	};
}(jQuery));


function dateFormatter(date) {
	/*if(Date.parse('2/6/2009')=== 1233896400000){
        return [date.getMonth()+1, date.getDate(), date.getFullYear()].join('/');
    }*/
	return [date.getMonth() + 1, date.getDate(), date.getFullYear()].join('/');
}

$(function () {
	$(window).bind('beforeunload', function () {
		if (document.activeElement) $(document.activeElement).blur();

	});

	$('.navbar-collapse .dropdown-menu').parent().on('hidden.bs.dropdown', function () {
		console.info("collapse", this);
		$('.navbar-collapse').collapse('hide');
	});

	$(".footer-bar-container").each(function () {
		var h = parseInt($(this).attr("data-height"));
		$(this).find(".footer-bar-contents").css("bottom",/*-h+*/"0px");
		$(this).css("bottom", -h + "px");
		$(this).find(".footer-bar-contents").css("height", h + "px");
	});

	$("body").on("click", ".footer-bar-tab .clicker", function () {
		var tab = $(this).parent(".footer-bar-tab");
		var on = !$(tab).hasClass("shifted");
		$(tab).toggleClass("shifted", on);
		var parent = $(tab).parent(".footer-bar-container");
		parent.toggleClass("shifted", on);
		parent.find(".footer-bar-contents").toggleClass("shifted", on);

		var curHeight = 0;
		$(".footer-bar-container").each(function () {
			var isShift = $(this).hasClass("shifted");
			var selfH = parseInt($(this).attr("data-height"));
			if (isShift) {
				curHeight += selfH;
			}
			$(this).css("bottom", -(-curHeight + selfH) + "px");
		});

		$(".body-full-width #main").css("padding-bottom", Math.max(20, curHeight) + "px");

		$(window).trigger("footer-resize", on);
	});

	$('.picture').each(function () {
		var picture = $(this);
		var bg = $(picture).css('background-image');
		if (bg && bg != "none") {
			$(picture).fadeToggle();
			var src = bg.replace(/(^url\()|(\)$|[\"\'])/g, '');
			var $img = $('<img>').attr('src', src).on('load', function () {
				// do something, maybe:
				$(picture).fadeIn();
			});
		}
	});
});


$(document).ready(function () {
	var event = new CustomEvent("jquery-loaded", {});
	document.dispatchEvent(event);
});

$(function () {
	//Adds links to hash nav buttons
	var updateFromHash = function () {
		var hash = window.location.hash;
		hash && $('ul.nav a[href="' + hash + '"]').tab('show');
	};
	updateFromHash();
	window.addEventListener("hashchange", updateFromHash, false);

	$(document).on("click", '.nav a', function (e) {
		$(this).tab('show');
		var scrollmem = $('body').scrollTop();
		window.location.hash = this.hash;
		$('html,body').scrollTop(scrollmem);
	});
});


$(function () {
	$(document).on("click", ".undoable .undo-button", function () {
		var undoable = $(this).closest(".undoable");
		var url = undoable.data("undo-url");
		var action = undoable.data("undo-action");
		if (typeof (url) !== "undefined") {
			$.ajax({
				url: url,
				success: function (data) {
					if (showJsonAlert(data)) {
						if (("" + action).indexOf("unclass") != -1) {
							$(undoable).removeClass("undoable");
						}
						if (("" + action).indexOf("remove") != -1) {
							$(undoable).remove();
						}
						if (("" + action).indexOf("unhide") != -1) {
							$(undoable).show();
						}
					}
				}
			});
		} else {
			showAlert("No action for undoable.", "Error!");
		}
	});
});


window.addEventListener("submit", function (e) {
	var form = e.target;
	if (form.getAttribute("enctype") === "multipart/form-data") {
		if (form.dataset.ajax) {
			e.preventDefault();
			e.stopImmediatePropagation();
			var xhr = new XMLHttpRequest();
			xhr.open(form.method, form.action);
			xhr.onreadystatechange = function () {
				if (xhr.readyState == 4 && xhr.status == 200) {
					if (form.dataset.ajaxUpdate) {
						var updateTarget = document.querySelector(form.dataset.ajaxUpdate);
						if (updateTarget) {
							updateTarget.innerHTML = xhr.responseText;
						}
					}
				}
			};
			xhr.send(new FormData(form));
		}
	}
}, true);

function debounce(func, wait, immediate) {
	var timeout;
	return function () {
		var context = this, args = arguments;
		var later = function () {
			timeout = null;
			if (!immediate) func.apply(context, args);
		};
		var callNow = immediate && !timeout;
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
		if (callNow) func.apply(context, args);
	};
};

Constants = {
	StartHubSettings: { transport: ['webSockets', 'longPolling'] }
};


function sendErrorReport() {
	try {
		console.log("Sending Error Report...");
		var message = "[";
		var mArray = [];
		for (var i in consoleStore) {
			mArray.push(JSON.stringify(consoleStore[i]));
		}
		message = "[" + mArray.join(",\n") + "]";
		function _send() {
			data = {};
			data.Console = message;
			data.Url = window.location.href;
			data.User = window.UserId;
			data.Org = window.OrgId;
			data.PageTitle = window.Title;
			data.Status = "JavascriptError";
			data.Subject = "Javascript Error - " + data.PageTitle;
			if (image != null) {
				data.ImageData = image;
			}
			$.ajax({
				method: "POST",
				url: "/support/email",
				data: data,
				success: function (d) {
					console.log("Report was sent.");
				},
				error: function (a, b, c) {
					console.error("Error sending report:");
					console.error(b, c);
				}
			});
		}
		try {
			$.getScript("/Scripts/home/screenshot.js").done(function () {
				try {
					console.log("...begin render");
					screenshotPage(function (res) {
						image = res;
						console.log("...end render");
						_send();
					});
				} catch (e) {
					_send();
				}
			}).error(function () {
				_send();
			});
		} catch (e) {
			_send();
		}
	} catch (e) {
		console.error("Error sending report:");
		console.error(e);
	}

}

function supportEmail(title, nil, defaultSubject, defaultBody) {
	var message = "[";
	var mArray = [];
	for (var i in consoleStore) {
		mArray.push(JSON.stringify(consoleStore[i]));
	}
	message = "[" + mArray.join(",\n") + "]";
	var fields = [
            { name: "Subject", text: "Subject", type: "text", value: defaultSubject },
            { name: "Body", text: "Body", type: "textarea", value: defaultBody }
	];

	if (typeof (window.UserId) === "undefined" || window.UserId == -1)
		fields.push({ name: "Email", text: "Email", type: "text",placeholder:"Your e-mail here" });

	var image = null;
	var show = function () {
		showModal({
			title: "How can we help you?",
			icon: "default",//{ icon: "modal-icon-default", title: "Contact Support", color: "#ef7622" },
			fields: fields,
			pushUrl: "/support/email",
			reformat: function (data) {
				data.Console = message;
				data.Url = window.location.href;
				data.User = window.UserId;
				data.Org = window.OrgId;
				data.PageTitle = title;

				if (image != null) {
					data.ImageData = image;
				}
			}
		});
	};
	try {
		$.getScript("/Scripts/home/screenshot.js").done(function () {
			try {
				console.log("begin render");
				screenshotPage(function (res) {
					image = res;
					console.log("end render");
				});
				show();
			} catch (e) {
				show();
			}
		}).error(function () {
			show();
		});
	} catch (e) {
		show();
	}

}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Create issues or todos or headlines
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
function imageListFormat(state) {
	if (!state.id) {
		return state.text;
	}
	var $state = $('<span><img style="max-width:32;max-height:32px"  src="' + $(state.element).data("img") + '" class="img-flag" /> ' + state.text + '</span>');
	return $state;
};

function getDataValues(self) {
	var data = {};
	[].forEach.call(self.attributes, function (attr) {
		if (/^data-/.test(attr.name)) {
			var camelCaseName = attr.name.substr(5).replace(/-(.)/g, function ($0, $1) {
				return $1.toUpperCase();
			});
			data[camelCaseName] = attr.value;
		}
	});
	return data;
}

$("body").on("click", ".issuesModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add an issue";
	showModal(title, "/Issues/" + m + "?" + parm, "/Issues/" + m);
});
$("body").on("click", ".todoModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a to-do";
	showModal(title, "/Todo/" + m + "?" + parm, "/Todo/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		if (found.length && found.val() == null)
			return "You must select at least one to-do owner.";
		return true;
	});
});
$("body").on("click", ".headlineModal:not(.disabled)", function () {
	var dat = getDataValues(this);
	var parm = $.param(dat);
	var m = dat.method;
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a people headline";
	showModal(title, "/Headlines/" + m + "?" + parm, "/Headlines/" + m, null, function () {
		var found = $('#modalBody').find(".select-user");
		//if (found.length && found.val() == null)
		//	return "You must select at least one to-do owner.";
		return true;
	});
});


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
function getParameterByName(name, url) {
	if (!url) url = window.location.href;
	name = name.replace(/[\[\]]/g, "\\$&");
	var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
	if (!results) return null;
	if (!results[2]) return '';
	return decodeURIComponent(results[2].replace(/\+/g, " "));
}


jQuery.cachedScript = function (url, options) {
	options = $.extend(options || {}, {
		dataType: "script",
		cache: true,
		url: url
	});
	return jQuery.ajax(options);
};

var tname = "";
var tmethod = ""
function startTour(name, method) {
	if (typeof (method) === "undefined" || method == null || (typeof (method) === "string" && method.trim() == ""))
		method = "start";
	$.getScript("/Scripts/Tour/lib/anno.js").done(function () {
		//ensureLoaded("/Scripts/Tour/lib/jquery.scrollintoview.min.js");
		//ensureLoaded("/Content/Tour/lib/anno.css");
		if (typeof (Tours) === "undefined") {
			Tours = {
				NextButton: function () {
					return {
						text: "Next",
						click: function (a, e) {
							var anno = a;
							$(anno.target).click();
							e.preventDefault();
						}
					};
				},
				clickToAdvance: function (anno) {

					var existingShow = anno.onShow;
					var existingHide = anno.onHide;

					anno.onShow = function (anno, $target, $annoElem) {
						var an = anno;
						if (typeof (existingShow) !== "undefined")
							existingShow(anno, $target, $annoElem);
						var handler = function (e) {
							console.log("c2a handler");
							if (anno._chainNext != null) {
								waitUntil(function () { return $(anno._chainNext.target).length > 0; }, function () {
									setTimeout(function () {
										if (typeof (anno.action) === "function")
											anno.action();
										an.switchToChainNext();
									}, 250);
								}, function () {
									showAlert("Could not load tour.");
								});
							} else {
								if (typeof (anno.action) === "function")
									anno.action();
								an.switchToChainNext();
							}
						}

						$target[0].addEventListener('click', handler, true) // `true` is essential                   
						return handler
					};
					anno.onHide = function (anno, $target, $annoElem, handler) {
						if (typeof (existingHide) !== "undefined")
							existingHide(anno, $target, $annoElem);
						if ($target.length > 0) {
							$target[0].removeEventListener('click', handler, true);
						}
					}
					return anno;
				},
				appendParams: function (anno, selector, tourName, tourMethod) {
					var existingShow = anno.onShow;
					var existingHide = anno.onHide;

					anno.onShow = function (anno, $target, $annoElem) {
						if (typeof (existingShow) !== "undefined")
							existingShow(anno, $target, $annoElem);
						tname = tourName;
						tmethod = tourMethod;
						var handler = function (e) {
							if (typeof (e.target.href) !== "undefined" && $(e.target).is(selector)) {
								e.preventDefault();
								if (e.target.href.indexOf("?") != -1) window.location.href = e.target.href + '&tname=' + tourName + "&tmethod=" + tourMethod;
								else window.location.href = e.target.href + '?tname=' + tourName + "&tmethod=" + tourMethod;
							}
							if (typeof (e.target.onclick) !== "undefined" && $(e.target).is(selector)) {
								e.preventDefault();
								var str = "" + e.target.onclick;
								var findQ = "'";
								var idx = str.indexOf("location.href='");
								if (idx == -1) {
									idx = str.indexOf('location.href="');
									var findQ = '"';
								}
								if (idx != -1) {
									idx += 14;
									var endQ = str.indexOf(findQ, idx + 1);
									var query = (str.substr(idx, endQ - idx).indexOf("?") == -1) ? "?" : "&";
									str = str.substr(0, endQ) + query + 'tname=' + tourName + "&tmethod=" + tourMethod + str.substr(endQ);
									e.target.onclick = eval(str);
								}
							}
						}
						$target[0].addEventListener('click', handler, true) // `true` is essential
						return handler
					};

					anno.onHide = function (anno, $target, $annoElem, handler) {
						if (typeof (existingHide) !== "undefined")
							existingHide(anno, $target, $annoElem, handler);
						$target[0].removeEventListener('click', handler, true);
						tname = "";
						tmethod = "";
					}
					return anno;
				}
			}
		};
		try {
			Anno.prototype.overlayClick = function () {
				console.log("overlay clicked");
			};
			$.getScript("/Scripts/Tour/" + name + ".js").done(function () {
				//debugger;
				Tours[name][method]();
			}, function () {
				//debugger;
				// showAlert("Something went wrong.");
			});
		} catch (e) {
			showAlert("Tour could not be loaded.");
		}
	});
}

function shouldBeginTour() {
	var tourName = getParameterByName("tname");
	if (typeof (tourName) !== "undefined" && tourName != null && (typeof (tourName) === "string" && tourName.trim() != "")) {
		startTour(tourName, getParameterByName("tmethod"));
	}
}
shouldBeginTour();


/**
 * @brief Wait for something to be ready before triggering a timeout
 * @param {callback} isready Function which returns true when the thing we're waiting for has happened
 * @param {callback} success Function to call when the thing is ready
 * @param {callback} error Function to call if we time out before the event becomes ready
 * @param {int} count Number of times to retry the timeout (default 300 or 6s)
 * @param {int} interval Number of milliseconds to wait between attempts (default 20ms)
// */
function waitUntil(isready, success, error, count, interval) {
	if (count === undefined) {
		count = 300;
	}
	if (interval === undefined) {
		interval = 20;
	}
	if (isready()) {
		success();
		return;
	}
	// The call back isn't ready. We need to wait for it
	setTimeout(function () {
		if (!count) {
			// We have run out of retries
			if (error !== undefined) {
				error();
			}
		} else {
			// Try again
			waitUntil(isready, success, error, count - 1, interval);
		}
	}, interval);
}

function waitUntilVisible(selector, onVisible) {
	waitUntil(function () {
		return $(selector).is(":visible");
	}, onVisible, function () { }, 60, 50);
}


//function msieversion() {
//	debugger;
//	var rv = false; // Return value assumes failure.
//	if (navigator.appName == 'Microsoft Internet Explorer') {
//		var ua = navigator.userAgent,
//			re = new RegExp("MSIE ([0-9]{1,}[\\.0-9]{0,})");
//		if (re.exec(ua) !== null) {
//			rv = parseFloat(RegExp.$1);
//		}
//	} else if (navigator.appName == "Netscape") {
//		/// in IE 11 the navigator.appVersion says 'trident'
//		/// in Edge the navigator.appVersion does not say trident
//		if (navigator.appVersion.indexOf('Trident') === -1) rv = 12;
//		else rv = 11;
//	}
//	return rv;
//}

function isIOS() {
	var iOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
	return iOS;
}

if (isIOS()) {

	function setTextareaPointerEvents(value) {
		var nodes = document.getElementsByClassName('scrollOver');
		for (var i = 0; i < nodes.length; i++) {
			nodes[i].style.pointerEvents = value;
		}
	}

	document.addEventListener('DOMContentLoaded', function () {
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchstart', function () {
		setTextareaPointerEvents('auto');
	});

	document.addEventListener('touchmove', function () {
		e.preventDefault();
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchend', function () {
		setTimeout(function () {
			setTextareaPointerEvents('none');
		}, 0);
	});
}

function msieversion() {
	var ua = window.navigator.userAgent;

	var msie = ua.indexOf('MSIE ');
	if (msie > 0) {
		// IE 10 or older => return version number
		return parseInt(ua.substring(msie + 5, ua.indexOf('.', msie)), 10);
	}

	var trident = ua.indexOf('Trident/');
	if (trident > 0) {
		// IE 11 => return version number
		var rv = ua.indexOf('rv:');
		return parseInt(ua.substring(rv + 3, ua.indexOf('.', rv)), 10);
	}

	var edge = ua.indexOf('Edge/');
	if (edge > 0) {
		// Edge (IE 12+) => return version number
		return parseInt(ua.substring(edge + 5, ua.indexOf('.', edge)), 10);
	}

	// other browser
	return false;
}


//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);


///POLYFILLS
Error.captureStackTrace = Error.captureStackTrace || function (obj) {
	if (Error.prepareStackTrace) {
		var frame = {
			isEval: function () { return false; },
			getFileName: function () { return "filename"; },
			getLineNumber: function () { return 1; },
			getColumnNumber: function () { return 1; },
			getFunctionName: function () { return "functionName" }
		};

		obj.stack = Error.prepareStackTrace(obj, [frame, frame, frame]);
	} else {
		obj.stack = obj.stack || obj.name || "Error";
	}
};




//tabbable.js
//https://github.com/marklagendijk/jquery.tabbable/blob/master/jquery.tabbable.min.js
!function (e) { "use strict"; function t(t) { var n = e(t), a = e(":focus"), r = 0; if (1 === a.length) { var i = n.index(a); i + 1 < n.length && (r = i + 1) } n.eq(r).focus() } function n(t) { var n = e(t), a = e(":focus"), r = n.length - 1; if (1 === a.length) { var i = n.index(a); i > 0 && (r = i - 1) } n.eq(r).focus() } function a(t) { function n(t) { return e.expr.filters.visible(t) && !e(t).parents().addBack().filter(function () { return "hidden" === e.css(this, "visibility") }).length } var a, r, i, u = t.nodeName.toLowerCase(), o = !isNaN(e.attr(t, "tabindex")); return "area" === u ? (a = t.parentNode, r = a.name, t.href && r && "map" === a.nodeName.toLowerCase() ? (i = e("img[usemap=#" + r + "]")[0], !!i && n(i)) : !1) : (/input|select|textarea|button|object/.test(u) ? !t.disabled : "a" === u ? t.href || o : o) && n(t) } e.focusNext = function () { t(":focusable") }, e.focusPrev = function () { n(":focusable") }, e.tabNext = function () { t(":tabbable") }, e.tabPrev = function () { n(":tabbable") }, e.extend(e.expr[":"], { data: e.expr.createPseudo ? e.expr.createPseudo(function (t) { return function (n) { return !!e.data(n, t) } }) : function (t, n, a) { return !!e.data(t, a[3]) }, focusable: function (t) { return a(t, !isNaN(e.attr(t, "tabindex"))) }, tabbable: function (t) { var n = e.attr(t, "tabindex"), r = isNaN(n); return (r || n >= 0) && a(t, !r) } }) }(jQuery);


/*
 * object.watch polyfill
 * 2012-04-03
 * By Eli Grey, http://eligrey.com
 * Public Domain.
 * NO WARRANTY EXPRESSED OR IMPLIED. USE AT YOUR OWN RISK.
 */


////Fix Submenus
//$('ul.dropdown-menu [data-toggle=dropdown]').on('click', function (event) {
//	event.preventDefault();
//	event.stopPropagation();
//	$('ul.dropdown-menu [data-toggle=dropdown]').parent().removeClass('open');
//	$(this).parent().addClass('open');
//});

/*!
 * deep-diff.
 * Licensed under the MIT License.
 */
//(function(e,t){"use strict";if(typeof define==="function"&&define.amd){define([],function(){return t()})}else if(typeof exports==="object"){module.exports=t()}else{e.DeepDiff=t()}})(this,function(e){"use strict";var t,n,r=[];if(typeof global==="object"&&global){t=global}else if(typeof window!=="undefined"){t=window}else{t={}}n=t.DeepDiff;if(n){r.push(function(){if("undefined"!==typeof n&&t.DeepDiff===p){t.DeepDiff=n;n=e}})}function i(e,t){e.super_=t;e.prototype=Object.create(t.prototype,{constructor:{value:e,enumerable:false,writable:true,configurable:true}})}function a(e,t){Object.defineProperty(this,"kind",{value:e,enumerable:true});if(t&&t.length){Object.defineProperty(this,"path",{value:t,enumerable:true})}}function f(e,t,n){f.super_.call(this,"E",e);Object.defineProperty(this,"lhs",{value:t,enumerable:true});Object.defineProperty(this,"rhs",{value:n,enumerable:true})}i(f,a);function l(e,t){l.super_.call(this,"N",e);Object.defineProperty(this,"rhs",{value:t,enumerable:true})}i(l,a);function u(e,t){u.super_.call(this,"D",e);Object.defineProperty(this,"lhs",{value:t,enumerable:true})}i(u,a);function s(e,t,n){s.super_.call(this,"A",e);Object.defineProperty(this,"index",{value:t,enumerable:true});Object.defineProperty(this,"item",{value:n,enumerable:true})}i(s,a);function o(e,t,n){var r=e.slice((n||t)+1||e.length);e.length=t<0?e.length+t:t;e.push.apply(e,r);return e}function c(e){var t=typeof e;if(t!=="object"){return t}if(e===Math){return"math"}else if(e===null){return"null"}else if(Array.isArray(e)){return"array"}else if(Object.prototype.toString.call(e)==="[object Date]"){return"date"}else if(typeof e.toString!=="undefined"&&/^\/.*\//.test(e.toString())){return"regexp"}return"object"}function h(t,n,r,i,a,p,b){a=a||[];var d=a.slice(0);if(typeof p!=="undefined"){if(i){if(typeof i==="function"&&i(d,p)){return}else if(typeof i==="object"){if(i.prefilter&&i.prefilter(d,p)){return}if(i.normalize){var y=i.normalize(d,p,t,n);if(y){t=y[0];n=y[1]}}}}d.push(p)}if(c(t)==="regexp"&&c(n)==="regexp"){t=t.toString();n=n.toString()}var v=typeof t;var g=typeof n;if(v==="undefined"){if(g!=="undefined"){r(new l(d,n))}}else if(g==="undefined"){r(new u(d,t))}else if(c(t)!==c(n)){r(new f(d,t,n))}else if(Object.prototype.toString.call(t)==="[object Date]"&&Object.prototype.toString.call(n)==="[object Date]"&&t-n!==0){r(new f(d,t,n))}else if(v==="object"&&t!==null&&n!==null){b=b||[];if(b.indexOf(t)<0){b.push(t);if(Array.isArray(t)){var k,m=t.length;for(k=0;k<t.length;k++){if(k>=n.length){r(new s(d,k,new u(e,t[k])))}else{h(t[k],n[k],r,i,d,k,b)}}while(k<n.length){r(new s(d,k,new l(e,n[k++])))}}else{var j=Object.keys(t);var w=Object.keys(n);j.forEach(function(a,f){var l=w.indexOf(a);if(l>=0){h(t[a],n[a],r,i,d,a,b);w=o(w,l)}else{h(t[a],e,r,i,d,a,b)}});w.forEach(function(t){h(e,n[t],r,i,d,t,b)})}b.length=b.length-1}}else if(t!==n){if(!(v==="number"&&isNaN(t)&&isNaN(n))){r(new f(d,t,n))}}}function p(t,n,r,i){i=i||[];h(t,n,function(e){if(e){i.push(e)}},r);return i.length?i:e}function b(e,t,n){if(n.path&&n.path.length){var r=e[t],i,a=n.path.length-1;for(i=0;i<a;i++){r=r[n.path[i]]}switch(n.kind){case"A":b(r[n.path[i]],n.index,n.item);break;case"D":delete r[n.path[i]];break;case"E":case"N":r[n.path[i]]=n.rhs;break}}else{switch(n.kind){case"A":b(e[t],n.index,n.item);break;case"D":e=o(e,t);break;case"E":case"N":e[t]=n.rhs;break}}return e}function d(e,t,n){if(e&&t&&n&&n.kind){var r=e,i=-1,a=n.path?n.path.length-1:0;while(++i<a){if(typeof r[n.path[i]]==="undefined"){r[n.path[i]]=typeof n.path[i]==="number"?[]:{}}r=r[n.path[i]]}switch(n.kind){case"A":b(n.path?r[n.path[i]]:r,n.index,n.item);break;case"D":delete r[n.path[i]];break;case"E":case"N":r[n.path[i]]=n.rhs;break}}}function y(e,t,n){if(n.path&&n.path.length){var r=e[t],i,a=n.path.length-1;for(i=0;i<a;i++){r=r[n.path[i]]}switch(n.kind){case"A":y(r[n.path[i]],n.index,n.item);break;case"D":r[n.path[i]]=n.lhs;break;case"E":r[n.path[i]]=n.lhs;break;case"N":delete r[n.path[i]];break}}else{switch(n.kind){case"A":y(e[t],n.index,n.item);break;case"D":e[t]=n.lhs;break;case"E":e[t]=n.lhs;break;case"N":e=o(e,t);break}}return e}function v(e,t,n){if(e&&t&&n&&n.kind){var r=e,i,a;a=n.path.length-1;for(i=0;i<a;i++){if(typeof r[n.path[i]]==="undefined"){r[n.path[i]]={}}r=r[n.path[i]]}switch(n.kind){case"A":y(r[n.path[i]],n.index,n.item);break;case"D":r[n.path[i]]=n.lhs;break;case"E":r[n.path[i]]=n.lhs;break;case"N":delete r[n.path[i]];break}}}function g(e,t,n){if(e&&t){var r=function(r){if(!n||n(e,t,r)){d(e,t,r)}};h(e,t,r)}}Object.defineProperties(p,{diff:{value:p,enumerable:true},observableDiff:{value:h,enumerable:true},applyDiff:{value:g,enumerable:true},applyChange:{value:d,enumerable:true},revertChange:{value:v,enumerable:true},isConflict:{value:function(){return"undefined"!==typeof n},enumerable:true},noConflict:{value:function(){if(r){r.forEach(function(e){e()});r=null}return p},enumerable:true}});return p});

//jquery.autoResize.js
(function (n) { n.fn.autoResize = function (t) { var i = n.extend({ onResize: function () { }, animate: !1, animateDuration: 150, animateCallback: function () { }, extraSpace: 0, limit: 1e3, useOriginalHeight: !1 }, t), u, r; return this.destroyList = [], u = this, r = null, this.filter("textarea").each(function () { var t = n(this).css({ resize: "none", "overflow-y": "hidden" }), c = i.useOriginalHeight ? t.height() : 0, e = function () { var f = {}, i; return n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), i = t.clone().removeAttr("id").removeAttr("name").css({ position: "absolute", top: 0, left: -9999 }).css(f).attr("tabIndex", "-1").insertBefore(t), r != null && n(r).remove(), r = i, u.destroyList.push(i), i }(), o = null, f = function () { var f = {}, r, u; if (n.each(["height", "width", "lineHeight", "textDecoration", "letterSpacing"], function (n, i) { f[i] = t.css(i) }), e.css(f), e.height(0).val(n(this).val()).scrollTop(1e4), r = Math.max(e.scrollTop(), c) + i.extraSpace, u = n(this).add(e), o !== r) { if (o = r, r >= i.limit) { n(this).css("overflow-y", ""); return } i.onResize.call(this); r = Math.max(20, r); i.animate && t.css("display") === "block" ? u.stop().animate({ height: r }, i.animateDuration, i.animateCallback) : u.height(r) } }, s, h; t.unbind(".dynSiz").bind("keyup.dynSiz", f).bind("keydown.dynSiz", f).bind("change.dynSiz", f); s = function () { f.call(t) }; n(window).bind("resize.dynSiz", function () { clearTimeout(h); h = setTimeout(s, 100) }); setTimeout(function () { f.call(t) }, 1) }), this.destroy = function () { for (var t = 0; t < this.destroyList.length; t++) n(this.destroyList[t]).remove(); this.destroyList = [] }, this } })(jQuery);