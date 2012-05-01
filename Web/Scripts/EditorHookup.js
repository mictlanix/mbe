/// <reference path="jquery-1.5.1.js" />
/// <reference path="jquery-ui.js" />

$(function () {
    $.datepicker.setDefaults($.datepicker.regional["es"]);
    $('.date').datepicker({ dateFormat: "yy-mm-dd", changeYear: true });
});
