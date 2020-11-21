using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Services;

namespace RestfulWebAPI.Api.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy,
            Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(mappingDictionary));
            }

            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return source;
            }

            // Name desc,Sex   
            var orderByAfterSplit = orderBy.Split(","); //根据逗号分隔字符串

            foreach (var orderByClause in orderByAfterSplit.Reverse())
                //反转  本次反转为了先排序后面的熟悉 再排序前面的属性 以保证排序的正确性
            {
                var trimmedOrderByClause = orderByClause.Trim(); //trim去掉首尾位置的空格

                 var orderDescending = trimmedOrderByClause.EndsWith("desc"); //字符串结尾是否为desc

                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ", StringComparison.Ordinal); //空格第一次出现的位置

                var propertyName = indexOfFirstSpace == -1  //为-1则没有出现直接返回字符串，否则返回空格索引前面的属性
                    ? trimmedOrderByClause
                    : trimmedOrderByClause.Remove(indexOfFirstSpace);

                if (!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentNullException($"没有找到key为{nameof(propertyName)}的映射");
                }

                var propertyMappingValue = mappingDictionary[propertyName];
                if (propertyMappingValue == null)
                {
                    throw new ArgumentNullException(nameof(propertyMappingValue));
                }

                foreach (var destinationProperty in propertyMappingValue.DestinnationProperties.Reverse())
                    //反转  本次反转因为若有两个对应的映射关系 先排序后面的 再排序前面的 以保证正确的排序顺序方式
                {
                    if (propertyMappingValue.Revert)
                    {
                        orderDescending = !orderDescending;
                    }
                    //安装System.Linq.dynamic.Core库 并且更改引用
                    source = source.OrderBy(destinationProperty 
                                            + (orderDescending ? " descending" : " ascending"));
                }
            }

            return source;
        }
    }
}
