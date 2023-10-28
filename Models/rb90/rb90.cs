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

namespace Oblak.Models.rb90;


public class PrijavaStatus
{
    public string Status { get; set; }
    public string Message { get; set; }
    public int Progress { get; set; }
}

public class Racun
{
    public int ID { get; set; }
    public int? ObjekatID { get; set; }
    public int? PrijavaID { get; set; }
    public DateTime Datum { get; set; }
    public int BrojNocenja { get; set; }
    public int BrojGostiju { get; set; }
    public string Kupac { get; set; }
    public string VrstaKupca { get; set; }
    public string PIB { get; set; }
    public string Dokument { get; set; }
    public string VrstaDokumenta { get; set; }
    public decimal Iznos { get; set; }
    public int NacinPlacanja { get; set; }
    public string Status { get; set; }
    public string ENU { get; set; }

    public List<Stavka> stavke { get; set; }
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



