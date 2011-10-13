using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.WebApp.Utils;

namespace Business.Essentials.WebApp.Controllers
{
    public class BarcodesController : Controller
    {
        //
        // GET: /Barcodes/Code128/abc123

        public ActionResult Code128(string id)
        {
            var ms = new MemoryStream(4 * 1024);
            var barcode = Code128Rendering.MakeBarcodeImage(id, 2, false);

            barcode.Save(ms, ImageFormat.Jpeg);
            ms.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(ms, "image/jpeg");
            result.FileDownloadName = string.Format("{0}.jpg", id);

            return result;
        }

    }
}
