using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Models;

namespace RestfulWebAPI.Api.ValidationAttributes
{
    public class EmployeeNoMustDifferentFromFirstNameAttribute : ValidationAttribute
    {
        //自定义一个属性标签并在EmployeeAddDto中使用
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var addDto = (EmployeeAddUpdateDto)validationContext.ObjectInstance;

            if (addDto.EmployeeNo == addDto.LastName)
            {
                return new ValidationResult(ErrorMessage, new[] { nameof(EmployeeAddUpdateDto) });
            }

            return ValidationResult.Success;
        }
    }
}
