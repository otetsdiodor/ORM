using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace ORM
{
    public class DbContext
    {
        public Dictionary<string, string> MatchedTypes = new Dictionary<string, string>();
        const string str = "Data Source=SHLONOV;Initial Catalog=TestBD;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        public DbContext()
        {
            FillMathchedTypes();
        }

        public bool IsTableExist(string name)
        {
            var list = GetDBTablesList();
            if (list.Contains(name))
                return true;

            return false;
        }

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

        public Dictionary<string, string> GetTableInfo(string TableName)
        {
            var cols = new Dictionary<string, string>();
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var sqlCommand = string.Format(@"SELECT TABLE_CATALOG,
                        TABLE_SCHEMA,	
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        CHARACTER_MAXIMUM_LENGTH
                   FROM INFORMATION_SCHEMA.columns
                   WHERE table_name = '{0}'", TableName);
                var command = new SqlCommand(sqlCommand, connection);
                var reader = command.ExecuteReader();
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

        public Dictionary<string, string> GetTypeInfo(string type)
        {
            var cols = new Dictionary<string, string>();
            var t = Type.GetType(type);
            foreach (var item in t.GetProperties())
            {
                cols.Add(item.Name, item.PropertyType.ToString());
            }
            return cols;
        }

        public bool IsValidTypeAndModel(string TypeFullname)
        {
            var name = TypeFullname.Split('.').LastOrDefault();
            if (IsTableExist(name))
            {
                Console.WriteLine("TABLE EXISTED");
                var TableInfo = GetTableInfo(name);
                var typeInfo = GetTypeInfo(TypeFullname);
                foreach (var item in TableInfo)
                {
                    if (typeInfo[item.Key] != MatchedTypes[item.Value])
                    {
                        throw new Exception("Types are not matched");
                    }
                    Console.WriteLine("MATCHED");
                }
            }
            return true;
        }
        private void FillMathchedTypes()
        {
            MatchedTypes.Add("bit", "System.Boolean");
            MatchedTypes.Add("int", "System.Int32");
            MatchedTypes.Add("bigint", "System.Int64");
            MatchedTypes.Add("decimal", "System.Decimal");
            MatchedTypes.Add("float", "System.Double");
            MatchedTypes.Add("char", "System.String");
            MatchedTypes.Add("nvarchar", "System.String");
            MatchedTypes.Add("varchar", "System.String");
            MatchedTypes.Add("text", "System.String");
            MatchedTypes.Add("ntext", "System.String");
            MatchedTypes.Add("datetime", "System.DateTime");
            MatchedTypes.Add("datetime2", "System.DateTime");
            MatchedTypes.Add("date", "System.DateTime");
            MatchedTypes.Add("time", "System.TimeSpan");
        }
        public IEnumerable<object> GetT(string TableName,Type t)
        {
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                Dictionary<string, string> dictionary = GetTableInfo(TableName);
                List<object> result = new List<object>();
                var commandText = string.Format("SELECT * FROM {0};", TableName);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var tmpList = new List<object>();
                    foreach (var item in dictionary)
                    {
                        tmpList.Add(reader[item.Key]);
                    }
                    
                    var tmp = Activator.CreateInstance(t, tmpList.ToArray());
                    result.Add(tmp);
                }
                return result;
            }
        }
        public object GetById(Type t, string id)
        {
            using (var connection = new SqlConnection(str))
            {
                Dictionary<string, string> dictionary = GetTableInfo(t.Name);
                var filedIdname = GetIdNameField(t);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", t.Name, filedIdname, id);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var tmpList = new List<object>();
                    foreach (var item in dictionary)
                    {
                        tmpList.Add(reader[item.Key]);
                    }

                    var tmp = Activator.CreateInstance(t, tmpList.ToArray());
                    return tmp;
                }
                return null;
            }
        }
        private string GetIdNameField(Type t)
        {
            var tmp = GetTypeInfo(t.FullName);
            foreach (var item in tmp)
            {
                if (item.Key.ToLower().Contains("id"))
                {
                    return item.Key;
                }
            }
            return null;
        }
    }
}

