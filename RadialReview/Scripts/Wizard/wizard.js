var logging = true;
if (logging) {
    $(window).on("wizard:first-page", function () { console.log("event: first-page"); });
    $(window).on("wizard:last-page", function () { console.log("event: last-page"); });
}


function setCompletion(percentage) {
    var pb = $(".progress-bar");
    $(pb).attr("aria-valuenow", percentage);
    $(pb).css("width", percentage + "%");
    $(pb).find(".sr-only").html(percentage + "% Complete");
}

var doneLoading = true;
var currentPage = null;
function changePage(page, first) {

	window.location.hash = page;
    if (page[0] == "#") {
        if (page[1] == "/")
            page = page.substr(2);
        else
            page = page.substr(1);
    }
    while (!doneLoading) {
        setTimeout(function () {
            changePage(page, first);
            console.log("trying again to change page");
        },300);
        return;
    }
    var pages = $(".wizard-page:not(.hidden)");
    function showPage(pg) {
        console.log("Changing page");
        var cpe = $(".wizard-page[data-page='" + pg + "']");
        if (cpe.length == 0 && typeof(first)==="undefined") {
            doneLoading = true;
            var fp = $(".wizard-page").first().attr("data-page");
            if (typeof (fp) !== "undefined") {
                changePage(fp, true);
            }
        } else {
            cpe.css("display", "none");
            //cpe.css("opacity", "0");
            setTimeout(function () { cpe.removeClass("hidden"); }, 2);

            cpe.fadeIn(function () {
            	cpe.css("opacity", "1");
            	cpe.removeClass("hidden");
                doneLoading = true;
                console.log("Changed page");
                currentPage = page;
                
                var total = $(".wizard-page");
                var i = 1;
                for (var t in total) {
                	if (arrayHasOwnIndex(total, t)) {
                		if ($(total[t]).attr("data-page") == pg)
                			break;
                		i = i + 1;
                	}
                }
                var completion = i / (total.length+1);

                $(window).trigger("wizard:changed-page", { page: page, completion: completion });

                $("[href='#" + pg + "']").addClass("selected");
                if ($(this).next(".wizard-page").length == 0) {
                    $(window).trigger("wizard:last-page");
                }
                if ($(this).prev(".wizard-page").length == 0) {
                    $(window).trigger("wizard:first-page");
                }
            });
        }
    }
    if (pages.length == 0) {
        showPage(page);
    } else {
        $(pages).fadeOut(250, function () {
            var pg=$(this).attr("data-page");
            $("[href='#" + pg + "']").removeClass("selected");

            $(this).addClass("hidden");
            showPage(page);
        });
    }
   
}

function initWizard(page) {
    $(".wizard-page").addClass("hidden");

    if (window.location.hash)
        changePage(window.location.hash);
    else if (typeof (page) !== "undefined") {
        changePage(page);
    } else {
        var pg = $(".wizard-page").first().attr("data-page");
        changePage(pg);
    }
    $(".wizard").removeClass("hidden");
}

function nextPage() {
    var next = $(".wizard-page[data-page='" + currentPage + "']").next(".wizard-page");
    if (next.length == 0)
        return false;
    var nextpage = $(next).attr("data-page");
    changePage(nextpage);

   
}
function backPage() {
    var prev = $(".wizard-page[data-page='" + currentPage + "']").prev(".wizard-page");
    if (prev.length == 0)
        return false;
    var prevpage = $(prev).attr("data-page");
    changePage(prevpage);    
}


window.onhashchange = function () {
    if (window.location.hash)
        changePage(window.location.hash);
};

$(function () {
    $("body").on("click", "[data-toggle='tooltip']", function () {
        $(".tooltip").tooltip('hide');
    });
    $(window).scroll(function () {
        if (window.innerWidth > 768) {
            $(".wizard-menu").css("top", window.scrollY);
        } else {
            $(".wizard-menu").css("top", 0);
        }
    });

    $(document).keydown(function (e) {
        if ($(body).hasClass("modal-open"))
            return true;
        if ($(e.target).is('input') && $(e.target).closest(".todo-text,.rock-pane").length == 0) {
            return true;
        }

        if (e.keyCode == 13) {
            setTimeout(function () {
                $(".create-row:visible").click();
            }, 100);
            return false;
        } else if (e.keyCode == 34) {
        	var b = $(".nextButton:visible");
        	b.click();
        } else if (e.keyCode == 33) {
        	var b = $(".backButton:visible");
        	b.click();
        }
    });
});

