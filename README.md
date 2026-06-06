# QLVP — Hệ thống quản lý văn phòng cao ốc (OfficePro)

Ứng dụng web ASP.NET Core 8 MVC quản lý văn phòng cho thuê: hợp đồng, hóa đơn, thanh toán, bảo trì và khách thuê.

## Yêu cầu

- .NET 8 SDK
- SQL Server (LocalDB hoặc SQL Server Express/Full)

## Cài đặt & chạy

```powershell
cd OfficeManagement.Web
dotnet ef database update
dotnet run
```

Mở trình duyệt: `http://localhost:5123`

## Cấu hình database

Sửa chuỗi kết nối trong `OfficeManagement.Web/appsettings.json`:

```json
"DefaultConnection": "Server=YOUR_SERVER;Database=OfficeManagement;Trusted_Connection=True;TrustServerCertificate=True;"
```

## Tài khoản demo (mật khẩu: `123456`)

| Tài khoản | Vai trò |
|-----------|---------|
| admin | Quản trị viên |
| huy01 | Quản lý tòa nhà |
| ktoan01 | Kế toán |
| thai01 | Kỹ thuật viên |
| vy01 | Khách thuê |

## Tính năng chính

- Quản lý văn phòng (số phòng, sức chứa, trạng thái)
- Hợp đồng thuê, hóa đơn hàng tháng
- Thanh toán chuyển khoản (QR) và tiền mặt tại lễ tân
- Yêu cầu sửa chữa & phân công kỹ thuật viên
- Dashboard theo vai trò (quản lý, kế toán, khách thuê…)

## Cấu trúc dự án

- `OfficeManagement.Web/` — ứng dụng ASP.NET Core MVC
- `QuanLyVP.sql` — script SQL tham khảo (schema tiếng Anh)

## License

Dự án học tập / nội bộ.
