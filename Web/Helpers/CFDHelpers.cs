// 
// CFDHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Mictlanix.BE.Model;
using Mictlanix.CFDLib;
using Mictlanix.FiscoClic.Client;
using Mictlanix.Servisim.Client;
using Mictlanix.ProFact.Client;

namespace Mictlanix.BE.Web.Helpers
{
	internal static class CFDHelpers
	{
		static readonly decimal CFDI_MIN_VERSION = 3.0m;

		public static dynamic IssueCFD (FiscalDocument item)
		{
			if (item.Issuer.Scheme == FiscalScheme.CFD) {
				return SignCFD (item);
			}

			switch (item.Provider) {
			case FiscalCertificationProvider.FiscoClic:
				return FiscoClicStamp (item);
			case FiscalCertificationProvider.Servisim:
				return ServisimStamp (item);
			case FiscalCertificationProvider.ProFact:
				return ProFactStamp (item);
			default:
				return null;
			}
		}

		public static bool CancelCFD (FiscalDocument item)
		{
			if (item.IsCancelled) {
				return false;
			}

			if (!item.IsCompleted || item.Version < CFDI_MIN_VERSION) {
				return true;
			}

			switch (item.Provider) {
			case FiscalCertificationProvider.FiscoClic:
				return FiscoClicCancel (item);
			case FiscalCertificationProvider.Servisim:
			case FiscalCertificationProvider.ProFact:
				return ProFactCancel (item);
			}

			return true;
		}

		public static dynamic SignCFD (FiscalDocument item)
		{
			var cfd = InvoiceToCFD (item);
			var cer = item.Issuer.Certificates.Single (x => x.Id == item.IssuerCertificateNumber);

			cfd.Sign (cer.KeyData, cer.KeyPassword);

			return cfd;
		}

		// TODO: credentials per taxpayer
		static Mictlanix.CFDv32.Comprobante FiscoClicStamp (FiscalDocument item)
		{
			var cfd = SignCFD (item);
			var cli = new FiscoClicClient (WebConfig.FiscoClicUser,
			                               WebConfig.FiscoClicPasswd,
			                               WebConfig.FiscoClicUrl);
			var tfd = cli.Stamp (cfd);

			cfd.Complemento = new List<object> ();
			cfd.Complemento.Add (tfd);

			return cfd;
		}

		// TODO: credentials per taxpayer
		static bool FiscoClicCancel (FiscalDocument item)
		{
			var cli = new FiscoClicClient (WebConfig.FiscoClicUser,
			                               WebConfig.FiscoClicPasswd,
			                               WebConfig.FiscoClicUrl);

			return cli.Cancel (item.Issuer.Id, item.StampId);
		}

		// TODO: credentials per taxpayer
		static Mictlanix.CFDv32.Comprobante ServisimStamp (FiscalDocument item)
		{
			var cfd = SignCFD (item);
			var cli = new ServisimClient (WebConfig.ServisimUser,
				WebConfig.ServisimPasswd, WebConfig.ServisimUrl);
			var id = string.Format ("{0}-{1:D6}", WebConfig.ServisimPartnerCode, item.Id);
			var timer = new System.Diagnostics.Stopwatch ();

			cli.EndRequest += (object sender, RequestEventArgs e) => {
				timer.Stop ();

				try {
					string text = "Time: " + timer.ElapsedMilliseconds + " ms\n" +
								"Request:\n" + e.Request + "\n" +
								"Response:\n" + e.Response + "\n";
					string path = HttpContext.Current.Server.MapPath (string.Format (WebConfig.LogFilePattern, id, DateTime.Now));
					File.WriteAllText (path, text);
				} catch {
				}
			};

			timer.Start ();
			var tfd = cli.Stamp (id, cfd);

			cfd.Complemento = new List<object> ();
			cfd.Complemento.Add (tfd);

			return cfd;
		}

		// TODO: credentials per taxpayer
		static bool ServisimCancel (FiscalDocument item)
		{
			var cli = new ServisimClient (WebConfig.ServisimUser,
				WebConfig.ServisimPasswd, WebConfig.ServisimUrl);

			return cli.Cancel (item.Issuer.Id, item.StampId);
		}

