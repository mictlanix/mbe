// 
// CFDHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2018 Mictlanix SAS de CV and contributors.
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
using System.Linq;
using Mictlanix.BE.Model;
using Mictlanix.CFDv33;
using Mictlanix.ProFact.Client;

namespace Mictlanix.BE.Web.Helpers {
	internal static class CFDHelpers {

		public static Comprobante IssueCFD (FiscalDocument item)
		{
			return ProFactStamp (item);
		}

		public static bool CancelCFD (FiscalDocument item)
		{
			if (item.IsCancelled) {
				return false;
			}

			return ProFactCancel (item);
		}

		public static Comprobante SignCFD (FiscalDocument item)
		{
			var cfd = InvoiceToCFDv33 (item);
			var cer = item.Issuer.Certificates.Single (x => x.Id == item.IssuerCertificateNumber);

			cfd.Sign (cer.KeyData, cer.KeyPassword);

			return cfd;
		}

		static Comprobante ProFactStamp (FiscalDocument item)
		{
			var cfd = SignCFD (item);
			var cli = new ProFactClient (WebConfig.ProFactUser, WebConfig.ProFactUrl);
			var id = string.Format ("{0}-{1:D6}", WebConfig.ProFactCode, item.Id);
			//System.IO.File.WriteAllText ("cfd.xml", cfd.ToXmlString ());
			var tfd = cli.Stamp (id, cfd);

			cfd.Complemento = new List<object> ();
			cfd.Complemento.Add (tfd);

			return cfd;
		}

		static bool ProFactCancel (FiscalDocument item)
		{
			if (item.Version > 3.2m) {
				var cli = new ProFactClient (WebConfig.ProFactUser, WebConfig.ProFactUrl);
				return cli.Cancel (item.Issuer.Id, item.StampId);
			} else {
				var cli = new ProFactClient (WebConfig.ProFactUser, WebConfig.ProFactUrlV32);
				return cli.CancelV32 (item.Issuer.Id, item.StampId);
			}
		}

		public static bool PrivateKeyTest (byte [] data, byte [] password)
		{
			return CFDLib.Utils.PrivateKeyTest (data, password);
		}

