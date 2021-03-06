using System;

namespace Mictlanix.BE.Web.Utils {
	public class CurrencyConverter {
		private static string [] MajorNames = {
	    "",
	    " mil",
	    " mill",
	    " mil mill",
	    " bill",
	    " mil bill",
	    " trill" };
		private static string [] HundredsNames = {
	    "",
	    " cien",
	    " doscientos",
	    " trescientos",
	    " cuatrocientos",
	    " quinientos",
	    " seiscientos",
	    " setecientos",
	    " ochocientos",
	    " novecientos" };
		private static string [] TensNames = {
	    "",
	    " diez",
	    " veinte",
	    " treinta",
	    " cuarenta",
	    " cincuenta",
	    " sesenta",
	    " setenta",
	    " ochenta",
	    " noventa" };
		private static string [] NumberNames = {
	    "",
	    " uno",
	    " dos",
	    " tres",
	    " cuatro",
	    " cinco",
	    " seis",
	    " siete",
	    " ocho",
	    " nueve",
	    " diez",
	    " once",
	    " doce",
	    " trece",
	    " catorce",
	    " quince",
	    " dieciséis",
	    " diecisiete",
	    " dieciocho",
	    " diecinueve",
	    " veinte",
	    " veintiuno",
	    " veintidós",
	    " veintitrés",
	    " veinticuatro",
	    " veinticinco",
	    " veintiséis",
	    " veintisiete",
	    " veintiocho",
	    " veintinueve" };

		private static string ConvertLessThanOneThousand (int number)
		{
			string soFar;

			if (number % 100 < 30) {
				soFar = NumberNames [number % 100];
				number /= 100;
			} else {
				soFar = NumberNames [number % 10];
				number /= 10;

				soFar = TensNames [number % 10] + ("".Equals (soFar) ? "" : " y")
			+ soFar;
				number /= 10;
			}

			if (number == 0) {
				return soFar;
			} else if (number == 1) {
				return "".Equals (soFar) ? "cien" : "ciento" + soFar;
			}

			return HundredsNames [number] + soFar;
		}

		public static string Convert (int number)
		{
			if (number == 0) {
				return "cero";
			}

			string prefix = "";

			if (number < 0) {
				number = -number;
				prefix = "negativo";
			}

			string soFar = "";
			int place = 0;

			do {
				int n = number % 1000;
				if (n != 0) {
					string s = ConvertLessThanOneThousand (n);
					if (place > 0) {
						soFar = (" uno".Equals (s) ? " un" : s) + MajorNames [place] +
							(place > 1 ? (n > 1 ? "ones" : "ón") : " ") + soFar;
					} else {
						soFar = s + soFar;
					}
				}
				place++;
				number /= 1000;
			} while (number > 0);

			return (prefix + soFar).Trim ();
		}

		public static string ToMXN (decimal d)
		{
			int units;
			int cents;
			string text;

			d = Math.Round (d, 2, MidpointRounding.AwayFromZero);
			units = (int) Math.Floor (d);
			cents = (int) Math.Round ((d - units) * 100);
			text = Convert (units);

			return ((units == 1 && cents == 0) ? "un peso " : text + " pesos ") +
				cents.ToString ("00") + "/100 m. n.";
		}

		public static string ToUSD (decimal d)
		{
			int units;
			int cents;
			string text;

			d = Math.Round (d, 2, MidpointRounding.AwayFromZero);
			units = (int) Math.Floor (d);
			cents = (int) Math.Round ((d - units) * 100);
			text = Convert (units);

			return ((units == 1 && cents == 0) ? "un dólar americano " : text + " doláres americanos ") +
				cents.ToString ("00") + "/100 USD";
		}

		public static string ToEUR (decimal d)
		{
			int units;
			int cents;
			string text;

			d = Math.Round (d, 2, MidpointRounding.AwayFromZero);
			units = (int) Math.Floor (d);
			cents = (int) Math.Round ((d - units) * 100);
			text = Convert (units);

			return ((units == 1 && cents == 0) ? "un euro " : text + " euros ") +
				cents.ToString ("00") + "/100 EUR";
		}
	}
}

