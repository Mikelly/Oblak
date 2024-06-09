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
			if (accessor != null) _user = accessor.HttpContext?.User.Identity?.Name ?? "unknown";
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
            modelBuilder.Entity<LegalEntity>().HasOne(a => a.Partner).WithMany(a => a.LegalEntities);
			modelBuilder.Entity<LegalEntity>().HasOne(a => a.Administrator).WithMany(a => a.Clients);

            modelBuilder.Entity<LegalEntity>().HasOne(a => a.PassThrough);
            modelBuilder.Entity<LegalEntity>().Property(a => a.Type).HasConversion(new EnumToStringConverter<LegalEntityType>());
            modelBuilder.Entity<LegalEntity>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Country>());

            modelBuilder.Entity<Partner>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Country>());
            modelBuilder.Entity<Partner>().Property(a => a.PartnerType).HasConversion(new EnumToStringConverter<PartnerType>());

            modelBuilder.Entity<Municipality>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Country>());
            modelBuilder.Entity<Municipality>().Property(a => a.ResidenceTaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Municipality>().HasMany(a => a.Properties).WithOne(a => a.Municipality).OnDelete(DeleteBehavior.NoAction);

			modelBuilder.Entity<LegalEntity>().Property(a => a.Type).HasConversion(new EnumToStringConverter<LegalEntityType>());
            modelBuilder.Entity<LegalEntity>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Country>());


            modelBuilder.Entity<Property>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Property>().HasOne(a => a.Municipality);
            modelBuilder.Entity<Property>().HasMany(a => a.GuestTokens);
            modelBuilder.Entity<Property>().HasMany(a => a.Groups).WithOne(a => a.Property).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Property>().Property(a => a.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.ResidenceTax).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.GeoLon).HasPrecision(12, 8);
            modelBuilder.Entity<Property>().Property(a => a.GeoLat).HasPrecision(12, 8);

            modelBuilder.Entity<PropertyUnit>().Property(a => a.Price).HasPrecision(12, 8);

            modelBuilder.Entity<Partner>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Country>());

            modelBuilder.Entity<Group>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Group>().HasMany(a => a.Persons);
            modelBuilder.Entity<Group>().HasOne(a => a.Property);			
			modelBuilder.Entity<Group>().HasOne(a => a.ResTaxPaymentType);
			modelBuilder.Entity<Group>().Property(a => a.ResTaxAmount).HasPrecision(18, 2);
			modelBuilder.Entity<Group>().Property(a => a.ResTaxFee).HasPrecision(18, 2);

			modelBuilder.Entity<Person>().UseTpcMappingStrategy();
            modelBuilder.Entity<Person>().HasOne(a => a.Group);
            modelBuilder.Entity<Person>().HasOne(a => a.Property);
            modelBuilder.Entity<Person>().HasOne(a => a.CheckInPoint);

            modelBuilder.Entity<MnePerson>().ToTable("MnePersons");
            modelBuilder.Entity<MnePerson>().HasOne(a => a.Group);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.Property);
			modelBuilder.Entity<MnePerson>().HasOne(a => a.ResTaxType);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.CheckInPoint);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.ResTaxPaymentType);
			modelBuilder.Entity<MnePerson>().Property(a => a.ResTaxAmount).HasPrecision(18, 2);
			modelBuilder.Entity<MnePerson>().Property(a => a.ResTaxFee).HasPrecision(18, 2);

			modelBuilder.Entity<SrbPerson>().ToTable("SrbPersons");
            modelBuilder.Entity<SrbPerson>().HasOne(a => a.Group);
            modelBuilder.Entity<SrbPerson>().HasOne(a => a.Property);
            modelBuilder.Entity<SrbPerson>().HasOne(a => a.CheckInPoint);

            modelBuilder.Entity<ResTaxCalc>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<ResTaxCalc>().HasOne(a => a.Property);
            modelBuilder.Entity<ResTaxCalc>().HasMany(a => a.Items);
            modelBuilder.Entity<ResTaxCalc>().Property(a => a.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<ResTaxCalcItem>().HasOne(a => a.ResTax);
            modelBuilder.Entity<ResTaxCalcItem>().Property(a => a.TaxPerNight).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxCalcItem>().Property(a => a.TotalTax).HasPrecision(18, 2);

			modelBuilder.Entity<ResTaxType>().HasOne(a => a.Partner);
            modelBuilder.Entity<ResTaxType>().Property(a => a.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<ResTaxPaymentType>().HasOne(a => a.Partner);
			modelBuilder.Entity<ResTaxPaymentType>().HasMany(a => a.PaymentFees).WithOne(a => a.ResTaxPaymentType).OnDelete(DeleteBehavior.NoAction); ;
            modelBuilder.Entity<ResTaxPaymentType>().Property(a => a.PaymentStatus).HasConversion(new EnumToStringConverter<Enums.ResTaxPaymentStatus>());

			modelBuilder.Entity<ResTaxFee>().HasOne(a => a.Partner);
			modelBuilder.Entity<ResTaxFee>().HasOne(a => a.ResTaxPaymentType);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.FeeAmount).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.FeePercentage).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.UpperLimit).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.LowerLimit).HasPrecision(18, 2);

            modelBuilder.Entity<SelfRegisterToken>().HasOne(a => a.Property);

            modelBuilder.Entity<Document>().HasMany(a => a.DocumentItems);
            modelBuilder.Entity<Document>().HasMany(a => a.DocumentPayments);
            modelBuilder.Entity<Document>().HasOne(a => a.Property);
            modelBuilder.Entity<Document>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Document>().HasOne(a => a.Group);
            modelBuilder.Entity<Document>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Document>().Property(a => a.ExchangeRate).HasPrecision(18, 6);
            modelBuilder.Entity<Document>().Property(a => a.DocumentType).HasConversion(new EnumToStringConverter<DocumentType>());
            modelBuilder.Entity<Document>().Property(a => a.TypeOfInvoce).HasConversion(new EnumToStringConverter<TypeOfInvoice>());
            modelBuilder.Entity<Document>().Property(a => a.InvoiceType).HasConversion(new EnumToStringConverter<InvoiceType>());
            modelBuilder.Entity<Document>().Property(a => a.Status).HasConversion(new EnumToStringConverter<DocumentStatus>());
            modelBuilder.Entity<Document>().Property(a => a.PartnerType).HasConversion(new EnumToStringConverter<BuyerType>());
            modelBuilder.Entity<Document>().Property(a => a.PartnerIdType).HasConversion(new EnumToStringConverter<BuyerIdType>());

            modelBuilder.Entity<DocumentItem>().HasOne(a => a.Document);
            modelBuilder.Entity<DocumentItem>().Property(a => a.Quantity).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.UnitPrice).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.UnitPriceWoVat).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.VatAmount).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.VatRate).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.Discount).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.DiscountAmount).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.FinalPrice).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.LineAmount).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.LineTotal).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.LineTotalWoVat).HasPrecision(18, 4);
            modelBuilder.Entity<DocumentItem>().Property(a => a.VatExempt).HasConversion(new EnumToStringConverter<MneVatExempt>());

            modelBuilder.Entity<DocumentPayment>().HasOne(a => a.Document);
            modelBuilder.Entity<DocumentPayment>().Property(a => a.PaymentType).HasConversion(new EnumToStringConverter<PaymentType>());
            modelBuilder.Entity<DocumentPayment>().Property(a => a.Amount).HasPrecision(18, 4);

            modelBuilder.Entity<FiscalEnu>().Property(a => a.AutoDeposit).HasPrecision(18, 4);

            modelBuilder.Entity<Item>().Property(a => a.VatExempt).HasConversion(new EnumToStringConverter<MneVatExempt>());
            modelBuilder.Entity<Item>().Property(a => a.Price).HasPrecision(18, 4);
            modelBuilder.Entity<Item>().Property(a => a.VatRate).HasPrecision(18, 4);

            modelBuilder.Entity<FiscalRequest>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<FiscalRequest>().Property(a => a.Amount).HasPrecision(18, 4);
            modelBuilder.Entity<FiscalRequest>().Property(a => a.RequestType).HasConversion(new EnumToStringConverter<FiscalRequestType>());
            
            modelBuilder.Entity<PaymentTransaction>().ToTable("PaymentTransactions");
            modelBuilder.Entity<PaymentTransaction>().HasOne(a => a.Document);
            modelBuilder.Entity<PaymentTransaction>().HasOne(a => a.Group);
            modelBuilder.Entity<PaymentTransaction>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<PaymentTransaction>().HasOne(a => a.Property);
            modelBuilder.Entity<PaymentTransaction>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentTransaction>().Property(a => a.SurchargeAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => new { pt.Id, pt.LegalEntityId })
                .HasDatabaseName("IX_PaymentTransaction_Id_LegalEntityId");

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => new { pt.MerchantTransactionId, pt.LegalEntityId })
                .HasDatabaseName("IX_PaymentTransaction_MerchantTransactionId_LegalEntityId");

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
        public DbSet<Partner> Partners { get; set; }
        public DbSet<CheckInPoint> CheckInPoints { get; set; }
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
        public DbSet<ResTaxCalc> ResTaxCalc { get; set; }
        public DbSet<ResTaxCalcItem> ResTaxCalcItems { get; set; }
		public DbSet<ResTaxType> ResTaxTypes { get; set; }
		public DbSet<ResTaxPaymentType> ResTaxPaymentTypes { get; set; }
		public DbSet<ResTaxFee> ResTaxFees { get; set; }
		public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }

        public string GuestList(int id) => throw new NotImplementedException();
        public string GroupDesc(int id) => throw new NotImplementedException();
        public string PropertyDesc(int id) => throw new NotImplementedException();
        public int Nights(int id, DateTime last) => throw new NotImplementedException();        
    }
}