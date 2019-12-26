using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Data.SqlClient;
using System.Text;

namespace ORM
{
    public class DbContext
    {
        public Dictionary<string, string> MatchedTypes = new Dictionary<string, string>();
        const string str = "Data Source=SHLONOV;Initial Catalog=TestBD;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        //const string str = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        List<string> LoadedTypes = new List<string>();


        public DbContext()
        {
            FillMathchedTypes();
        }

        private bool IsTableExist(string name)
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

        private Dictionary<string, string> GetTableInfo(string TableName)
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

        //public IEnumerable<object> GetT(string TableName, Type t)
        //{
        //    using (var connection = new SqlConnection(str))
        //    {
        //        connection.Open();
        //        Dictionary<string, string> dictionary = GetTableInfo(TableName);
        //        List<object> result = new List<object>();
        //        var commandText = string.Format("SELECT * FROM {0};", TableName);
        //        var command = new SqlCommand(commandText, connection);
        //        var reader = command.ExecuteReader();
        //        var forlist = GetForeighKeysList(TableName);
        //        while (reader.Read())
        //        {
        //            var tmpList = new List<object>();
        //            foreach (var item in dictionary)
        //            {
        //                tmpList.Add(reader[item.Key]);
        //                if (forlist.Contains(item.Key))
        //                {
        //                    Type type = AppDomain.CurrentDomain.GetAssemblies()
        //                        .SelectMany(x => x.GetTypes())
        //                        .FirstOrDefault(x => x.Name == item.Key.Replace("Id", ""));
        //                    object o = GetById(type, tmpList.LastOrDefault().ToString());
        //                    var index = tmpList.IndexOf(forlist[0]);
        //                    tmpList.Add(o);
        //                }
        //            }
        //            var tmp = Activator.CreateInstance(t, tmpList.ToArray());
        //            result.Add(tmp);
        //        }
        //        return result;
        //    }
        //}
        public List<object> Get(Type t)
        {
            using (var connection = new SqlConnection(str))
            {
                var resultList = new List<object>();
                LoadedTypes.Add(t.Name);
                Dictionary<string, string> dictionary = GetTableInfo(t.Name);
                //var filedIdname = GetIdNameField(t);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0};", t.Name); // 
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var forlist = GetForeighKeysList(t.Name);

                while (reader.Read())
                {
                    var tmpList = new List<object>();
                    foreach (var item in dictionary)
                    {
                        tmpList.Add(reader[item.Key]);
                        if (forlist.Contains(item.Key))
                        {
                            Type type = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(x => x.Name == item.Key.Replace("Id", ""));
                            object o = GetById(type, tmpList.LastOrDefault().ToString());
                            var index = tmpList.IndexOf(forlist[0]);
                            tmpList.Add(o);
                        }
                    }
                    var oneToManyTypes = GetTypeOneToMany(t);
                    foreach (var TypeName in oneToManyTypes)
                    {
                        Type type = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(x => x.Name == TypeName);
                        var idValue = tmpList.FirstOrDefault().ToString();
                        var foreighkeys = GetForeighKeysList(TypeName);
                        var searchRes = "";
                        foreach (var item in foreighkeys)
                        {
                            if (item.Contains(t.Name))
                            {
                                searchRes = item;
                            }
                        }
                        var smt = getList(idValue, searchRes, type);

                        Type listType = typeof(List<>).MakeGenericType(new Type[] { type });
                        var listInstance = (IList)Activator.CreateInstance(listType);
                        foreach (var item in smt)
                        {
                            listInstance.Add(item);
                        }
                        tmpList.Add(listInstance);
                    }
                    resultList.Add(Activator.CreateInstance(t, tmpList.ToArray()));
                }
                return resultList;
            }
        }

        public object GetById(Type t, string id)
        {
            using (var connection = new SqlConnection(str))
            {
                LoadedTypes.Add(t.Name);
                Dictionary<string, string> dictionary = GetTableInfo(t.Name);
                var filedIdname = GetIdNameField(t);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", t.Name, filedIdname, id);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var forlist = GetForeighKeysList(t.Name);

                while (reader.Read())
                {
                    var tmpList = new List<object>();
                    foreach (var item in dictionary)
                    {
                        tmpList.Add(reader[item.Key]);
                        if (forlist.Contains(item.Key))
                        {
                            Type type = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(x => x.Name == item.Key.Replace("Id", ""));
                            object o = GetById(type, tmpList.LastOrDefault().ToString());
                            var index = tmpList.IndexOf(forlist[0]);
                            tmpList.Add(o);
                        }
                    }
                    var oneToManyTypes = GetTypeOneToMany(t);
                    foreach (var TypeName in oneToManyTypes)
                    {
                        Type type = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(x => x.Name == TypeName);
                        var idValue = tmpList.FirstOrDefault().ToString();
                        var foreighkeys = GetForeighKeysList(TypeName);
                        var searchRes = "";
                        foreach (var item in foreighkeys)
                        {
                            if (item.Contains(t.Name))
                            {
                                searchRes = item;
                            }
                        }
                        var smt = getList(idValue, searchRes, type);

                        Type listType = typeof(List<>).MakeGenericType(new Type[] { type });
                        var listInstance = (IList)Activator.CreateInstance(listType);
                        foreach (var item in smt)
                        {
                            listInstance.Add(item);
                        }
                        tmpList.Add(listInstance);
                    }
                    var tmp = Activator.CreateInstance(t, tmpList.ToArray());
                    return tmp;
                }
                return null;
            }
        }

