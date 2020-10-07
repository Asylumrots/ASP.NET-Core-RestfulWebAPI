using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.ValidationAttributes;

namespace RestfulWebAPI.Api.Models
{
    [EmployeeNoMustDifferentFromFirstName(ErrorMessage = "员工编号不可以等于名")]
    public abstract class EmployeeAddUpdateDto : IValidatableObject //2.使用IValidatableObject 实现接口自定义复杂的验证
    {
        [Display(Name = "员工名称")]
        [Required(ErrorMessage = "{0}不能为空")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "{0}的长度必须是{1}")]
        public string EmployeeNo { get; set; }

        [Display(Name = "名")]
        [Required(ErrorMessage = "{0}不能为空")]
        public string FirstName { get; set; }

        [Display(Name = "姓"), Required(ErrorMessage = "{0}不能为空")]
        public string LastName { get; set; }

        [Display(Name = "性别")]
        public Gender Gender { get; set; }

        [Display(Name = "出生日期")]
        public DateTime DateOfBirth { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FirstName == LastName)
            {
                yield return new ValidationResult("姓和名不能一样", new[] { nameof(FirstName), nameof(LastName) });
            }
            //yield return 用于返回 IEnumerable、IEnumerable<T>、IEnumerator 或 IEnumerator<T>，一次返回一个元素。
            //yield break用于终止循环遍历
        }
    }
}
