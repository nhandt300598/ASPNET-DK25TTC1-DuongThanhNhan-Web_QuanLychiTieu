using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuanLyChiTieu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace QuanLyChiTieu.Controllers
{ 
    [Authorize]
    public class AdminController : Controller
    {
         private readonly IConfiguration _config;
        private readonly string connectionString;
         
        public AdminController(IConfiguration config)
        {
            _config = config;
            connectionString = _config.GetConnectionString("DefaultConnection");
        } 
 
        public async Task<IActionResult> Index()
        {
            if (User.Identity.Name != "admin") return RedirectToAction("Index", "Home");
             
            var danhSach = new List<QuanLyChiTieu.Models.TaiKhoanViewModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT TaiKhoan, Email, TrangThai FROM NguoiDung WHERE TaiKhoan != 'admin' ORDER BY TaiKhoan";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        { 
                            danhSach.Add(new QuanLyChiTieu.Models.TaiKhoanViewModel
                            {
                                TaiKhoan = reader["TaiKhoan"].ToString(),
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : string.Empty,
                                TrangThai = Convert.ToBoolean(reader["TrangThai"])
                            });
                        }
                    }
                }
            }
            return View(danhSach);
        }
         
        public async Task<IActionResult> ToggleStatus(string id, bool status)
        {
            if (User.Identity.Name != "admin") return Unauthorized();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE NguoiDung SET TrangThai = @TrangThai WHERE TaiKhoan = @TaiKhoan";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TrangThai", !status); 
                    cmd.Parameters.AddWithValue("@TaiKhoan", id);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("Index");
        }
         
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string username, string newPassword)
        {
            if (User.Identity.Name != "admin") return Unauthorized();

            string hashedPwd = HashPassword(newPassword);
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE NguoiDung SET MatKhau = @MatKhau WHERE TaiKhoan = @TaiKhoan";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@MatKhau", hashedPwd);
                    cmd.Parameters.AddWithValue("@TaiKhoan", username);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            } 
            TempData["SuccessMessage"] = $"Đã cập nhật mật khẩu cho tài khoản '{username}'";
            return RedirectToAction("Index");
        }
         
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Report()
        {
            var model = new UserReportViewModel();
            var list = new List<UserStatItem>();

            string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=DB_QuanLyChiTieu;Integrated Security=True;Encrypt=False;";

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = @"SELECT 
                            u.TaiKhoan,
                            COUNT(g.Id) AS SoGiaoDich,
                            ISNULL(SUM(CASE WHEN g.LoaiGiaoDich = N'Chi' THEN g.SoTien ELSE 0 END), 0) AS TongChi,
                            ISNULL(SUM(CASE WHEN g.LoaiGiaoDich = N'Thu' THEN g.SoTien ELSE 0 END), 0) AS TongThu
                         FROM NguoiDung u
                         LEFT JOIN GiaoDich g ON u.TaiKhoan = g.TaiKhoan
                         WHERE u.TaiKhoan != 'admin' -- Loại trừ ông admin ra khỏi bảng phong thần
                         GROUP BY u.TaiKhoan
                         ORDER BY TongChi DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new UserStatItem
                            {
                                TaiKhoan = r["TaiKhoan"].ToString(),
                                TongSoGiaoDich = Convert.ToInt32(r["SoGiaoDich"]),
                                TongTienChi = Convert.ToDecimal(r["TongChi"]),
                                TongTienThu = Convert.ToDecimal(r["TongThu"])
                            });
                        }
                    }
                }
            }

            model.DanhSach = list;             
            model.VuaTiTieu = list.OrderByDescending(x => x.TongTienChi).FirstOrDefault();
            model.VuaChamChi = list.OrderByDescending(x => x.TongSoGiaoDich).FirstOrDefault();
            return View(model);
        }

       
        [HttpGet]
        public async Task<IActionResult> Category()
        {
            var list = new List<DanhMuc>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, TenDanhMuc, LoaiGiaoDich FROM DanhMuc WHERE TaiKhoan = 'admin' OR TaiKhoan IS NULL ORDER BY LoaiGiaoDich DESC, TenDanhMuc ASC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new DanhMuc
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                TenDanhMuc = r["TenDanhMuc"].ToString(),
                                LoaiGiaoDich = r["LoaiGiaoDich"].ToString()
                            });
                        }
                    }
                }
            }
            return View(list);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string tenDanhMuc, string loaiGiaoDich)
        {
            if (string.IsNullOrEmpty(tenDanhMuc) || string.IsNullOrEmpty(loaiGiaoDich))
            {
                TempData["ErrorMessage"] = "Vui lòng điền tên danh mục và chọn loại!";
                return RedirectToAction("Category");
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string checkQ = "SELECT COUNT(1) FROM DanhMuc WHERE TenDanhMuc = @Ten AND TaiKhoan = 'admin'";
                using (SqlCommand chk = new SqlCommand(checkQ, con))
                {
                    chk.Parameters.AddWithValue("@Ten", tenDanhMuc.Trim());
                    await con.OpenAsync();
                    if ((int)await chk.ExecuteScalarAsync() > 0)
                    {
                        TempData["ErrorMessage"] = "Danh mục này đã tồn tại trên hệ thống!";
                        return RedirectToAction("Category");
                    }
                }
                 
                string insertQ = "INSERT INTO DanhMuc (TenDanhMuc, LoaiGiaoDich, TaiKhoan) VALUES (@Ten, @Loai, 'admin')";
                using (SqlCommand cmd = new SqlCommand(insertQ, con))
                {
                    cmd.Parameters.AddWithValue("@Ten", tenDanhMuc.Trim());
                    cmd.Parameters.AddWithValue("@Loai", loaiGiaoDich);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["SuccessMessage"] = $"Đã tạo mục [{tenDanhMuc}] thành công!";
            return RedirectToAction("Category");
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM DanhMuc WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            TempData["SuccessMessage"] = "Đã gỡ bỏ danh mục khỏi hệ thống thành công!";
            return RedirectToAction("Category");
        }

     
        [HttpGet]
        public async Task<IActionResult> Broadcast()
        {
            var list = new List<ThongBaoViewModel>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 10 Id, NoiDung, LoaiAlert, NgayHetHan, TrangThai FROM ThongBaoHeThong ORDER BY Id DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new ThongBaoViewModel
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                NoiDung = r["NoiDung"].ToString(),
                                LoaiAlert = r["LoaiAlert"].ToString(),
                                NgayHetHan = Convert.ToDateTime(r["NgayHetHan"]),
                                TrangThai = Convert.ToBoolean(r["TrangThai"])
                            });
                        }
                    }
                }
            }
            return View(list);
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBroadcast(string noiDung, string loaiAlert, DateTime ngayHetHan)
        {
            if (string.IsNullOrEmpty(noiDung) || ngayHetHan <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Nội dung không được rỗng và thời gian hết hạn phải ở tương lai!";
                return RedirectToAction("Broadcast");
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                await con.OpenAsync();
                 
                string tatCu = "UPDATE ThongBaoHeThong SET TrangThai = 0 WHERE TrangThai = 1";
                using (SqlCommand cmdTat = new SqlCommand(tatCu, con)) { await cmdTat.ExecuteNonQueryAsync(); }
                 
                string insertQ = "INSERT INTO ThongBaoHeThong (NoiDung, LoaiAlert, NgayHetHan, TrangThai) VALUES (@NoiDung, @Loai, @HetHan, 1)";
                using (SqlCommand cmd = new SqlCommand(insertQ, con))
                {
                    cmd.Parameters.AddWithValue("@NoiDung", noiDung.Trim());
                    cmd.Parameters.AddWithValue("@Loai", loaiAlert);
                    cmd.Parameters.AddWithValue("@HetHan", ngayHetHan);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["SuccessMessage"] = "Đã phát sóng thông báo lên toàn bộ màn hình người dùng!";
            return RedirectToAction("Broadcast");
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopBroadcast(int id)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE ThongBaoHeThong SET TrangThai = 0 WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            TempData["SuccessMessage"] = "Đã gỡ sóng thông báo khẩn cấp!";
            return RedirectToAction("Broadcast");
        }

        public async Task<IActionResult> AuditLog()
        {
            List<NhatKyHeThong> danhSach = new List<NhatKyHeThong>();
            

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string sql = "SELECT * FROM NhatKyHeThong ORDER BY ThoiGianTao DESC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            danhSach.Add(new NhatKyHeThong
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                TaiKhoan = reader["TaiKhoan"].ToString(),
                                HanhDong = reader["HanhDong"].ToString(),
                                ChiTiet = reader["ChiTiet"].ToString(),
                                ThoiGianTao = Convert.ToDateTime(reader["ThoiGianTao"]),
                                DiaChiIP = reader["DiaChiIP"].ToString()
                            });
                        }
                    }
                }
            }
            return View(danhSach);
        }

    }
}