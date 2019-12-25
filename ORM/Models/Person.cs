using System;
using System.Collections.Generic;
using System.Text;

namespace ORM
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public Person(int id, string name,int age,int depId,Department dep)
        {
            Id = id;
            Name = name;
            Age = age;
            DepartmentId = depId;
            Department = dep;
        }
    }
}
