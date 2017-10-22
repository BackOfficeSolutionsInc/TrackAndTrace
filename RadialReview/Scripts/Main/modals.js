/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  obj ={																																	    ///
///      title:,																															    ///
///      icon : <success,warning,danger,info,primary,default> or {icon:"css icon name",title:"Title Text!",color:"Hex-Color"}				    ///
///      fields: [{																															    ///
///          name: (optional)																												    ///
///          text: (optional)																												    ///
///          type: <text,textarea,checkbox,radio,span,div,header,h1,h2,h3,h4,h5,h6,number,date,datetime,time,file,yesno,label,select>(optional)	///
///				   (if type=radio or select) options:[{text,value},...]																		    ///
///          value: (optional)																												    ///
///          placeholder: (optional)																										    ///
///          classes: (optional)																											    ///
///      },...],																															    ///
///		 contents: jquery object (optional, overrides fields)																				    ///
///      pushUrl:"",																														    ///
///      success:function(formData,contentType),																							    ///
///      complete:function,																													    ///
///      cancel:function,																													    ///  
///      reformat: function,                                                                                                                    ///
///      validation: function(data),                                                                                                            ///
///      noCancel: bool																														    ///
///  }																																		    ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

	$("#modalTitle").html(title);
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
    var runAfter = [];
    var runAfterAnimation = [];
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
	var recalculateModalHeight = false;

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
			icon = icon.replace(" ", ".");
			//debugger;
    
			try {
	            document.styleSheets[0].insertRule("." + custom + " ." + icon + ":after{content: '" + title + "' !important;}", 0);
				document.styleSheets[0].insertRule("." + custom + " ." + icon + ":before{ background-color: " + color + ";}", 0);
				document.styleSheets[0].insertRule("." + custom + " #modalOk{ background-color: " + color + ";}", 0);
			} catch (e) {
				console.error(e);
			}

			runAfterAnimation.push(function () {
			    try {
			        //debugger;
			        var modalHeaderHeight = 125;
			        var titleDiv = $("<div class='modal-icon-title'>" + title + "</div>");
			        $("." + custom+" .modal-content").append(titleDiv);
			        modalHeaderHeight += $(titleDiv).height();
			        modalHeaderHeight += $("#modalTitle").height();
			        debugger;
			        titleDiv.remove();
			        document.styleSheets[0].insertRule("." + custom + ".modal-icon .modal-header{ height: " + modalHeaderHeight + "px !important;}", 0);
			    } catch (e) {
			        console.error(e);
			    }
			});
		}

	}

	$("#modal #class-container").attr("class", obj.modalClass);

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	var allowed = ["text", "hidden", "textarea", "checkbox", "radio", "number", "date", "time", "datetime", "header", "span", "div", "h1", "h2", "h3", "h4", "h5", "h6", "file", "yesno", "label", "img", "select"];
	var addLabel = ["text", "textarea", "checkbox", "radio", "number", "date", "time", "datetime", "file", "select"];
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
			if (arrayHasOwnIndex(obj.fields, f)) {
				if (typeof (obj.fields[f]) !== "object") {
					allDeep = false;
					break;
				}
			}
		}
		if (!allDeep) {
			obj.fields = [obj.fields];
		}
	}

	var fieldsTypeIsArray = Array.isArray(obj.fields);//typeof (obj.fields);

	var contentType = null;

	var builder = '<div class="form-horizontal modal-builder">';
	var genInput = function (type, name, eid, placeholder, value, others, classes, tag) {
		others = others || "";
		classes = classes || "form-control blend";
		if (type == "number")
			others += " step=\"any\"";
		if (typeof (tag) === "undefined")
			tag = "input";

		if (type == "checkbox" && ((typeof (value) === "string" && (value.toLowerCase() === 'true')) || (typeof (value) === "boolean" && value)))
			others += "checked";

		if (type == "datetime") {
			var newVal = parseJsonDate(value, true).toISOString().substring(0, 19);
			type = "datetime-local";
			if (newVal)
				value = newVal;
		}

		return '<' + tag + ' type="' + escapeString(type) + '" class="' + classes + '"' +
                      ' name="' + escapeString(name) + '" id="' + eid + '" ' +
                      placeholder + ' value="' + escapeString(value) + '" ' + others + '/>';
	}

	var defaultLabelColumnClass = obj.labelColumnClass || "col-sm-2";
	var defaultValueColumnClass = obj.valueColumnClass || "col-sm-10";


	if (!obj.contents) {
		for (var f in obj.fields) {
			if (arrayHasOwnIndex(obj.fields, f)) {
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
					if (Object.prototype.toString.call(value) === '[object Date]' && (/*type == "datetime" ||*/ type == "date")) {
						value = value.toISOString().substring(0, 10);
					}

					if (type == "file")
						contentType = 'enctype="multipart/form-data"';

					if (tags.indexOf(type) != -1) {
						var txt = value || text;
						input = "<" + type + " name=" + escapeString(name) + '" id="' + eid + '" class="' + classes + '">' + txt + '</' + type + '>';
					} else if (type == "textarea") {
						input = '<textarea class="form-control blend verticalOnly ' + classes + '" rows=5 name="' + escapeString(name) + '" id="' + eid + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
					} else if (type == "date" /*|| type=="datetime"*/) {
						var guid = generateGuid();
						var curName = name;
						var curVal = originalValue;
						var localize = field.localize;
						input = '<div class="date-container date-' + guid + ' ' + classes + '" id="' + eid + '"></div>';
						runAfter.push(function () {
							var dateGenFunc = generateDatepicker;
							if (localize == true)
								dateGenFunc = generateDatepickerLocalize;
							dateGenFunc('.date-' + guid, curVal, curName, eid);
						});
					} else if (type == "yesno") {
						var selectedYes = (value == true) ? 'checked="checked"' : "";
						var selectedNo = (value == true) ? "" : 'checked="checked"';
						input = '<div class="form-group input-yesno ' + classes + '">' +
									'<label for="true" class="col-xs-4 control-label"> Yes </label>' +
									'<div class="col-xs-2">' + genInput("radio", name, eid, placeholder, "true", selectedYes) + '</div>' +
									'<label for="false" class="col-xs-1 control-label"> No </label>' +
									'<div class="col-xs-2">' + genInput("radio", name, eid, placeholder, "false", selectedNo) + '</div>' +
								'</div>';
					} else if (type == "img") {
						input = "<img src='" + field.src + "' class='" + classes + "'/>";
					} else if (type == "radio") {
						if (field.options != null && field.options.length > 0) {
							var fieldName = name;
							input = "<fieldset id='group_" + fieldName + "'><table>";
							for (var oid in field.options) {
								if (arrayHasOwnIndex(field.options, oid)) {
									var option = field.options[oid];
									if (!option.value) {
										console.warn("option has no value " + fieldName + "," + oid);
									}
									var radioId = eid + "_" + oid;
									var selected = option.checked || false;
									if (selected)
										selected = "checked";
									var radio = genInput("radio", fieldName, radioId, null, option.value, selected, option.classes || " ");
									var optionText = option.text || option.value;

									input += '<tr class="form-group">' +
												'<td><label for="' + radioId + '" class="pull-right ' + (option.labelColumnClass || "") + ' control-label" style="padding-right:10px;">' + optionText + '</label></td>' +
												'<td><div class="' + (option.valueColumnClass || "") + '" style="padding-top: 5px;">' + radio + '</div></td>' +
											 '</tr>';
								}
							}
							input += "</table></fieldset>";
						} else {
							console.warn("radio field requires an 'options' array");
						}
					} else if (type == "select") {
						if (field.options != null && field.options.length > 0) {
							var fieldName = name;
							input = $(genInput("", fieldName, eid, null, null, selected, classes, "select"));
							for (var oid in field.options) {
								if (arrayHasOwnIndex(field.options, oid)) {
									var option = field.options[oid];
									if (!option.value) {
										console.warn("option has no value " + fieldName + "," + oid);
									}
									var optionId = eid + "_" + oid;
									var selected = option.checked || false;
									if (selected)
										selected = "selected";
									var optionText = option.text || option.value;
									var option = $(genInput("", fieldName, optionId, null, option.value, selected, " ", "option"));
									option.text(optionText);
									$(input).append(option);
								}
							}
							//input = $(input).html();
							input = $(input).wrapAll('<div>').parent().html();
							//debugger;
							//input += "</table></fieldset>";
						} else {
							console.warn("radio field requires an 'options' array");
						}
					} else {
						input = genInput(type, name, eid, placeholder, value, null, classes);
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
							console.warn("Unhandled onchange type:" + typeof (onchange) + " for " + eid);
						}
					}

				} catch (e) {
					console.error(e);
				}
			}
		}
		builder += "</div>";
	} else {
		builder = $(obj.contents);
	}

	_bindModal(builder, obj.title, undefined, obj.validation, onSuccess, onCancel, reformat, onClose, contentType);
	setTimeout(function () {
	    debugger;
		for (var i = 0; i < runAfter.length; i++) {
			runAfter[i]();
		}
	}, 1);
	setTimeout(function () {
	    for (var i = 0; i < runAfterAnimation.length; i++) {
	        runAfterAnimation[i]();
	    }	    
	},250);
}

