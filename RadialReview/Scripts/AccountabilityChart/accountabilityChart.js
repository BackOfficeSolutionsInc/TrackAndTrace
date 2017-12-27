
var expandAll = function () {
	var rs = angular.element("[ng-controller]").scope();

	rs.$apply(function () {
		rs.$emit("ExpandAllNodes");
	});
}
var collapseAll = function () {
	var rs = angular.element("[ng-controller]").scope();
	rs.$apply(function () {
		rs.$emit("CollapseAllNodes");
	});
}

function generateAccNodes() {
	var data = angular.element("[ng-controller]").scope().model.data.Root;
	var copy = {};
	function dive(parent, output) {
		output.x = parent.x;
		output.y = parent.y;
		output.width = parent.width;
		output.height = parent.height;

		if (parent._compact) {
			output.side = parent._compact.side;
			output.isLeaf = parent._compact.isLeaf;
		}

		output.Roles = []
		if (parent.User)
			output.Name = parent.User.Name;
		if (parent.Group && parent.Group.Position)
			output.Position = parent.Group.Position.Name
		else if (parent.Name)
			output.Position = parent.Name;

		if (parent.Group && parent.Group.RoleGroups) {
			//Roles
			for (var i in parent.Group.RoleGroups) {
				if (arrayHasOwnIndex(parent.Group.RoleGroups, i)) {
					if (parent.Group.RoleGroups[i].Roles) {
						for (var j in parent.Group.RoleGroups[i].Roles) {
							if (arrayHasOwnIndex(parent.Group.RoleGroups[i].Roles, j)) {
								if (parent.Group.RoleGroups[i].Roles[j])
									output.Roles.push(parent.Group.RoleGroups[i].Roles[j].Name)
							}
						}
					}
				}
			}
		}

		output.children = []
		var pc = parent.children;
		if (pc) {
			for (var k in pc) {
				if (arrayHasOwnIndex(pc, k)) {
					var oc = {};
					dive(pc[k], oc);
					output.children.push(oc);
				}
			}
		}

		output.hasHiddenChildren = false;
		if (parent._children && parent._children.length) {
			output.hasHiddenChildren = true;
		}
	}

	dive(data, copy);
	return copy;
}

var genPdf = function (val) {

	function compactify(shouldCompact) {
		angular.element($("[ng-controller]")).scope().$apply(function () {
			angular.element($("[ng-controller]")).scope().compact = shouldCompact;
		});
	}
	compactify(false);

	var fields = [
			{ text: "Width (inches)", name: "pw", type: "text", value: 11 },
			{ text: "Height (inches)", name: "ph", type: "text", value: 8.5 },
			{ text: "Scale to one page", name: "fit", type: "checkbox", value: false },
            { text: "Compress Chart", name: "compact", type: "checkbox", value: false, onchange: function () { compactify($(this).is(":checked")); } },
            { text: "Department Wise", name: "department", type: "checkbox", value: false }
	];

	var selected = null;
	var scope = angular.element($("[ng-controller]")).scope();
	if (scope !== null && scope.search !== null && scope.search.selected !== null) {
        selected = scope.search.selected.Id;

        if (!val) {

            fields.push({
                text: " ", name: "which", type: "radio", options: [
                    { value: "full", text: "Full chart", checked: true },
                    { value: "visible", text: "Only visible" },
                    { value: "selected", text: "Selected" }
                ]
            });
        }
        else {
            fields.push({
                text: " ", name: "which", type: "radio", options: [
                    { value: "full", text: "All Child", checked: true },
                    { value: "visible", text: "Only visible" },
                    { value: "selected", text: "Selected" }
                ]
            });
        }
    } else {

        if (!val) {
            fields.push({
                text: " ", name: "which", type: "radio", options: [
                    { value: "full", text: "Full chart", checked: true },
                    { value: "visible", text: "Only visible" }
                ]
            });
        }
        else {
            fields.push({
                text: " ", name: "which", type: "radio", options: [
                    { value: "full", text: "All Child", checked: true },
                    { value: "visible", text: "Only visible" }
                ]
            });
        }

		fields.push({
			type: "span",
			classes:"gray",
			text: "Use the search box to create a chart for one person."
		})
	}

	showModal({
		title: "Generate PDF",
		fields: fields,
		success: function (d) {
			var ajax = {
                url: "/pdf/ac?fit=" + d.fit + "&pw=" + d.pw + "&ph=" + d.ph + "&compact=" + d.compact + "&department=" + d.department,
				method: "POST",
				dataType: 'native',
				xhrFields: {
					responseType: 'blob'
				},
				contentType: "application/json; charset=utf-8",
				//processData: true,
				success: function (blob) {
					console.log(blob.size);
					var link = document.createElement('a');
					link.href = window.URL.createObjectURL(blob);
					link.download = "Accountability Chart.pdf";
					link.click();
				}, error: function (D) {
					showAlert("An error occurred");
				}
			};
			if (d.which === "visible") {
				ajax.data = JSON.stringify(generateAccNodes());
			}
			if (d.which === "selected") {
				ajax.url += "&selected=" + selected;
			}

			$.ajax(ajax);
		},
		close: function () {
			angular.element($("[ng-controller]")).scope().$apply(function () {
				angular.element($("[ng-controller]")).scope().compact = false;
			});
		}
	});
};


