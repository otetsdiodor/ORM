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
            var comrep = new Repository<Company>();


            var dep = new Department(13, "Roma Deps", 0,null);

            var pers = new Person(34, "Vasya1", 21, 13, dep);
            var pers1 = new Person(35, "Vasya2", 21, 13, dep);
            var list = new List<Person>(); list.Add(pers); list.Add(pers1);
            dep.persons = list;
            rep.Add(dep);
        }
    }
}