		static Comprobante InvoiceToCFDv33 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);
			var cfd = new Comprobante {
				TipoDeComprobante = (c_TipoDeComprobante)FDT2TDC (item.Type),
				NoCertificado = item.IssuerCertificateNumber.PadLeft (20, '0'),
				Serie = item.Batch,
				Folio = item.Serial.ToString (),
				Fecha = item.Issued.GetValueOrDefault (),
				MetodoPago = item.Terms == PaymentTerms.Immediate ? c_MetodoPago.PagoEnUnaSolaExhibicion : c_MetodoPago.PagoEnParcialidadesODiferido,
				MetodoPagoSpecified = true,
				FormaPago = (c_FormaPago)(int)item.PaymentMethod,
				FormaPagoSpecified = true,
				LugarExpedicion = item.IssuedLocation,
				SubTotal = item.Subtotal,
				Total = item.Total,
				Moneda = item.Currency.GetDisplayName (),
				TipoCambio = item.ExchangeRate,
				TipoCambioSpecified = item.Currency != CurrencyCode.MXN,
				Sello = item.IssuerDigitalSeal,
				Certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				Emisor = new ComprobanteEmisor {
					Rfc = item.Issuer.Id,
					Nombre = item.IssuerName,
					RegimenFiscal = (c_RegimenFiscal)int.Parse (item.IssuerRegime.Id),
				},
				Receptor = new ComprobanteReceptor {
					Rfc = item.Recipient,
					Nombre = item.RecipientName,
					UsoCFDI = CfdiUsage2UsoCFDI (item.Usage.Id)
				},
				Conceptos = new ComprobanteConcepto [item.Details.Count]
			};

			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i] = new ComprobanteConcepto {
					Cantidad = detail.Quantity,
					ClaveUnidad = detail.UnitOfMeasurement.Id,
					Unidad = detail.UnitOfMeasurementName,
					NoIdentificacion = detail.ProductCode,
					ClaveProdServ = detail.ProductService.Id,
					Descripcion = detail.ProductName,
					ValorUnitario = detail.NetPrice,
					Importe = detail.Subtotal,
					Descuento = detail.Discount,
					DescuentoSpecified = detail.Discount > 0m
				};

				if (detail.Subtotal == detail.Discount) {
					i++;
					continue;
				}

				if (detail.TaxRate >= 0m) {
					cfd.Conceptos [i].Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado [] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = detail.TaxBase,
								Importe = detail.Taxes,
								ImporteSpecified = true,
								TasaOCuota = detail.TaxRate,
								TasaOCuotaSpecified = true
							}
						},
						Retenciones = item.RetentionRate <= 0m ? null : new ComprobanteConceptoImpuestosRetencion [] {
							new ComprobanteConceptoImpuestosRetencion {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = detail.TaxBase,
								Importe = detail.RetentionTaxes,
								TasaOCuota = item.RetentionRate
							}
						}
					};
				} else {
					cfd.Conceptos [i].Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado [] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Exento,
								Base = detail.TaxBase
							}
						}
					};
				}

				i++;
			}

			if (item.Discount > 0) {
				cfd.Descuento = item.Discount;
				cfd.DescuentoSpecified = true;
			}

			cfd.Impuestos = new ComprobanteImpuestos ();

			var taxes = new List<ComprobanteImpuestosTraslado> ();

			if (cfd.Conceptos.Any (c => c.Impuestos != null && c.Impuestos.Traslados.Any (x => x.TipoFactor == c_TipoFactor.Exento))) {
				taxes.Add (new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Exento
				});
			}

			if (cfd.Conceptos.Any (c => c.Impuestos != null && c.Impuestos.Traslados.Any (x => x.TasaOCuota == decimal.Zero))) {
				taxes.Add (new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					TasaOCuota = 0.000000m,
					Importe = 0.00m
				});
			}

			if (cfd.Conceptos.Any (c => c.Impuestos != null && c.Impuestos.Traslados.Any (x => x.TasaOCuota == WebConfig.DefaultVAT))) {
				taxes.Add (new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					TasaOCuota = WebConfig.DefaultVAT,
					Importe = cfd.Conceptos.Where (x => x.Impuestos != null).Sum (c => c.Impuestos.Traslados.Where (x => x.TasaOCuota == WebConfig.DefaultVAT).Sum (x => x.Importe))
				});
			}

			cfd.Impuestos.Traslados = taxes.ToArray ();
			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Impuestos.Traslados.Sum (x => x.Importe);
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;

			if (item.RetentionRate > 0m) {
				cfd.Impuestos.Retenciones = new ComprobanteImpuestosRetencion [] {
					new ComprobanteImpuestosRetencion {
						Impuesto = c_Impuesto.IVA,
						Importe = item.RetentionTaxes,
					}
				};

				cfd.Impuestos.TotalImpuestosRetenidos = cfd.Impuestos.Retenciones.Sum (x => x.Importe);
				cfd.Impuestos.TotalImpuestosRetenidosSpecified = true;
			}

			return cfd;
		}

		// FiscalDocumentType -> TipoDeComprobante
		static int FDT2TDC (FiscalDocumentType type)
		{
			switch (type) {
			case FiscalDocumentType.Invoice:
			case FiscalDocumentType.FeeReceipt:
			case FiscalDocumentType.RentReceipt:
			case FiscalDocumentType.DebitNote:
				return (int)c_TipoDeComprobante.Ingreso;
			case FiscalDocumentType.CreditNote:
				return (int)c_TipoDeComprobante.Egreso;
			case FiscalDocumentType.PaymentReceipt:
				return (int)c_TipoDeComprobante.Pago;
			}

			throw new ArgumentOutOfRangeException (nameof (type));
		}

		// FiscalDocumentType -> TipoDeComprobante
		static c_UsoCFDI CfdiUsage2UsoCFDI (string code)
		{
			switch (code) {
			case "G01":
				return c_UsoCFDI.AdquisicionDeMercancias;
			case "G02":
				return c_UsoCFDI.DevolucionesDescuentosOBonificaciones;
			case "G03":
				return c_UsoCFDI.GastosEnGeneral;
			case "I01":
				return c_UsoCFDI.Construcciones;
			case "I02":
				return c_UsoCFDI.MobilarioYEquipoDeOficinaPorInversiones;
			case "I03":
				return c_UsoCFDI.EquipoDeTransporte;
			case "I04":
				return c_UsoCFDI.EquipoDeComputoYAccesorios;
			case "I05":
				return c_UsoCFDI.DadosTroquelesMoldesMatricesYHerramental;
			case "I06":
				return c_UsoCFDI.ComunicacionesTelefonicas;
			case "I07":
				return c_UsoCFDI.ComunicacionesSatelitales;
			case "I08":
				return c_UsoCFDI.OtraMaquinariaYEquipo;
			case "D01":
				return c_UsoCFDI.HonorariosMedicosDentalesYGastosHospitalarios;
			case "D02":
				return c_UsoCFDI.GastosMedicosPorIncapacidadODiscapacidad;
			case "D03":
				return c_UsoCFDI.GastosFunerales;
			case "D04":
				return c_UsoCFDI.Donativos;
			case "D05":
				return c_UsoCFDI.InteresesRealesEfectivamentePagadosPorCreditosHipotecarios;
			case "D06":
				return c_UsoCFDI.AportacionesVoluntariasAlSAR;
			case "D07":
				return c_UsoCFDI.PrimasPorSegurosDeGastosMedicos;
			case "D08":
				return c_UsoCFDI.GastosDeTransportacionEscolarObligatoria;
			case "D09":
				return c_UsoCFDI.DepositosEnCuentasParaElAhorroPrimasQueTenganComoBasePlanesDePensiones;
			case "D10":
				return c_UsoCFDI.PagosPorServiciosEducativos;
			case "P01":
				return c_UsoCFDI.PorDefinir;
			}

			throw new ArgumentOutOfRangeException (nameof (code));
		}

	}
}
