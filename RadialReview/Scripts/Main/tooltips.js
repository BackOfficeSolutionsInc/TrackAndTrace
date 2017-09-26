
$(function () {
    setTimeout(function () {
        if (window.tooltips && window.tooltips.length > 0) {
            var allListeners = [];

            var removeAllTooltips = function () {
                for (var listener in allListeners) {
                    if (arrayHasOwnIndex(allListeners, listener)) {
                        //debugger;
                        //allListeners[listener].waitUntilExists("remove");
                    }
                }
            }

            for (var iter in window.tooltips) {
                if (arrayHasOwnIndex(window.tooltips, iter)) {
                    try {
                        var i = window.tooltips[iter];
                        var id = i.TooltipId;
                        var item = $(i.Selector);
                        allListeners.push(item);
                        item.waitUntilExists(function () {
                            removeAllTooltips();
                            var idd = id;
                            showModal({
                                modalClass: " modal-icon-info",
                                icon: { title: i.Title, },
                                title: i.HtmlBody,
                                close: function () {
                                    $.ajax("/ClientSuccess/MarkTooltip/" + idd);
                                }

                            });
                        }, true)
                    } catch (e) {
                        console.warn("Tooltip failed", e);
                    }
                }
            }
        }
    }, 1500);
});