USE [DB_QuanLyChiTieu]
GO
/****** Object:  Table [dbo].[DanhMuc]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DanhMuc](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TenDanhMuc] [nvarchar](100) NOT NULL,
	[LoaiGiaoDich] [nvarchar](50) NOT NULL,
	[TaiKhoan] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GiaoDich]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GiaoDich](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NgayGiaoDich] [datetime] NULL,
	[SoTien] [decimal](18, 0) NOT NULL,
	[LoaiGiaoDich] [nvarchar](50) NOT NULL,
	[DanhMuc] [nvarchar](100) NOT NULL,
	[GhiChu] [nvarchar](255) NULL,
	[TaiKhoan] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[HanMucThang]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HanMucThang](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaiKhoan] [nvarchar](100) NOT NULL,
	[Thang] [int] NOT NULL,
	[Nam] [int] NOT NULL,
	[SoTien] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [UQ_TaiKhoan_Thang_Nam] UNIQUE NONCLUSTERED 
(
	[TaiKhoan] ASC,
	[Thang] ASC,
	[Nam] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NguoiDung]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NguoiDung](
	[TaiKhoan] [varchar](50) NOT NULL,
	[MatKhau] [varchar](255) NOT NULL,
	[TrangThai] [bit] NOT NULL,
	[HanMuc] [decimal](18, 2) NOT NULL,
	[Email] [nvarchar](255) NULL,
	[SoLanDangNhapSai] [int] NOT NULL,
	[ThoiGianKhoa] [datetime] NULL,
	[LanDangNhapCuoi] [datetime] NULL,
	[HoTen] [nvarchar](100) NULL,
	[NgaySinh] [date] NULL,
	[SoDienThoai] [varchar](15) NULL,
PRIMARY KEY CLUSTERED 
(
	[TaiKhoan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NhatKyHeThong]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NhatKyHeThong](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaiKhoan] [nvarchar](50) NULL,
	[HanhDong] [nvarchar](100) NULL,
	[ChiTiet] [nvarchar](max) NULL,
	[DiaChiIP] [varchar](50) NULL,
	[ThoiGianTao] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ThongBaoHeThong]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ThongBaoHeThong](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NoiDung] [nvarchar](500) NOT NULL,
	[LoaiAlert] [varchar](20) NULL,
	[NgayHetHan] [datetime] NOT NULL,
	[TrangThai] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[XacThucOTP]    Script Date: 30/06/2026 11:11:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[XacThucOTP](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaiKhoan] [nvarchar](100) NOT NULL,
	[MaOTP] [varchar](6) NOT NULL,
	[NgayTao] [datetime] NOT NULL,
	[HetHan] [datetime] NOT NULL,
	[DaSuDung] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[GiaoDich] ADD  DEFAULT (getdate()) FOR [NgayGiaoDich]
GO
ALTER TABLE [dbo].[NguoiDung] ADD  DEFAULT ((1)) FOR [TrangThai]
GO
ALTER TABLE [dbo].[NguoiDung] ADD  DEFAULT ((10000000)) FOR [HanMuc]
GO
ALTER TABLE [dbo].[NguoiDung] ADD  DEFAULT ((0)) FOR [SoLanDangNhapSai]
GO
ALTER TABLE [dbo].[NhatKyHeThong] ADD  DEFAULT (getdate()) FOR [ThoiGianTao]
GO
ALTER TABLE [dbo].[ThongBaoHeThong] ADD  DEFAULT ('warning') FOR [LoaiAlert]
GO
ALTER TABLE [dbo].[ThongBaoHeThong] ADD  DEFAULT ((1)) FOR [TrangThai]
GO
ALTER TABLE [dbo].[XacThucOTP] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[XacThucOTP] ADD  DEFAULT ((0)) FOR [DaSuDung]
GO
