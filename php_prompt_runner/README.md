# PHP Prompt Runner

Ứng dụng PHP đơn giản cho phép chạy tối đa 300 prompt tuần tự, hỗ trợ khai báo nhiều tài khoản Google Flow với khả năng chọn mức song song (1-5 prompt mỗi tài khoản). Bạn có thể chọn độ phân giải đầu ra (720p hoặc 1080p), tải xuống kết quả từng prompt hoặc toàn bộ, và chạy lại một prompt bất kỳ.

## Cách chạy

```bash
php -S localhost:8000 -t php_prompt_runner
```

Sau đó truy cập `http://localhost:8000` để nhập prompt và cấu hình tài khoản. Ứng dụng lưu lịch sử chạy gần nhất trong `php_prompt_runner/data/runs.json` (đã được ignore trong git).

## Mô tả nhanh
- **Nhập prompt**: mỗi dòng tương ứng một prompt; có thể nhập đến 300 dòng.
- **Tài khoản**: thêm nhiều tài khoản, mỗi tài khoản có cookie (tuỳ chọn) và mức song song 1–5.
- **Độ phân giải**: chọn 720p hoặc 1080p.
- **Tải xuống**: video đầu ra dạng `.mp4` cho từng prompt hoặc tải toàn bộ video ở dạng `.zip`.
- **Chạy lại**: bấm "Chạy lại" bên cạnh prompt để xử lý lại với cùng cấu hình.

> Lưu ý: phần xử lý prompt trong `PromptRunner.php` hiện mô phỏng kết quả. Bạn có thể thay thế hàm `executePrompt()` bằng lời gọi thật tới Google Flow hoặc API khác.
