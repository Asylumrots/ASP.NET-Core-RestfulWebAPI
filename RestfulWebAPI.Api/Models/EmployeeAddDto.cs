using RestfulWebAPI.Api.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using RestfulWebAPI.Api.ValidationAttributes;

namespace RestfulWebAPI.Api.Models
{
    public class EmployeeAddDto : EmployeeUpdateDto //2.使用IValidatableObject 实现接口自定义复杂的验证
    {
    }
}
