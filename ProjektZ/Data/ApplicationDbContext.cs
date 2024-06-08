using Microsoft.EntityFrameworkCore;
using ProjektZ.Models;
using System.Collections.Generic;

namespace ProjektZ.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
    }
}
