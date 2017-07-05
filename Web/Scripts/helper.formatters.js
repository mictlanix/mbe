function ProductFormatter(item) {
    var htmlPrice = '';
    var tagQuantity = '';

    if (typeof (item.price) != 'undefined') {
        htmlPrice = '&nbsp;<span>Precio: ' + item.price + '</span>';
    }

    if (typeof (item.quantity) != 'undefined') {
        tagQuantity = "&nbsp;<span>Cantidad:" + item.quantity + '</span>';
    }

    var fmt = "<li title='" + item.name + "'>" +
                "<img style='float:left;max-width:50px;height:50px;' src='" + item.url + "' alt=''/>" +
                "<div style='margin:6px 0 0 52px;height:45px;'>" +
                  "<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" + item.name + "</div>" +
                  "<span>Código: " + item.code + " Modelo: " + item.model + " SKU: " + item.sku + "</span>" + htmlPrice +
                    tagQuantity +
                "</div>" +
              "</li>";

    return fmt;
}

function ExpenseFormatter(item) {

	var desc = "";

	if (typeof (item.comment) != 'undefined' && item.comment !== null) {
		desc = "&nbsp<span style='font-weight:normal;'>Descripción: " + item.comment + "</span>";
	}

	
	var fmt = "<li title='" + item.name + "'> " +
						"<div style='margin:6px 0 0 52px;height:45px;' >"+
							"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" +
								"<span>Nombre: " + item.name +"</span><br/>"+ desc +
							"</div>" + 
						"</div >"+
					"</li > ";

	return fmt;
}

function CustomerFormatter(item) {
	 
	var fmt = "<li title='" + item.name + "'>" +
		"<div style='margin:6px 0 0 52px;height:45px;'>" +
		"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" + item.name + "</div>" +
		"<span>Código: " + item.code + "</span>" +
		"</div>" +
		"</li>";

	return fmt;
}
