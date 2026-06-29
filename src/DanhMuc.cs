using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class DanhMuc
    {
        public int Id { get; set; }
        public string TenDanhMuc { get; set; }
        public string LoaiGiaoDich { get; set; } 
        public string TaiKhoan { get; set; }  
    }
}
