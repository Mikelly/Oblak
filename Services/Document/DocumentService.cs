<<<<<<< HEAD
﻿using Oblak.Data;
using Oblak.Models.EFI;
using Kendo.Mvc.Extensions;
using Oblak.Helpers;
using Microsoft.EntityFrameworkCore;
=======
﻿using EfiService;
using Oblak.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using Oblak.Models.rb90;
using Kendo.Mvc.Extensions;
using Oblak.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Telerik.Windows.Documents.Flow.FormatProviders.Docx;
using Telerik.Windows.Documents.Flow.Model.Editing;
using Telerik.Windows.Documents.Flow.Model;
using Telerik.Windows.Documents.Flow.FormatProviders.Pdf;
using Oblak.Services;
using System.Xml.Serialization;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
using Oblak.Data.Enums;

namespace Oblak.Services;

public class DocumentService
{
    readonly IWebHostEnvironment _env;
    readonly HttpContext _context;
    readonly ILogger<EfiClient> _logger;
    readonly ApplicationDbContext _db;
    readonly IConfiguration _config;
    readonly ApplicationUser _appUser;
    readonly eMailService _eMailService;
    readonly IWebHostEnvironment _webHostEnvironment;

    public DocumentService(
        IConfiguration config,
        IHttpContextAccessor contextAccesor,
        IWebHostEnvironment env,        
        ILogger<EfiClient> logger,
        ApplicationDbContext db,
        eMailService eMailService,
        IWebHostEnvironment webHostEnvironment
        ) 
    {
        _config = config;
        _context = contextAccesor.HttpContext!;
        _env = env;
        _logger = logger;
        _db = db;
        _eMailService = eMailService;
        _webHostEnvironment = webHostEnvironment;        
        _appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == _context.User.Identity!.Name)!;
    }


    public async Task<Document> CreateInvoice(Group g, PaymentType? pay)
    {
        var enu = _db.FiscalEnu.Where(a => a.PropertyId == g.PropertyId).FirstOrDefault();

<<<<<<< HEAD
        var doc = new Document();
        doc.GroupId = g.Id;
        doc.PropertyId = g.PropertyId;
        doc.LegalEntityId = g.LegalEntityId;
        doc.InvoiceDate = DateTime.Now;
        doc.BusinessUnitCode = g.Property.BusinessUnitCode;
        doc.FiscalEnuCode = enu == null ? "" : enu.FiscalEnuCode;
        doc.OperatorCode = enu == null ? "" : enu.OperatorCode;

        var person = g.Persons.First() as MnePerson;

        if (person != null)
        {
            doc.PartnerName = $"{person.FirstName} {person.LastName}";
            doc.PartnerIdType = person.DocumentType switch { "1" => BuyerIdType.Passport, "2" => BuyerIdType.ID, _ => BuyerIdType.Passport };
            doc.PartnerIdNumber = person.DocumentNumber;
            doc.PartnerType = Data.Enums.BuyerType.Person;
            doc.PartnerAddress = person.PermanentResidenceAddress ?? "";
        }

=======

    public Document CreateInvoice(int? g, int? o, int? gost, int? noc)
    {   
        Item brv = null;
        Item btx = null;
        CheckItems(out brv, out btx);

        List<Stavka> stavke = new List<Stavka>();
        
        var obj = _db.Properties.FirstOrDefault(a => a.Id == o.Value);
        var npl = obj.PaymentType;
        var cij = obj.Price ?? 0m;
        var bor = (obj.ResidenceTaxYN ?? true) ? (obj.ResidenceTax ?? 0m) : 0m;

        stavke.Add(
            new Stavka() { 
                Artikal = brv.Id, 
                Cijena = cij, 
                Kolicina = (npl == "A" ? noc.Value : noc.Value * gost.Value) 
            });

        if (bor > 0)
            stavke.Add(
                new Stavka()
                {
                    Artikal = btx.Id,
                    Cijena = bor,
                    Kolicina = noc.Value * gost.Value
                });                
       
        var doc = new Document();            
        doc.No = 0;
        doc.Status = DocumentStatus.Active;
        doc.CurrencyCode = "EUR";
        doc.ExchangeRate = 1;
        obj.BusinessUnitCode = obj.BusinessUnitCode;
        doc.FiscalEnuCode = obj.FiscalEnuCode; 
        doc.InvoiceDate = DateTime.Now;
        doc.GroupId = g;
        doc.TypeOfInvoce = TypeOfInvoice.Cash;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        _db.Documents.Add(doc);
        _db.SaveChanges();

        doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
        _db.SaveChanges();

        (var bor, var btax) = CheckItems();

        var boravak_buff = g.Persons.Select(a => a as MnePerson).ToList().Select(a => new
        {
            Item = bor,
            Quant = (int)Math.Round(((a.CheckOut ?? DateTime.Now) - a.CheckIn).TotalDays, 0),
			Price = g.Property.Price ?? 1m
        }).ToList();

		var boravak = boravak_buff.GroupBy(a => new { a.Item, a.Price })
	        .Select(a => new { Item = a.Key.Item, Quant = a.Max(b => b.Quant), Price = a.Key.Price }).ToList();

		var taxes_buff = g.Persons.Select(a => a as MnePerson).ToList().Select(a => new
        {
            Item = btax,
            Quant = (int)Math.Round(((a.CheckOut ?? DateTime.Now) - a.CheckIn).TotalDays, 0),            
            Price = (a.Property.ResidenceTax ?? 1) * ((new DateTime(1, 1, 1) + (DateTime.Now - a.BirthDate)).Year - 1) switch { >= 18 => 1m, >= 12 => 0.5m, _ => 0m }
        }).Where(a => a.Price != 0).ToList();
        
        var taxes = taxes_buff.GroupBy(a => new { a.Item, a.Price })
            .Select(a => new { Item = a.Key.Item, Quant = a.Sum(b => b.Quant), Price = a.Key.Price }).ToList();

        foreach (var i in boravak.Union(taxes))
        {
            var sd = new DocumentItem();
            sd.DocumentId = doc.Id;
            sd.ItemId = i.Item.Id;
            sd.ItemCode = i.Item.Code ?? "";
            sd.ItemName = i.Item.Name;
            sd.ItemUnit = i.Item.Unit;
            sd.UnitPrice = i.Price;
            sd.Quantity = (decimal)i.Quant;
            sd.Discount = 0;
            sd.FinalPrice = i.Price;
            sd.VatExempt = i.Item.VatExempt;
            _db.DocumentItems.Add(sd);
            _db.SaveChanges();
            SetItemValues(sd);
            _db.SaveChanges();
        }

<<<<<<< HEAD
        doc.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum();
        _db.SaveChanges();

        Payment(doc, pay ?? PaymentType.Cash);
=======
        Payment(doc, PaymentType.Cash);
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        return doc;
    }


<<<<<<< HEAD
    public Document CreateInvoice(Invoice racun, Property property)
    {
        var enu = _db.FiscalEnu.FirstOrDefault(a => a.PropertyId == property.Id);
=======
    public (Document, string, string) CreateRacun(Invoice racun, Property property)
    {
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        var doc = _db.Documents.Where(a => a.Id == racun!.Id).FirstOrDefault();
        if (doc == null)
        {
            doc = new Document();
            _db.Documents.Add(doc);
            doc.LegalEntityId = property.LegalEntityId;
            doc.PropertyId = property.Id;
            doc.IdEncrypted = string.Empty;
            doc.BusinessUnitCode = property.BusinessUnitCode;
<<<<<<< HEAD
            doc.FiscalEnuCode = enu == null ? "" : enu.FiscalEnuCode;
            doc.OperatorCode = enu == null ? "" : enu.OperatorCode;
=======
            doc.FiscalEnuCode = property.FiscalEnuCode;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
            doc.Status = DocumentStatus.Active;
            doc.No = 0;
            doc.ExternalNo = "";
            doc.OrdinalNo = 0;
            doc.InvoiceDate = racun.InvoiceDate;
            doc.GroupId = racun.GroupId;
<<<<<<< HEAD
=======
            doc.OperatorCode = _appUser.EfiOperator;
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

            if (racun.PartnerId.HasValue)
            {
            }
            else
            {
                doc.PartnerName = racun.PartnerName;
                doc.PartnerType = racun.PartnerType;
                doc.PartnerIdType = racun.PartnerIdType;
                doc.PartnerIdNumber = racun.PartnerIdNumber;
<<<<<<< HEAD
                if (doc.PartnerType == Data.Enums.BuyerType.Company) doc.PartnerIdType = BuyerIdType.TaxIdNumber;
=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
            }

            _db.SaveChanges();
            doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
            _db.SaveChanges();
        }
        else
        {
            if (doc.Status == DocumentStatus.Fiscalized)
            {
                return doc;
            }
        }

<<<<<<< HEAD
        var to_delete = doc.DocumentItems.Where(a => racun.DocumentItems.Any(b => b.Id != 0 && b.Id == a.Id) == false).ToList();
=======
        var to_delete = doc.DocumentItems.Where(a => racun.DocumentItems.Any(b => b.ID != 0 && b.ID == a.Id) == false).ToList();
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        foreach (var del in to_delete) _db.DocumentItems.Remove(del);
        _db.SaveChanges();

        foreach (var stavka in racun.DocumentItems)
        {
            var s = UpdateStavka(doc, stavka, "N");
        }

<<<<<<< HEAD
        _db.Entry(doc).Collection(a => a.DocumentPayments).Load();

=======
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        Payment(doc, racun.PaymentType);

        _db.SaveChanges();

        return doc;
    }


    public Document StornoInvoice(Document doc)
    {
        var storno = new Document();
        storno.DocumentId = doc.Id;
        storno.LegalEntityId = doc.LegalEntityId;
        storno.PartnerIdNumber = doc.PartnerIdNumber;
        storno.PartnerId = doc.PartnerId;
        storno.PartnerIdType = doc.PartnerIdType;
        storno.PartnerName = doc.PartnerName;
        storno.OperatorCode = doc.OperatorCode;
        storno.Status = DocumentStatus.Active;
        storno.Amount = -doc.Amount;
        storno.BusinessUnitCode = doc.BusinessUnitCode;
        storno.FiscalEnuCode = doc.FiscalEnuCode;
        storno.CurrencyCode = doc.CurrencyCode;
        storno.DocumentType = DocumentType.CorrectiveInvoice;
        storno.ExchangeRate = doc.ExchangeRate;
        storno.GroupId = doc.GroupId;
        storno.InvoiceDate = DateTime.Now;
        storno.PropertyId = doc.PropertyId;

        storno.DocumentItems = doc.DocumentItems.Select(a => new DocumentItem()
        {
            ItemId = a.ItemId,
            ItemCode = a.ItemCode,
            ItemName = a.ItemName,
            ItemUnit = a.ItemUnit,
            UnitPrice = a.UnitPrice,
            UnitPriceWoVat = a.UnitPriceWoVat,
            Discount = a.Discount,
            DiscountAmount = -a.DiscountAmount,
            Quantity = -a.Quantity,
            FinalPrice = a.FinalPrice,
            VatRate = a.VatRate,
            VatExempt = a.VatExempt,
            VatAmount = -a.VatAmount,
            LineAmount = -a.LineAmount,
            LineTotalWoVat = -a.LineTotalWoVat,
            LineTotal = -a.LineTotal
        }).ToList();

        storno.DocumentPayments = doc.DocumentPayments.Select(a => new DocumentPayment()
        {
            Amount = -a.Amount,
            PaymentType = a.PaymentType
        }).ToList();

        _db.Documents.Add(storno);
        _db.SaveChanges();

        storno.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(storno.Id.ToString()));
        _db.SaveChanges();

        return storno;
    }



    public (Item, Item) CheckItems()
    {
        var le = _appUser.LegalEntityId;

        var boravak = _db.Items.Where(a => a.LegalEntityId == le && a.Code == "BORAV").FirstOrDefault();
        if (boravak == null)
        {
            boravak = new Item();
            boravak.LegalEntityId = le;
            boravak.Code = "BORAV";
            boravak.Name = "Usluga smještaja";
            boravak.Description = "Usluga smještaja";
            boravak.Unit = "KOM";
            boravak.VatRate = _appUser.LegalEntity.InVat ? 21 : 0; // Konfigurisati stopu poreza
            boravak.Price = 100;
            _db.Items.Add(boravak);
            _db.SaveChanges();
        }

        var btax = _db.Items.Where(a => a.LegalEntityId == le && a.Code == "BTAX").FirstOrDefault();
        if (btax == null)
        {
            btax = new Item();
            btax.LegalEntityId = le;
            btax.Code = "BTAX";
            btax.Name = "Boravišna taksa";
            btax.Description = "Boravišna taksa";
            btax.Unit = "KOM";
            btax.VatRate = 0;
            btax.VatExempt = MneVatExempt.VAT_CL20;
            btax.Price = 1;
            _db.Items.Add(btax);
            _db.SaveChanges();
        }

        return (boravak, btax);
    }


    public void SetItemValues(DocumentItem s)
    {
        s.ItemUnit = s.Item.Unit;
        s.UnitPriceWoVat = s.UnitPrice * 100m / (100m + s.VatRate);
        s.LineAmount = s.Quantity * s.UnitPrice;
        s.LineTotalWoVat = s.Quantity * s.FinalPrice * 100m / (100m + s.VatRate);
        s.VatAmount = s.Quantity * s.FinalPrice * s.VatRate / (100m + s.VatRate);
        s.LineTotal = s.Quantity * s.FinalPrice;
    }


    public void Payment(Document doc, PaymentType pType)
    {
        var to_delete = doc.DocumentPayments.Where(a => a.PaymentType != pType).ToList();
        _db.RemoveRange(to_delete);
        _db.SaveChanges();

        var payment = doc.DocumentPayments.Where(a => a.PaymentType == pType).FirstOrDefault();
        if (payment == null)
        {
            payment = new DocumentPayment();
            payment.DocumentId = doc.Id;
            payment.PaymentType = pType;
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            _db.DocumentPayments.Add(payment);
            _db.SaveChanges();
        }
        else
        {
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            payment.PaymentType = pType;
            _db.SaveChanges();
        }

        if (doc.DocumentPayments.All(a => a.PaymentType == PaymentType.BankAccount)) doc.TypeOfInvoce = TypeOfInvoice.NonCash;
        else doc.TypeOfInvoce = TypeOfInvoice.Cash;

        _db.SaveChanges();
    }


    


    //public Document CreateInvoice(int? g, int? o, int? gost, int? noc)
    //{   
    //    Item brv = null;
    //    Item btx = null;
    //    CheckItems(out brv, out btx);

    //    List<InvoiceItem> stavke = new List<InvoiceItem>();
        
    //    var obj = _db.Properties.FirstOrDefault(a => a.Id == o.Value);
    //    var npl = obj.PaymentType;
    //    var cij = obj.Price ?? 0m;
    //    var bor = (obj.ResidenceTaxYN ?? true) ? (obj.ResidenceTax ?? 0m) : 0m;

    //    stavke.Add(
    //        new InvoiceItem() { 
    //            ItemId = brv.Id, 
    //            Price = cij, 
    //            Quantity = (npl == "A" ? noc.Value : noc.Value * gost.Value) 
    //        });

    //    if (bor > 0)
    //        stavke.Add(
    //            new InvoiceItem()
    //            {
    //                ItemId = btx.Id,
    //                Price = bor,
    //                Quantity = noc.Value * gost.Value
    //            });                
       
    //    var doc = new Document();            
    //    doc.No = 0;
    //    doc.Status = DocumentStatus.Active;
    //    doc.CurrencyCode = "EUR";
    //    doc.ExchangeRate = 1;
    //    obj.BusinessUnitCode = obj.BusinessUnitCode;
    //    doc.FiscalEnuCode = obj.FiscalEnuCode; 
    //    doc.InvoiceDate = DateTime.Now;
    //    doc.GroupId = g;
    //    doc.TypeOfInvoce = TypeOfInvoice.Cash;
    //    _db.Documents.Add(doc);
    //    _db.SaveChanges();
    //    doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
    //    _db.SaveChanges();            
        

    //    foreach (var s in doc.DocumentItems.ToList())
    //    {
    //        _db.DocumentItems.Remove(s);
    //    }
    //    _db.SaveChanges();

    //    foreach (var i in stavke)
    //    {
    //        var art = _db.Items.Where(a => a.Id == i.ItemId).FirstOrDefault();

    //        var stavka = new DocumentItem();

    //        stavka.DocumentId = doc.Id;
    //        stavka.ItemId = art.Id;
    //        stavka.ItemUnit = art.Unit;
    //        stavka.Quantity = i.Quantity;
    //        stavka.UnitPrice = i.Price;
    //        stavka.Discount = 0;
    //        stavka.VatRate = art.VatRate;
    //        stavka.VatExempt = art.VatExempt;

    //        _db.DocumentItems.Add(stavka);
    //        _db.SaveChanges();

    //        SetItemValues(stavka);

    //        _db.SaveChanges();
    //    }

    //    Payment(doc, PaymentType.Cash);

    //    return doc;
    //}

        


    public void DeleteRacun(Document doc)
    {
        return;
    }


    public DocumentItem UpdateStavka(Document doc, InvoiceItem s, string delete)
    {
        DocumentItem sd = null;
        if (s.Id == 0)
        {
            sd = new DocumentItem();
            sd.DocumentId = doc.Id;
            doc.DocumentItems.Add(sd);
        }
        else
        {
            sd = _db.DocumentItems.Where(a => a.Id == s.Id).FirstOrDefault();
        }

        if (delete == "Y")
        {
            _db.DocumentItems.Remove(sd);
            _db.SaveChanges();

            return new DocumentItem();
        }


<<<<<<< HEAD
        var art = _db.Items.Where(a => a.Id == s.ItemId).FirstOrDefault();
=======
        var art = _db.Items.Where(a => a.Id == s.Artikal).FirstOrDefault();
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        sd.DocumentId = doc.Id;
        sd.ItemId = art.Id;
        sd.ItemCode = art.Code;
        sd.ItemUnit = art.Unit;
        sd.ItemName = art.Name;
<<<<<<< HEAD
        sd.Quantity = s.Quantity;
        sd.UnitPrice = s.Price;        
=======
        sd.Quantity = s.Kolicina;
        sd.UnitPrice = s.Cijena;        
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        sd.Discount = 0;
        sd.FinalPrice = sd.UnitPrice * (100m - sd.Discount) / 100m;
        sd.VatRate = art.VatRate;
        _db.SaveChanges();
        _db.Entry(sd).Reference(a => a.Item).Load();
        SetItemValues(sd);
        _db.SaveChanges();

        return sd;
    }


    //public void SetItemValues(DocumentItem s)    {
    //    s.ItemUnit = s.Item.Unit;        
    //    s.UnitPriceWoVat = s.UnitPrice * 100m / (100m + s.VatRate);        
    //    s.LineAmount = s.Quantity * s.UnitPrice;
    //    s.LineTotalWoVat = s.Quantity * s.FinalPrice * 100m / (100m + s.VatRate);
    //    s.VatAmount = s.Quantity * s.FinalPrice * s.VatRate / (100m + s.VatRate);
    //    s.LineTotal = s.Quantity * s.FinalPrice;
    //}

<<<<<<< HEAD

    public Invoice Doc2Invoice(Document d)
    {
=======
    public void Payment(Document doc, PaymentType pType)
    {
        //var pType = p switch
        //{
        //    1 => PaymentType.Cash, 
        //    2 => PaymentType.BankAccount, 
        //    3 => PaymentType.CreditCard,
        //    _ => PaymentType.Cash
        //};
        
        var payment = doc.DocumentPayments.FirstOrDefault();
        if (payment == null)
        {
            payment = new DocumentPayment();
            payment.DocumentId = doc.Id;                
            payment.PaymentType = pType;
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            _db.DocumentPayments.Add(payment);
            _db.SaveChanges();
        }
        else
        {
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            payment.PaymentType = pType;
            _db.SaveChanges();
        }
    }


    public Invoice Doc2Racun(Document d)
    {
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
        var r = new Invoice();
        r.Id = d.Id;
        r.InvoiceDate = d.InvoiceDate;
        r.GroupId = d.GroupId;
        r.FiscalEnuCode = d.FiscalEnuCode;
        r.BusinessUnitCode = d.BusinessUnitCode;
        r.Status = d.Status;
        r.PartnerName = d.PartnerName;
        r.PartnerIdNumber = d.PartnerIdNumber;
        r.PartnerType = d.PartnerType;
<<<<<<< HEAD
        r.PartnerIdType = d.PartnerIdType;    
        r.Amount = d.Amount;
=======
        r.PartnerIdType = d.PartnerIdType;                     
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

        var pay = d.DocumentPayments.ToList();

        if (pay.Any())
        {
            r.Amount = pay.Sum(a => a.Amount);
            r.PaymentType = pay.First().PaymentType;
        }
        else
        {
            r.Amount = 0;
            r.PaymentType = 0;
        }

<<<<<<< HEAD
        r.DocumentItems = new List<InvoiceItem>();

        foreach (var s in d.DocumentItems.ToList())
        {
            r.DocumentItems.Add(new InvoiceItem()
=======
        r.DocumentItems = new List<Stavka>();

        foreach (var s in d.DocumentItems.ToList())
        {
            r.DocumentItems.Add(new Stavka()
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144
            {
                Id = s.Id,
                InvoiceId = s.DocumentId,
                ItemId = s.ItemId,
                Unit = s.ItemUnit,
                Price = s.FinalPrice,
                Quantity = s.Quantity,
                Amount = s.LineTotal
            });
        }

        return r;
    }


<<<<<<< HEAD
    
=======
    public void CheckItems(out Item boravak, out Item btax)
    {
        boravak = _db.Items.Where(a => a.Code == "BORAV").Where(a => a.LegalEntityId == _appUser.LegalEntityId).FirstOrDefault();
        if (boravak == null)
        {
            boravak = new Item();
            boravak.LegalEntityId = _appUser.LegalEntityId;
            boravak.Code = "BORAV";
            boravak.Name = "Usluga smještaja";
            boravak.Unit = "KOM";
            boravak.VatRate = _appUser!.LegalEntity.InVat ? 21m : 0m;            
            _db.Items.Add(boravak);
            _db.SaveChanges();           
        }

        btax = _db.Items.Where(a => a.Code == "BTAX").Where(a => a.LegalEntityId == _appUser.LegalEntityId).FirstOrDefault();        
        if (btax == null)
        {
            btax = new Item();
            btax.LegalEntityId = _appUser.LegalEntityId;
            btax.Code = "BTAX";
            btax.Name = "Boravišna taksa";
            btax.Unit = "KOM";
            btax.VatRate = _appUser.LegalEntity.InVat ? 21m : 0m;
            btax.VatExempt = MneVatExempt.VAT_CL20;
            _db.Items.Add(boravak);
            _db.SaveChanges();
        }
    }
>>>>>>> 579dec8aee400fe2cc7b097420fe5d3e419ae144

    public (int, int) DocumentNumbers(string bu)
    {
        var no = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.No).Max() ?? 0;
        var ordno = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.OrdinalNo).Max() ?? 0;

        return (no + 1, ordno + 1);
    }


    public string ExternalNumber(Document doc)
    {
        return $"{doc.No}/{doc.InvoiceDate.Year}";
    }


    public async Task<byte[]> InvoicePdf(int docId, int reportId)
    {
        var reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
        var deviceInfo = new System.Collections.Hashtable();
        var reportSource = new Telerik.Reporting.UriReportSource();

        var report = _db.Reports.FirstOrDefault(a => a.Id == reportId);
        var path = Path.Combine(_env.ContentRootPath, "Reports", report.Path);

        reportSource.Uri = path;
                
        reportSource.Parameters.Add("document", docId);

        Telerik.Reporting.Processing.RenderingResult result = reportProcessor.RenderReport("PDF", reportSource, deviceInfo);

        if (!result.HasErrors)
        {
            return result.DocumentBytes;
        }

        return null;
    }


    public async Task InvoiceEmail(int docId, int reportId, string email)
    {        
        var bytes = await InvoicePdf(docId, reportId);
        var template = _config["REPORTING:MNE:InvoiceEmailTemplate"]!;
        await _eMailService.SendMail(_config["SENDGRID:EmailAddress"]!, email, template, new
        {
            subject = $@"donotreply: Faktura",
            body = $"Poštovani,\n\nU prilogu se faktura za smještaj.\n\nSrdačan pozdrav,"
        }, ("Faktura.pdf", new MemoryStream(bytes)));
    }
}
