using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using QuanLyChiTieu.Models;
using System.Text.RegularExpressions;
using QuanLyChiTieu.Helpers;
using Microsoft.Extensions.Configuration;

namespace QuanLyChiTieu.Controllers
{
    public class AccountController : Controller
    {        
        private readonly IConfiguration _config;
        private readonly string connectionString;
         
        public AccountController(IConfiguration config)
        {
            _config = config;
            connectionString = _config.GetConnectionString("DefaultConnection");
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            List<TaiKhoanViewModel> danhSach = new List<TaiKhoanViewModel>();
            string connectionString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = @"SELECT 
                            u.TaiKhoan, 
                            u.Email, 
                            u.TrangThai, 
                            u.LanDangNhapCuoi,
                            (SELECT COUNT(1) FROM GiaoDich g WHERE g.TaiKhoan = u.TaiKhoan AND g.NgayGiaoDich >= DATEADD(day, -30, GETDATE())) AS SoGiaoDich
                         FROM NguoiDung u 
                         ORDER BY u.TaiKhoan ASC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    await con.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            danhSach.Add(new TaiKhoanViewModel
                            {
                                TaiKhoan = reader["TaiKhoan"].ToString(),
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "",
                                TrangThai = Convert.ToBoolean(reader["TrangThai"]),

                                // Bổ sung đọc 2 cột mới:
                                LanDangNhapCuoi = reader["LanDangNhapCuoi"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["LanDangNhapCuoi"]) : null,
                                SoGiaoDich30Ngay = Convert.ToInt32(reader["SoGiaoDich"])
                            });
                        }
                    }
                }
            }
            return View(danhSach);
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            //kiểm tra đăng nhập
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            string dbHashedPwd = string.Empty;
            bool isActivated = false;
            int soLanSai = 0;
            DateTime? thoiGianKhoa = null;
            bool userExists = false;

            string connectionString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string querySelect = "SELECT MatKhau, TrangThai, SoLanDangNhapSai, ThoiGianKhoa FROM NguoiDung WHERE TaiKhoan = @TaiKhoan";
                using (SqlCommand cmd = new SqlCommand(querySelect, con))
                {
                    cmd.Parameters.AddWithValue("@TaiKhoan", username.Trim());
                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            userExists = true;
                            dbHashedPwd = reader["MatKhau"].ToString();
                            isActivated = Convert.ToBoolean(reader["TrangThai"]);
                            soLanSai = Convert.ToInt32(reader["SoLanDangNhapSai"]);
                            thoiGianKhoa = reader["ThoiGianKhoa"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ThoiGianKhoa"]) : null;
                        }
                    }
                }
                 
                if (!userExists)
                {
                    ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                    return View();
                }
                 //nhập sai mật khẩu nhiều quá lock tài khoản
                if (thoiGianKhoa.HasValue && thoiGianKhoa.Value > DateTime.Now)
                { 
                    var minutesLeft = Math.Ceiling((thoiGianKhoa.Value - DateTime.Now).TotalMinutes);
                    ViewBag.Error = $"Tài khoản của bạn tạm thời bị khóa do nhập sai quá 5 lần! Vui lòng thử lại sau {minutesLeft} phút.";
                    return View();
                }
                 
                if (!isActivated)
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị Quản trị viên tạm ngưng!";
                    return View();
                }
                 
                string inputHashedPwd = HashPassword(password);

                if (inputHashedPwd == dbHashedPwd)
                { 
                    if (soLanSai > 0 || thoiGianKhoa != null)
                    {
                        string queryReset = "UPDATE NguoiDung SET SoLanDangNhapSai = 0, ThoiGianKhoa = NULL , LanDangNhapCuoi = GETDATE() WHERE TaiKhoan = @TaiKhoan";
                        using (SqlCommand cmdReset = new SqlCommand(queryReset, con))
                        {
                            cmdReset.Parameters.AddWithValue("@TaiKhoan", username.Trim());
                            await cmdReset.ExecuteNonQueryAsync();
                        }
                    }
                     
                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    if (string.Equals(username.Trim(), "admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Report", "Admin"); 
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                { 
                    soLanSai += 1;
                    string queryUpdateError = "";

                    if (soLanSai >= 5)
                    { 
                        queryUpdateError = "UPDATE NguoiDung SET SoLanDangNhapSai = @SoLan, ThoiGianKhoa = @ThoiGian WHERE TaiKhoan = @TaiKhoan";
                    }
                    else
                    { 
                        queryUpdateError = "UPDATE NguoiDung SET SoLanDangNhapSai = @SoLan WHERE TaiKhoan = @TaiKhoan";
                    }

                    using (SqlCommand cmdUpdate = new SqlCommand(queryUpdateError, con))
                    {
                        cmdUpdate.Parameters.AddWithValue("@SoLan", soLanSai);
                        if (soLanSai >= 5)
                        {
                            cmdUpdate.Parameters.AddWithValue("@ThoiGian", DateTime.Now.AddMinutes(15));
                        }
                        cmdUpdate.Parameters.AddWithValue("@TaiKhoan", username.Trim());
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                     
                    if (soLanSai >= 5)
                    {
                        ViewBag.Error = "Bạn đã nhập sai mật khẩu quá 5 lần. Tài khoản tạm thời bị khóa trong 15 phút!";
                    }
                    else
                    {
                        ViewBag.Error = $"Tài khoản hoặc mật khẩu không chính xác! (Bạn còn {5 - soLanSai} lần thử)";
                    }

                    return View();
                }
            }
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email, string hoTen, DateTime ngaySinh, string soDienThoai)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản, mật khẩu và email!";
                return View();
            }

            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email.Trim(), emailPattern))
            {
                ViewBag.Error = "Địa chỉ Email không đúng cú pháp (Ví dụ hợp lệ: ten_cua_ban@gmail.com)!";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            string connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string checkQuery = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TaiKhoan";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@TaiKhoan", username);
                    await con.OpenAsync();
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        ViewBag.Error = "Tài khoản này đã tồn tại. Vui lòng chọn tên khác!";
                        return View();
                    }
                }

                string hashedPwd = HashPassword(password);
                string insertQuery = "INSERT INTO NguoiDung (TaiKhoan, MatKhau, Email, HoTen, NgaySinh, SoDienThoai) VALUES (@TaiKhoan, @MatKhau, @Email, @HoTen, @NgaySinh, @SoDienThoai)";
                using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                {
                    insertCmd.Parameters.AddWithValue("@TaiKhoan", username.Trim());
                    insertCmd.Parameters.AddWithValue("@MatKhau", hashedPwd);
                    insertCmd.Parameters.AddWithValue("@Email", email.Trim());
                    insertCmd.Parameters.AddWithValue("@HoTen", hoTen ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@NgaySinh", ngaySinh);
                    insertCmd.Parameters.AddWithValue("@SoDienThoai", soDienThoai ?? (object)DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();

                    string nguoiThucHien = User.Identity.IsAuthenticated ? User.Identity.Name : "Khách/Admin";
                    ThaoTacHeThong.GhiNhatKy(HttpContext, nguoiThucHien, "TAO_TAI_KHOAN", $"Đã tạo tài khoản mới: {username} ({hoTen})");
                }
            }
            ModelState.Clear();
            ViewBag.Success = "Đăng ký tài khoản thành công.";
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string username, string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp nhau!";
                return View();
            }
             
            string hashedOld = HashPassword(oldPassword);
            string hashedNew = HashPassword(newPassword);

            string connectionString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string checkQuery = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TK AND MatKhau = @MKCu";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@TK", username);
                    checkCmd.Parameters.AddWithValue("@MKCu", hashedOld);

                    await con.OpenAsync();
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        ViewBag.Error = "Tài khoản hoặc Mật khẩu hiện tại không chính xác!";
                        return View();
                    }
                }
                 
                string updateQuery = "UPDATE NguoiDung SET MatKhau = @MKMoi WHERE TaiKhoan = @TK";
                using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                {
                    updateCmd.Parameters.AddWithValue("@MKMoi", hashedNew);
                    updateCmd.Parameters.AddWithValue("@TK", username);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            ViewBag.Success = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            return View();
        }


        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string username, string email)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string checkQuery = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TK AND Email = @Email";
                using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                {
                    cmd.Parameters.AddWithValue("@TK", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    await con.OpenAsync();
                    if ((int)await cmd.ExecuteScalarAsync() == 0)
                    {
                        ViewBag.Error = "Thông tin tài khoản hoặc email không chính xác!";
                        return View();
                    }
                }

                string otp = new Random().Next(100000, 999999).ToString();
                DateTime hetHan = DateTime.Now.AddMinutes(5); 

                string insertQuery = "INSERT INTO XacThucOTP (TaiKhoan, MaOTP, HetHan) VALUES (@TK, @OTP, @HetHan)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.AddWithValue("@TK", username);
                    cmd.Parameters.AddWithValue("@OTP", otp);
                    cmd.Parameters.AddWithValue("@HetHan", hetHan);
                    await cmd.ExecuteNonQueryAsync();
                }

                try
                {
                    GuiEmailOTP(email, otp);
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Không thể gửi email lúc này. Lỗi: " + ex.Message;
                    return View();
                }
            }

            return RedirectToAction("VerifyOTP", new { username = username });
        }

        private void GuiEmailOTP(string emailNhan, string maOTP)
        {
            var fromAddress = new MailAddress("nhanduong3511@gmail.com", "Hệ thống Quản lý Tài chính");
            var toAddress = new MailAddress(emailNhan);

            string fromPassword = "xzwelkuigqnpkpot";

            string subject = "[QUAN LY CHI TIEU] - Mã xác thực khôi phục mật khẩu";
            string body = $"Chào bạn,\n\nMã OTP để thiết lập lại mật khẩu của bạn là: {maOTP}\nMã này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai!";


            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
            {
                smtp.Send(message);
            }
        }
       
        [HttpGet]
        public IActionResult VerifyOTP(string username)
        {
            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string username, string otp)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            { 
                string query = "SELECT COUNT(1) FROM XacThucOTP WHERE TaiKhoan = @TK AND MaOTP = @OTP AND DaSuDung = 0 AND HetHan > GETDATE()";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TK", username);
                    cmd.Parameters.AddWithValue("@OTP", otp);
                    await con.OpenAsync();
                    if ((int)await cmd.ExecuteScalarAsync() == 0)
                    {
                        ViewBag.Username = username;
                        ViewBag.Error = "Mã OTP không chính xác hoặc đã hết hạn!";
                        return View();
                    }
                }
            } 
            return RedirectToAction("ResetPassword", new { username = username, otp = otp });
        }
        
        [HttpGet]
        public IActionResult ResetPassword(string username, string otp)
        {
            ViewBag.Username = username;
            ViewBag.OTP = otp;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string username, string otp, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Username = username; ViewBag.OTP = otp;
                ViewBag.Error = "Mật khẩu xác nhận không trùng khớp!";
                return View();
            }

            string hashedNew = HashPassword(newPassword); 

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                await con.OpenAsync();
                string disableOTP = "UPDATE XacThucOTP SET DaSuDung = 1 WHERE TaiKhoan = @TK AND MaOTP = @OTP";
                using (SqlCommand cmd = new SqlCommand(disableOTP, con))
                {
                    cmd.Parameters.AddWithValue("@TK", username);
                    cmd.Parameters.AddWithValue("@OTP", otp);
                    await cmd.ExecuteNonQueryAsync();
                }

                string updatePass = "UPDATE NguoiDung SET MatKhau = @MKMoi WHERE TaiKhoan = @TK";
                using (SqlCommand cmd = new SqlCommand(updatePass, con))
                {
                    cmd.Parameters.AddWithValue("@MKMoi", hashedNew);
                    cmd.Parameters.AddWithValue("@TK", username);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            ViewBag.Success = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            string tkHienTai = User.Identity.Name;
            UserProfileViewModel model = new UserProfileViewModel { TaiKhoan = tkHienTai };

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Email, HoTen, NgaySinh, SoDienThoai FROM NguoiDung WHERE TaiKhoan = @TK";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TK", tkHienTai);
                    await con.OpenAsync();
                     
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) // Nếu tìm thấy người dùng
                        {
                            model.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "";
                            model.HoTen = reader["HoTen"] != DBNull.Value ? reader["HoTen"].ToString() : "";
                            model.SoDienThoai = reader["SoDienThoai"] != DBNull.Value ? reader["SoDienThoai"].ToString() : "";                             
                            if (reader["NgaySinh"] != DBNull.Value)
                            {
                                model.NgaySinh = Convert.ToDateTime(reader["NgaySinh"]);
                            }
                        }
                    }
                }
            }
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            string tkHienTai = User.Identity.Name;
            model.TaiKhoan = tkHienTai;
             
            bool muonDoiPass = !string.IsNullOrEmpty(model.MatKhauMoi) || !string.IsNullOrEmpty(model.XacNhanMatKhauMoi);

            if (muonDoiPass && string.IsNullOrEmpty(model.MatKhauCu))
            {
                ViewBag.Error = "Vui lòng nhập [Mật khẩu hiện tại] để cho phép đổi mật khẩu mới!";
                return View(model);
            }

            if (muonDoiPass && (model.MatKhauMoi != model.XacNhanMatKhauMoi))
            {
                ViewBag.Error = "Mật khẩu mới và ô xác nhận không trùng khớp!";
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(connectionString))  
            {
                await con.OpenAsync();
                               
                if (!string.IsNullOrEmpty(model.MatKhauCu))
                {
                    string hashedOld = HashPassword(model.MatKhauCu);

                    string checkQ = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TK AND MatKhau = @MKCu";
                    using (SqlCommand chkCmd = new SqlCommand(checkQ, con))
                    {
                        chkCmd.Parameters.AddWithValue("@TK", tkHienTai);
                        chkCmd.Parameters.AddWithValue("@MKCu", hashedOld);
                        if ((int)await chkCmd.ExecuteScalarAsync() == 0)
                        {
                            ViewBag.Error = "Mật khẩu hiện tại không chính xác!";
                            return View(model);
                        }
                    }

                    string hashedNew = HashPassword(model.MatKhauMoi);

                    string updateBoth = @"UPDATE NguoiDung 
                                  SET Email = @Email, 
                                      HoTen = @HoTen, 
                                      NgaySinh = @NgaySinh, 
                                      SoDienThoai = @SoDienThoai, 
                                      MatKhau = @MKMoi 
                                  WHERE TaiKhoan = @TK";

                    using (SqlCommand cmd = new SqlCommand(updateBoth, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(model.Email) ? DBNull.Value : model.Email.Trim());
                        cmd.Parameters.AddWithValue("@HoTen", string.IsNullOrEmpty(model.HoTen) ? DBNull.Value : model.HoTen.Trim());
                        cmd.Parameters.AddWithValue("@NgaySinh", model.NgaySinh.HasValue ? (object)model.NgaySinh.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SoDienThoai", string.IsNullOrEmpty(model.SoDienThoai) ? DBNull.Value : model.SoDienThoai.Trim());
                        cmd.Parameters.AddWithValue("@MKMoi", hashedNew);
                        cmd.Parameters.AddWithValue("@TK", tkHienTai);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    ViewBag.Success = "Đã cập nhật thành công hồ sơ cá nhân và Mật khẩu!";
                }

               
                else
                {
                    string updateInfo = @"UPDATE NguoiDung 
                                  SET Email = @Email, 
                                      HoTen = @HoTen, 
                                      NgaySinh = @NgaySinh, 
                                      SoDienThoai = @SoDienThoai 
                                  WHERE TaiKhoan = @TK";

                    using (SqlCommand cmd = new SqlCommand(updateInfo, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(model.Email) ? DBNull.Value : model.Email.Trim());
                        cmd.Parameters.AddWithValue("@HoTen", string.IsNullOrEmpty(model.HoTen) ? DBNull.Value : model.HoTen.Trim());
                        cmd.Parameters.AddWithValue("@NgaySinh", model.NgaySinh.HasValue ? (object)model.NgaySinh.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SoDienThoai", string.IsNullOrEmpty(model.SoDienThoai) ? DBNull.Value : model.SoDienThoai.Trim());
                        cmd.Parameters.AddWithValue("@TK", tkHienTai);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    ViewBag.Success = "Cập nhật thông tin hồ sơ thành công!";
                }
            }

            return View(model);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Profile(UserProfileViewModel model)
        //{
        //    string tkHienTai = User.Identity.Name;
        //    model.TaiKhoan = tkHienTai; 

        //    bool muonDoiPass = !string.IsNullOrEmpty(model.MatKhauMoi) || !string.IsNullOrEmpty(model.XacNhanMatKhauMoi);

        //    if (muonDoiPass && string.IsNullOrEmpty(model.MatKhauCu))
        //    {
        //        ViewBag.Error = "Vui lòng nhập [Mật khẩu hiện tại] để cho phép đổi mật khẩu mới!";
        //        return View(model);
        //    }

        //    if (muonDoiPass && (model.MatKhauMoi != model.XacNhanMatKhauMoi))
        //    {
        //        ViewBag.Error = "Mật khẩu mới và ô xác nhận không trùng khớp!";
        //        return View(model);
        //    }

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        await con.OpenAsync();

        //        if (!string.IsNullOrEmpty(model.MatKhauCu))
        //        {
        //            string hashedOld = HashPassword(model.MatKhauCu);

        //            string checkQ = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TK AND MatKhau = @MKCu";
        //            using (SqlCommand chkCmd = new SqlCommand(checkQ, con))
        //            {
        //                chkCmd.Parameters.AddWithValue("@TK", tkHienTai);
        //                chkCmd.Parameters.AddWithValue("@MKCu", hashedOld);
        //                if ((int)await chkCmd.ExecuteScalarAsync() == 0)
        //                {
        //                    ViewBag.Error = "Mật khẩu hiện tại không chính xác!";
        //                    return View(model);
        //                }
        //            }

        //            string hashedNew = HashPassword(model.MatKhauMoi);
        //            string updateBoth = "UPDATE NguoiDung SET Email = @Email, MatKhau = @MKMoi WHERE TaiKhoan = @TK";
        //            using (SqlCommand cmd = new SqlCommand(updateBoth, con))
        //            {
        //                cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(model.Email) ? DBNull.Value : model.Email.Trim());
        //                cmd.Parameters.AddWithValue("@MKMoi", hashedNew);
        //                cmd.Parameters.AddWithValue("@TK", tkHienTai);
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //            ViewBag.Success = "Đã cập nhật thành công cả Email và Mật khẩu!";
        //        }
        //        else
        //        {
        //            string updateEmail = "UPDATE NguoiDung SET Email = @Email WHERE TaiKhoan = @TK";
        //            using (SqlCommand cmd = new SqlCommand(updateEmail, con))
        //            {
        //                cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(model.Email) ? DBNull.Value : model.Email.Trim());
        //                cmd.Parameters.AddWithValue("@TK", tkHienTai);
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //            ViewBag.Success = "Cập nhật địa chỉ Email thành công!";
        //        }
        //    }

        //    return View(model);
        //}

    }
}