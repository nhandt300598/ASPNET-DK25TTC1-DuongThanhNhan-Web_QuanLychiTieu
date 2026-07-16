using Microsoft.AspNetCore.Mvc;
using QuanLyChiTieu.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace QuanLyChiTieu.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // lấy chuỗi kết nối từ appsettings.json.
          private readonly IConfiguration _config;
        private readonly string connectionString;
         
        public HomeController(IConfiguration config)
        {
            _config = config;
            connectionString = _config.GetConnectionString("DefaultConnection");
        }
        // Hàm dùng chung để lấy danh sách từ điển danh mục dưới SQL
        private async Task<List<string>> GetDanhSachDanhMucAsync()
        {
            List<string> danhSach = new List<string>(); 

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT TenDanhMuc FROM DanhMuc ORDER BY LoaiGiaoDich DESC, TenDanhMuc";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            danhSach.Add(reader["TenDanhMuc"].ToString());
                        }
                    }
                }
            }
            return danhSach;
        }

        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string loaiGiaoDich, string tuKhoa, int trang = 1, int pageSize = 10)
        {
            string taiKhoanHienTai = User.Identity.Name;
            List<GiaoDich> danhSachTatCa = new List<GiaoDich>(); 
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                await con.OpenAsync();
                string query = "SELECT Id, NgayGiaoDich, SoTien, LoaiGiaoDich, DanhMuc, GhiChu FROM GiaoDich WHERE TaiKhoan = @TaiKhoan ORDER BY NgayGiaoDich DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoanHienTai);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            danhSachTatCa.Add(new GiaoDich
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NgayGiaoDich = Convert.ToDateTime(reader["NgayGiaoDich"]),
                                SoTien = Convert.ToDecimal(reader["SoTien"]),
                                LoaiGiaoDich = reader["LoaiGiaoDich"].ToString(),
                                DanhMuc = reader["DanhMuc"].ToString(),
                                GhiChu = reader["GhiChu"] != DBNull.Value ? reader["GhiChu"].ToString() : string.Empty
                            });
                        }
                    }
                }

                string queryDM = "SELECT TenDanhMuc FROM DanhMuc ORDER BY TenDanhMuc ASC";
                List<string> dsDm = new List<string>();

                using (SqlCommand cmdDM = new SqlCommand(queryDM, con))
                { 
                    using (SqlDataReader readerDM = await cmdDM.ExecuteReaderAsync())
                    {
                        while (await readerDM.ReadAsync())
                        {
                            dsDm.Add(readerDM["TenDanhMuc"].ToString());
                        }
                    }
                }
                ViewBag.DanhSachDanhMuc = dsDm;
            }
 
            ViewBag.DataGoc = danhSachTatCa;
 
            DateTime now = DateTime.Now;
            DateTime lastMonth = now.AddMonths(-1);

            ViewBag.TongThu = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Thu").Sum(g => g.SoTien);
            ViewBag.TongChi = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Chi").Sum(g => g.SoTien);

            decimal thuThangNay = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Thu" && g.NgayGiaoDich.Month == now.Month && g.NgayGiaoDich.Year == now.Year).Sum(g => g.SoTien);
            decimal chiThangNay = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Chi" && g.NgayGiaoDich.Month == now.Month && g.NgayGiaoDich.Year == now.Year).Sum(g => g.SoTien);
            decimal thuThangTruoc = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Thu" && g.NgayGiaoDich.Month == lastMonth.Month && g.NgayGiaoDich.Year == lastMonth.Year).Sum(g => g.SoTien);
            decimal chiThangTruoc = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Chi" && g.NgayGiaoDich.Month == lastMonth.Month && g.NgayGiaoDich.Year == lastMonth.Year).Sum(g => g.SoTien);

            ViewBag.PhanTramThu = Math.Round(thuThangTruoc == 0 ? (thuThangNay > 0 ? 100 : 0) : (double)((thuThangNay - thuThangTruoc) / thuThangTruoc) * 100, 1);
            ViewBag.PhanTramChi = Math.Round(chiThangTruoc == 0 ? (chiThangNay > 0 ? 100 : 0) : (double)((chiThangNay - chiThangTruoc) / chiThangTruoc) * 100, 1);

            ViewBag.NamHienTai = now.Year;
            ViewBag.ThuNamNay = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Thu" && g.NgayGiaoDich.Year == now.Year).Sum(g => g.SoTien);
            ViewBag.ChiNamNay = danhSachTatCa.Where(g => g.LoaiGiaoDich == "Chi" && g.NgayGiaoDich.Year == now.Year).Sum(g => g.SoTien);

            // 3. XỬ LÝ LỌC
            var queryLoc = danhSachTatCa.AsQueryable();
            if (tuNgay.HasValue) queryLoc = queryLoc.Where(g => g.NgayGiaoDich.Date >= tuNgay.Value.Date);
            if (denNgay.HasValue) queryLoc = queryLoc.Where(g => g.NgayGiaoDich.Date <= denNgay.Value.Date);
            if (!string.IsNullOrEmpty(loaiGiaoDich)) queryLoc = queryLoc.Where(g => g.LoaiGiaoDich == loaiGiaoDich);
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                string kw = tuKhoa.ToLower();
                queryLoc = queryLoc.Where(g => g.DanhMuc.ToLower().Contains(kw) || (g.GhiChu != null && g.GhiChu.ToLower().Contains(kw)));
            }

            int tongSoDong = queryLoc.Count();
            int tongSoTrang = (int)Math.Ceiling((double)tongSoDong / pageSize);
            var danhSachPhanTrang = queryLoc.Skip((trang - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.LoaiGiaoDich = loaiGiaoDich;
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangHienTai = trang;
            ViewBag.TongSoTrang = tongSoTrang;
            ViewBag.PageSize = pageSize;

            // HẠN MỨC
            decimal hanMucThang = 10000000;
            using (SqlConnection conHM = new SqlConnection(connectionString))
            {
                string queryHM = "SELECT HanMuc FROM NguoiDung WHERE TaiKhoan = @TK";
                using (SqlCommand cmdHM = new SqlCommand(queryHM, conHM))
                {
                    cmdHM.Parameters.AddWithValue("@TK", taiKhoanHienTai);
                    conHM.Open();
                    var resultHM = cmdHM.ExecuteScalar();
                    if (resultHM != null && resultHM != DBNull.Value) hanMucThang = Convert.ToDecimal(resultHM);
                }
            }

            ViewBag.HanMucThang = hanMucThang;
            ViewBag.ChiThangNay = chiThangNay;
            ViewBag.PhanTramHanMuc = Math.Round(hanMucThang > 0 ? (double)(chiThangNay / hanMucThang) * 100 : 0, 1);
            ViewBag.ThangHienTai = now.Month;

            return View(danhSachPhanTrang);
        }


      
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            string tkHienTai = User.Identity.Name; 
            List<DanhMuc> dsDanhMuc = new List<DanhMuc>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Kéo đúng danh mục của hệ thống + của riêng ông này đổ vào ViewBag
                string query = "SELECT TenDanhMuc, LoaiGiaoDich FROM DanhMuc WHERE TaiKhoan IS NULL OR TaiKhoan = @TK ORDER BY TenDanhMuc ASC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TK", tkHienTai);
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dsDanhMuc.Add(new DanhMuc
                            {
                                TenDanhMuc = reader["TenDanhMuc"].ToString(),
                                LoaiGiaoDich = reader["LoaiGiaoDich"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.DsDanhMuc = dsDanhMuc; 
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(GiaoDich model)
        {
            string taiKhoanHienTai = User.Identity.Name;

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "INSERT INTO GiaoDich (NgayGiaoDich, SoTien, LoaiGiaoDich, DanhMuc, GhiChu, TaiKhoan) " +
                               "VALUES (@NgayGiaoDich, @SoTien, @LoaiGiaoDich, @DanhMuc, @GhiChu, @TaiKhoan)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                { 
                    cmd.Parameters.AddWithValue("@NgayGiaoDich", model.NgayGiaoDich);
                    cmd.Parameters.AddWithValue("@SoTien", model.SoTien);
                    cmd.Parameters.AddWithValue("@LoaiGiaoDich", model.LoaiGiaoDich);
                    cmd.Parameters.AddWithValue("@DanhMuc", model.DanhMuc);
                     
                    cmd.Parameters.AddWithValue("@GhiChu", model.GhiChu ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoanHienTai);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync(); 
                }
            }
             
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateBulk(List<QuanLyChiTieu.Models.GiaoDich> danhSachGiaoDich)
        {
            string taiKhoanHienTai = User.Identity?.Name;
            if (string.IsNullOrEmpty(taiKhoanHienTai))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn!";
                return RedirectToAction(nameof(Index));
            }

            var danhSachThucTe = danhSachGiaoDich
                .Where(x => x.SoTien > 0)
                .ToList();

            if (danhSachThucTe.Count == 0)
            {
                TempData["ErrorMessage"] = "Bạn chưa nhập số tiền cho bất kỳ khoản chi nào!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var gd in danhSachThucTe)
            {
                gd.TaiKhoan = taiKhoanHienTai;
            }
 
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                await con.OpenAsync();
 
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string queryDanhMuc = @"
                    IF NOT EXISTS (SELECT 1 FROM DanhMuc WHERE TenDanhMuc = @TenDM)
                    BEGIN
                        INSERT INTO DanhMuc (TenDanhMuc, LoaiGiaoDich) VALUES (@TenDM, N'Chi')
                    END";

                        string queryGiaoDich = "INSERT INTO GiaoDich (LoaiGiaoDich, DanhMuc, SoTien, NgayGiaoDich, GhiChu, TaiKhoan) VALUES (@Loai, @DM, @Tien, @Ngay, @GhiChu, @User)";

                        foreach (var gd in danhSachThucTe)
                        {
                            using (SqlCommand cmdDm = new SqlCommand(queryDanhMuc, con, transaction))
                            {
                                cmdDm.Parameters.Add("@TenDM", SqlDbType.NVarChar, 100).Value = gd.DanhMuc;
                                await cmdDm.ExecuteNonQueryAsync();
                            }
                             
                            using (SqlCommand cmdGd = new SqlCommand(queryGiaoDich, con, transaction))
                            {
                                cmdGd.Parameters.Add("@Loai", SqlDbType.NVarChar, 10).Value = gd.LoaiGiaoDich;
                                cmdGd.Parameters.Add("@DM", SqlDbType.NVarChar, 100).Value = gd.DanhMuc;
                                cmdGd.Parameters.Add("@Tien", SqlDbType.Decimal).Value = gd.SoTien;
                                cmdGd.Parameters.Add("@Ngay", SqlDbType.DateTime).Value = gd.NgayGiaoDich;
                                cmdGd.Parameters.Add("@GhiChu", SqlDbType.NVarChar, 250).Value = gd.GhiChu;
                                cmdGd.Parameters.Add("@User", SqlDbType.NVarChar, 100).Value = gd.TaiKhoan;

                                await cmdGd.ExecuteNonQueryAsync();
                            }
                        }
                         
                        transaction.Commit();
                        TempData["SuccessMessage"] = $"Đã ghi nhận thành công gói {danhSachThucTe.Count} khoản chi cố định và đồng bộ danh mục!";
                    }
                    catch (Exception ex)
                    { 
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình lưu dữ liệu: " + ex.Message;
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.DanhSachDanhMuc = await GetDanhSachDanhMucAsync();
            var giaoDich = new GiaoDich();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, NgayGiaoDich, SoTien, LoaiGiaoDich, DanhMuc, GhiChu FROM GiaoDich WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // Nếu tìm thấy dòng dữ liệu có Id tương ứng
                        if (await reader.ReadAsync())
                        {
                            giaoDich.Id = Convert.ToInt32(reader["Id"]);
                            giaoDich.NgayGiaoDich = Convert.ToDateTime(reader["NgayGiaoDich"]);
                            giaoDich.SoTien = Convert.ToDecimal(reader["SoTien"]);
                            giaoDich.LoaiGiaoDich = reader["LoaiGiaoDich"].ToString();
                            giaoDich.DanhMuc = reader["DanhMuc"].ToString();
                            giaoDich.GhiChu = reader["GhiChu"] != DBNull.Value ? reader["GhiChu"].ToString() : string.Empty;
                        }
                        else
                        {
                            return NotFound(); 
                        }
                    }
                }
            }
             
            return View(giaoDich);
        }
         
        [HttpPost]
        public async Task<IActionResult> Edit(GiaoDich model)
        {

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "UPDATE GiaoDich SET NgayGiaoDich = @NgayGiaoDich, SoTien = @SoTien, " +
                               "LoaiGiaoDich = @LoaiGiaoDich, DanhMuc = @DanhMuc, GhiChu = @GhiChu " +
                               "WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", model.Id);  
                    cmd.Parameters.AddWithValue("@NgayGiaoDich", model.NgayGiaoDich);
                    cmd.Parameters.AddWithValue("@SoTien", model.SoTien);
                    cmd.Parameters.AddWithValue("@LoaiGiaoDich", model.LoaiGiaoDich);
                    cmd.Parameters.AddWithValue("@DanhMuc", model.DanhMuc);
                    cmd.Parameters.AddWithValue("@GhiChu", model.GhiChu ?? (object)DBNull.Value);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
             
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int id)
        {

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "DELETE FROM GiaoDich WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();  
                }
            } 
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            string taiKhoanHienTai = User.Identity.Name;
            List<GiaoDich> danhSachGiaoDich = new List<GiaoDich>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, NgayGiaoDich, SoTien, LoaiGiaoDich, DanhMuc, GhiChu FROM GiaoDich WHERE TaiKhoan = @TaiKhoan ORDER BY NgayGiaoDich DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoanHienTai);
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            danhSachGiaoDich.Add(new GiaoDich
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NgayGiaoDich = Convert.ToDateTime(reader["NgayGiaoDich"]),
                                SoTien = Convert.ToDecimal(reader["SoTien"]),
                                LoaiGiaoDich = reader["LoaiGiaoDich"].ToString(),
                                DanhMuc = reader["DanhMuc"].ToString(),
                                GhiChu = reader["GhiChu"] != DBNull.Value ? reader["GhiChu"].ToString() : string.Empty
                            });
                        }
                    }
                }
            }
             
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Lịch Sử Giao Dịch");
                var currentRow = 1;
                 
                worksheet.Cell(currentRow, 1).Value = "Ngày Giao Dịch";
                worksheet.Cell(currentRow, 2).Value = "Loại";
                worksheet.Cell(currentRow, 3).Value = "Danh Mục";
                worksheet.Cell(currentRow, 4).Value = "Số Tiền (VNĐ)";
                worksheet.Cell(currentRow, 5).Value = "Ghi Chú";
                 
                worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
                 
                foreach (var item in danhSachGiaoDich)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.NgayGiaoDich.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 2).Value = item.LoaiGiaoDich;
                    worksheet.Cell(currentRow, 3).Value = item.DanhMuc;
                    worksheet.Cell(currentRow, 4).Value = item.SoTien;
                    worksheet.Cell(currentRow, 5).Value = item.GhiChu;
                }
                 
                worksheet.Columns().AdjustToContents();
                 
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoChiTieu.xlsx");
                }
            }
        }


        [HttpPost]
        public async Task<IActionResult> CapNhatHanMuc(decimal hanMucMoi)
        {
            string taiKhoanHienTai = User.Identity?.Name;
            if (string.IsNullOrEmpty(taiKhoanHienTai))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE NguoiDung SET HanMuc = @HanMuc WHERE TaiKhoan = @TaiKhoan";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@HanMuc", SqlDbType.Decimal).Value = hanMucMoi;
                    cmd.Parameters.Add("@TaiKhoan", SqlDbType.NVarChar, 100).Value = taiKhoanHienTai;

                    await con.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        // DÒNG QUAN TRỌNG NHẤT: Gán thông báo thành công vào TempData
                        TempData["SuccessMessage"] = "Cập nhật hạn mức chi tiêu thành công!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy tài khoản để cập nhật hạn mức!";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [Route("Home/Error404")]
        public IActionResult Error404()
        { 
            Response.StatusCode = 404;
            return View();
        }

    }

}