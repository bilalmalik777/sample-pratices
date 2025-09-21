using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSystem.Repoistory.Entities
{
    public class Book : EntityBase
    {
        public string Title { get; set; }

        public string ISBN { get; set; }

        public static void ConfigurTable(EntityTypeBuilder<Book> entity)
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        }
    }
}
