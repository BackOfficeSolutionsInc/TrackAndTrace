
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

$(document).ready(function () {
	var e = new CustomEvent("jquery-loaded", {});
	document.dispatchEvent(e);
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

function getParameterByName(name, url) {
	if (!url) url = window.location.href;
	name = name.replace(/[\[\]]/g, "\\$&");
	var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
	if (!results) return null;
	if (!results[2]) return '';
	return decodeURIComponent(results[2].replace(/\+/g, " "));
}

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



function getKeySelector(selector, prefix) {
	prefix = prefix || "";
	var output = { selector: selector, key: false };

	if ($(selector).data("key")) {
		output.key = prefix + $(selector).data("key");
	} else if ($(selector).attr("name")) {
		output.key = prefix + $(selector).attr("name");
		output.selector = "[name=" + $(selector).attr("name") + "]";
	} else if ($(selector).attr("id")) {
		output.key = prefix + $(selector).attr("id");
		output.selector = "#" + $(selector).attr("id");
	}

	return output;
}

function refresh() { location.reload(); }


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
			console.warn("Using FormData will not work on IE9");
			xhr.send(new FormData(form));
		}
	}
}, true);


function toTitleCase(str) {
	return str.replace(/\w\S*/g, function (txt) { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); });
}
