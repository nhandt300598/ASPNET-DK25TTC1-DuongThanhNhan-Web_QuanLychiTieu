using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuanLyChiTieu.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace QuanLyChiTieu.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=DB_QuanLyChiTieu;Integrated Security=True;Encrypt=False;";
         
        public async Task<IActionResult> Index()
        {
            string tkHienTai = User.Identity.Name;
            List<DanhMuc> ds = new List<DanhMuc>();

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "SELECT Id, TenDanhMuc, LoaiGiaoDich, TaiKhoan FROM DanhMuc WHERE TaiKhoan IS NULL OR TaiKhoan = @TK ORDER BY LoaiGiaoDich DESC, TenDanhMuc ASC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TK", tkHienTai);
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ds.Add(new DanhMuc
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                TenDanhMuc = reader["TenDanhMuc"].ToString(),
                                LoaiGiaoDich = reader["LoaiGiaoDich"].ToString(),
                                TaiKhoan = reader["TaiKhoan"] != DBNull.Value ? reader["TaiKhoan"].ToString() : null
                            });
                        }
                    }
                }
            }
            return View(ds);
        }
         
        [HttpPost]
        public async Task<IActionResult> Create(string tenDanhMuc, string loaiGiaoDich)
        {
            if (!string.IsNullOrEmpty(tenDanhMuc))
            {
                string tkHienTai = User.Identity.Name;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO DanhMuc (TenDanhMuc, LoaiGiaoDich, TaiKhoan) VALUES (@Ten, @Loai, @TK)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Ten", tenDanhMuc.Trim());
                        cmd.Parameters.AddWithValue("@Loai", loaiGiaoDich);
                        cmd.Parameters.AddWithValue("@TK", tkHienTai);  

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }
         
        public async Task<IActionResult> Delete(int id)
        {
            string tkHienTai = User.Identity.Name;
            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "DELETE FROM DanhMuc WHERE Id = @Id AND TaiKhoan = @TK";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TK", tkHienTai);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}