using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Mictlanix.BE.Model.Validation
{
    class UniqueProductCodeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            Product item;
            Product param = validationContext.ObjectInstance as Product;

            var qry = from x in Product.Queryable
                      where x.Code == value.ToString()
                      select x;

            item = qry.SingleOrDefault();

            if ((param.Id == 0 && qry.Count() > 0) || (param.Id != 0 && item != null && param.Id != item.Id))
            {
                var message = FormatErrorMessage(validationContext.DisplayName);
                return new ValidationResult(message);
            }

            return null;
        }
    }
}
