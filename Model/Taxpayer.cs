using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{   
    [ActiveRecord("taxpayer")]
    public class Taxpayer : ActiveRecordLinqBase<Taxpayer>
    {
        public Taxpayer()
		{
		}

		[PrimaryKey("taxpayer_id")]
        public string Id { get; set; }
		
        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }
		
		[Property("approval_number")]
        [Display(Name = "ApprovalNumber", ResourceType = typeof(Resources))]
        public int ApprovalNumber { get; set; }
		
		[Property("approval_year")]
        [Display(Name = "ApprovalYear", ResourceType = typeof(Resources))]
        public int ApprovalYear { get; set; }
		
		[Property("certificate_number")]
        [Display(Name = "CertificateNumber", ResourceType = typeof(Resources))]
        public decimal CertificateNumber { get; set; }

		[Property]
        [Display(Name = "Street", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(45, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Street { get; set; }

        [Property("exterior_number")]
        [Display(Name = "ExteriorNumber", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ExteriorNumber { get; set; }

        [Property("interior_number")]
        [Display(Name = "InteriorNumber", ResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string InteriorNumber { get; set; }

        [Property]
        [Display(Name = "Neighborhood", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Neighborhood { get; set; }

        [Property]
        [Display(Name = "Borough", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Borough { get; set; }

        [Property]
        [Display(Name = "State", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string State { get; set; }

        [Property]
        [Display(Name = "Country", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Country { get; set; }

        [Property("zip_code")]
        [Display(Name = "ZipCode", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ZipCode { get; set; }
		
        public string StreetAndNumer {
			get { return string.Format("{0} {1} {2}", Street, ExteriorNumber, InteriorNumber).Trim(); }
		}
    }
}
