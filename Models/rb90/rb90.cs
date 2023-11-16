using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using System.Text.Json.Serialization;

namespace Oblak.Models.rb90;


public class PrijavaStatus
{
    public string Status { get; set; }
    public string Message { get; set; }
    public int Progress { get; set; }
}

public class Invoice
{
    public int Id { get; set; }
    public int? PropertyId { get; set; }
    public int? GroupId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int? PartnerId { get; set; }
    public string PartnerName { get; set; }
    public BuyerType PartnerType { get; set; }    
    public BuyerIdType PartnerIdType { get; set; }
    public string PartnerIdNumber { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DocumentStatus Status { get; set; }    
    public string? FiscalEnuCode { get; set; }
    public string? BusinessUnitCode { get; set; }

    public List<Stavka> DocumentItems { get; set; }
}

public class Stavka
{
    public int ID { get; set; }
    public int RacunID { get; set; }
    public int Artikal { get; set; }
    public decimal Kolicina { get; set; }
    public string JedinicaMjere { get; set; }
    public decimal Cijena { get; set; }
    public decimal Iznos { get; set; }
}