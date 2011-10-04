/// <reference path="jquery-1.5.1.js" />
/// <reference path="jquery-ui.js" />

$(document).ready(function () {
    $.datepicker.setDefaults($.datepicker.regional["es"]);
    $('.date').datepicker({ dateFormat: "yy-mm-dd", changeYear: true });
    //$(".date").datepicker("option", $.datepicker.regional["es"]);
});
