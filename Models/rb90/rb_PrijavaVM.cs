namespace Oblak.Models.rb90
{
    public partial class rb_PrijavaVM
    {
        public int ID { get; set; }
        public int FirmaID { get; set; }
        public int ObjekatID { get; set; }
        public int? GrupaID { get; set; }
        public string Naziv { get; set; }
        public string Obj_Naziv { get; set; }
        public string Obj_RegBr { get; set; }
        public string Obj_Adresa { get; set; }
        public string Tip { get; set; }
        public string Prezime { get; set; }
        public string Ime { get; set; }
        public string JMBG { get; set; }
        public string Pol { get; set; }
        public string Drzava { get; set; }
        public string Rodj_Mjesto { get; set; }
        public string Rodj_Drzava { get; set; }
        public System.DateTime Rodj_Datum { get; set; }
        public string Preb_Drzava { get; set; }
        public string Preb_Mjesto { get; set; }
        public string Preb_Adresa { get; set; }
        public string Borav_Mjesto { get; set; }
        public string Borav_Adresa { get; set; }
        public System.DateTime Borav_Prijava { get; set; }
        public Nullable<System.DateTime> Borav_Odjava { get; set; }
        public string LD_Vrsta { get; set; }
        public string LD_Broj { get; set; }
        public System.DateTime LD_Rok { get; set; }
        public string LD_Drzava { get; set; }
        public string LD_Organ { get; set; }
        public string Visa_Vrsta { get; set; }
        public string Visa_Broj { get; set; }
        public Nullable<System.DateTime> Visa_Od { get; set; }
        public Nullable<System.DateTime> Visa_Do { get; set; }
        public string Visa_Mjesto { get; set; }
        public string Ulaz_Mjesto { get; set; }
        public Nullable<System.DateTime> Ulaz_Datum { get; set; }
        public string Kor_Prezime { get; set; }
        public string Kor_Ime { get; set; }
        public string Kor_JMBG { get; set; }
        public string Ostalo { get; set; }
        public Nullable<int> RefID { get; set; }
        public bool Obrisan { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public string UserCreated { get; set; }
        public Nullable<System.DateTime> UserCreatedDate { get; set; }
        public string UserModified { get; set; }
        public Nullable<System.DateTime> UserModifiedDate { get; set; }
    }
}
