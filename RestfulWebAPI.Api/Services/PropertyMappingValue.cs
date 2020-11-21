using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestfulWebAPI.Api.Services
{
    public class PropertyMappingValue
    {
        //例如 FirstName 和 LastName
        public IEnumerable<string> DestinnationProperties { get; set; }

        //排序
        public bool Revert { get; set; }

        public PropertyMappingValue(IEnumerable<string> destinnationProperties, bool revert = false)
        {
            DestinnationProperties = destinnationProperties ?? throw new ArgumentNullException(nameof(destinnationProperties));
            Revert = revert;
        }
    }
}
