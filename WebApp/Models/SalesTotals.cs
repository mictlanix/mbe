using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.WebApp.Models
{
    public class SalesTotals 
    {
        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public decimal Subtotal { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Taxes", ResourceType = typeof(Resources))]
        public decimal Taxes
        {
            get { return Total - Subtotal; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Total", ResourceType = typeof(Resources))]
        public decimal Total { get; set; }
    }
}
