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