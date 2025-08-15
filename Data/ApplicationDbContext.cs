using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oblak.Data.Enums;
using Oblak.Models.Api;
using System.Reflection;

namespace Oblak.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly IHttpContextAccessor _accessor;
        private string _user;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor accessor) : base(options)
        {
            _accessor = accessor;  
		}

        public ApplicationDbContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;  
        }	 
        public override int SaveChanges()
        {
            _user = GetCurrentUserName();
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

            modelBuilder.Entity<ApplicationUser>().Property(a => a.Type).HasConversion(new EnumToStringConverter<UserType>()).HasPrecision(450);

            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Properties).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Groups).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.Documents).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasMany(a => a.ResTaxes).WithOne(a => a.LegalEntity).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<LegalEntity>().HasOne(a => a.Partner).WithMany(a => a.LegalEntities);
            modelBuilder.Entity<LegalEntity>().HasOne(a => a.Administrator).WithMany(a => a.Clients);

            modelBuilder.Entity<LegalEntity>().HasOne(a => a.PassThrough);
            modelBuilder.Entity<LegalEntity>().Property(a => a.Type).HasConversion(new EnumToStringConverter<LegalEntityType>()).HasPrecision(450);
            modelBuilder.Entity<LegalEntity>().Property(a => a.Country).HasConversion(new EnumToStringConverter<CountryEnum>()).HasPrecision(450);

            modelBuilder.Entity<TaxPayment>().HasOne(a => a.TaxPaymentType);
            modelBuilder.Entity<TaxPayment>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<TaxPayment>().HasOne(a => a.Agency);
            modelBuilder.Entity<TaxPayment>().HasOne(a => a.CheckInPoint);
            modelBuilder.Entity<TaxPayment>().Property(a => a.TaxType).HasConversion(new EnumToStringConverter<TaxType>()).HasPrecision(450);
            modelBuilder.Entity<TaxPayment>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<TaxPayment>().Property(a => a.Fee).HasPrecision(18, 2);

            modelBuilder.Entity<TaxPaymentBalance>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<TaxPaymentBalance>().HasOne(a => a.Agency);
            modelBuilder.Entity<TaxPaymentBalance>().Property(a => a.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<TaxPaymentBalance>().Property(a => a.TaxType).HasConversion(new EnumToStringConverter<TaxType>()).HasPrecision(450);

            modelBuilder.Entity<TaxPaymentType>().HasOne(a => a.Partner);
            modelBuilder.Entity<TaxPaymentType>().Property(a => a.TaxPaymentStatus).HasConversion(new EnumToStringConverter<TaxPaymentStatus>()).HasPrecision(450);

            modelBuilder.Entity<Agency>().HasOne(a => a.Partner);
            modelBuilder.Entity<Agency>().HasOne(a => a.Country);
            modelBuilder.Entity<Agency>().HasMany(a => a.ExcursionInvoices).WithOne(a => a.Agency).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Excursion>().HasOne(a => a.Agency);
            modelBuilder.Entity<Excursion>().HasOne(a => a.Country);
            modelBuilder.Entity<Excursion>().Property(a => a.ExcursionTaxAmount).HasPrecision(18, 2);

            modelBuilder.Entity<ExcursionInvoice>().HasOne(a => a.Agency).WithMany(a => a.ExcursionInvoices).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ExcursionInvoice>().HasOne(a => a.TaxPaymentType).WithMany(a => a.ExcursionInvoices).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ExcursionInvoice>().HasOne(a => a.CheckInPoint);
            modelBuilder.Entity<ExcursionInvoice>().Property(a => a.BillingAmount).HasPrecision(18, 2);
            modelBuilder.Entity<ExcursionInvoice>().Property(a => a.BillingFee).HasPrecision(18, 2);
            modelBuilder.Entity<ExcursionInvoice>().Property(a => a.Status).HasConversion(new EnumToStringConverter<TaxInvoiceStatus>()).HasPrecision(450);

            modelBuilder.Entity<ExcursionInvoiceItem>().HasOne(a => a.ExcursionInvoice).WithMany(a => a.ExcursionInvoiceItems).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ExcursionInvoiceItem>().HasOne(a => a.Country);
            modelBuilder.Entity<ExcursionInvoiceItem>().Property(a => a.Price).HasPrecision(18, 2);
            modelBuilder.Entity<ExcursionInvoiceItem>().Property(a => a.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<Partner>().Property(a => a.Country).HasConversion(new EnumToStringConverter<CountryEnum>()).HasPrecision(450);
            modelBuilder.Entity<Partner>().Property(a => a.PartnerType).HasConversion(new EnumToStringConverter<PartnerType>()).HasPrecision(450);

            modelBuilder.Entity<PartnerTaxSetting>().Property(a => a.TaxType).HasConversion(new EnumToStringConverter<TaxType>()).HasPrecision(450);
            modelBuilder.Entity<PartnerTaxSetting>().Property(a => a.TaxPrice).HasPrecision(18, 2);

            modelBuilder.Entity<Municipality>().Property(a => a.Country).HasConversion(new EnumToStringConverter<Enums.CountryEnum>()).HasPrecision(450);
            modelBuilder.Entity<Municipality>().Property(a => a.ResidenceTaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Municipality>().HasMany(a => a.Properties).WithOne(a => a.Municipality).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LegalEntity>().Property(a => a.Type).HasConversion(new EnumToStringConverter<LegalEntityType>()).HasPrecision(450);
            modelBuilder.Entity<LegalEntity>().Property(a => a.Country).HasConversion(new EnumToStringConverter<CountryEnum>()).HasPrecision(450);

            modelBuilder.Entity<Property>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Property>().HasOne(a => a.Municipality);
            modelBuilder.Entity<Property>().HasMany(a => a.GuestTokens);
            modelBuilder.Entity<Property>().HasMany(a => a.Groups).WithOne(a => a.Property).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Property>().Property(a => a.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.ResidenceTax).HasPrecision(18, 2);
            modelBuilder.Entity<Property>().Property(a => a.GeoLon).HasPrecision(12, 8);
            modelBuilder.Entity<Property>().Property(a => a.GeoLat).HasPrecision(12, 8);

            modelBuilder.Entity<PropertyUnit>().Property(a => a.Price).HasPrecision(12, 8);

            modelBuilder.Entity<Partner>().Property(a => a.Country).HasConversion(new EnumToStringConverter<CountryEnum>()).HasPrecision(450);

            modelBuilder.Entity<Vessel>().HasOne(a => a.Partner);
            modelBuilder.Entity<Vessel>().HasOne(a => a.Country);
            modelBuilder.Entity<Vessel>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Vessel>().Property(a => a.VesselType).HasConversion(new EnumToStringConverter<VesselType>()).HasPrecision(450);

            modelBuilder.Entity<Group>().HasOne(a => a.LegalEntity);
            modelBuilder.Entity<Group>().HasMany(g => g.Persons).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Group>().HasMany(g => g.PaymentTransactions).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Group>().HasOne(a => a.Property);
            modelBuilder.Entity<Group>().HasOne(a => a.Vessel);
            modelBuilder.Entity<Group>().HasOne(a => a.CheckInPoint);
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
            modelBuilder.Entity<MnePerson>().HasOne(a => a.ResTaxPaymentType);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.ResTaxExemptionType);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.CheckInPoint);
            modelBuilder.Entity<MnePerson>().HasOne(a => a.Computer).WithMany(b => b.MnePersons).HasForeignKey(a => a.ComputerCreatedId); ;
            modelBuilder.Entity<MnePerson>().Property(a => a.ResTaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<MnePerson>().Property(a => a.ResTaxFee).HasPrecision(18, 2);
            modelBuilder.Entity<MnePerson>().Property(a => a.ResTaxStatus).HasConversion(new EnumToStringConverter<ResTaxStatus>()); 

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

            modelBuilder.Entity<ResTaxExemptionType>().HasOne(a => a.Partner);

            modelBuilder.Entity<ResTaxHistory>().HasOne(a => a.Person).WithMany(a => a.ResTaxHistory).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ResTaxHistory>().Property(a => a.PrevResTaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxHistory>().Property(a => a.PrevResFeeAmount).HasPrecision(18, 2);

            modelBuilder.Entity<ResTaxPaymentType>().HasOne(a => a.Partner);
            modelBuilder.Entity<ResTaxPaymentType>().HasMany(a => a.PaymentFees).WithOne(a => a.ResTaxPaymentType).OnDelete(DeleteBehavior.NoAction); ;
            modelBuilder.Entity<ResTaxPaymentType>().Property(a => a.PaymentStatus).HasConversion(new EnumToStringConverter<Enums.TaxPaymentStatus>());

            modelBuilder.Entity<ResTaxFee>().HasOne(a => a.Partner);
            modelBuilder.Entity<ResTaxFee>().HasOne(a => a.ResTaxPaymentType);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.FeeAmount).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.FeePercentage).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.UpperLimit).HasPrecision(18, 2);
            modelBuilder.Entity<ResTaxFee>().Property(a => a.LowerLimit).HasPrecision(18, 2);

            modelBuilder.Entity<NauticalTax>().HasOne(a => a.Partner);
            modelBuilder.Entity<NauticalTax>().Property(a => a.VesselType).HasConversion(new EnumToStringConverter<VesselType>());
            modelBuilder.Entity<NauticalTax>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<NauticalTax>().Property(a => a.LowerLimitLength).HasPrecision(18, 2);
            modelBuilder.Entity<NauticalTax>().Property(a => a.UpperLimitLength).HasPrecision(18, 2);

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

            modelBuilder.Entity<PaymentMethod>().ToTable("PaymentMethods");
            modelBuilder.Entity<PaymentMethod>().HasOne(a => a.PaymentTransaction);
            modelBuilder.Entity<PaymentMethod>().HasOne(a => a.LegalEntity);

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
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(TotalNightsWithinPeriod), new[] { typeof(int), typeof(DateTime), typeof(DateTime) })!)
                .HasName("TotalNightsWithinPeriod");

            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(PropertyDesc), new[] { typeof(int) })!)
                .HasName("PropertyDesc");

            modelBuilder
                .HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(Balance), new[] { typeof(string), typeof(int), typeof(int) })!)
                .HasName("Balance");

            modelBuilder.Entity<Computer>().HasOne(a => a.Partner).WithMany(a => a.Computers);
            modelBuilder.Entity<Computer>().HasMany(a => a.MnePersons).WithOne(a => a.Computer);
            modelBuilder.Entity<Computer>().HasMany(a => a.ComputerLogs).WithOne(a => a.Computer).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Computer>().HasIndex(c => c.PCName);

            modelBuilder.Entity<ComputerLog>().HasIndex(cl => cl.ComputerId);
            modelBuilder.Entity<ComputerLog>().HasIndex(cl => cl.Seen);

            modelBuilder.Entity<MnePerson>(b =>
            {
                b.ToTable("MnePersons", tb =>
                {
                    tb.HasTrigger("trg_MnePersons_Insert");
                    tb.HasTrigger("trg_MnePersons_Update");
                    tb.HasTrigger("trg_MnePersons_Delete");
                });
            });

            modelBuilder.Entity<MneGuestListDto>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });
        }

        public DbSet<LegalEntity> LegalEntities { get; set; }
		public DbSet<TaxPayment> TaxPayments { get; set; }
        public DbSet<TaxPaymentBalance> TaxPaymentBalances { get; set; }
        public DbSet<TaxPaymentType> TaxPaymentTypes { get; set; }
		public DbSet<Partner> Partners { get; set; }
        public DbSet<PartnerTaxSetting> PartnerTaxSettings { get; set; }
        public DbSet<Agency> Agencies { get; set; }
        public DbSet<Excursion> Excursions { get; set; }
        public DbSet<ExcursionInvoice> ExcursionInvoices { get; set; }
        public DbSet<ExcursionInvoiceItem> ExcursionInvoiceItems { get; set; }
        public DbSet<CheckInPoint> CheckInPoints { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentItem> DocumentItems { get; set; }
        public DbSet<DocumentPayment> DocumentPayments { get; set; }
        public DbSet<Item> Items { get; set; }        
        public DbSet<FiscalEnu> FiscalEnu { get; set; }
        public DbSet<FiscalRequest> FiscalRequests { get; set; }
        public DbSet<Property> Properties { get; set; }
		public DbSet<Vessel> Vessels { get; set; }
		public DbSet<PropertyUnit> PropertyUnits { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<MnePerson> MnePersons { get; set; }
        public DbSet<SrbPerson> SrbPersons { get; set; }
        public DbSet<CodeList> CodeLists { get; set; }        
        public DbSet<SelfRegisterToken> SelfRegisterTokens { get; set; }
        public DbSet<ResTaxCalc> ResTaxCalc { get; set; }
        public DbSet<ResTaxCalcItem> ResTaxCalcItems { get; set; }
		public DbSet<ResTaxType> ResTaxTypes { get; set; }
        public DbSet<ResTaxExemptionType> ResTaxExemptionTypes { get; set; }
        public DbSet<ResTaxPaymentType> ResTaxPaymentTypes { get; set; }
		public DbSet<ResTaxFee> ResTaxFees { get; set; }
        public DbSet<ResTaxHistory> ResTaxHistory { get; set; }
        public DbSet<NauticalTax> NauticalTax { get; set; }
		public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Log> Logs { get; set; }
		public DbSet<MneMupData> MneMupData { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<ComputerLog> ComputerLogs { get; set; }


        public string GuestList(int id) => throw new NotImplementedException();
        public string GroupDesc(int id) => throw new NotImplementedException();
        public string PropertyDesc(int id) => throw new NotImplementedException();
        public int Nights(int id, DateTime last) => throw new NotImplementedException();
        public int TotalNightsWithinPeriod(int personId, DateTime from, DateTime to) => throw new NotImplementedException();
        public decimal Balance(string taxType, int legalEntity, int agency) => throw new NotImplementedException();

        private string GetCurrentUserName()
        {
            return _accessor.HttpContext?.User?.Identity?.Name ?? "unknown";
        }
    }
}