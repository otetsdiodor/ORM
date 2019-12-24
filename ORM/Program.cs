using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace ORM
{
    class Program
    {
        static void Main(string[] args)
        {
            //var rep = new Repository<Person>();
            var rep = new Repository<Department>();
            //var tmp = rep.GetTs();

            var tmp3 = rep.GetById("5");
            //var person = new Person(4, "KANE", 55);
            //rep.Add(person);
            var context = new DbContext();
            context.get("5", typeof(Person));
        }
    }
}
