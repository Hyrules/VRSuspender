using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace VRSuspender.Utils.Validations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotNullOrEmptyOrWhiteSpaceValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {

            return (string.IsNullOrEmpty((string)value) || string.IsNullOrWhiteSpace((string)value)) ? new ValidationResult("Field cannot be empty, null or whitespaces.") : ValidationResult.Success;
        }
    }
}
