using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace Business.Essentials.WebApp.Models
{
    public class Search
    {
        [Required]
        [Display(Name = "Pattern", ResourceType = typeof(Resources))]
        [StringLength(42, MinimumLength = 2, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Pattern { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }
}