//     jQuery Ajax Native Plugin

//     (c) 2015 Tarik Zakaria Benmerar, Acigna Inc.
//      jQuery Ajax Native Plugin may be freely distributed under the MIT license.
(function (root, factory) {
	if (typeof define === 'function' && define.amd) {
		// AMD. Register as an anonymous module.
		define(['jquery'], factory);
	} else if (typeof exports === 'object') {
		// Node. Does not work with strict CommonJS, but
		// only CommonJS-like environments that support module.exports,
		// like Node.
		module.exports = factory(require('jquery'));
	} else {
		// Browser globals (root is window)
		factory(root.jQuery);
	}
}(this, function ($) {
	var ajaxSettings = $.ajaxSettings;
	ajaxSettings.responseFields.native = 'responseNative';
	ajaxSettings.converters['* native'] = true;
	var support = {},
        xhrId = 0,
        xhrSuccessStatus = {
			// file protocol always yields status code 0, assume 200
			0: 200,
			// Support: IE9
			// #1450: sometimes IE returns 1223 when it should be 204
			1223: 204
        },
        xhrCallbacks = {},
        xhrSupported = jQuery.ajaxSettings.xhr();
	// Support: IE9
	// Open requests must be manually aborted on unload (#5280)
	if (window.ActiveXObject) {
		$(window).on("unload", function () {
			for (var key in xhrCallbacks) {
				if (arrayHasOwnIndex(xhrCallbacks, key)) {
					xhrCallbacks[key]();
				}
			}
		});
	}
	support.cors = !!xhrSupported && ("withCredentials" in xhrSupported);
	support.ajax = xhrSupported = !!xhrSupported;

	//Native Data Type Ajax Transport
	$.ajaxTransport('native', function (options) {
		var callback;
		// Cross domain only allowed if supported through XMLHttpRequest
		if (support.cors || xhrSupported && !options.crossDomain) {
			return {
				send: function (headers, complete) {
					var i,
                        xhr = options.xhr(),
                        id = ++xhrId,
                        responses = {};

					xhr.open(options.type, options.url, options.async, options.username, options.password);

					// Apply custom fields if provided
					if (options.xhrFields) {
						for (i in options.xhrFields) {
							if (arrayHasOwnIndex(options.xhrFields, i)) {
								xhr[i] = options.xhrFields[i];
							}
						}
					}

					// Override mime type if needed
					if (options.mimeType && xhr.overrideMimeType) {
						xhr.overrideMimeType(options.mimeType);
					}

					// X-Requested-With header
					// For cross-domain requests, seeing as conditions for a preflight are
					// akin to a jigsaw puzzle, we simply never set it to be sure.
					// (it can always be set on a per-request basis or even using ajaxSetup)
					// For same-domain requests, won't change header if already provided.
					if (!options.crossDomain && !headers["X-Requested-With"]) {
						headers["X-Requested-With"] = "XMLHttpRequest";
					}

					// Set headers
					for (i in headers) {
						if (arrayHasOwnIndex(headers, i)) {
							xhr.setRequestHeader(i, headers[i]);
						}
					}

					// Callback
					callback = function (type) {
						return function () {
							if (callback) {
								delete xhrCallbacks[id];
								callback = xhr.onload = xhr.onerror = null;

								if (type === "abort") {
									xhr.abort();
								} else if (type === "error") {
									complete(
                                        // file: protocol always yields status 0; see #8605, #14207
                                        xhr.status,
                                        xhr.statusText
                                    );
								} else {
									// The native response associated with the responseType
									// Stored in the xhr.response attribute (XHR2 Spec)
									if (xhr.response) {
										responses.native = xhr.response;
									}

									complete(
                                        xhrSuccessStatus[xhr.status] || xhr.status,
                                        xhr.statusText,
                                        responses,
                                        xhr.getAllResponseHeaders()
                                    );
								}
							}
						};
					};

					// Listen to events
					xhr.onload = callback();
					xhr.onerror = callback("error");

					// Create the abort callback
					callback = xhrCallbacks[id] = callback("abort");

					try {
						// Do send the request (this may raise an exception)
						xhr.send(options.hasContent && options.data || null);
					} catch (e) {
						// #14683: Only rethrow if this hasn't been notified as an error yet
						if (callback) {
							throw e;
						}
					}
				},

				abort: function () {
					if (callback) {
						callback();
					}
				}
			};
		}
	});


	//$.getNative wrapper
	$.getNative = function (url, callback) {
		return $.ajax({
			dataType: 'native',
			url: url,
			xhrFields: {
				responseType: 'arraybuffer'
			},
			success: callback
		});
	}
}));