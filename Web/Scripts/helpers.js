$(function () {
	$.ajaxSetup({ cache: false });
	
	$(document).click(hidePopups);
	$('.gbgt').click(function (e) {
		e.stopPropagation();
		hidePopups();
        $(this).parent().addClass('gbto');
        $(this).next().css({ "visibility": "visible" });
    });
	
    $.datepicker.setDefaults($.datepicker.regional["es"]);
    $('input.date').datepicker({ dateFormat: "yy-mm-dd", changeYear: true });
});
function onSearchBegin() {
    $("#search-results").html(null);
}
function hidePopups() {
	$('.gbgt').each(function () {
        $(this).parent().removeClass('gbto');
        $(this).next().css({ "visibility": "hidden" });
  	});
}