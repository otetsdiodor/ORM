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
            //var rep = new Repository<Person>();
            //var rep = new Repository<Department>();
            //var tmp = rep.GetTs();

            //var tmp3 = rep.GetById("1");
            //var person = new Person(4, "KANE", 55);
            //rep.Add(person);
            var context = new DbContext();
            var company = context.get("1", typeof(Company));
            var company1 = (Company)context.get("1", typeof(Company));
            //var person = context.getOntToOne("1",typeof(Person));
            var department = context.getOneToMany("1",typeof(Department));
            Console.WriteLine(company1.Devis);
        }
    }
}
