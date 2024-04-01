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

namespace Oblak.Models.EFI;


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

    public List<InvoiceItem> DocumentItems { get; set; }
}

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
}