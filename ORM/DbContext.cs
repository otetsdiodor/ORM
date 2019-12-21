using System;
using System.Reflection;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace ORM
{
    public class DbContext
    {


        string str = "Data Source=SHLONOV;Initial Catalog=Vasya;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        private List<string> GetDBTablesList()
        {
            var TableNames = new List<string>();
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES", connection);
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    object tableName = reader["TABLE_NAME"];
                    TableNames.Add(tableName.ToString());
                }
                reader.Close();
            }
            return TableNames;
        }

        private Dictionary<string, string> GetTableInfo(string TableName)
        {
            var cols = new Dictionary<string, string>();
            using (var connection = new SqlConnection(str))
            {
                var command = new SqlCommand(str, connection);
                var reader = command.ExecuteReader();
                str = string.Format(@"SELECT TABLE_CATALOG,
                        TABLE_SCHEMA,	
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH
                   FROM INFORMATION_SCHEMA.columns
                   WHERE table_name = '{0}'", TableName);
                command.CommandText = str;
                reader.Close();
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    object colName = reader["COLUMN_NAME"];
                    object colType = reader["DATA_TYPE"];
                    cols.Add(colName.ToString(), colType.ToString());
                }
                return cols;
            }
        }

        private Dictionary<string, string> GetTypeInfo(string type)
        {
            var cols = new Dictionary<string, string>();
            var t = Type.GetType(type);
            foreach (var item in t.GetProperties())
            {
                cols.Add(item.Name, item.PropertyType.ToString());
            }
            return cols;
        }
    }
}