function _bindModal(html, title, callback, validation, onSuccess, onCancel, reformat, onClose, contentType) {
	$('#modalBody').html("");
	setTimeout(function () {

	    var error = $(html).find(".error-page");
	    if (error.length >= 1) {
	        $('#modal').modal('hide');
	        var msg= error.find(".error-message").text() || "An error occurred.";
	        showAlert(msg);
	        return;
	    }

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
			formData[name] = v == "true" ? "True" : "False";
		});

		if (typeof (reformatArg) === "function") {
			var o = reformatArg(formData);
			if (typeof (o) !== "undefined" && o != null)
				formData = o;//Data was returned, otherwise formdata was manipulated
		}

		if (validationArg) {
			var message = undefined;
			if (typeof (validationArg) === "string") {
				message = window[validationArg](formData);
				//message = eval(validationArg + '()');
			} else if (typeof (validationArg) === "function") {
			    message = validationArg(formData);
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
				window[onSuccessArg](formData, contentTypeArg);
				//eval(onSuccessArg + "(formData," + contentTypeArg + ")");
			} else if (typeof onSuccessArg === "function") {
				onSuccessArg(formData, contentTypeArg);
			}
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				window[onCloseArg]();
				//eval(onCloseArg + "()");
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal button[data-dismiss='modal']").unbind('click.radialModal');


	$("#modal button[data-dismiss='modal']").on("click.radialModal", function () {
		if (typeof onCancelArg === "string") {
			window[onCancelArg]();
			//	eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (typeof onCancelArg === "string") {
			//eval(onCancelArg + "()");
			window[onCancelArg]();
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				//eval(onCloseArg + "()");
				window[onCloseArg]();
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
			if (typeof (callbackArg) === "string") {
				//eval(callbackArg + '()');
				window[callbackArg]();
			} else if (typeof (callbackArg) === "function")
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
		console.warn("Using FormData will not work on IE9");
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
						window[onSuccessArg](data, formData);
						//eval(onSuccessArg + "(data,formData)");
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
							window[onCompleteArg](data, formData);
							//eval(onCompleteArg + "(data,formData)");
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