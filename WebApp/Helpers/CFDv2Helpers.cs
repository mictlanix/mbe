// 
// CashHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using Business.Essentials.Model;
using Mictlanix.CFDv2;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;

namespace Business.Essentials.WebApp.Helpers
{
	public static class CFDv2Helpers
	{
		public static string GetInvoiceXML (int id)
		{
			string xml;
			Comprobante cfd;
			SalesInvoice item;

			item = SalesInvoice.Find (id);
			
			if (!item.IsCompleted)
				return null;

			cfd = InvoiceToCFD (item);
			xml = SerializeToXML (cfd);

			return EncodeBase64 (xml);
		}

		public static Comprobante IssueCFD (SalesInvoice item)
		{
			Comprobante cfd;
			Taxpayer taxpayer;
			string batch;
			int serial;
			DateTime dt;

			dt = DateTime.Now;
			dt = new DateTime (dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Unspecified);
            
			batch = Configuration.InvoicesBatch;
			serial = (from x in SalesInvoice.Queryable
                      where x.Batch == batch
                      select x.Serial).Max ().GetValueOrDefault ();
			serial += 1;

			taxpayer = item.Issuer;

			cfd = new Comprobante
            {
                tipoDeComprobante = ComprobanteTipoDeComprobante.ingreso,
                noAprobacion = taxpayer.ApprovalNumber.ToString (),
                anoAprobacion = taxpayer.ApprovalYear.ToString (),
                noCertificado = taxpayer.CertificateNumber.ToString ().PadLeft (20, '0'),
                serie = batch,
                folio = serial.ToString (),
                fecha = dt,
                formaDePago = "Pago en una sola exhibición",
                certificado = Configuration.Certificate,
                Emisor = new ComprobanteEmisor
                {
                    nombre = taxpayer.Name,
                    rfc = taxpayer.Id,
                    DomicilioFiscal = new t_UbicacionFiscal
                    {
                        calle = taxpayer.Street,
                        noExterior = taxpayer.ExteriorNumber,
                        noInterior = taxpayer.InteriorNumber,
                        colonia = taxpayer.Neighborhood,
                        codigoPostal = taxpayer.ZipCode,
                        municipio = taxpayer.Borough,
                        estado = taxpayer.State,
                        pais = taxpayer.Country
                    }
                },
                Receptor = new ComprobanteReceptor
                {
                    nombre = item.BillToName,
                    rfc = item.BillToTaxId,
                    Domicilio = new t_Ubicacion
                    {
                        calle = item.Street,
                        noExterior = item.ExteriorNumber,
                        noInterior = item.InteriorNumber,
                        colonia = item.Neighborhood,
                        codigoPostal = item.ZipCode,
                        municipio = item.Borough,
                        estado = item.State,
                        pais = item.Country
                    }
                },
                Conceptos = new ComprobanteConcepto[item.Details.Count],
                Impuestos = new ComprobanteImpuestos
                {
                    Traslados = new ComprobanteImpuestosTraslado[1]
                }
            };
            
			int i = 0;
			foreach (SalesInvoiceDetail detail in item.Details) {
				cfd.Conceptos [i++] = new ComprobanteConcepto
                {
                    cantidad = detail.Quantity,
                    unidad = detail.UnitOfMeasurement,
                    noIdentificacion = detail.ProductCode,
                    descripcion = detail.ProductName,
                    valorUnitario = detail.Price,
                    importe = detail.Total
                };
			}

			cfd.Impuestos.Traslados [0] = new ComprobanteImpuestosTraslado
            {
                impuesto = ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.VAT * 100m
            };
			
			cfd.descuentoSpecified = true;
			// cfd.descuento = 
			
			cfd.subTotal = item.Subtotal;
			cfd.total = item.Total;

			string original_string = OriginalString (cfd);
			cfd.sello = SHA1WithRSA (Convert.FromBase64String (Configuration.PrivateKey),
                                     Convert.FromBase64String (Configuration.PrivateKeyPassword),
                                     original_string);

			item.ApprovalNumber = int.Parse (cfd.noAprobacion);
			item.ApprovalYear = int.Parse (cfd.anoAprobacion);
			item.CertificateNumber = decimal.Parse (cfd.noCertificado);
			item.Issued = cfd.fecha;
			item.Batch = cfd.serie;
			item.Serial = int.Parse (cfd.folio);
			item.OriginalString = original_string;
			item.DigitalSeal = cfd.sello;

			File.WriteAllText (string.Format (@"{0}\DataPrint-CFD-{1}-{2:D5}.xml",
                                              Configuration.CFDsPath,
                                              item.Batch, item.Serial),
                              				  SerializeToXML (cfd));

			return cfd;
		}

