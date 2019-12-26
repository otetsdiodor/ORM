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
        private string GetTablenameFromAttribute(Type t)
        {
            object[] attrs = t.GetCustomAttributes(false);
            foreach (TableAttribute attr in attrs)
            {
                return attr.Name;
            }
            return null;
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
                        IS_NULLABLE,
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
        public bool isNullable(string TableName, string colName)
        {
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var sqlCommand = string.Format(@"SELECT TABLE_CATALOG,
                        TABLE_SCHEMA,	
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        CHARACTER_MAXIMUM_LENGTH
                   FROM INFORMATION_SCHEMA.columns
                   WHERE table_name = '{0}' and COLUMN_NAME = '{1}'", TableName,colName);
                var command = new SqlCommand(sqlCommand, connection);
                var reader = command.ExecuteReader();
                reader.Close();
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    object nulable = reader["IS_NULLABLE"];
                    if (nulable.ToString() == "NO")
                    {
                        return false;
                    }
                    return true;
                }
                return false;
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

        public List<object> Get(Type t)
        {
            var TableName = GetTablenameFromAttribute(t);
            if (TableName == null)
            {
                TableName = t.Name;
            }
            using (var connection = new SqlConnection(str))
            {
                var resultList = new List<object>();
                LoadedTypes.Add(TableName);
                Dictionary<string, string> dictionary = GetTableInfo(TableName);
                //var filedIdname = GetIdNameField(t);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0};", TableName); // 
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var forlist = GetForeighKeysList(TableName);

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
                            if (item.Contains(TableName))
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
            var TableName = GetTablenameFromAttribute(t);
            if (TableName == null)
            {
                TableName = t.Name;
            }
            using (var connection = new SqlConnection(str))
            {
                LoadedTypes.Add(TableName);
                Dictionary<string, string> dictionary = GetTableInfo(TableName);
                var filedIdname = GetIdNameField(t);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", TableName, filedIdname, id);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var forlist = GetForeighKeysList(TableName);

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
                            if (item.Contains(TableName))
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
            var TableName = GetTablenameFromAttribute(t);
            if (TableName == null)
            {
                TableName = t.Name;
            }
            using (var connection = new SqlConnection(str))
            {
                Dictionary<string, string> dictionary = GetTableInfo(TableName);
                connection.Open();
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", TableName, IdName, Id);
                var command = new SqlCommand(commandText, connection);
                var reader = command.ExecuteReader();
                var foreignKeys = GetForeighKeysList(TableName); // added
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
        public void Update(Type type,object o)
        {
            var typeInfo = GetTypeInfo(type.FullName);
            var tabINfo = GetTableInfo(type.Name);
            var setString = "SET ";
            var count = 0;
            var PropValuesList = TypeValues(type,o);
            
            foreach (var item in tabINfo)
            {
                setString += $"{item.Key} = '{PropValuesList[item.Key]}', ";
                count++;
            }
            setString = setString.Remove(setString.Length - 2);
            var commandText = string.Format("UPDATE {0} {1} WHERE {2} = '{3}'",type.Name,setString, GetIdNameField(type), PropValuesList[GetIdNameField(type)]);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText, connection);
                command.ExecuteNonQuery();
            }
            var flag = true;
            foreach (var item in typeInfo)
            {
                foreach (var itm2 in tabINfo)
                {
                    if (item.Key == itm2.Key)
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    if (PropValuesList[item.Key] != null)
                    {
                        if (item.Value.Contains("List"))
                        {
                            foreach (var it in GetTypeOneToMany(type))
                            {
                                foreach (var it2 in (IList)PropValuesList[item.Key])
                                {
                                    Type type2 = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(x => x.GetTypes())
                                        .FirstOrDefault(x => x.Name == it);
                                    Update(type2, it2);
                                }
                            }
                        }
                        else
                        {
                            Type type1 = AppDomain.CurrentDomain.GetAssemblies()
                                    .SelectMany(x => x.GetTypes())
                                    .FirstOrDefault(x => x.Name == item.Key);
                            Update(type1, PropValuesList[item.Key]);
                        }
                    }                                
                }
                flag = true;
            }
        }

        private Dictionary<string,object> TypeValues(Type type,object o)
        {
            var result = new Dictionary<string, object>();
            foreach (var item in type.GetProperties())
            {
                result.Add(item.Name,item.GetValue(o));
            }
            return result;
        }
        public void Delete(Type t,string id)
        {
            var TableName = GetTablenameFromAttribute(t);
            if (TableName == null)
            {
                TableName = t.Name;
            }
            var idName = GetIdNameField(t);
            var commText = string.Format("DELETE FROM {0} WHERE {1} = '{2}'", TableName, idName, id);
            foreach (var item in GetTypeOneToMany(t))
            {
                Type type2 = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(x => x.GetTypes())
                                        .FirstOrDefault(x => x.Name == item);
                var forKeys = GetForeighKeysList(item);
                foreach (var key in forKeys)
                {
                    if (isNullable(item,key))
                    {
                        var commTextUpdate = string.Format("UPDATE {0} SET {1} = NULL",item,key);
                        using (var connection = new SqlConnection(str))
                        {
                            connection.Open();
                            var command = new SqlCommand(commTextUpdate, connection);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            using (var connection = new SqlConnection(str))                
            {
                connection.Open();
                var command = new SqlCommand(commText, connection);
                command.ExecuteNonQuery();
            }
        }
        public void Add(Type type,object o)
        {
            var TableName = GetTablenameFromAttribute(type);
            if (TableName == null)
            {
                TableName = type.Name;
            }
            var tmp = TypeValues(type,o);
            var values = "";
            var tableInfo = GetTableInfo(TableName);
            var fk = GetForeighKeysList(TableName);
            var om = GetTypeOneToMany(type);

            foreach (var item in fk)
            {
                Type type2 = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(x => x.GetTypes())
                                        .FirstOrDefault(x => x.Name == item.Replace("Id",""));
                var value = GetById(type2, tmp[item].ToString());
                if (value == null)
                {
                    if(tmp[item.Replace("Id", "")] == null)
                        throw new Exception("MUST BE NOT NULL");
                    else
                    {
                        Add(type2, tmp[item.Replace("Id", "")]);
                    }
                }
            }          
            foreach (var pair in tableInfo)
            {
                values += $"'{tmp[pair.Key]}',";
            }
            values = values.Remove(values.Length-1);
            var commandText = string.Format("INSERT INTO {0} VALUES ({1})", TableName, values);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText,connection);
                command.ExecuteNonQuery();
            }
            foreach (var item in om)
            {
                var id = "";
                Type type2 = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(x => x.GetTypes())
                                        .FirstOrDefault(x => x.Name == item);
                foreach (var val in tmp)
                {
                    
                    if (val.Value.GetType().FullName.Contains(type2.Name))
                    {
                        foreach (var Nasted in (IList)val.Value)
                        {
                            id = TypeValues(type2, Nasted)["Id"].ToString();
                            var value = GetById(type2, id);
                            if (value != null)
                            {
                                throw new Exception("Must be null");
                            }
                            Add(type2, Nasted);
                        }
                    }
                }

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