        public List<object> getList(string Id,string IdName, Type t)
        {
            using (var connection = new SqlConnection(str))
            {
                Dictionary<string, string> dictionary = GetTableInfo(t.Name);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", t.Name, IdName, Id);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var foreignKeys = GetForeighKeysList(t.Name); // added
                List<object> result = new List<object>();

                while (reader.Read())
                {
                    var tmpList = new List<object>();
                    foreach (var item in dictionary)
                    {
                        tmpList.Add(reader[item.Key]);
                        if (foreignKeys.Contains(item.Key))
                        {
                            if (LoadedTypes.Contains(item.Key.Replace("Id", "")))
                            {
                                tmpList.Add(null);
                            }
                            else
                            {
                                Type type = AppDomain.CurrentDomain.GetAssemblies()
                                    .SelectMany(x => x.GetTypes())
                                    .FirstOrDefault(x => x.Name == item.Key.Replace("Id", ""));
                                object o = GetById(type, tmpList.LastOrDefault().ToString());
                                tmpList.Add(o);
                            }
                        }
                    }
                    result.Add(Activator.CreateInstance(t, tmpList.ToArray()));
                }
                return result;
            }
        }

        private List<string> GetTypeOneToMany(Type t)
        {
            var result = new List<string>();
            foreach (var item in t.GetProperties())
            {
                if (item.PropertyType.FullName.Contains("List"))
                {
                    result.Add(item.PropertyType.GenericTypeArguments.First().Name);
                }
            }
            return result;
        }

        public string GetIdNameField(Type t)
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
        public void Update<T>(T t)
        {
            var type = typeof(T);
            var typeInfo = GetTypeInfo(type.FullName);
            var setString = "SET ";
            var count = 0;
            var PropValuesList = TypeValues(t);
            
            foreach (var item in typeInfo)
            {
                setString += $"{item.Key} = '{PropValuesList[item.Key]}', ";
                count++;
            }
            setString = setString.Remove(setString.Length - 2);
            var commandText = string.Format("UPDATE {0} {1} WHERE {2} = '{3}'",type.Name,setString, GetIdNameField(typeof(T)), PropValuesList[GetIdNameField(typeof(T))]);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText, connection);
                command.ExecuteNonQuery();
            }
        }

        private Dictionary<string,string> TypeValues<T>(T t)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in typeof(T).GetProperties())
            {
                result.Add(item.Name,item.GetValue(t).ToString());
            }
            return result;
        }
        public void Delete(Type t,string id)
        {
            var idName = GetIdNameField(t);
            var commText = string.Format("DELETE FROM {0} WHERE {1} = '{2}'", t.Name, idName, id);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commText, connection);
                command.ExecuteNonQuery();
            }
        }
        public void Add<T>(T item)
        {
            var tmp = TypeValues(item);
            var values = "";
            foreach (var pair in tmp)
            {
                values += $"'{pair.Value}',";
            }
            values = values.Remove(values.Length-1);
            var commandText = string.Format("INSERT INTO {0} VALUES ({1})",typeof(T).Name,values);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText,connection);
                command.ExecuteNonQuery();
            }
        }
        private List<string> GetForeighKeysList(string tableName)
        {
            var comandText = string.Format(@"SELECT TABLE_NAME
                , CONSTRAINT_NAME
                , COLUMN_NAME
                , ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_NAME = '{0}' and CONSTRAINT_NAME IN (
                SELECT CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS)
                ORDER BY TABLE_NAME, ORDINAL_POSITION", tableName);
            using (var connection = new SqlConnection(str))
            {
                var result = new List<string>();
                connection.Open();
                var command = new SqlCommand(comandText, connection);
                var reader = command.ExecuteReader();
                while(reader.Read()) // TODO: Прописать if has rows
                {
                    result.Add(reader["COLUMN_NAME"].ToString());
                }
                return result;
            }
        }
    }
}