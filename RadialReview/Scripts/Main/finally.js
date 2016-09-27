$(function () {
    $(".remember").each(function (i) {
        var m = getKeySelector(this, "remember-");
        if (m.key) {
            var val = load(m.key);
            if (val) {
                setVal(m.selector, val);
            }
        }
    });

    function onRemember() {
        var m = getKeySelector(this, "remember-");
        if (m.key) {
            var val = getVal(m.selector);
            save(m.key, val);
        }
    }

    $(".remember").on("change", onRemember);
    $(".remember").on("hidden.bs.collapse", onRemember);
    $(".remember").on("shown.bs.collapse", onRemember);



    $('.navbar-collapse').on('shown.bs.collapse', function () {
        if ($(window).width() < 768) {
            $(this).find(".btn-group.heading").addClass("open");
        }
    });
    $('.navbar-collapse').on('hidden.bs.collapse', function () {
        $(this).find(".btn-group.heading").removeClass("open");
    });
});