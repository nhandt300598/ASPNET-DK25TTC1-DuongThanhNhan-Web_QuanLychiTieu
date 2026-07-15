using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class UserStatItem
    {
        public string TaiKhoan { get; set; }
        public int TongSoGiaoDich { get; set; }
        public decimal TongTienChi { get; set; }
        public decimal TongTienThu { get; set; }
    }
     
    public class UserReportViewModel
    {
        public UserStatItem VuaTiTieu { get; set; }      
        public UserStatItem VuaChamChi { get; set; }     
        public List<UserStatItem> DanhSach { get; set; }  
    }
}
