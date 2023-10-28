using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oblak.Data.Enums;
using System.Reflection;

namespace Oblak.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        readonly string _user;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor accessor) : base(options)
        {

        }

        public ApplicationDbContext(IHttpContextAccessor accessor)
        {
            if (accessor != null) _user = accessor.HttpContext?.User.Identity?.Name ?? "unknown";
        }

		public override int SaveChanges()
		{
			if (_user != null)
			{
				foreach (var e in this.ChangeTracker.Entries().Where(a => a.State == EntityState.Modified || a.State == EntityState.Added))
				{
					DateTime now = DateTime.Now;
					Type T = e.Entity.GetType();

					PropertyInfo? ucd = T.GetProperty("UserCreatedDate") ?? null;
					PropertyInfo? umd = T.GetProperty("UserModifiedDate") ?? null;
					PropertyInfo? uc = T.GetProperty("UserCreated") ?? null;
					PropertyInfo? um = T.GetProperty("UserModified") ?? null;

					if (uc != null && e.State == EntityState.Added)
						uc.SetValue(e.Entity, _user);
					if (um != null && (e.State == EntityState.Modified || e.State == EntityState.Added))
						um.SetValue(e.Entity, _user);
					if (ucd != null && e.State == EntityState.Added) 
                        ucd.SetValue(e.Entity, now);
					if (umd != null && (e.State == EntityState.Modified || e.State == EntityState.Added))
						umd.SetValue(e.Entity, now);
				}
			}

			return base.SaveChanges();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<IdentityUser>(b => { b.ToTable("Users"); });
			modelBuilder.Entity<IdentityUserClaim<string>>(b => { b.ToTable("UserClaims"); });
			modelBuilder.Entity<IdentityUserLogin<string>>(b => { b.ToTable("UserLogins"); });
			modelBuilder.Entity<IdentityUserToken<string>>(b => { b.ToTable("UserTokens"); });
			modelBuilder.Entity<IdentityRole>(b => { b.ToTable("Roles"); });
			modelBuilder.Entity<IdentityRoleClaim<string>>(b => { b.ToTable("RoleClaims"); });
			modelBuilder.Entity<IdentityUserRole<string>>(b => { b.ToTable("UserRoles"); });

            modelBuilder.Entity<ApplicationUser>().Property(a => a.Type).HasConversion(new EnumToStringConverter<UserType>());
            
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Properties).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Groups).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Documents).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.ResTaxes).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Property>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Property>().HasMany(a => a.GuestTokens);
            modelBuilder.Entity<Property>().HasMany(a => a.Groups).WithOne(a => a.Property).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Property>().Property(a => a.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.ResidenceTax).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.GeoLon).HasPrecision(12, 8);
            modelBuilder.Entity<Property>().Property(a => a.GeoLat).HasPrecision(12, 8);

            modelBuilder.Entity<Group>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Group>().HasMany(a => a.Persons);
            modelBuilder.Entity<Group>().HasOne(a => a.Property);

            modelBuilder.Entity<Person>().UseTpcMappingStrategy();
            modelBuilder.Entity<Person>().HasOne(a => a.Group);
            modelBuilder.Entity<Person>().HasOne(a => a.Property);

            modelBuilder.Entity<MnePerson>().ToTable("MnePersons");
            modelBuilder.Entity<MnePerson>().HasOne(a => a.Group);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.Property);

            modelBuilder.Entity<SrbPerson>().ToTable("SrbPersons");
            modelBuilder.Entity<SrbPerson>().HasOne(a => a.Group);
            modelBuilder.Entity<SrbPerson>().HasOne(a => a.Property);

            modelBuilder.Entity<ResTax>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<ResTax>().HasOne(a => a.Property);
            modelBuilder.Entity<ResTax>().HasMany(a => a.Items);
            modelBuilder.Entity<ResTax>().Property(a => a.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<ResTaxItem>().HasOne(a => a.ResTax);
            modelBuilder.Entity<ResTaxItem>().Property(a => a.TaxPerNight).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxItem>().Property(a => a.TotalTax).HasPrecision(18, 2);
                        
            modelBuilder.Entity<SelfRegisterToken>().HasOne(a => a.Property);

            modelBuilder.Entity<Document>().HasMany(a => a.DocumentItems);
            modelBuilder.Entity<Document>().HasMany(a => a.DocumentPayments);
            modelBuilder.Entity<Document>().HasOne(a => a.Property);
            modelBuilder.Entity<Document>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Document>().HasOne(a => a.Group);
            modelBuilder.Entity<Document>().Property(a => a.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<FiscalRequest>().HasOne(a => a.LegalEntity);


            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(GuestList), new[] { typeof(int) })!)
                .HasName("GuestList");

            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(GroupDesc), new[] { typeof(int) })!)
                .HasName("GroupDesc");

            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(Nights), new[] { typeof(int), typeof(DateTime) })!)
                .HasName("Nights");

            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(PropertyDesc), new[] { typeof(int) })!)
                .HasName("PropertyDesc");

        }

        public DbSet<LegalEntity> LegalEntities { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentItem> DocumentItems { get; set; }
        public DbSet<DocumentPayment> DocumentPayments { get; set; }
        public DbSet<Item> Items { get; set; }        
        public DbSet<FiscalEnu> FiscalEnu { get; set; }
        public DbSet<FiscalRequest> FiscalRequests { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyUnit> PropertyUnits { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<MnePerson> MnePersons { get; set; }
        public DbSet<SrbPerson> SrbPersons { get; set; }
        public DbSet<CodeList> CodeLists { get; set; }        
        public DbSet<SelfRegisterToken> SelfRegisterTokens { get; set; }
        public DbSet<ResTax> ResTaxes { get; set; }
        public DbSet<ResTaxItem> ResTaxItems { get; set; }

        public string GuestList(int id) => throw new NotImplementedException();
        public string GroupDesc(int id) => throw new NotImplementedException();
        public string PropertyDesc(int id) => throw new NotImplementedException();
        public int Nights(int id, DateTime last) => throw new NotImplementedException();        
    }
}