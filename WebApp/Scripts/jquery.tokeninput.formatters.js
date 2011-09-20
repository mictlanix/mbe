function ProductFormatter(item) {
    var fmt = "<li>" +
                "<img style='max-width:50px;height:50px;vertical-align:top;' src='" + item.url + "' title='" + item.name + "'/>" +
                "<div style='display:inline-block;height:50px;vertical-align:top;'>" +
                  "<span style='font-weight:bold;'>" + item.name + "</span><br/>" +
                  "<span>Código:" + item.code + " SKU: " + item.sku + "</span>" +
                "</div>" +
              "</li>";

    return fmt;
}

function SalesOrderDetailFormatter(item) {
    var fmt = "<li id='item" + item.id + "'>" +
                "<img style='min-width:50px;max-width:50px;height:50px;vertical-align:top;' src='" + item.url + "' title='" + item.name + "'/>" +
                "<div style='display:inline-block;height:60px;vertical-align:top;'>" +
                  "<span style='font-weight:bold;'>" + item.name + "</span><br/>" +
                  "<span><b>Código: </b>" + item.code + "<b> SKU: </b>" + item.sku +
                  "<br/><b> Supermostrador: </b>" + item.price +
                  "<b> Cantidad: </b><div class='edit' style='display:inline-block;min-width:60px;text-align:right;background:yellow;'>" + item.quantity + "</div></span>" +
                "</div>" +
              "</li>";

    return fmt;
}