		public static Comprobante InvoiceToCFD (SalesInvoice item)
		{
			Comprobante cfd;
			Taxpayer taxpayer;

			taxpayer = item.Issuer;

			cfd = new Comprobante
            {
                tipoDeComprobante = ComprobanteTipoDeComprobante.ingreso,
                noAprobacion = item.ApprovalNumber.ToString (),
                anoAprobacion = item.ApprovalYear.ToString (),
                noCertificado = item.CertificateNumber.ToString ().PadLeft (20, '0'),
                serie = item.Batch,
                folio = item.Serial.ToString (),
                fecha = item.Issued.Value,
                formaDePago = "Pago en una sola exhibición",
                certificado = Configuration.Certificate,
                sello = item.DigitalSeal,
                Emisor = new ComprobanteEmisor
                {
                    nombre = taxpayer.Name,
                    rfc = taxpayer.Id,
                    DomicilioFiscal = new t_UbicacionFiscal
                    {
                        calle = taxpayer.Street,
                        noExterior = taxpayer.ExteriorNumber,
                        noInterior = taxpayer.InteriorNumber,
                        colonia = taxpayer.Neighborhood,
                        codigoPostal = taxpayer.ZipCode,
                        municipio = taxpayer.Borough,
                        estado = taxpayer.State,
                        pais = taxpayer.Country
                    }
                },
                Receptor = new ComprobanteReceptor
                {
                    nombre = item.BillToName,
                    rfc = item.BillToTaxId,
                    Domicilio = new t_Ubicacion
                    {
                        calle = item.Street,
                        noExterior = item.ExteriorNumber,
                        noInterior = item.InteriorNumber,
                        colonia = item.Neighborhood,
                        codigoPostal = item.ZipCode,
                        municipio = item.Borough,
                        estado = item.State,
                        pais = item.Country
                    }
                },
                Conceptos = new ComprobanteConcepto[item.Details.Count],
                Impuestos = new ComprobanteImpuestos
                {
                    Traslados = new ComprobanteImpuestosTraslado[1]
                },
                subTotal = item.Subtotal,
                total = item.Total
            };

			int i = 0;
			foreach (SalesInvoiceDetail detail in item.Details) {
				cfd.Conceptos [i++] = new ComprobanteConcepto
                {
                    cantidad = detail.Quantity,
                    unidad = detail.UnitOfMeasurement,
                    noIdentificacion = detail.ProductCode,
                    descripcion = detail.ProductName,
                    valorUnitario = detail.Price,
                    importe = detail.Total
                };
			}

			cfd.Impuestos.Traslados [0] = new ComprobanteImpuestosTraslado
            {
                impuesto = ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Math.Round (item.Details.Select (x => new decimal? (x.TaxRate)).Max ().GetValueOrDefault () * 100m, 2)
            };

			return cfd;
		}

		static string OriginalString (Comprobante cfd)
		{
			string xml = SerializeToXML (cfd);
			var resolver = new EmbeddedResourceResolver ();
			MemoryStream input = new MemoryStream (Encoding.UTF8.GetBytes (xml));
			Stream xsl_stream = resolver.GetResource ("cadenaoriginal_2_0.xslt");

			using (StringWriter output = new StringWriter()) {
				XslCompiledTransform xslt = new XslCompiledTransform ();
				xslt.Load (XmlReader.Create (xsl_stream), XsltSettings.TrustedXslt, resolver);
				xslt.Transform (XmlReader.Create (input), null, output);
				return output.ToString ();
			}
		}

		static string SHA1WithRSA (byte[] data, byte[] password, string message)
		{
			ISigner signer;
			byte[] signature;
			AsymmetricKeyParameter key;
            
			key = PrivateKeyFactory.DecryptKey (UTF8Encoding.UTF8.GetString (password).ToCharArray (), data);
			signer = SignerUtilities.GetSigner ("SHA1WithRSA");
			signer.Init (true, key);

			data = System.Text.Encoding.UTF8.GetBytes (message);
			signer.BlockUpdate (data, 0, data.Length);
			signature = signer.GenerateSignature ();

			return Convert.ToBase64String (signature);
		}

		static string SerializeToXML<T> (T obj)
		{
			MemoryStream ms;
			XmlSerializer xs;
			XmlTextWriter xml;

			ms = new MemoryStream ();
			xs = new XmlSerializer (typeof(T));
			xml = new XmlTextWriter (ms, Encoding.UTF8);

			XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces (new XmlQualifiedName[] {
                        new XmlQualifiedName ("", "http://www.sat.gob.mx/cfd/2"),
                        new XmlQualifiedName ("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    });

			xs.Serialize (xml, obj, xmlns);

			return Encoding.UTF8.GetString (ms.ToArray ());
		}
        
		static public byte[] DecodeBase64 (string data)
		{
			return System.Convert.FromBase64String (data);
		}

		static public string EncodeBase64 (byte[] data)
		{
			return System.Convert.ToBase64String (data, Base64FormattingOptions.None);
		}

		static public string EncodeBase64 (string data)
		{
			return EncodeBase64 (Encoding.UTF8.GetBytes (data));
		}
	}
}
