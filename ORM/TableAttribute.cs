using System;
using System.Collections.Generic;
using System.Text;

namespace ORM
{
    public class TableAttribute : System.Attribute
    {
        public string Name { get; set; }

        public TableAttribute()
        { }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }
}
