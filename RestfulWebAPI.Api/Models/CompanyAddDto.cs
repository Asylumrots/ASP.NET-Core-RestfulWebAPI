using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RestfulWebAPI.Api.Models
{
    public class CompanyAddDto
    {
        //1.验证方式ModelState 写标签限制 不需要使用ModelState.IsVidata ApiController自动集成
        //参考网址：https://docs.microsoft.com/zh-cn/aspnet/core/tutorials/first-mvc-app/details?view=aspnetcore-3.1
        [Display(Name = "公司名称")]
        [Required(ErrorMessage = "{0}不能为空")]
        [MaxLength(10, ErrorMessage = "{0}的最大长度不可以超过{1}")]
        public string Name { get; set; }
        [Display(Name = "公司简介")]
        [StringLength(50, MinimumLength = 10, ErrorMessage = "{0}的长度范围是{2}-{1}")]
        public string Introduction { get; set; }

        public ICollection<EmployeeAddDto> Employees { get; set; } = new List<EmployeeAddDto>();

    }
}
