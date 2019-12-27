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

            var tmp = repPers.GetById("22");
            var com = comrep.GetById("1");
            var dep = rep.GetById("6");

            //var tmp = repPers.GetTs();
            //var com = comrep.GetTs();
            //var dep = rep.GetTs();
            tmp.Age = 228;
            repPers.Update(tmp);
            com.Devis = "SOME NEW DECVIS";
            dep.Name = "THE BEST ALO BLET";
            comrep.Update(com);
            rep.Update(dep);


            var tmp2 = repPers.GetById("22");
            //var com3 = comrep.GetById("1");
            //var dep3 = rep.GetById("6");

        }
    }
}
