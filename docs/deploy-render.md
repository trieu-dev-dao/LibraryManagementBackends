# Triển khai LibraryManagement trên Render

Backend chạy ASP.NET Core 10 và cần **SQL Server riêng**. Image chỉ chứa ứng dụng. Nó không chứa SQL Server, dữ liệu hay cấu hình trên máy phát triển.

## 1. Chuẩn bị SQL Server

- Tạo trước cơ sở dữ liệu trên SQL Server hoặc Azure SQL mà Render truy cập được qua TCP. Có thể dùng SQL Server Express nếu máy chủ hỗ trợ kết nối TCP từ Render. Render Postgres không dùng được với provider EF Core SQL Server hiện tại.
- Dùng tên DNS và cổng TCP cố định, ví dụ `tcp:sql.example.com,1433`, cùng tài khoản SQL riêng. Chuỗi kết nối Windows đến instance cục bộ và xác thực Windows hiện tại không dùng được từ container Linux trên Render.
- Cho phép kết nối từ Render ở firewall, dùng TLS với chứng chỉ được tin cậy và tránh mở cổng SQL cho toàn Internet. Nếu chạy Docker trên máy phát triển và dùng SQL Server trên máy đó, chọn `host.docker.internal` cùng cổng TCP đã cấu hình. `localhost` trong container chỉ chính container.

## 2. Cấu hình lúc chạy

Đặt các biến sau trong mục **Environment** của Render. Khi thử Docker tại máy, đặt các biến tương ứng trong tệp cục bộ `.env.docker`, mỗi dòng theo dạng `KEY=giá_trị`. Dùng thông tin riêng cho từng môi trường.

| Biến | Giá trị |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Ví dụ `Server=tcp:sql.example.com,1433;Database=LibraryManagement;User ID=library_app;Password=<mật_khẩu_SQL>;Encrypt=True;TrustServerCertificate=False;`. |
| `JwtSettings__SecretKey` | Khóa ngẫu nhiên riêng của môi trường, ít nhất 32 byte UTF-8. Giữ ổn định giữa các lần deploy để token đang còn hạn vẫn hợp lệ. |
| `JwtSettings__Issuer`, `JwtSettings__Audience` | Các định danh riêng cho dịch vụ và đối tượng sử dụng token, ví dụ `library-api` và `library-frontend`. |
| `JwtSettings__ExpirationInMinutes` | Tuỳ chọn. Mặc định `60`. |
| `Authentication__Google__ClientId` | Google OAuth Web Client ID khi bật đăng nhập hoặc đăng ký Google. Dùng cùng Client ID với frontend và đăng ký origin frontend trong Google Cloud. |
| `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … | Mỗi biến chứa một origin frontend đầy đủ, ví dụ `https://library.example.com`. Không thêm đường dẫn hoặc dấu `/` cuối. |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Bắt buộc khi khởi động. Dùng thông tin tài khoản quản trị. |
| `LIBRARIAN_EMAIL`, `LIBRARIAN_PASSWORD` | Bắt buộc khi khởi động. Dùng email khác với tài khoản quản trị. |
| `ADMIN_FULLNAME`, `LIBRARIAN_FULLNAME` | Tuỳ chọn. |
| `PORT` | Render cấp, mặc định `10000`. Image dùng `8080` mặc định khi chạy tại máy. Ứng dụng lắng nghe HTTP trên `0.0.0.0` tại cổng này. |

Mật khẩu tài khoản phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Lưu chuỗi kết nối, khóa JWT và mật khẩu ở cấu hình runtime. Không đưa chúng vào Docker build args hoặc Git. Khi chạy tại máy, dùng giá trị thử riêng và giữ `.env.docker` ngoài Git.

## 3. Build và chạy Docker tại máy

Chạy từ thư mục chứa `LibraryManagement.csproj`.

```powershell
docker build -t library-api:local .
docker run --rm --name library-api --env-file .env.docker -p 127.0.0.1:8080:8080 library-api:local
```

Ở terminal khác, gọi `curl.exe --fail http://localhost:8080/health`. Kết quả mong đợi là HTTP 200 và `{"status":"ok"}`. Nếu đặt `PORT` trong tệp môi trường, sửa cổng bên trong container trong tuỳ chọn `-p`. Dừng bằng `docker stop library-api`.

## 4. Tạo Web Service trên Render

1. Kết nối repo backend và chọn **Web Service → Docker**. Đặt thư mục gốc dịch vụ là thư mục chứa project. Dùng `./Dockerfile`, build context là thư mục đó và để trống Docker Command để dùng entrypoint của image.
2. Thêm các biến ở trên, đặt **Health Check Path** thành `/health`, rồi deploy. Bản production nhận HTTP từ proxy. Render kết thúc TLS và chuyển hướng HTTP công khai sang HTTPS, nên Kestrel không chuyển hướng lần nữa khi chạy với `PORT`.
3. Kiểm tra `https://<tên-dịch-vụ>.onrender.com/health`. Đặt URL API của frontend thành `https://<tên-dịch-vụ>.onrender.com/api` và kiểm tra đăng nhập từ origin đã khai báo.

## 5. Migration, tài khoản ban đầu và health check

Mỗi lần khởi động, backend chạy EF Core `MigrateAsync()` trước, sau đó bảo đảm có vai trò `Admin`, `Librarian`, `User` và hai tài khoản nhân viên. Tài khoản SQL cần quyền đọc, ghi và áp dụng các thay đổi schema còn thiếu. Sao lưu cơ sở dữ liệu trước khi nâng phiên bản có migration và khởi động một instance trước khi mở rộng lần triển khai đầu.

Hai cặp email và mật khẩu vẫn bắt buộc khi khởi động. Nếu email đã tồn tại với đúng vai trò, ứng dụng không đặt lại mật khẩu. Email trùng với tài khoản có vai trò khác, lỗi kết nối hoặc lỗi migration đều có thể chặn startup.

`/health` chỉ trả về 200 sau khi startup hoàn tất. Các lần gọi sau đó không truy vấn SQL Server. Nếu endpoint chưa mở, xem log startup của dịch vụ và kiểm tra kết nối SQL, quyền migration và cấu hình tài khoản ban đầu.

Tài liệu tham khảo: [Render Web Services](https://render.com/docs/web-services), [Render Health Checks](https://render.com/docs/health-checks).
