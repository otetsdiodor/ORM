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
            var com = new Company(1, "BEST", "BLA BLA BLA LBA");
            comrep.Add(com);
        }
    }
}
