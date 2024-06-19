using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Models.Domain;


namespace PrinterBackEnd.Data
{
    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<CatArea> Cat_Areas { get; set; }
        public DbSet<CatFolioConsec> Cat_FolioConsec { get; set; }
        public DbSet<CatMaquina> Cat_Maquinas { get; set; }
        public DbSet<CatOperador> Cat_Operadores { get; set; }
        public DbSet<CatOrden> Cat_Ordenes { get; set; }
        public DbSet<CatProducto> Cat_Productos { get; set; }
        public DbSet<CatTurno> Cat_Turnos { get; set; }
        public DbSet<ProdEtiquetasRFID> ProdEtiquetasRFID { get; set; }
        public DbSet<ProdExtrasDestiny> ProdExtrasDestiny { get; set; }
        public DbSet<ProdExtrasQuality> ProdExtrasQuality { get; set; }
        public DbSet<MaxIds> MaxIds { get; set; }
        public DbSet<vwInfoExtraDestiny> VwInfoExtraDestiny { get;set; }
        
    }
}