		static Mictlanix.CFDv32.Comprobante ProFactStamp (FiscalDocument item)
		{
			var cfd = SignCFD (item);
			var cli = new ProFactClient (WebConfig.ProFactUser, WebConfig.ProFactUrl);
			var id = string.Format ("{0}-{1:D6}", WebConfig.ProFactCode, item.Id);
			var tfd = cli.Stamp (id, cfd);

			cfd.Complemento = new List<object> ();
			cfd.Complemento.Add (tfd);

			return cfd;
		}

		static bool ProFactCancel (FiscalDocument item)
		{
			var cli = new ProFactClient (WebConfig.ProFactUser, WebConfig.ProFactUrl);

			return cli.Cancel (item.Issuer.Id, item.StampId);
		}

		public static bool PrivateKeyTest (byte[] data, byte[] password)
		{
			return Mictlanix.CFDLib.Utils.PrivateKeyTest (data, password);
		}

		public static dynamic InvoiceToCFD (FiscalDocument item)
		{
			switch (Convert.ToInt32 (item.Version * 10)) {
			case 32:
				return InvoiceToCFDv32 (item);
			case 22:
				return InvoiceToCFDv22 (item);
			case 20:
				return InvoiceToCFDv20 (item);
			}
			
			switch (item.Issuer.Scheme) {
			case FiscalScheme.CFD:
				return InvoiceToCFDv22 (item);
			case FiscalScheme.CFDI:
				return InvoiceToCFDv32 (item);
			}

			return null;
		}
		
