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
function bindOpenDialog(selector) {
	$(selector).off('click');
    $(selector).on('click', function (e) {
		var dlg = $('<div></div>');
        e.preventDefault();

        dlg.addClass('dialog')
	        .attr('id', $(this).attr('data-dialog-id'))
	        .appendTo('body')
	        .dialog({
	            title:$(this).attr('data-dialog-title'),
	            close:function(){$(this).remove()},
	            modal:true,
	            resizable:false,
	            width:666
	        })
	        .load(this.href, function() {
				dlg.dialog('option', 'position', { my: 'center', at: 'center', of: window } );
			});
    });
}
function bindCloseDialog(selector) {
	$(selector).off('click');
	$(selector).on("click", function (e) {
	    e.preventDefault();
	    $(this).closest(".dialog").dialog("close");
	});
}
function refreshDiv(selector){
	var item = $(selector)
	item.load(item.data('url'));
}