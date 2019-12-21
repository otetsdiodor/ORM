using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace ORM
{
    class Program
    {
        static List<string> Tables = new List<string>();
        static void Main(string[] args)
        {
            var str = "Data Source=SHLONOV;Initial Catalog=Vasya;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            //using (var connection = new SqlConnection(str))
            //{
            //    connection.Open();
            //    var command = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES",connection);
            //    var reader = command.ExecuteReader();

            //    while (reader.Read()) // построчно считываем данные
            //    {
            //        object tableName = reader["TABLE_NAME"];
            //        Tables.Add(tableName.ToString());
            //        Console.WriteLine("{0}", tableName);
            //    }

            //    str = @"SELECT TABLE_CATALOG,
            //            TABLE_SCHEMA,	
            //            TABLE_NAME,
            //            COLUMN_NAME,
            //            DATA_TYPE,
            //            CHARACTER_MAXIMUM_LENGTH
            //       FROM INFORMATION_SCHEMA.columns
            //       WHERE table_name = 'Person'";
            //    command.CommandText = str;
            //    reader.Close();
            //    reader = command.ExecuteReader();

            //    while (reader.Read()) // построчно считываем данные
            //    {
            //        object id = reader["COLUMN_NAME"];
            //        Console.WriteLine("{0}", id);
            //    }
            //}
            DbContext lol = new DbContext();
            
            Console.WriteLine(typeof(Person));

            var t = typeof(Person);
            foreach (var item in t.GetProperties())
            {
                Console.WriteLine(item.PropertyType + "     " + item.Name);
            }
        }
    }
}
