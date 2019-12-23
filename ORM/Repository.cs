using System;
using System.Collections.Generic;
using System.Text;

namespace ORM
{
    public class Repository<T>
    {
        DbContext context = new DbContext();
        public Repository()
        {
            if (context.IsValidTypeAndModel(typeof(T).FullName))
            {
                Console.WriteLine("URAAAAAA");
            }
        }
        public IEnumerable<T> GetTs()
        {
            var name = typeof(T).Name;
            var res = context.GetT(name, typeof(T));
            List<T> result = new List<T>();
            foreach (var item in res)
            {
                result.Add((T)item);
            }
            return result;
        }
        public T GetById(string id)
        {
            return (T)context.GetById(typeof(T), id);
        }
    }
}
