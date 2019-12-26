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
            var rep = new Repository<Person>();

            var tmp3 = rep.GetById("5");
            var tmp = rep.GetTs();
        }
    }
}
