using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models.DTOs
{
    /// <summary>
    /// DTO объект ПУ
    /// Содержит информацию о показаниях ПУ плательщика
    /// </summary>
    public partial class INREG_COUNTER
    {
        public long ID { get; set; }
        public string IRC_COUNTER_UID { get; set; }
        public long IRC_PAYER_ID { get; set; }
        public string IRC_ALIAS { get; set; }
        public int? IRC_DEC_SCALE { get; set; }
        public DateTime IRC_PRE_DATE { get; set; }
        public decimal IRC_PRE_VALUE_1 { get; set; }
        public decimal? IRC_PRE_VALUE_2 { get; set; }
        public decimal? IRC_PRE_VALUE_3 { get; set; }
        public int? IRC_CHK_VALUE { get; set; }
        public int IRC_SERVICEPROVIDER_ID { get; set; }
        public decimal? IRC_TARIFF_1 { get; set; }
        public decimal? IRC_TARIFF_2 { get; set; }
        public decimal? IRC_TARIFF_3 { get; set; }
        public decimal? IRC_VOLUME_1 { get; set; }
        public decimal? IRC_VOLUME_2 { get; set; }
        public decimal? IRC_VOLUME_3 { get; set; }
        public decimal? IRC_SUMMA { get; set; }
        public int? IRC_ORDERLABEL { get; set; }
        public decimal? IRC_OLD_VALUE_1 { get; set; }
        public decimal? IRC_OLD_VALUE_2 { get; set; }
        public decimal? IRC_OLD_VALUE_3 { get; set; }

        public virtual INREG_PAYER INREG_PAYER { get; set; }
    }
}
