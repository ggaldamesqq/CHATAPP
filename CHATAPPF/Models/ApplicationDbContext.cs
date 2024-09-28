using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace CHATAPPF.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MensajeChat> MensajeChat { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MensajeChat>()
                .HasKey(m => m.IDMensajeChat); // Define la clave primaria
        }
    }
   
    public class MensajeChat
    {
        [Key]
        public int IDMensajeChat { get; set; }
        public int IDUsuario { get; set; }
        public int IDComunidad { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }
    }
}
