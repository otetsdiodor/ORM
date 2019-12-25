using System;
using System.Collections.Generic;
using System.Text;

namespace ORM.Models
{
    class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Devis { get; set; }
        public Company(int id, string name, string devis)
        {
            Id = id;
            Name = name;
            Devis = devis;
        }
    }
}
