using EfiService;
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


    public Document Storno(Document doc)
    {
        return doc;
    }


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
        _db.Documents.Add(doc);
        _db.SaveChanges();
        doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
        _db.SaveChanges();            
        

        foreach (var s in doc.DocumentItems.ToList())
        {
            _db.DocumentItems.Remove(s);
        }
        _db.SaveChanges();

        foreach (var i in stavke)
        {
            var art = _db.Items.Where(a => a.Id == i.Artikal).FirstOrDefault();

            var stavka = new DocumentItem();

            stavka.DocumentId = doc.Id;
            stavka.ItemId = art.Id;
            stavka.ItemUnit = art.Unit;
            stavka.Quantity = i.Kolicina;
            stavka.UnitPrice = i.Cijena;
            stavka.Discount = 0;
            stavka.VatRate = art.VatRate;
            stavka.VatExempt = art.VatExempt;

            _db.DocumentItems.Add(stavka);
            _db.SaveChanges();

            SetItemValues(stavka);

            _db.SaveChanges();
        }

        Payment(doc, PaymentType.Cash);

        return doc;
    }


    public (Document, string, string) CreateRacun(Invoice racun, Property property)
    {
        var doc = _db.Documents.Where(a => a.Id == racun!.Id).FirstOrDefault();
        if (doc == null)
        {
            doc = new Document();
            _db.Documents.Add(doc);
            doc.LegalEntityId = property.LegalEntityId;
            doc.PropertyId = property.Id;
            doc.IdEncrypted = string.Empty;
            doc.BusinessUnitCode = property.BusinessUnitCode;
            doc.FiscalEnuCode = property.FiscalEnuCode;
            doc.Status = DocumentStatus.Active;
            doc.No = 0;
            doc.ExternalNo = "";
            doc.OrdinalNo = 0;
            doc.InvoiceDate = racun.InvoiceDate;
            doc.GroupId = racun.GroupId;
            doc.OperatorCode = _appUser.EfiOperator;

            if (racun.PartnerId.HasValue)
            {
            }
            else
            {
                doc.PartnerName = racun.PartnerName;
                doc.PartnerType = racun.PartnerType;
                doc.PartnerIdType = racun.PartnerIdType;
                doc.PartnerIdNumber = racun.PartnerIdNumber;
            }

            _db.SaveChanges();
            doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
            _db.SaveChanges();
        }
        else
        {
            if (doc.Status == DocumentStatus.Fiscalized)
            {
                return (doc, "", "Ne možete mijenjati račun koji je fiskalizovan");                                        
            }
        }

        var to_delete = doc.DocumentItems.Where(a => racun.DocumentItems.Any(b => b.ID != 0 && b.ID == a.Id) == false).ToList();
        foreach (var del in to_delete) _db.DocumentItems.Remove(del);
        _db.SaveChanges();

        foreach (var stavka in racun.DocumentItems)
        {
            var s = UpdateStavka(doc, stavka, "N");
        }

        Payment(doc, racun.PaymentType);

        _db.SaveChanges();

        var rac = Doc2Racun(doc);

        return (doc, "", "");
    }


    public void DeleteRacun(Document doc)
    {
        return;
    }


    public DocumentItem UpdateStavka(Document doc, Stavka s, string delete)
    {
        DocumentItem sd = null;
        if (s.ID == 0)
        {
            sd = new DocumentItem();
            sd.DocumentId = doc.Id;
            doc.DocumentItems.Add(sd);
        }
        else
        {
            sd = _db.DocumentItems.Where(a => a.Id == s.ID).FirstOrDefault();
        }

        if (delete == "Y")
        {
            _db.DocumentItems.Remove(sd);
            _db.SaveChanges();

            return new DocumentItem();
        }


        var art = _db.Items.Where(a => a.Id == s.Artikal).FirstOrDefault();

        sd.DocumentId = doc.Id;
        sd.ItemId = art.Id;
        sd.ItemCode = art.Code;
        sd.ItemUnit = art.Unit;
        sd.ItemName = art.Name;
        sd.Quantity = s.Kolicina;
        sd.UnitPrice = s.Cijena;        
        sd.Discount = 0;
        sd.FinalPrice = sd.UnitPrice * (100m - sd.Discount) / 100m;
        sd.VatRate = art.VatRate;
        _db.SaveChanges();
        _db.Entry(sd).Reference(a => a.Item).Load();
        SetItemValues(sd);
        _db.SaveChanges();

        return sd;
    }


    public void SetItemValues(DocumentItem s)    {
        s.ItemUnit = s.Item.Unit;        
        s.UnitPriceWoVat = s.UnitPrice * 100m / (100m + s.VatRate);        
        s.LineAmount = s.Quantity * s.UnitPrice;
        s.LineTotalWoVat = s.Quantity * s.FinalPrice * 100m / (100m + s.VatRate);
        s.VatAmount = s.Quantity * s.FinalPrice * s.VatRate / (100m + s.VatRate);
        s.LineTotal = s.Quantity * s.FinalPrice;
    }

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
        r.PartnerIdType = d.PartnerIdType;                     

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

        r.DocumentItems = new List<Stavka>();

        foreach (var s in d.DocumentItems.ToList())
        {
            r.DocumentItems.Add(new Stavka()
            {
                ID = s.Id,
                RacunID = s.DocumentId,
                Artikal = s.ItemId,
                JedinicaMjere = s.ItemUnit,
                Cijena = s.FinalPrice,
                Kolicina = s.Quantity,
                Iznos = s.LineTotal
            });
        }

        return r;
    }


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

    public (int, int) DocumentNumbers(string bu)
    {
        var no = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.No).Max() ?? 0;
        var ordno = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.OrdinalNo).Max() ?? 0;

        return (no + 1, ordno + 1);
    }

    public string ExternalNumber(Document doc)
    {
        return "";
    }

    public async Task<Stream> InvoicePdf(int id)
    {
        var docxProvider = new DocxFormatProvider();
        var pdfProvider = new PdfFormatProvider();
        RadFlowDocument document;

        var path = _webHostEnvironment.WebRootPath + "/templates/invoice.docx";

        using (Stream input = File.OpenRead(path))
        {
            document = docxProvider.Import(input);
        }

        RadFlowDocumentEditor editor = new RadFlowDocumentEditor(document);
        editor.ReplaceText("Point", "trtvrt");
        editor.ReplaceText("<Expr3>", "");

        var stream = new MemoryStream();
        pdfProvider.Export(document, stream);
        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    public async Task InvoiceEmail(int id, string email)
    {
        var tax = _db.ResTaxes.FirstOrDefault(a => a.Id == id);
        var obj = _db.Properties.FirstOrDefault(a => a.Id == tax.PropertyId);
        var pdfStream = await InvoicePdf(id);
        var template = _config["SendGrid:Templates:EfiInvoice"]!;
        await _eMailService.SendMail(_appUser.Email, email, template, new
        {
            subject = $@"donotreply: Prijava boravišne takse",
            body = $"Poštovani,\n\nU prilogu se nalazi prijava boravišne takse za period od {tax.DateFrom.ToString("dd.MM.yyyy")} od {tax.DateTo.ToString("dd.MM.yyyy")} za smještajni objekat {obj.Name}.\n\nSrdačan pozdrav,"
        }, ("Boravišna taksa.pdf", pdfStream));
    }
}
