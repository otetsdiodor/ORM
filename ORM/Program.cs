using ORM.Models;
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
            var rep = new Repository<Department>();
            var repPers = new Repository<Person>();

            //var p = new Person(26, "Danya", 22, 12, null);
            //var p1 = new Person(27, "Danya1", 22, 12, null);
            //var p2 = new Person(28, "Danya2", 22, 12, null);
            //var p3 = new Person(29, "Danya3", 22, 12, null);
            //var list = new List<Person>();
            //list.Add(p);
            //list.Add(p1);
            //list.Add(p2);
            //list.Add(p3);
            //var d = new Department(12, "SUPER NEW", 0, list);
            //rep.Add(d);
        }
    }
}
