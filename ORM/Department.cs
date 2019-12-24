using System.Collections.Generic;

namespace ORM
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CountOfEmloyees { get; set; }
        public List<Person> persons { get; set; }
        public Department(int id, string name, int count,List<Person> pers)
        {
            Id = id;
            Name = name;
            CountOfEmloyees = count;
            persons = pers;
        }
    }
}