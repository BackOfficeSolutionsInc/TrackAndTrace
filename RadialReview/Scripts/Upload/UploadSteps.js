/*
    1. Upload File          (uploadFileUrl)
    2. Select Data          (uploadSelectionUrl)
    3. Confirm Selection    (confirmSelectionUrl)
*/

function UploadSteps(args) {
	//=================================================
	//Add these to args (Required)
	this.uploadFileUrl = null;
	this.uploadFileData = {};
	this.uploadSelectionUrl = null;
	this.uploadSelectionData = {};
	this.confirmSelectionUrl = null;
	this.confirmSelectionData = {};
	this.windowSelector = "#window";
	this.notificationSelector = "#notifications";

	//=================================================
	//Add these to args (Optional)
	this.defaultData = {};
	this.currentlySelectedClass = "ui-selected";
	this.currentlySelectingClass = "ui-selecting";
	this.previouslySelectedClass = "wasSelected";
	this.confirmationText = "Does everything look correct?";
	this.afterUpload = function (resultObject) { }
	//=================================================

	this.currentSelectionStep = 0;
	this.selectionSteps = [];

	var validationFunc = null;
	var errors = [];

	var instructionAlert = function () { return this.notificationSelector + " .instruction"; }
	var errorAlert = function () { return this.notificationSelector + " .errors"; }
	var nextButton = function () { return this.notificationSelector + " .nextButton"; }
	var backButton = function () { return this.notificationSelector + " .backButton"; }
	var skipButton = function () { return this.notificationSelector + " .skipButton"; }
	var tableSelector = function () { return this.windowSelector + " .selector-table"; }
	var that = this;

	var getFormData = function ($form) {
		var unindexed_array = $form.serializeArray();
		var indexed_array = {};

		$.map(unindexed_array, function (n, i) {
			if (n['name'] in indexed_array && indexed_array[n['name']] == "true") {
				//skip. checkboxes dont work correctly
			} else {
				indexed_array[n['name']] = n['value'];
			}
		});

		return indexed_array;
	}

	this.showLoading = function () {
		$(this.windowSelector).html("<center><div class='loader centered alignCenter'><div class='component '><div>Loading</div><img src='/Content/select2-spinner.gif' /></div></div></center>");
	}

	this.stackRect = [[]];

	this.initFileSelect = function () {
		var data = $.extend({}, this.defaultData, this.uploadFileData);


		var dataStr = "";
		for (var d in data) {
			if (arrayHasOwnIndex(data, d)) {
				dataStr += "<input type=hidden name='" + d + "' value='" + data[d] + "'/>";
			}
		}
		//var submit = $("<input class='btn btn-primary'/>");
		// 
		$(".instructions").show();
		$(that.notificationSelector).hide();

		var uploadForm = $("<form action='javascript:;' enctype='multipart/form-data' method='post' accept-charset='utf-8'>   " +
        "    <div class='row'>                                                                 " +
        "        <div class='col-xs-2'>                                                        " +
        "            <label for='file'>Filename:</label>                                       " +
        "        </div>                                                                        " +
        "        <div class='col-xs-8'>                                                        " +
        "            <input class='blend' type='file' name='File' id='File' />                 " +
        "        </div>                                                                        " +
        "        <div class='col-xs-2 submit'>                                                 " +
        "            <input class='btn btn-primary' type='submit'/>                            " +
        "        </div>                                                                        " +
        "    </div>                                                                            " +
             dataStr +
        "</form>                                                                               ");

		$(uploadForm).submit(function (e) {
			that.showLoading();
			e.preventDefault();
			console.warn("Using FormData will not work on IE9");
			var formData = new FormData($(this)[0]);
			console.log("upload_url: " + that.uploadFileUrl);
			$.ajax({
				url: that.uploadFileUrl,
				data: formData,
				type: "POST",
				async: false,
				cache: false,
				contentType: false,
				processData: false,
				enctype: 'multipart/form-data',
				success: function (d) {
					//    if (!d || typeof (d.Html) === "undefined")
					//        throw "Expects obj.Html from '" + that.uploadFileUrl + "'";
					//    if (typeof (d.Data) !== "undefined")
					//        $.extend(true, that.uploadSelectionData, d.Data);
					var html = processSuccess(d, that.uploadSelectionData);
					if (that.afterUpload)
						that.afterUpload(d);
					$(that.windowSelector).html(html);
					that.initSelection();
					$(".instructions").hide();
					try {
						debugger;
						console.log("path: " + $(html).find("#file_name").val());
					} catch (e) {
						console.error(e);
					}

				},
				error: function (d, e) {
					showAlert(e);
					that.initFileSelect();
					console.error(d);
					sendErrorReport();
					//$(this.windowSelector).html(uploadForm);
				}
			});
			return false;
		});
		$(this.windowSelector).html(uploadForm);
	}

	this.initSelection = function () {
		var button = $("<button disabled class='nextButton btn btn-primary pull-right btn-sm' style='margin-top:-4px;'>Next</button>");
		$(button).click(function () {
			that.nextSelectionStep();
		});
		var btnSkip = $("<button class='skipButton btn btn-default pull-right btn-sm' style='margin-top:-4px;'>Skip</button>");
		$(btnSkip).click(function () {
			that.skipSelectionStep();
		});
		var btnBack = $("<button class='backButton btn btn-default pull-right btn-sm' style='margin-top:-4px;'>Back</button>");
		$(btnBack).click(function () {
			that.backSelectionStep();
		});
		$(notificationSelector).html("<div class='alert alert-info next-container'><span class='instruction'>Please wait...</span></div><div class=' errors alert alert-danger'></div>")
		$(notificationSelector).find(".next-container").append(button);
		$(notificationSelector).find(".next-container").append(btnBack);
		$(notificationSelector).find(".next-container").append(btnSkip);
		$(notificationSelector + " .errors").hide();
		$(skipButton()).hide();
		this.currentSelectionStep = 0;
		var start = null, end;
		$(nextButton()).show();
		$(notificationSelector).show();

		var that = this;
		var table = $(this.windowSelector).find("table");
		$(table).find("tr").each(function (i) {
			$(this).find("td").each(function (j) {
				$(this).data("row", i);
				$(this).data("col", j);
				$(this).attr("data-row", i);
				$(this).attr("data-col", j);
			});
		});

		$("td", table).mousedown(function () {
			$("." + that.currentlySelectedClass).removeClass(that.currentlySelectedClass);
			start = [$(this).data("col"), $(this).data("row")];
		});
		//var lastMouseOver = new Date().getTime();

		//var mouseOverDebounce = function () {

		//    //if (lastMouseOver + 1000 > new Date().getTime()) {
		//        var cur = [$(this).data("col"), $(this).data("row")];

		//        if (start != null) {
		//            end = cur;
		//            var minx = Math.min(cur[0], start[0]);
		//            var miny = Math.min(cur[1], start[1]);
		//            var maxx = Math.max(cur[0], start[0]);
		//            var maxy = Math.max(cur[1], start[1]);
		//            var str = [];
		//            for (var i = minx; i <= maxx ; i++) {
		//                for (var j = miny; j <= maxy ; j++) {
		//                    str.push("td[data-row=" + j + "][data-col=" + i + "]");
		//                }
		//            }

		//            $("." + that.currentlySelectingClass).removeClass(that.currentlySelectingClass);
		//            $(str.join(","), table).addClass(that.currentlySelectingClass);
		//        }
		//        //lastMouseOver = new Date().getTime();
		//   // }
		//};
		$("td", table).mouseenter(function () {
			var cur = [$(this).data("col"), $(this).data("row")];

			if (start != null) {
				end = cur;
				var minx = Math.min(cur[0], start[0]);
				var miny = Math.min(cur[1], start[1]);
				var maxx = Math.max(cur[0], start[0]);
				var maxy = Math.max(cur[1], start[1]);
				var str = [];
				for (var i = minx; i <= maxx ; i++) {
					for (var j = miny; j <= maxy ; j++) {
						str.push("td[data-row=" + j + "][data-col=" + i + "]");
					}
				}
				try {
					console.log("selection " + that.selectionSteps[that.currentSelectionStep - 1].message + ": " + minx + "," + miny + "," + maxx + "," + maxy);
				} catch (e) {
					console.error(e);
				}
				$("." + that.currentlySelectingClass).removeClass(that.currentlySelectingClass);
				$(str.join(","), table).addClass(that.currentlySelectingClass);
			}
		});

		$(document).mouseup(function () {
			if (start != null && end != null) {
				start = null; end = null;
				$("." + that.currentlySelectingClass).addClass(that.currentlySelectedClass).removeClass(that.currentlySelectingClass);
				$(nextButton()).attr("disabled", true);

				if (validationFunc != null) {
					var rect = getRect();
					that.stackRect[that.stackRect.length - 1] = rect;
					if (validationFunc(rect)) {
						$(errorAlert()).html("").hide();
						$(nextButton()).attr("disabled", null);
					} else {
						$(nextButton()).attr("disabled", true);
						$(errorAlert()).html(errors.join("<br/>")).show();
						//$("#table").selectable("refresh");
						errors = [];

					}
				}
			} else if (start != null && end == null) {
				$(nextButton()).attr("disabled", true);
				start = null; end = null;
			} else {
				start = null; end = null;
				// $(".ui-selected").removeClass("ui-selected");
			}
		});

		this.nextSelectionStep();
	}

	this.initConfirmSelection = function (html) {
		var form = $("<form action='javascript:;' enctype='multipart/form-data' method='post' accept-charset='utf-8'>   " +
            "<input class='btn btn-success finalSubmit' type='submit' value='Submit'/>" +
                 html +
            "</form>");
		$(that.windowSelector).html(form);
		$(that.notificationSelector).hide();

		$(form).submit(function (e) {
			debugger;
			e.preventDefault();
			var d = getFormData($(this));
			that.showLoading();
			that.submitConfirmSelection(d);
		});

	}

	this.skipSelectionStep = function () {
		$(".ui-selected").removeClass("ui-selected");
		that.stackRect[that.stackRect.length - 1] = [];
		this.nextSelectionStep();
	};

	//this.backSelectionStep = function () {
	//    this.currentSelectionStep -= 2;
	//    this.nextSelectionStep();
	//};

	this.nextSelectionStep = function () {
		$("." + this.currentlySelectedClass).addClass(this.previouslySelectedClass).removeClass(this.currentlySelectedClass);
		if (this.currentSelectionStep == 0)
			$(backButton()).hide();
		else
			$(backButton()).show();


		if (this.currentSelectionStep >= this.selectionSteps.length) {
			$(instructionAlert()).css("color", "rgba(0, 0, 0, 0.73)").html("Please wait...");
			$(nextButton()).attr("disabled", "true");
			validationFunc = null;
			this.submitSelection();
			//$(this.windowSelector).html("<center>Please Wait</center>");
			this.showLoading();
			return;
		}
		if (this.selectionSteps[this.currentSelectionStep].skipable) {
			$(skipButton()).show();
		} else {
			$(skipButton()).hide();
		}

		$(nextButton()).attr("disabled", "true");
		$(errorAlert()).hide();
		errors = [];
		$(instructionAlert()).css("color", "#ef7622").html(this.selectionSteps[this.currentSelectionStep].message).show();
		//$("#table").selectable("refresh");
		$(tableSelector()).css("display", null);
		validationFunc = this.selectionSteps[this.currentSelectionStep].func;
		that.stackRect.push([]);
		this.currentSelectionStep += 1;
	}

	this.backSelectionStep = function () {
		this.currentSelectionStep = Math.max(0, this.currentSelectionStep - 2);
		var selection = that.stackRect.pop();
		deselectRect(selection, "ui-selected");
		var selection = that.stackRect.pop();
		deselectRect(selection, "wasSelected");

		this.nextSelectionStep();
	}

	var processSuccess = function (d, extendData) {
		if (typeof (d.Error) !== "undefined") {
			if (showJsonAlert(d)) {
				if (!d || typeof (d.Html) === "undefined")
					throw "Expects obj.Html from '" + that.uploadSelectionUrl + "'";
				if (typeof (d.Data) !== "undefined")
					$.extend(true, extendData, d.Data);

				return d.Html;
			}
		} else {
			//$(d).find("input[type='hidden'")
			return d;
		}
	}

	this.submitSelection = function () {
		var data = $.extend({}, this.defaultData, this.uploadSelectionData);
		//var data = {
		//    userRect: userRect,
		//    dateRect: dateRect,
		//    measurableRect: measurableRect,
		//    goalRect: goalRect,
		//    RecurrenceId: $("[name='RecurrenceId']").val(),
		//    Path: $("[name='Path']").val(),
		//    UseAWS: $("[name='UseAWS']").val()
		//};
		$.ajax({
			url: that.uploadSelectionUrl,
			method: "post",
			data: JSON.stringify(data),
			traditional: true,
			//dataType: "json",
			contentType: 'application/json; charset=utf-8',
			success: function (d) {
				var html = processSuccess(d, that.confirmSelectionData);
				that.initConfirmSelection(html);
			},
			error: function (d, e) {
				showHtmlErrorAlert(d, "Error uploading file.");
				that.initFileSelect();
				console.error(d);
				sendErrorReport();
			}
		});
	}

	this.submitConfirmSelection = function (formData) {
		var data = $.extend({}, this.defaultData, this.confirmSelectionData);
		$.extend(true, data, formData);
		$.ajax({
			url: that.confirmSelectionUrl,
			method: "post",
			data: data,
			//dataType: "json",  
			traditional: true,
			//dataType: 'json',
			//contentType: 'application/json; charset=utf-8',
			success: function (d) {
				if (!showJsonAlert(d)) {
					$(that.windowSelector).html("<center>An error occurred. " + d.Message + " </center>");
					setTimeout(that.initFileSelect, 2000);
				}

			},
			error: function (e) {
				showAlert("An error occurred.");
				that.initFileSelect();
				console.error(e);
				sendErrorReport();
			}
		});
	}

	this.addSelectionStep = function (instructions, validate, skipable) {
		this.selectionSteps.push({ message: instructions, func: validate, skipable: skipable });
	}

	var deselectRect = function (rect, toremove) {
		for (var i = rect[0]; i <= rect[2]; i++) {
			for (var j = rect[1]; j <= rect[3]; j++) {
				$("[data-row=" + j + "][data-col=" + i + "]").removeClass(toremove);
			}
		}
		//var y = $(this).data("row");
		//var x = $(this).data("col");
	}

	var getRect = function () {
		var minx = 10000000;
		var miny = 10000000;
		var maxx = 0;
		var maxy = 0;
		var count = 0;
		$("." + this.currentlySelectingClass + ", ." + this.currentlySelectedClass).each(function () {
			var y = $(this).data("row");
			var x = $(this).data("col");
			miny = Math.min(miny, y);
			minx = Math.min(minx, x);
			maxy = Math.max(maxy, y);
			maxx = Math.max(maxx, x);
			count += 1;
		});
		if (count == 0)
			return null;
		return [minx, miny, maxx, maxy, count];
	}

	this.addSelectionData = function (data, value) {
		if (typeof (value) !== "undefined") {
			var o = {};
			o[data] = value;
			this.addSelectionData(o);
		} else {
			this.uploadSelectionData = $.extend(true, this.uploadSelectionData, data);
		}
	}

	this.verify = {
		atLeastOneCell: function (rect) {
			if (rect != null)
				return true;
			errors.push("You must select at least one cell.");
			return false;
		},
		eitherColumnOrRow: function (rect) {
			if (rect[0] == rect[2] || rect[1] == rect[3])
				return true;
			errors.push("You must select either a row or column.");
			return false;
		},
		similarSelection: function (rect1, rect2) {
			if ((rect1[0] == rect1[2] && rect1[1] == rect2[1] && rect1[3] == rect2[3]) || (rect1[1] == rect1[3] && rect1[0] == rect2[0] && rect1[2] == rect2[2]))
				return true;
			errors.push("Invalid selection. Cells must match previous selection.");
			return false;
		},
		eitherAboveOrBelow: function (rect1, rect2) {
			if (rect1[1] > rect2[1] || rect1[3] < rect2[3])
				return true;
			errors.push("Invalid selection. Cells must be above or below the other selection.");
			return false;
		},
		rightOf: function (rect1, rect2) {
			if (rect1[2] < rect2[0])
				return true;
			errors.push("Invalid selection. Cells must be to the right the other selection.");
			return false;
		}
	};


	$.extend(this, args);
	this.initFileSelect();
	return this;
}