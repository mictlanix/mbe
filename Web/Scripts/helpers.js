$(function () {
    $.datepicker.setDefaults($.datepicker.regional["es"]);
    $('.date').datepicker({ dateFormat: "yy-mm-dd", changeYear: true });
});
function onSearchBegin() {
    $("#search-results").html(null);
}