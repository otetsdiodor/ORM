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
        List<string> LoadedTypes = new List<string>();

        public DbContext()
        {
            FillMathchedTypes();
            
        }
        private string GetTablenameFromAttribute(Type t)
        {
            object[] attrs = t.GetCustomAttributes(typeof(TableAttribute),false);
            foreach (TableAttribute attr in attrs)
                return attr.Name;

            return null;
        }
        private bool IsTableExist(string TableName)
        {
            var dbTables = GetDBTablesList();
            if (dbTables.Contains(TableName))
                return true;

            return false;
        }

        private List<string> GetDBTablesList()
        {
            var TableNames = new List<string>();
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var reader = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES", connection).ExecuteReader();

                while (reader.Read())
                {
                    var tableName = reader["TABLE_NAME"];
                    TableNames.Add(tableName.ToString());
                }
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
                var reader = new SqlCommand(sqlCommand, connection).ExecuteReader();

                while (reader.Read())
                {
                    var colName = reader["COLUMN_NAME"];
                    var colType = reader["DATA_TYPE"];
                    cols.Add(colName.ToString(), colType.ToString());
                }
                return cols;
            }
        }

        public bool IsNullable(string TableName, string colName)
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
                var reader = new SqlCommand(sqlCommand, connection).ExecuteReader();

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

        private Dictionary<string, string> GetTypeInfo(string typeFullName)
        {
            var cols = new Dictionary<string, string>();
            //var t = Type.GetType(typeFullName);
            var t = GetTypeByName(typeFullName.Split('.').LastOrDefault());
            foreach (var item in t.GetProperties())
                if (item.GetCustomAttribute(typeof(SkipAttribute)) == null)
                    cols.Add(item.Name, item.PropertyType.ToString());

            return cols;
        }

        public bool IsValidTypeAndModel(string TypeFullname)
        {
            var tableName = TypeFullname.Split('.').LastOrDefault();
            if (IsTableExist(tableName))
            {
                Console.WriteLine("TABLE EXISTED"); 
                var TableInfo = GetTableInfo(tableName);
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

        private Type GetTypeByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(x => x.GetTypes())
                                .FirstOrDefault(x => x.Name == name);
        }
        public List<object> Get(Type t)
        {
            var TableName = (GetTablenameFromAttribute(t) == null) ? t.Name : GetTablenameFromAttribute(t);

            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                LoadedTypes.Add(TableName);
                var resultList = new List<object>();
                var TableInfo = GetTableInfo(TableName);
                var commandText = string.Format("SELECT * FROM {0};", TableName); // 
                var reader = new SqlCommand(commandText, connection).ExecuteReader();

                while (reader.Read())
                {
                    var CtorParams = new List<object>();
                    foreach (var item in TableInfo)
                    {
                        if (item.Key == "Descriminator")
                            t = GetTypeByName(reader[item.Key].ToString());
                        else
                            CtorParams.Add(reader[item.Key]);
                        if (GetForeighKeysList(TableName).Contains(item.Key))
                        {
                            var type = GetTypeByName(item.Key.Replace("Id", ""));
                            var o = GetById(type, CtorParams.LastOrDefault().ToString(),false);
                            CtorParams.Add(o);
                        }
                    }
                    foreach (var TypeName in GetTypeOneToMany(t))
                    {
                        var type = GetTypeByName(TypeName);
                        var searchRes = "";

                        foreach (var item in GetForeighKeysList(TypeName))
                            if (item.Contains(TableName))
                                searchRes = item;

                        var listOfType = getList(CtorParams.FirstOrDefault().ToString(),
                            searchRes,
                            type);

                        var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new Type[] { type }));
                        foreach (var item in listOfType)
                            listInstance.Add(item);

                        CtorParams.Add(listInstance);
                    }
                    resultList.Add(Activator.CreateInstance(t, CtorParams.ToArray()));
                }
                return resultList;
            }
        }

        public object GetById(Type t, string id, bool flag = true)
        {
            var TableName = (GetTablenameFromAttribute(t) == null) ? t.Name : GetTablenameFromAttribute(t);

            using (var connection = new SqlConnection(str))
            {
                LoadedTypes.Add(TableName);
                connection.Open();
                var TableInfo = GetTableInfo(TableName);
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", TableName, GetIdNameField(t), id);
                var reader = new SqlCommand(commandText, connection).ExecuteReader();

                while (reader.Read())
                {
                    var CtorParams = new List<object>();
                    foreach (var item in TableInfo)
                    {
                        if (item.Key == "Descriminator")
                            t = GetTypeByName(reader[item.Key].ToString());
                        else
                            CtorParams.Add(reader[item.Key]);
                        if (GetForeighKeysList(TableName).Contains(item.Key))
                            if (flag)
                            {
                                var type = GetTypeByName(item.Key.Replace("Id", ""));
                                var o = GetById(type, CtorParams.LastOrDefault().ToString(),false);
                                CtorParams.Add(o);
                            }
                            else
                                CtorParams.Add(null);
                    }

                    foreach (var TypeName in GetTypeOneToMany(t))
                    {
                        if (flag)
                        {
                            var type = GetTypeByName(TypeName);
                            var smt = (GetTablenameFromAttribute(type) == null) ? type.Name : GetTablenameFromAttribute(type);
                            var searchRes = "";

                            foreach (var item in GetForeighKeysList(smt))
                                if (item.Contains(TableName))
                                    searchRes = item;

                            var listOfType = getList(CtorParams.FirstOrDefault().ToString(), searchRes, type,false);

                            var listInstance = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new Type[] { type }));
                            foreach (var item in listOfType)
                                listInstance.Add(item);

                            CtorParams.Add(listInstance);
                        }
                        else
                            CtorParams.Add(null);
                    }
                    return Activator.CreateInstance(t, CtorParams.ToArray());
                }
                return null;
            }
        }

        public List<object> getList(string Id,string IdName, Type t,bool flag = true)
        {
            var TableName = (GetTablenameFromAttribute(t) == null) ? t.Name : GetTablenameFromAttribute(t); 

            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var TableInfo = GetTableInfo(TableName);
                var commandText = string.Format("SELECT * FROM {0} WHERE {1} = '{2}';", TableName, IdName, Id);
                var reader = new SqlCommand(commandText, connection).ExecuteReader();
                var result = new List<object>();

                while (reader.Read())
                {
                    var CtorParams = new List<object>();
                    foreach (var item in TableInfo)
                    {
                        if (item.Key == "Descriminator")
                            t = GetTypeByName(reader[item.Key].ToString());
                        else
                            CtorParams.Add(reader[item.Key]);
                        if (GetForeighKeysList(TableName).Contains(item.Key))
                            if(flag)
                            {
                                if (LoadedTypes.Contains(item.Key.Replace("Id", "")))
                                    CtorParams.Add(null);
                                else
                                {
                                    var type = GetTypeByName(item.Key.Replace("Id", ""));
                                    var o = GetById(type, CtorParams.LastOrDefault().ToString(), false);
                                    CtorParams.Add(o);
                                }
                            }
                        else
                                CtorParams.Add(null);
                    }
                    result.Add(Activator.CreateInstance(t, CtorParams.ToArray()));
                }
                return result;
            }
        }

        private List<string> GetTypeOneToMany(Type t)
        {
            var result = new List<string>();
            foreach (var item in t.GetProperties())
                if (item.PropertyType.FullName.Contains("List") && item.GetCustomAttribute(typeof(SkipAttribute)) == null)
                    result.Add(item.PropertyType.GenericTypeArguments.First().Name);

            return result;
        }

        public string GetIdNameField(Type t)
        {
            var tmp = GetTypeInfo(t.FullName);
            foreach (var item in tmp)
                if (item.Key.ToLower().Contains("id"))
                    return item.Key;

            return null;
        }

        public void Update(object o,bool fl = true)
        {
            var type = o.GetType();
            var TableName = (GetTablenameFromAttribute(type) == null) ?  type.Name : GetTablenameFromAttribute(type);

            var tabINfo = GetTableInfo(TableName);
            var setString = "SET ";
            var count = 0;
            var PropValuesList = TypeValues(o);

            foreach (var item in tabINfo)
            {
                if(item.Key == "Descriminator")
                    setString += $"{item.Key} = '{type.Name}', ";
                else
                    setString += $"{item.Key} = '{PropValuesList[item.Key]}', ";
                count++;
            }
            setString = setString.Remove(setString.Length - 2);
            var commandText = string.Format("UPDATE {0} {1} WHERE {2} = '{3}'", TableName, setString, GetIdNameField(type), PropValuesList[GetIdNameField(type)]);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText, connection);
                command.ExecuteNonQuery();
            }
            var flag = true;
            var listLoaded = new List<string>();
            if(fl)
            {
                foreach (var item in GetTypeInfo(type.FullName))
                {
                    foreach (var itm2 in tabINfo)
                        if (item.Key == itm2.Key)
                            flag = false;

                    if (flag && PropValuesList[item.Key] != null)
                        if (item.Value.Contains("List"))
                        {
                            var tmp = PropValuesList[item.Key].GetType();
                            var it = getsmt(GetTypeOneToMany(type),tmp);
                            foreach (var it2 in (IList)PropValuesList[item.Key])
                            {
                                var tmp2 = it2.GetType();
                                var valTmp = TypeValues(it2);
                                if (GetById(GetTypeByName(it), valTmp["Id"].ToString()) == null)
                                {
                                    Add(it2);
                                }
                                else
                                    Update(it2, false);
                            }
                               
                        }
                        else
                            Update(PropValuesList[item.Key], false);

                    flag = true;
                }
            }            
        }
        public string getsmt(List<string> list,Type type)
        {
            foreach (var item in list)
            {
                if (type.FullName.Contains(item))
                {
                    return item;
                }
            }
            return null;
        }
        private Dictionary<string,object> TypeValues(object o)
        {
            var type = o.GetType();
            var result = new Dictionary<string, object>();
            foreach (var item in type.GetProperties())
                if (item.GetCustomAttribute(typeof(SkipAttribute)) == null)
                    result.Add(item.Name,item.GetValue(o));

            return result;
        }
        public void Delete(Type t,string id,string idName = null)
        {
            var TableName = (GetTablenameFromAttribute(t) == null) ? t.Name : GetTablenameFromAttribute(t);
            if (idName == null)
            {
                idName = GetIdNameField(t);
            }
            var commText = string.Format("DELETE FROM {0} WHERE {1} = '{2}'", TableName, idName, id);
            foreach (var item in GetTypeOneToMany(t))
                foreach (var key in GetForeighKeysList(item))
                    if (IsNullable(item,key))
                    {
                        var commTextUpdate = string.Format("UPDATE {0} SET {1} = NULL",item,key);
                        using (var connection = new SqlConnection(str))
                        {
                            connection.Open();
                            var command = new SqlCommand(commTextUpdate, connection);
                            command.ExecuteNonQuery();
                        }
                    }

            using (var connection = new SqlConnection(str))                
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(commText, connection);
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    var lol = e.Message.Split("dbo.")[1].Split('"')[0]; 
                    var kek = GetForeighKeysList(lol);
                    foreach (var item in kek)
                    {
                        if (item.Contains(TableName))
                        {
                            var type = GetTypeByName(lol);
                            Delete(type,id,item);
                        }
                    }
                    Delete(t, id);
                }               
            }
        }
        public void Add(object o)
        {
            var type = o.GetType();
            var TableName = (GetTablenameFromAttribute(type) == null) ? type.Name : GetTablenameFromAttribute(type);
            var ValuesOfType = TypeValues(o);
            var values = "";

            foreach (var item in GetForeighKeysList(TableName))
            {
                var type2 = GetTypeByName(item.Replace("Id", ""));
                if (GetById(type2, ValuesOfType[item].ToString()) == null)
                    if(ValuesOfType[item.Replace("Id", "")] == null)
                        /*throw new Exception("MUST BE NOT NULL")*/
                        Console.WriteLine("HIIIIIII");
                    else
                    {
                        //var lol = ValuesOfType[item.Replace("Id", "")].GetType();
                        Add(ValuesOfType[item.Replace("Id", "")]);
                    }
            }         
            
            foreach (var pair in GetTableInfo(TableName))
            {
                if (pair.Key == "Descriminator")
                    values += $"'{type.Name}',";
                else
                    values += $"'{ValuesOfType[pair.Key]}',";
            }

                
            values = values.Remove(values.Length-1);

            var commandText = string.Format("INSERT INTO {0} VALUES ({1})", TableName, values);
            using (var connection = new SqlConnection(str))
            {
                connection.Open();
                var command = new SqlCommand(commandText,connection);
                command.ExecuteNonQuery();
            }
            foreach (var item in GetTypeOneToMany(type))
            {
                var type2 = GetTypeByName(item);
                foreach (var val in ValuesOfType)                   
                    if (val.Value.GetType().FullName.Contains(type2.Name))
                        foreach (var Nasted in (IList)val.Value)
                        {
                            if (GetById(type2, TypeValues(Nasted)["Id"].ToString()) == null)
                                //throw new Exception("Must be null");
                            Add(Nasted);
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
                connection.Open();
                var result = new List<string>();
                var reader = new SqlCommand(comandText, connection).ExecuteReader();
                while(reader.Read())
                    result.Add(reader["COLUMN_NAME"].ToString());

                return result;
            }
        }
    }
}