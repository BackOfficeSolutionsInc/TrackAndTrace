
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
								setTimeout(function () {
									an.switchToChainNext();
								}, 1);
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
