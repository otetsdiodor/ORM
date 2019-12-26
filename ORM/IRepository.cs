using System.Collections.Generic;

namespace ORM
{
    public interface IRepository<T>
    {
        public IEnumerable<T> GetTs();
        public T GetById(string id);
        public void Update(T item);
        public void Delete(string id);
        public void Add(T item);
    }
}