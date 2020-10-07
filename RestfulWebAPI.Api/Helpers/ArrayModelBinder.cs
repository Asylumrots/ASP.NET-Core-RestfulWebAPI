using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RestfulWebAPI.Api.Helpers
{
    public class ArrayModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            //如果作用的类型不是IsEnumerable的话
            if (!bindingContext.ModelMetadata.IsEnumerableType)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            //获得这个值                 值提供者
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).ToString();

            //如果字符串为空 表示传递参数为空
            if (string.IsNullOrWhiteSpace(value))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            //获得IEnumerable里面的类型
            var elementType = bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0];
            //创建转换器
            var converter = TypeDescriptor.GetConverter(elementType);

            //将字符串转换成IsEnumerable<类型>
            var values = value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => converter.ConvertFromString(x.Trim())).ToArray();

            //吧values 的 object类型转换成具体类型
            var typedValues = Array.CreateInstance(elementType, values.Length);
            values.CopyTo(typedValues, 0);
            //绑定
            bindingContext.Model = typedValues;

            //返回
            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            return Task.CompletedTask;
        }
    }
}
