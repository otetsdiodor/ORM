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
            var tmp = rep.GetTs();
            var tmp2 = rep.GetById("3");
        }
    }
}
