using Microsoft.Data.SqlClient;
using System;
using Microsoft.AspNetCore.Http;

namespace QuanLyChiTieu.Helpers
{
    public static class ThaoTacHeThong
    {
        // Nhớ cập nhật chuỗi kết nối của bạn ở đây
        private static string chuoiKetNoi = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=DB_QuanLyChiTieu;Integrated Security=True;Encrypt=False;";

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
