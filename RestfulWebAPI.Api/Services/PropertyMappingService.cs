using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.Models;

namespace RestfulWebAPI.Api.Services
{
    //创建一个接口IPropertyMappingService 方便core进行 注入使用
    public class PropertyMappingService : IPropertyMappingService
    {
        //创建一个字典存放 employee属性的映射关系
        public Dictionary<string, PropertyMappingValue> EmployeePropertyMapping
            = new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)//使用相同的string命名规范
            {
                {"Id", new PropertyMappingValue(new List<string>{"Id"}) },
                {"CompanyId", new PropertyMappingValue(new List<string>{"CompanyId"}) },
                {"EmployeeNo", new PropertyMappingValue(new List<string>{"EmployeeNo"}) },
                {"Name", new PropertyMappingValue(new List<string>{"FirstName", "LastName"})},
                {"GenderDisplay", new PropertyMappingValue(new List<string>{"Gender"})},
                {"Age", new PropertyMappingValue(new List<string>{"DateOfBirth"}, true)}//为true反转顺序
            };

        public Dictionary<string, PropertyMappingValue> CompanyPropertyMapping
            = new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)//使用相同的string命名规范
            {
                {"Id", new PropertyMappingValue(new List<string>{"Id"}) },
                {"CompanyName", new PropertyMappingValue(new List<string>{"Name"}) }
            };

        //不支持泛型解析：使用标记接口解决 markInterface  
        //private IList<PropertyMapping<TSource, TDestination>> propertyMappings;
        private readonly IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();//创建一个对象防止空指针异常

        public PropertyMappingService()
        {
            _propertyMappings.Add(new PropertyMapping<EmployeeDto, Employee>(EmployeePropertyMapping));
            _propertyMappings.Add(new PropertyMapping<CompanyDto,Company>(CompanyPropertyMapping));
        }

        //根据类型来获得不同的映射关系                             例如:EmployeeDto -> Employee

        //通过TSource,TDestination来取得记录
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource,TDestination>()
        {
            var matchingMapping = 
                _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            var propertyMappings = matchingMapping.ToList();
            //如果结果为一条则表示正确
            if (propertyMappings.Count == 1)
            {
                return propertyMappings.First().MappingDictionary;
            }

            throw new Exception($"无法找到唯一的映射关系:{typeof(TSource)},{typeof(TDestination)}");

        }

        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (propertyMapping==null)
            {
                return true;
            }

            var fieldAfterSplit = fields.Split(",");

            foreach (var field in fieldAfterSplit)
            {
                var trimmedField = field.Trim();
                var indexOfFirstSpace = trimmedField.IndexOf(" ", StringComparison.Ordinal);
                var propertyName = indexOfFirstSpace == -1 ? trimmedField : trimmedField.Remove(indexOfFirstSpace);

                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