		static Mictlanix.CFDv32.Comprobante InvoiceToCFDv32 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);
			var cfd = new CFDv32.Comprobante {
				tipoDeComprobante = (Mictlanix.CFDv32.ComprobanteTipoDeComprobante)FDT2TDC (item.Type),
				noCertificado = item.IssuerCertificateNumber.PadLeft (20, '0'),
				serie = item.Batch,
				folio = item.Serial.ToString (),
				fecha = item.Issued.GetValueOrDefault (),
				metodoDePago = item.PaymentMethod.GetDisplayName(),
				NumCtaPago = item.PaymentReference,
				LugarExpedicion = item.IssuedLocation,
				subTotal = item.Subtotal,
				total = item.Total,
				Moneda = item.Currency.GetDisplayName (),
				TipoCambio = item.FormattedValueFor (x => x.ExchangeRate),
				sello = item.IssuerDigitalSeal,
				formaDePago = Resources.SinglePayment,
				certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				Emisor = new Mictlanix.CFDv32.ComprobanteEmisor {
					rfc = item.Issuer.Id,
					nombre = item.IssuerName,
					RegimenFiscal = new Mictlanix.CFDv32.ComprobanteEmisorRegimenFiscal [1] {
						new Mictlanix.CFDv32.ComprobanteEmisorRegimenFiscal {
							Regimen = item.IssuerRegime
						}
					},
					DomicilioFiscal = (item.IssuerAddress == null) ? null : new Mictlanix.CFDv32.t_UbicacionFiscal {
						calle = item.IssuerAddress.Street,
						noExterior = item.IssuerAddress.ExteriorNumber,
						noInterior = item.IssuerAddress.InteriorNumber,
						colonia = item.IssuerAddress.Neighborhood,
						codigoPostal = item.IssuerAddress.PostalCode,
						localidad = item.IssuerAddress.Locality,
						municipio = item.IssuerAddress.Borough,
						estado = item.IssuerAddress.State,
						pais = item.IssuerAddress.Country
					},
					ExpedidoEn = (item.IssuedAt == null) ? null : new Mictlanix.CFDv32.t_Ubicacion {
						calle = item.IssuedAt.Street,
						noExterior = item.IssuedAt.ExteriorNumber,
						noInterior = item.IssuedAt.InteriorNumber,
						colonia = item.IssuedAt.Neighborhood,
						codigoPostal = item.IssuedAt.PostalCode,
						localidad = item.IssuedAt.Locality,
						municipio = item.IssuedAt.Borough,
						estado = item.IssuedAt.State,
						pais = item.IssuedAt.Country
					}
				},
				Receptor = new Mictlanix.CFDv32.ComprobanteReceptor {
					rfc = item.Recipient,
					nombre = item.RecipientName,
					Domicilio = (item.RecipientAddress == null) ? null : new Mictlanix.CFDv32.t_Ubicacion {
						calle = item.RecipientAddress.Street,
						noExterior = item.RecipientAddress.ExteriorNumber,
						noInterior = item.RecipientAddress.InteriorNumber,
						colonia = item.RecipientAddress.Neighborhood,
						codigoPostal = item.RecipientAddress.PostalCode,
						localidad = item.RecipientAddress.Locality,
						municipio = item.RecipientAddress.Borough,
						estado = item.RecipientAddress.State,
						pais = item.RecipientAddress.Country
					}
				},
				Conceptos = new Mictlanix.CFDv32.ComprobanteConcepto [item.Details.Count],
				Impuestos = new Mictlanix.CFDv32.ComprobanteImpuestos {
					Traslados = new Mictlanix.CFDv32.ComprobanteImpuestosTraslado [1]
				}
			};

			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i++] = new Mictlanix.CFDv32.ComprobanteConcepto {
					cantidad = detail.Quantity,
					unidad = detail.UnitOfMeasurement,
					noIdentificacion = detail.ProductCode,
					descripcion = detail.ProductName,
					valorUnitario = detail.NetPrice,
					importe = detail.Subtotal
				};
			}

			// TODO: VAT Summaries
			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv32.ComprobanteImpuestosTraslado {
				impuesto = Mictlanix.CFDv32.ComprobanteImpuestosTrasladoImpuesto.IVA,
				importe = item.Taxes,
				tasa = WebConfig.DefaultVAT * 100m
			};

			cfd.Impuestos.totalImpuestosTrasladados = cfd.Impuestos.Traslados.Sum (x => x.importe);
			cfd.Impuestos.totalImpuestosTrasladadosSpecified = true;

			if (item.RetentionRate > 0m) {
				cfd.Impuestos.Retenciones = new Mictlanix.CFDv32.ComprobanteImpuestosRetencion[] {
					new Mictlanix.CFDv32.ComprobanteImpuestosRetencion {
						impuesto = Mictlanix.CFDv32.ComprobanteImpuestosRetencionImpuesto.IVA,
						importe = item.RetentionTaxes
					}
				};

				cfd.Impuestos.totalImpuestosRetenidos = cfd.Impuestos.Retenciones.Sum (x => x.importe);
				cfd.Impuestos.totalImpuestosRetenidosSpecified = true;
			}

			return cfd;
		}

		static Mictlanix.CFDv22.Comprobante InvoiceToCFDv22 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);
			var cfd = new Mictlanix.CFDv22.Comprobante {
				tipoDeComprobante = (Mictlanix.CFDv22.ComprobanteTipoDeComprobante)FDT2TDC (item.Type),
				noAprobacion = item.ApprovalNumber.ToString (),
				anoAprobacion = item.ApprovalYear.ToString (),
				noCertificado = item.IssuerCertificateNumber.ToString ().PadLeft (20, '0'),
				serie = item.Batch,
				folio = item.Serial.ToString (),
				fecha = item.Issued.GetValueOrDefault (),
				metodoDePago = item.PaymentMethod.GetDisplayName(),
				NumCtaPago = item.PaymentReference,
				LugarExpedicion = item.IssuedLocation,
				subTotal = item.Subtotal,
				total = item.Total,
				Moneda = item.Currency.GetDisplayName (),
				TipoCambio = item.FormattedValueFor (x => x.ExchangeRate),
				sello = item.IssuerDigitalSeal,
				formaDePago = Resources.SinglePayment,
				certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				Emisor = new Mictlanix.CFDv22.ComprobanteEmisor {
					rfc = item.Issuer.Id,
					nombre = item.IssuerName,
					RegimenFiscal = new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal [1] {
						new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal {
							Regimen = item.IssuerRegime
						}
					},
					DomicilioFiscal = (item.IssuerAddress == null) ? null : new Mictlanix.CFDv22.t_UbicacionFiscal {
                        calle = item.IssuerAddress.Street,
						noExterior = item.IssuerAddress.ExteriorNumber,
						noInterior = item.IssuerAddress.InteriorNumber,
						colonia = item.IssuerAddress.Neighborhood,
						codigoPostal = item.IssuerAddress.PostalCode,
						localidad = item.IssuerAddress.Locality,
						municipio = item.IssuerAddress.Borough,
						estado = item.IssuerAddress.State,
						pais = item.IssuerAddress.Country
                    },
                    ExpedidoEn = (item.IssuedAt == null) ? null : new Mictlanix.CFDv22.t_Ubicacion {
                        calle = item.IssuedAt.Street,
                        noExterior = item.IssuedAt.ExteriorNumber,
                        noInterior = item.IssuedAt.InteriorNumber,
                        colonia = item.IssuedAt.Neighborhood,
                        codigoPostal = item.IssuedAt.PostalCode,
						localidad = item.IssuedAt.Locality,
                        municipio = item.IssuedAt.Borough,
                        estado = item.IssuedAt.State,
                        pais = item.IssuedAt.Country
                    }
                },
				Receptor = new Mictlanix.CFDv22.ComprobanteReceptor {
					rfc = item.Recipient,
                    nombre = item.RecipientName,
					Domicilio = (item.RecipientAddress == null) ? null : new Mictlanix.CFDv22.t_Ubicacion {
                        calle = item.RecipientAddress.Street,
						noExterior = item.RecipientAddress.ExteriorNumber,
						noInterior = item.RecipientAddress.InteriorNumber,
						colonia = item.RecipientAddress.Neighborhood,
						codigoPostal = item.RecipientAddress.PostalCode,
						localidad = item.RecipientAddress.Locality,
						municipio = item.RecipientAddress.Borough,
						estado = item.RecipientAddress.State,
						pais = item.RecipientAddress.Country
                    }
                },
				Conceptos = new Mictlanix.CFDv22.ComprobanteConcepto [item.Details.Count],
				Impuestos = new Mictlanix.CFDv22.ComprobanteImpuestos {
                    Traslados = new Mictlanix.CFDv22.ComprobanteImpuestosTraslado [1]
                }
			};
            
			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i++] = new Mictlanix.CFDv22.ComprobanteConcepto {
					cantidad = detail.Quantity,
					unidad = detail.UnitOfMeasurement,
					noIdentificacion = detail.ProductCode,
					descripcion = detail.ProductName,
					valorUnitario = detail.NetPrice,
					importe = detail.Subtotal
				};
			}

			// TODO: VAT Summaries
			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv22.ComprobanteImpuestosTraslado {
				impuesto = Mictlanix.CFDv22.ComprobanteImpuestosTrasladoImpuesto.IVA,
				importe = item.Taxes,
				tasa = WebConfig.DefaultVAT * 100m
			};
			cfd.Impuestos.totalImpuestosTrasladados = cfd.Impuestos.Traslados.Sum (x => x.importe);
			cfd.Impuestos.totalImpuestosTrasladadosSpecified = true;

			return cfd;
		}
		
		static Mictlanix.CFDv20.Comprobante InvoiceToCFDv20 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);

			var cfd = new Mictlanix.CFDv20.Comprobante {
				tipoDeComprobante = (Mictlanix.CFDv20.ComprobanteTipoDeComprobante)FDT2TDC (item.Type),
				noAprobacion = item.ApprovalNumber.ToString (),
				anoAprobacion = item.ApprovalYear.ToString (),
				noCertificado = item.IssuerCertificateNumber.ToString ().PadLeft (20, '0'),
				serie = item.Batch,
				folio = item.Serial.ToString (),
				fecha = item.Issued.GetValueOrDefault (),
				formaDePago = Resources.SinglePayment,
				certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				Emisor = new Mictlanix.CFDv20.ComprobanteEmisor
				{
					rfc = item.Issuer.Id,
					nombre = item.IssuerName,
					DomicilioFiscal = new Mictlanix.CFDv20.t_UbicacionFiscal
					{
						calle = item.IssuerAddress.Street,
						noExterior = item.IssuerAddress.ExteriorNumber,
						noInterior = item.IssuerAddress.InteriorNumber,
						colonia = item.IssuerAddress.Neighborhood,
						codigoPostal = item.IssuerAddress.PostalCode,
						localidad = item.IssuerAddress.Locality,
						municipio = item.IssuerAddress.Borough,
						estado = item.IssuerAddress.State,
						pais = item.IssuerAddress.Country
					}
				},
				Receptor = new Mictlanix.CFDv20.ComprobanteReceptor
				{
					rfc = item.Recipient,
					nombre = item.RecipientName,
					Domicilio = new Mictlanix.CFDv20.t_Ubicacion
					{
						calle = item.RecipientAddress.Street,
						noExterior = item.RecipientAddress.ExteriorNumber,
						noInterior = item.RecipientAddress.InteriorNumber,
						colonia = item.RecipientAddress.Neighborhood,
						codigoPostal = item.RecipientAddress.PostalCode,
						localidad = item.RecipientAddress.Locality,
						municipio = item.RecipientAddress.Borough,
						estado = item.RecipientAddress.State,
						pais = item.RecipientAddress.Country
					}
				},
				Conceptos = new Mictlanix.CFDv20.ComprobanteConcepto[item.Details.Count],
				Impuestos = new Mictlanix.CFDv20.ComprobanteImpuestos
				{
					Traslados = new Mictlanix.CFDv20.ComprobanteImpuestosTraslado[1]
				},
				subTotal = item.Subtotal,
				total = item.Total,
				sello = item.IssuerDigitalSeal
			};

			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i++] = new Mictlanix.CFDv20.ComprobanteConcepto {
					cantidad = detail.Quantity,
					unidad = detail.UnitOfMeasurement,
					noIdentificacion = detail.ProductCode,
					descripcion = detail.ProductName,
					valorUnitario = detail.NetPrice,
					importe = detail.Subtotal
				};
			}

			// TODO: VAT Summaries
			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv20.ComprobanteImpuestosTraslado {
				impuesto = Mictlanix.CFDv20.ComprobanteImpuestosTrasladoImpuesto.IVA,
				importe = item.Taxes,
				tasa = WebConfig.DefaultVAT * 100m
			};

			return cfd;
		}

		public static IEnumerable<CFDv2ReportItem> MonthlyReport (IEnumerable<FiscalDocument> items)
		{
			var list = new List<CFDv2ReportItem> (items.Count ());
			
			foreach (var item in items) {
				var state = true;
				var dup = false;
				
				if (item.IsCancelled) {
					var dt1 = item.Issued.Value;
					var dt2 = item.CancellationDate.Value;
										
					if (dt1.Year == dt2.Year && dt1.Month == dt2.Month) {
						dup = true;
					} else {
						state = false;
					}
				}
				
				var row = new CFDv2ReportItem {
					Taxpayer = item.Recipient,
					Batch = item.Batch,
					Serial = item.Serial.Value,
					ApprovalYear = item.ApprovalYear.Value,
					ApprovalNumber = item.ApprovalNumber.Value,
					Date = item.Issued.Value,
					Amount = item.Total,
					Taxes = item.Taxes,
					IsActive = state,
					Type = (CFDv2ReportItemType)FDT2TDC (item.Type)
				};
				
				list.Add (row);
				
				if (dup) {
					row = (CFDv2ReportItem)row.Clone ();
					row.IsActive = false;
					list.Add (row);
				}
			}
			
			return list;
		}

		// FiscalDocumentType -> TipoDeComprobante
		static int FDT2TDC (FiscalDocumentType type)
		{
			switch (type) {
			case FiscalDocumentType.Invoice:
			case FiscalDocumentType.FeeReceipt:
			case FiscalDocumentType.RentReceipt:
			case FiscalDocumentType.DebitNote:
				return (int)Mictlanix.CFDv32.ComprobanteTipoDeComprobante.ingreso;
			case FiscalDocumentType.CreditNote:
				return (int)Mictlanix.CFDv32.ComprobanteTipoDeComprobante.egreso;
			}

			throw new ArgumentOutOfRangeException ("type");
		}
	}
}
