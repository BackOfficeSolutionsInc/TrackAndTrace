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
});