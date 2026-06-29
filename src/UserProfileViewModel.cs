using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class UserProfileViewModel
    {
        public string TaiKhoan { get; set; }
        public string Email { get; set; }
        public string MatKhauCu { get; set; }
        public string MatKhauMoi { get; set; }
        public string XacNhanMatKhauMoi { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string SoDienThoai { get; set; } 
    }
}
