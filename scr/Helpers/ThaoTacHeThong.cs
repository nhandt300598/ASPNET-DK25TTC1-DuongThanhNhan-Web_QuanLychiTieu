using Microsoft.Data.SqlClient;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace QuanLyChiTieu.Helpers
{
    public static class ThaoTacHeThong
    { 
         private static string? chuoiKetNoi; 
        public static void KhoiTao(IConfiguration config)
        {
            chuoiKetNoi = config.GetConnectionString("DefaultConnection");
        }

        public static void GhiNhatKy(HttpContext context, string taiKhoan, string hanhDong, string chiTiet)
        {
            try
            { 
                string diaChiIP = "Unknown";

                if (context != null && context.Connection != null && context.Connection.RemoteIpAddress != null)
                {
                    diaChiIP = context.Connection.RemoteIpAddress.ToString();
                    if (diaChiIP == "::1") diaChiIP = "127.0.0.1";
                }
                 
                using (SqlConnection conn = new SqlConnection(chuoiKetNoi))
                { 
                    string sql = @"INSERT INTO NhatKyHeThong (TaiKhoan, HanhDong, ChiTiet, DiaChiIP) 
                           VALUES (@TaiKhoan, @HanhDong, @ChiTiet, @DiaChiIP)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan ?? "Guest");
                        cmd.Parameters.AddWithValue("@HanhDong", hanhDong);
                        cmd.Parameters.AddWithValue("@ChiTiet", chiTiet ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DiaChiIP", diaChiIP);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            { 
                System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + ex.Message);
            }
        }
    }
}
