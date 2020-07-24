using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonBind
{
    public class JsonKey : Attribute
    {
        public string Name { get; set; }

        public JsonKey(string name)
        {
            Name = name;
        }
    }
}
