using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Entities;

namespace RestfulWebAPI.Api.Models
{
    public class EmployeeUpdateDto : EmployeeAddUpdateDto //2.使用IValidatableObject 实现接口自定义复杂的验证
    {
    }
}
