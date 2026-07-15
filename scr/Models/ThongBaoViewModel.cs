using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyChiTieu.Models
{
    public class ThongBaoViewModel
    {
        public int Id { get; set; }
        public string NoiDung { get; set; }
        public string LoaiAlert { get; set; }
        public DateTime NgayHetHan { get; set; }
        public bool TrangThai { get; set; }
    }
}
