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
function changePage(page) {
    if (page[0] == "#")
        page = page.substr(1);
    while (!doneLoading) {
        setTimeout(function () {
            changePage(page);
            console.log("trying again to change page");
        }, 250);
        return;
    }
    var pages = $(".wizard-page:not(.hidden)");
    function showPage(pg) {
        console.log("Changing page");
        $(".wizard-page[data-page='" + pg + "']").removeClass("hidden").fadeIn(function () {
            doneLoading = true;
            console.log("Changed page");
            currentPage = page;

            $("[href='#" + pg + "']").addClass("selected");
            if ($(this).next(".wizard-page").length == 0) {
                $(window).trigger("wizard:last-page");
            }
            if ($(this).prev(".wizard-page").length == 0) {
                $(window).trigger("wizard:first-page");
            }
        });
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
