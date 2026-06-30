using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class TaiKhoanViewModel
    {
        public string TaiKhoan { get; set; }
        public string Email { get; set; }
        public bool TrangThai { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public int SoGiaoDich30Ngay { get; set; }
    }
}
