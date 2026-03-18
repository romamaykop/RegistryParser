using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models.DTOs
{
    /// <summary>
    /// DTO объект услуги
    /// Содержит информацию об услугах плательщика
    /// </summary>
    public partial class INREG_SERVICE
    {
        public long ID { get; set; }
        public long IRS_PAYER_ID { get; set; }
        public int IRS_SERVICEPROVIDER_ID { get; set; }
        public decimal IRP_PAYSUMMA { get; set; }
        public decimal? IRP_FINESUMMA { get; set; }
        public decimal IRP_FULLSUMMA { get; set; }
        public decimal? IRS_ADD_FINE { get; set; }
        public decimal? IRS_ADD_TOTAL { get; set; }
        public int IRS_CALC_TYPE_ID { get; set; }
        public string IRS_CALC_TYPE_NAME { get; set; }
        public decimal IRS_TARIFF { get; set; }
        public decimal IRS_FLOWRATE { get; set; }
        public string IRS_SRVNAME { get; set; }
        public decimal? FIELD_6 { get; set; }
        public decimal? FIELD_7 { get; set; }
        public decimal? FIELD_8 { get; set; }
        public decimal? FIELD_9 { get; set; }
        public decimal? FIELD_10 { get; set; }

        public virtual INREG_PAYER INREG_PAYER { get; set; }
    }
}
