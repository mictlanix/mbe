function ProductFormatter(item) {
    var htmlPrice = '';

    if (typeof (item.price) != 'undefined') {
        htmlPrice = '&nbsp;<span>Precio: ' + item.price + '</span>';
    }

    var fmt = "<li title='" + item.name + "'>" +
                "<img style='float:left;max-width:50px;height:50px;' src='" + item.url + "' alt=''/>" +
                "<div style='margin:6px 0 0 52px;height:45px;'>" +
                  "<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" + item.name + "</div>" +
                  "<span>Código: " + item.code + " Modelo: " + item.model + " SKU: " + item.sku + "</span>" + htmlPrice +
                "</div>" +
              "</li>";

    return fmt;
}

