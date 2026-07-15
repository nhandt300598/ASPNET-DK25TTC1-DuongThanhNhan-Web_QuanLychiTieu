using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyChiTieu.Models
{
    public class GiaoDich
    {
        public int Id { get; set; }

        [Display(Name = "Ngày")]
        public DateTime NgayGiaoDich { get; set; }

        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [Display(Name = "Loại")]    
        public string LoaiGiaoDich { get; set; } // "Thu" hoặc "Chi"

        [Display(Name = "Danh mục")]
        public string DanhMuc { get; set; }

        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }
        public string TaiKhoan { get; set; }
        public bool LaDinhKy { get; set; }
    }
}