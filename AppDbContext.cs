using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SocxoBlurbCommentGenerator
{
    public class AppDbContext :DbContext
    {
        private readonly string _connectionString;

        public AppDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Define DbSet properties for your tables
        public DbSet<Requests> Requests { get; set; }
        public DbSet<Blurbs> Blurbs { get; set; } // Corrected pluralization
        public DbSet<ClientIds> ClientIds { get; set; } // Corrected class name to singular

        // Configure the database connection
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }
    }
}
