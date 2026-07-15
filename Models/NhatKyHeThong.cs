using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class NhatKyHeThong
    {
        public int Id { get; set; }
        public string TaiKhoan { get; set; }
        public string HanhDong { get; set; }
        public string ChiTiet { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string DiaChiIP { get; set; }
    }
}
