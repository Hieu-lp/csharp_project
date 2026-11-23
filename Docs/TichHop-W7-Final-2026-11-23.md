# Bàn giao tiếp nối BookingFlight — 23/11/2026

**Đợt phản hồi mới về tổ chức code:** xem
[bỏ Require, tập trung exception, tách CRUD và so sánh code trước–sau](DonGianHoa-BUS-DAO-GUI-2026-11-23.md).
Bảng kiểm thử bên dưới ghi đợt trước; số mới sau đợt phản hồi là 74 Management,
82 Booking, 75 GUI, 17 HTTP và hai bộ JavaScript đạt.

Báo cáo cho nhánh `feature/integration-w7-final`, nền Git `be67664`.
Đây là kết quả hoàn thiện phần Hiếu và rà soát phần Thịnh hiện có; chưa phải
nghiệm thu toàn bộ đồ án hoặc tích hợp đầy đủ Hiếu–Phú–Thịnh–Quân.
Các thay đổi của đợt này đang ở working tree, chưa commit/push.

## A. Git và trạng thái tích hợp

Đã kiểm tra status, diff, lịch sử, remote và fetch trước khi sửa. Working tree
ban đầu sạch tại `feature/flight-management`, HEAD `e620fd9`. Không thấy
AGENTS.md áp dụng trong repository hoặc thư mục cha.

| Thành phần | Bằng chứng từ Git đã fetch | Kết luận |
|---|---|---|
| `origin/develop` | `be67664` | Nền làm việc hiện tại. |
| Hiếu, PR #4 | Merge `152aa30`, nhận đến `9b20abc` | Phần quản lý đã vào develop, nhưng chưa có toàn bộ sửa sau đó. |
| Hiếu, commit sau rà soát | `origin/feature/flight-management` = `e620fd9`; không là ancestor của develop | Ba sửa lỗi, kiểm tra bổ sung và tài liệu trong commit này chưa có đầy đủ trên develop. |
| Phú, PR #5 | Merge `ffdbf90` | Đã từng được merge. |
| Phú, PR #6 | Merge `be67664`, gồm revert `dc01a22` | Code CUSTOMER/EMPLOYEE/DISCOUNT_CODE đã bị revert khỏi cây source hiện tại. Commit không ghi lý do. |
| Nhánh Phú | `origin/feature/customer-management` = `2eb8c17` | Có tồn tại và đã push; chỉ rà soát ở ref này. |

Như vậy giả định “Phú chưa từng merge” không đúng với lịch sử. Kết quả hiện tại
vẫn là **develop chưa có các module Phú hoạt động**. Các số PR ở trên được xác
minh từ merge commit và cây source, không phải kết quả kiểm tra review/CI trên
GitHub API. Có thể mở [PR #4](https://github.com/Hieu-lp/csharp_project/pull/4),
[PR #5](https://github.com/Hieu-lp/csharp_project/pull/5) và
[PR #6](https://github.com/Hieu-lp/csharp_project/pull/6) để xem trao đổi.

Theo quyền được nêu trong yêu cầu mới, đã fast-forward develop cục bộ từ
`0a73767` đến `be67664` và tạo `feature/integration-w7-final`. Sau đó áp dụng
patch chọn lọc từ `e620fd9` để bảo toàn sửa lỗi/test/tài liệu Hiếu. Không tạo
merge commit, cherry-pick hoặc sửa nhánh Phú; không commit, push, merge PR.
Không lấy thay đổi chỉ định dạng của BookingBUS từ commit cũ.

Lưu ý khi tích hợp sau: vì commit Phú đã nằm trong lịch sử develop, cả
`git log origin/develop..origin/feature/customer-management` và diff ba chấm
đều rỗng. Đối chiếu **hai cây source** bằng
`git diff origin/develop origin/feature/customer-management` mới thấy 27 file,
2.911 dòng thêm và 13 dòng bỏ tại snapshot này. Merge lại nguyên nhánh cũ có
thể chỉ báo up-to-date và không đưa code đã revert trở lại. Cần một PR/commit
khôi phục được nhóm duyệt; chưa thực hiện cách nào trong đợt này.

## B. Solution và kiến trúc

[Solution](../FlightBookingSystem.slnx) hiện build tám project, gồm bốn lớp ứng
dụng và các project kiểm tra:

| Lớp | Trách nhiệm và đường đi thực tế |
|---|---|
| GUI desktop | [Form1](../src/FlightBooking.WinForms/Form1.cs), [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs), [SelectionDialog](../src/FlightBooking.WinForms/SelectionDialog.cs). Nhận control, tạo DTO, gọi BUS, render ListView, ErrorProvider/thông báo. |
| GUI HTTP | [Razor Pages](../src/FlightBooking.Web/Pages/), session draft, model binding, antiforgery; gọi BookingBUS. |
| BUS | [ManagementBUS và ManagementSearch](../src/FlightBooking.BUS/ManagementBUS.cs), [BookingBUS](../src/FlightBooking.BUS/BookingBUS.cs). Quyết định nghiệp vụ, kiểm tra dữ liệu, lọc ArrayList, phối hợp DAO. |
| DAO | [ManagementDAO/ManagementData](../src/FlightBooking.DAO/ManagementDAO.cs), [BookingDAO/BookingData](../src/FlightBooking.DAO/BookingDAO.cs), [MyConnection](../src/FlightBooking.DAO/MyConnection.cs). Mở kết nối, tham số, reader, transaction, SQL Server. |
| DTO | [BookingModels](../src/FlightBooking.DTO/BookingModels.cs), [ManagementModels](../src/FlightBooking.DTO/ManagementModels.cs). Chỉ dữ liệu/thuộc tính, không chứa SQL hoặc control. |

DAO đọc List<DTO>, BUS trả ArrayList cho màn hình quản lý và thực hiện tìm kiếm
trên ArrayList. `ManagementDAO.Write` giữ một connection/transaction
SERIALIZABLE cho cả kiểm tra và ghi. Callback tại BUS mô tả các bước dùng chung
transaction đó; không được tách từng bước thành kết nối riêng.

## C. Đối chiếu trực tiếp QLSV3Lop

Đã giải nén và đọc source GUI/BUS/DAO/DTO của
[QLSV3Lop.rar](../../QLSV3Lop.rar), đọc [W7](../../Downloads/C-w7-YeuCauDoAn.pdf)
7 trang và [3 lớp](../../c-3-layer-.pdf) 32 trang. Đợt này kiểm tra lại hash tài
liệu không đổi, đọc lại BUS mẫu và nội dung W7. Chi tiết trang/hình cùng hash
đã ghi trong [báo cáo đối chiếu gốc](DoiChieu-W7-QLSV3Lop.md).

| Khía cạnh | Mẫu QLSV3Lop đã đọc | BookingFlight sau chỉnh sửa |
|---|---|---|
| Tổ chức | Một project có thư mục GUI/BUS/DAO/DTO | Nhiều project, thêm GUI Web; giữ hướng phụ thuộc đúng. |
| Thêm/sửa/xóa | SinhVienBUS.Them/Sua/Xoa, DAO tương ứng | SaveAirport/SaveFlight/SaveFare dùng ID=0 để thêm, ID>0 để sửa; Delete riêng. Giữ API đang dùng, các nhánh có thân hàm rõ. |
| Đọc danh sách | DocDSSV: kết nối → command → reader → DTO → ArrayList | Cùng luồng đọc; List<DTO> nội bộ DAO được BUS bọc ArrayList. |
| BUS/ArrayList | MaDaTonTai, TimTheoMa, TimTheoHo dùng foreach | ManagementSearch.Filter dùng foreach, if/else, không còn LINQ kết hợp các điều kiện lọc. |
| Tìm nâng cao | TimKiemNangCao gọi DAO lọc SQL | BookingFlight giữ lọc danh mục trong BUS đúng W7 mục 2/8. |
| ListView | Items/SubItems và Tag DTO, SelectedIndexChanged | Render/Fill/UpdateChildren cùng cơ chế, thêm ListView con theo FlightId. |
| Kết nối | SqlServerConnect.TaoKetNoi, command tham số | MyConnection đúng tên W7; giữ SqlClient, tham số, using và transaction dùng chung. |
| Trùng/hợp lệ | Trim/if ở BUS, MaDaTonTai duyệt danh sách | BUS quyết định; DAO kiểm tra trùng trong transaction có khóa, DB giữ PK/FK/UNIQUE. Không dùng cache để kết luận không trùng. |
| Sự kiện | btnThem_Click/btnSua_Click gọi BUS và đổ ListView | Ui.Button gắn sự kiện; Save/Delete/Run quản lý bận/lỗi, chạy SQL nền. Giữ cơ chế đang đúng để giao diện không treo. |
| DTO | Class có get/set | Giữ record và so sánh giá trị cho snapshot chống sửa đè. Không đổi chỉ để giống cú pháp mẫu. |

Bản archive đang cung cấp đã dùng SQL Server và truy vấn tham số hóa. Ví dụ
MySQL/nối chuỗi trong PDF 3 lớp cũ không được đưa vào dự án. Các phép nối chuỗi
SQL còn trong DAO chỉ chọn mảnh truy vấn cố định như có/không có khóa hoặc
WHERE; giá trị từ GUI vẫn truyền bằng tham số.

## D. Thay đổi đã thực hiện và phần được bảo toàn

| File | Phương thức/thay đổi |
|---|---|
| [ManagementBUS](../src/FlightBooking.BUS/ManagementBUS.cs) | Constructor thường; bỏ generic wrapper Database ở BUS; Airports/Flights/Fares, Save/Delete từng danh mục có thân hàm đầy đủ; DatabaseError ánh xạ lỗi giữ nguyên nội dung; ValidateAirport/Flight/FlightChange/Fare có biến trung gian; SaveFare khóa hai chuyến theo ID tăng dần. |
| [ManagementSearch](../src/FlightBooking.BUS/ManagementBUS.cs) | Value dùng switch thường, Filter dùng foreach/if cho AND/OR/NOT, Match dùng các nhánh kiểu dữ liệu/toán tử rõ ràng. Vẫn kiểm tra mọi điều kiện để không bỏ qua giá trị sai sau điều kiện đã đủ quyết định kết quả. |
| [ManagementDAO](../src/FlightBooking.DAO/ManagementDAO.cs) | Constructor thường; Command/Execute/Scalar, đọc danh sách/chi tiết, kiểm tra trùng/tham chiếu và Save/Delete có thân hàm rõ. FlightRow tách FlightDTO và hai bộ đếm. Giữ generic Read/Write trong DAO để bảo toàn phạm vi transaction. |
| [MyConnection](../src/FlightBooking.DAO/MyConnection.cs) | Constructor thường và trường readonly, giữ nguyên Open, kiểm tra cấu hình và Dispose khi mở lỗi. |
| [ManagementModels](../src/FlightBooking.DTO/ManagementModels.cs) | ManagementException chuyển sang constructor thường. Giữ record DTO, chữ ký và tính bất biến của các snapshot. |
| [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs) | Save tách rõ lấy ngày/giờ từ control, tạo DTO và gọi BUS; Number/SelectedId có thân hàm; UpdateChildren dùng foreach/if. Bảo toàn các sửa từ e620fd9: ShowFares tải mới/chờ lần tải đầu, ErrorProvider đúng ô, khôi phục độ rộng editor theo kích thước tab. Giữ Run/Task.Run để SQL không chặn luồng GUI. |
| [ManagementChecks](../tests/FlightBooking.ManagementChecks/Program.cs) | Khôi phục 53 checks; thêm 4 ca giữ ngữ nghĩa lọc/điều kiện sai và 3 ca BUS báo thiếu kết nối cho Airports/Flights/Fares. Tổng 60. |
| [Solution](../FlightBookingSystem.slnx), [WinFormsChecks](../tests/FlightBooking.WinFormsChecks/) | Khôi phục project và 74 GUI checks đã có, gồm kiểm tra cache, picker, lỗi đầu vào, hủy chỉnh sửa và bố cục. |
| [HTTP chỉ đọc](../tests/FlightBooking.Checks/readonly.http.ps1), [preflight SQL](../tests/FlightBooking.Checks/preflight-readonly.sql) | Khôi phục công cụ kiểm tra an toàn đã có; không thêm hành động book/cancel. |

Ba lỗi ưu tiên được giữ: trạng thái tiếng Việt lọc đúng, số phân nhóm sai như
`1.5` không bị đọc thành `15`, ShowFares lấy dữ liệu mới. Giữ cả guard ghế đã cấp,
snapshot chống sửa đè, thứ tự khóa chuyến, FK, giá lịch sử và transaction.

Đã đối chiếu tự động trước/sau DAO: toàn bộ **74 chuỗi SQL và tên tham số giữ
nguyên thứ tự**. Đây là kiểm tra source, không thay thế nghiệm thu SQL ghi.
BookingBUS, BookingDAO, Booking DTO và toàn bộ Web không có diff so với nền
develop trong đợt này; sửa constructor MyConnection giữ nguyên giao diện dùng
chung cho Booking.

## E. Đối chiếu đầy đủ 16 mục W7

“Có code” nghĩa là tìm được luồng trong source. “Đã thử” ghi rõ mức kiểm tra;
không suy từ test C# sang kết luận SQL CRUD/rollback đạt. Các trách nhiệm dưới
đây dựa trên phân công người dùng, không phải tự phân công lại nhóm.

| W7 / trang | Yêu cầu | Source và hiện trạng | Bằng chứng thực tế / phần còn thiếu | Phụ trách |
|---|---|---|---|---|
| 1 / 1 | 3-Layer ver2, MyConnection | [MyConnection.Open](../src/FlightBooking.DAO/MyConnection.cs), DAO/BUS/GUI tách lớp, DTO truyền dữ liệu | Build và đọc SQL qua GUI/HTTP đạt; SQL ghi chưa thử. | Hiếu, Thịnh; nhóm thống nhất lớp chung |
| 2 / 1 | Tìm trên ArrayList trong BUS | [ManagementSearch.Filter](../src/FlightBooking.BUS/ManagementBUS.cs), BookingBUS.Filter | C# và tìm/lọc GUI chỉ đọc đạt. | Hiếu, Thịnh |
| 3 / 1–2 | Main-sub, hai ListView 1-n | [UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs): FLIGHT → FARE_CLASS | GUI qua 21 chuyến đạt; main-sub hóa đơn riêng chưa có trên cây hiện tại. | Hiếu; Quân với hóa đơn |
| 4 / 3 | Cửa sổ chọn qua static | [SelectionDialog.SelectedValue](../src/FlightBooking.WinForms/SelectionDialog.cs), PickAirport/PickFlight | Chọn/Hủy/X, đường xử lý Esc và xóa lựa chọn cũ đạt GUI. | Hiếu |
| 5 / 4 | Tính duy nhất | [SaveAirport/SaveFare](../src/FlightBooking.BUS/ManagementBUS.cs), PK/UNIQUE trong [schema](../Database/02_CreateTables.sql) | Có code; SELECT xác minh index/FK. Chưa thử ghi trùng/đồng thời; CUSTOMER của Phú chưa tích hợp. | Từng chủ module |
| 6 / 4 | Số lượng đổi theo nghiệp vụ | [ReserveSeats/Cancel](../src/FlightBooking.DAO/BookingDAO.cs), ValidateFlightChange | C# guard ghế đạt, số liệu hiện tại khớp qua SELECT. Đặt/trừ/hoàn ghế SQL và đồng bộ WinForms sau Web chưa thử. | Hiếu, Thịnh; Quân phần vé/thanh toán |
| 7 / 4 | Field không được nhập | [BuildEditor/UpdateButtons](../src/FlightBooking.WinForms/ManagementPage.cs), Checkout | GUI ID/đã bán/còn/doanh thu chỉ đọc đạt; HTTP sửa ExpectedTotal được server tính lại. | Hiếu, Thịnh |
| 8 / 4 | AND/OR/NOT, > >= < <= <>, một/nhiều field | [Filter/Match](../src/FlightBooking.BUS/ManagementBUS.cs) | C# đạt; GUI có 2 điều kiện, AND/OR và NOT phủ định cả nhóm. Không có ngôn ngữ biểu thức lồng tùy ý; W7 không yêu cầu điều đó. | Hiếu; Phú cho module riêng |
| 9 / 4 | Table tham số | Chưa thấy bảng/chức năng tham số trên cây develop hiện tại | Chưa triển khai; appsettings hoặc enum trạng thái không được tính thay bảng tham số. | Nhóm thống nhất người phụ trách |
| 10 / 4–5 | Thu/chi/lợi nhuận theo ngày/tháng/quý/năm; ma trận NV/KH/sản phẩm theo quý | Có bộ đếm total_revenue; chưa có module thống kê W7 đầy đủ | Chưa triển khai/kiểm thử trong cây hiện tại. Doanh số đơn UNPAID không được coi là tiền đã thu. | Quân |
| 11 / 5 | Import/export xls | Chưa thấy chức năng trên cây hiện tại | Chưa triển khai/kiểm thử; không thay bằng CSV rồi báo đáp ứng. | Quân |
| 12 / 5 | Report PDF | Chưa thấy xuất báo cáo PDF trong ứng dụng | Chưa triển khai/kiểm thử; PDF tài liệu gốc không phải chức năng report. | Quân |
| 13 / 5–6 | Desktop, menu trái, form con | Hiếu hiện có Form1 với 3 tab. MainForm có ở snapshot Phú đã bị revert | Chỉ đáp ứng một phần; chưa có menu chung trên nhánh đang chạy. Chưa thử MainForm Phú hoặc DPI của nó. | Phú và nhóm |
| 14 / 6–7 | Báo cáo cuối kỳ | Có tài liệu kỹ thuật trong Docs | Chưa có DOC/PDF cuối kỳ đủ mẫu; checklist cụ thể bên dưới. | Cả nhóm |
| 15 / 7 | Tiếng Việt trong CSDL | NVARCHAR/SqlClient tham số, nhãn Unicode | Dữ liệu/nhãn Việt đọc qua GUI và HTTP được kiểm tra. Chưa thử vòng ghi/đọc Unicode. Hậu tố kết nối MySQL trong W7 không áp dụng cho SQL Server. | Cả nhóm |
| 16 / 7 | Tuần 9–10 kiểm tra giấy 10 phút 1–2 hàm; tuần 11–12 chấm | Quy định tổ chức, không phải feature | Chưa xác nhận lịch lớp; các luồng mục I hỗ trợ ôn, không đánh dấu “test phần mềm PASS”. | Cả nhóm |

Báo cáo cuối kỳ cần trang bìa với tên học phần, đề tài, nhóm/lớp, GVHD Phùng
Thái Thiên Trang, dòng tháng 12/2026; nội dung I giới thiệu, II ảnh mô hình CSDL,
III chức năng/ảnh form/công thức/đầu vào–đầu ra, IV cài đặt/cây GUI–BUS–DAO/kết
nối/tool, V link source và DOC/PDF. Tối đa 20 trang, Times New Roman 13, không
có trang mục lục. Bản bàn giao Markdown này chưa thay thế báo cáo đó.

## F. Build và kiểm thử thực tế

| Mốc kiểm tra trong đợt này | Restore/build | Management | Booking | GUI / HTTP |
|---|---|---|---|---|
| Develop nguyên trạng be67664 | Đạt, 0 lỗi/0 cảnh báo | 37 | 82 | Chưa có project GUI checks trên cây này |
| Sau khôi phục phần Hiếu e620fd9 | Đạt, 0 lỗi/0 cảnh báo | 53 | 82 | Giữ bộ kiểm tra GUI/HTTP để chạy tiếp |
| Nhóm 1: tìm kiếm/constructor BUS | Đạt, 0 lỗi/0 cảnh báo | 57 | 82 | 74 / 17 đạt |
| Nhóm 2: các nghiệp vụ BUS | Đạt, 0 lỗi/0 cảnh báo | 60 | 82 | 74 / 17 đạt |
| Nhóm 3: DAO/MyConnection/exception | Đạt, 0 lỗi/0 cảnh báo | 60 | 82 | 74 / 17 đạt |
| Nhóm 4: tạo DTO và ListView con tại GUI | Đạt, 0 lỗi/0 cảnh báo | 60 | 82 | 74 / 17 đạt |

Hai script JavaScript [search-form.checks.cjs](../tests/FlightBooking.Checks/search-form.checks.cjs)
và [checkout.checks.cjs](../tests/FlightBooking.Checks/checkout.checks.cjs) đã
chạy đạt sau nhóm 1, nhóm DAO và bản GUI cuối. Kiểm tra sự kiện tìm chuyến/đổi số người, đổi
chiều sân bay, trạng thái ngày về, Enter áp mã, chặn gửi nhanh và lỗi tuổi.

74 GUI checks gồm kiểm tra ban đầu 35 ca và các ca tương tác/bố cục bổ sung.
Chạy trên Windows thật, chỉ đọc SQL và thử lỗi đầu vào bằng BUS không cấu hình
kết nối. Có kiểm tra liên kết ListView, ShowFares hết cache, picker/static/Esc,
ErrorProvider, giữ dữ liệu khi lỗi, hủy chỉnh sửa/cảnh báo chưa lưu, trường hệ
thống và nút Lưu/Hủy ngoài vùng cuộn. Không gọi Lưu với DTO hợp lệ vào DB chính.

Màn hình thực tế **1366×768, DPI 96 (100%)**. Các ca cửa sổ trong vùng làm việc
1366×768 đạt. Windows giới hạn yêu cầu 1920×1080 xuống 1386×788 nên các ca đó
đã **SKIP**; không coi đặt Size thành kiểm thử độ phân giải. DPI 125% và 150%
chưa thử. Checklist và cách thử từng tổ hợp ở
[báo cáo đối chiếu](DoiChieu-W7-QLSV3Lop.md).

Đã xem ảnh Form1 chụp từ vòng GUI cuối: tab, danh sách và nút Lưu/Hủy hiện trong
vùng cửa sổ. Chưa xác nhận mọi chuỗi dài đều hiện trọn: tên sân bay dài trong
ComboBox đóng còn bị giới hạn theo chiều rộng ô. Không tính kiểm tra nút vừa
chiều rộng là bằng chứng toàn bộ chữ trên form đều không bị cắt; cần xem thêm
chuỗi dài/picker trong vòng nghiệm thu giao diện.

17 HTTP checks chạy website thật với SQL chỉ đọc: tìm/chọn ưu đãi, hai hành
khách riêng người đặt, server quote dương/tính lại giá bị sửa, giữ dữ liệu khi
lỗi mã, bỏ mã, chặn đổi số người ngoài session, chuyển về tra cứu khi thiếu
quyền, giới hạn số người và antiforgery. Không gửi action book/cancel. Website
do phiên kiểm tra khởi động được tự dừng khi xong.

SQL cục bộ kiểm tra bằng Windows authentication với server `.`, chỉ thấy
FlightBookingDB ONLINE; chưa thấy DB hậu tố `_ManagementTests`/`_BookingTests`.
Preflight chỉ SELECT xác nhận 11 bảng gốc, FK cần thiết enabled/trusted,
unique index ghế ISSUED được bật. Không có BOOKING_WEB_REQUEST. Các truy vấn
kiểm tra lệch số ghế, số detail/quantity và chuyến/vé/booking đều trả 0 lỗi.
Lần SELECT cuối ghi nhận 8 sân bay, 21 chuyến, 41 hạng, 81 Booking và 141 vé,
trùng số dòng đã đọc trước các nhóm sửa; không thực hiện DML/DDL trong đợt này.

**Chưa kiểm thử SQL ghi**: CRUD thành công/xóa thật, từ chối trùng/FK trong hai
giao dịch, đặt/hủy/hoàn ghế, idempotency, rollback, tranh chấp ghế/mã giảm giá,
giá lịch sử và đồng bộ số ghế WinForms sau Web. Có code cho các cơ chế này,
nhưng không đủ điều kiện nghiệm thu. Không chạy schema/seed/script 08 trên DB
chính và không tự tạo DB test.

### Luồng Booking đã rà soát ở source

| Nội dung | Luồng hiện có | Mức xác nhận |
|---|---|---|
| Chọn chuyến/hạng | SelectFlight/MakeQuote đọc Flight/Fares, chặn sai chuyến/số người/hết ghế/ngày bay | C# và GET/quote HTTP đạt |
| Giá server | QuoteJourney và MakeJourneyQuote tính từ DB; Create kiểm tra lại ExpectedTotal | Quote HTTP đạt; nhánh Create SQL chưa thử |
| Đặt/trừ ghế | Create → khóa request/chuyến → ReserveSeats → InsertBooking/Invoice/Passenger/SaveRequest | Có code, chưa SQL integration |
| Hủy/hoàn ghế | CancelJourney/Cancel → CheckCancellation → BookingData.Cancel; chỉ hủy đơn/hóa đơn phù hợp | C# quy tắc đạt, SQL chưa thử |
| Gửi lặp | sp_getapplock/LockRequest, hash khóa và payload, FindRequest trả đơn cũ | Có code; thiếu bảng BOOKING_WEB_REQUEST trên DB hiện tại, chưa thử SQL |
| Rollback/đồng thời | Transaction chung cho hai lượt, khóa chuyến theo ID, UPDLOCK/HOLDLOCK, reserve có điều kiện và unique ghế | Có code; chưa thử rollback hoặc race trên SQL |
| Giá lịch sử | InsertPassenger lưu price/unit_price/subtotal; SaveFare chỉ đổi FARE_CLASS | Đối chiếu source, chưa thử ghi rồi sửa giá |
| Tiền đã thu | InsertInvoice tạo UNPAID; ReserveSeats không cộng total_revenue, SaveFlight không sửa total_revenue khi update | Source đúng hướng; chưa nghiệm thu quy trình thu tiền/thống kê của Quân |
| WinForms thấy Web | ReadAll/Reload đọc lại DB, snapshot từ chối sửa dữ liệu ghế cũ, ShowFares refresh | Đọc/refresh GUI đạt; chưa thực hiện chu trình Web ghi → WinForms |

## G. Rà soát Phú và các điểm cần tích hợp

Đã đọc commit/diff và DTO/BUS/DAO/WinForms tại
[snapshot 2eb8c17](https://github.com/Hieu-lp/csharp_project/tree/2eb8c17),
đối chiếu MainForm, Program, cấu hình kết nối và tests. Không compile/test,
merge hoặc sửa code Phú; không có bằng chứng runtime tích hợp Phú trong đợt này.

1. **Booking hiện không gọi BUS/DAO mới của Phú.** BookingDAO tự dùng các bảng
   CUSTOMER và DISCOUNT_CODE đã có trong schema gốc. Vì thế thiếu module GUI
   Phú không chặn build hoặc quote hiện tại. Lookup/Details/Create/Cancel vẫn
   phụ thuộc BOOKING_WEB_REQUEST; merge Phú không tự cung cấp bảng này.
2. **Cấu hình kết nối khác nhau.** Web và Form1 hiện dùng
   `ConnectionStrings__FlightBookingDB` (Form1 có dự phòng file local). DAO Phú
   gọi MyConnection.CreateConnection đọc `FLIGHTBOOKING_DB_CONNECTION`.
   Khi tích hợp phải thống nhất hoặc đặt cả hai cùng một DB đã xác minh;
   không để ứng dụng nối hai DB mà tưởng đã đồng bộ. MyConnection Phú vẫn có
   constructor/Open, thêm static CreateConnection trả kết nối chưa mở.
3. **DiscountCodeDTO chung.** Phú chuyển DTO từ BookingModels sang file riêng,
   thêm Description/DiscountType/DiscountValue; giữ record, constructor 11
   tham số và alias Type/Value dạng init cho Booking. Chỉ giữ một định nghĩa,
   bảo toàn constructor, alias và `with` khi xử lý conflict.
4. **used_count.** DiscountCodeDAO.Update của Phú không ghi đè used_count;
   BUS đọc số thực và kiểm tra usage_limit. BookingDAO.UseDiscount/Cancel quản
   lý lượt dùng. Đây là thiết kế cần giữ; race đổi limit/đặt/hủy vẫn cần SQL test.
5. **Quy tắc khách hàng khác nhau.** BookingData.InsertCustomer tạo bản ghi
   mới mỗi lần đặt. CustomerBUS Phú kiểm tra duy nhất phone/email/passport.
   Có nguy cơ các đơn lặp tạo dữ liệu mà GUI Phú từ chối cập nhật sau này.
   Cần nhóm quyết định tái sử dụng khách hay thay quy tắc, thử trên DB riêng;
   chưa kết luận lỗi SQL đã tái hiện, chưa sửa hoặc gộp khách hiện có.
6. **MainForm/Program.** Phú đổi entry point sang MainForm/menu trái và nhúng
   ba form của mình; quản lý chuyến Hiếu được mở bằng Form1.ShowDialog, chưa
   nhúng cùng vùng form con. MainForm tối thiểu 1150×760, mặc định 1450×920;
   cần test lại 1366×768 và các DPI trước nghiệm thu W7 mục 13.
7. **Tìm kiếm/GUI.** Form Phú dùng DataGridView, tìm từ khóa. SearchAdvanced
   trong BUS đã có nhưng chưa thấy UI nâng cao tương ứng. NOT trong bộ tìm
   Phú là AND NOT điều kiện tiếp theo theo thứ tự; NOT của Hiếu phủ định cả
   nhóm. Không hợp nhất hai bộ tìm rồi vô tình đổi ngữ nghĩa. BUS Phú trả
   `string?` lỗi/null thành công, GUI MessageBox; chưa thấy ErrorProvider.
8. **Tests và conflict.** Phú bổ sung DTO reference/EditorSession vào project
   ManagementChecks và thêm lời gọi tests trong Program. Sau tích hợp phải
   giữ cả 60 ca Hiếu hiện tại lẫn tests Phú, MyConnection và DiscountCodeDTO
   dùng chung, MainForm/Program. Không chép đè project test theo một phía.

## H. Mở WinForms và website trên Windows

Mở PowerShell tại repository. Máy này có .NET SDK 10 và SQL Server local.
Chạy build trước:

```powershell
Set-Location 'E:\csharp_project'
dotnet restore FlightBookingSystem.slnx
dotnet build FlightBookingSystem.slnx --no-restore
```

Để **xem/tìm/quote chỉ đọc** với DB hiện tại, đặt trong từng cửa sổ PowerShell:

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=.;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Cửa sổ thứ nhất mở quản lý ba tab:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Chọn dòng → xem thông tin/hạng con, tìm nhanh/nâng cao, mở picker, đổi tab.
Để thử ErrorProvider/cảnh báo chưa lưu có thể nhập thiếu dữ liệu rồi Hủy;
không Lưu hợp lệ hoặc xác nhận Xóa khi đang nối DB chính. Nút Tải lại đọc
hiện trạng mới từ SQL, không giữ giả định số ghế giữa các lần thao tác.

Cửa sổ thứ hai mở Web:

```powershell
dotnet run --no-build --project src/FlightBooking.Web --no-launch-profile -- --urls http://127.0.0.1:5087 --environment Development
```

Mở [trang chủ](http://127.0.0.1:5087/), chọn chuyến đang có và số người → nhập
thông tin → xem/tính lại giá. Không dùng các ID/ngày seed cũ làm điều kiện luôn
đúng. [Tra cứu](http://127.0.0.1:5087/Booking/Lookup) hiển thị form, nhưng đọc
đơn thực cần BOOKING_WEB_REQUEST mà DB hiện tại thiếu. Không bấm Đặt hoặc Hủy.
Ctrl+C dừng Web; đóng cửa sổ dừng WinForms.

Các lệnh kiểm tra không ghi SQL:

```powershell
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks
node tests/FlightBooking.Checks/search-form.checks.cjs
node tests/FlightBooking.Checks/checkout.checks.cjs
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout
# Khi website ở cổng 5087 đang chạy:
& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl 'http://127.0.0.1:5087'
```

Không thêm `--sql` hoặc `--demo-rollback` với cấu hình DB chính. Các bộ SQL
integration hiện có tự áp dụng script/schema trên DB test; cần duyệt cả quyền
tạo schema và dữ liệu trước khi chạy, không chỉ kiểm tra hậu tố tên DB.

## I. Tám luồng hàm để giải thích khi bảo vệ

Các tên dưới đây là tên thật trong source. Tên Them/Sua/Xoa/DocDSSV của mẫu
được dùng để liên hệ nghiệp vụ, không thêm API giả vào BookingFlight.

1. **GUI lấy dữ liệu — ManagementPage.Save.** Đầu vào là control đang chỉnh
   sửa và original snapshot. GUI tạo AirportDTO/FlightEdit/FareClassDTO rồi
   gọi Save tương ứng ở BUS bằng Task.Run; Run quản lý bận/lỗi. Thêm dùng
   previousId=0; sửa dùng ID gốc. Sau commit GUI về chế độ xem trước khi tải
   lại, tránh gửi lại insert nếu chỉ lần đọc sau đó bị lỗi. Đầu ra là thông
   báo/kết quả ListView; ErrorProvider đánh dấu ô sai, không có SQL ở GUI.
   Xem [Save/Run/MarkErrors](../src/FlightBooking.WinForms/ManagementPage.cs).
2. **Thêm sân bay — ManagementBUS.SaveAirport → ManagementData.SaveAirport.**
   Input AirportDTO ID=0; ValidateAirport trim/kiểm tra tên/thành phố. Trong
   Write, AirportDuplicate kiểm tra cặp tên/thành phố với tham số và khóa;
   nếu hợp lệ, nhánh INSERT OUTPUT trả ID SQL Server sinh. Lỗi dừng trước
   commit. Liên hệ SinhVienBUS.Them và DAO.Them của mẫu.
   Xem [BUS](../src/FlightBooking.BUS/ManagementBUS.cs), [DAO](../src/FlightBooking.DAO/ManagementDAO.cs).
3. **Sửa chuyến — SaveFlight → ValidateFlightChange → ManagementData.SaveFlight.**
   Input FlightEdit và FlightManagementDTO gốc. Khóa FLIGHT rồi so snapshot;
   kiểm tra tuyến/lịch/trạng thái nếu đã có Booking, bộ đếm và ghế thực tế.
   Ghế còn = tổng mới − số vé hiệu lực; UPDATE không đổi tickets_sold hoặc
   total_revenue. Output số dòng sửa, GUI tiếp tục dùng ID cũ. Liên hệ Sua
   của mẫu, bổ sung guard cho dữ liệu nhiều ứng dụng cùng dùng.
   Xem [BUS](../src/FlightBooking.BUS/ManagementBUS.cs), [DAO](../src/FlightBooking.DAO/ManagementDAO.cs).
4. **Xóa hạng — DeleteFare.** GUI yêu cầu xác nhận; BUS nhận snapshot, khóa
   chuyến rồi đọc hạng, từ chối snapshot cũ hoặc FareUsed. DAO DELETE theo
   @id trong cùng transaction; FK tiếp tục bảo vệ. Output void/thông báo
   thành công; không xóa dây chuyền vé/Booking. Liên hệ Xoa của mẫu.
   Xem [BUS](../src/FlightBooking.BUS/ManagementBUS.cs), [DAO](../src/FlightBooking.DAO/ManagementDAO.cs).
5. **Đọc danh sách — ManagementBUS.Airports.** Không có điều kiện đầu vào.
   DAO.Read → MyConnection.Open → ManagementData.Airports → ExecuteReader →
   while tạo AirportDTO → List; BUS bọc ArrayList. GUI ReadAll/Render tạo
   ListViewItem và gắn DTO vào Tag. Output danh sách hiện tại, tương ứng
   DocDSSV ở mẫu. Xem [BUS](../src/FlightBooking.BUS/ManagementBUS.cs),
   [DAO](../src/FlightBooking.DAO/ManagementDAO.cs), [GUI](../src/FlightBooking.WinForms/ManagementPage.cs).
6. **Tìm nâng cao — ManagementSearch.Filter/Match.** Input ArrayList, tối đa
   hai điều kiện từ GUI, AND/OR và cờ NOT. Filter duyệt foreach; Value lấy
   trường DTO, Match đọc đúng số Việt/ngày/trạng thái rồi so toán tử.
   Mọi điều kiện đều được kiểm tra, sau đó kết hợp và phủ định cả nhóm nếu
   chọn NOT. Output ArrayList mới; không thay source và không gửi SQL cho
   từng điều kiện. Xem [Filter/Match](../src/FlightBooking.BUS/ManagementBUS.cs).
7. **Main-sub/chọn — UpdateChildren và ShowFares.** Input snapshot chuyến
   đang chọn; ListView con chỉ nhận FareClassDTO có FlightId tương ứng.
   Khi mở tab hạng, ShowFares chờ tải đầu rồi ReadAll lại, đặt scope/chuyến
   mặc định. Picker trả SelectedValue static và xóa nó khi Hủy/X/Esc để
   tránh lấy lựa chọn cũ. Output các dòng hạng và control đúng chuyến.
   Xem [GUI](../src/FlightBooking.WinForms/ManagementPage.cs),
   [picker](../src/FlightBooking.WinForms/SelectionDialog.cs).
8. **Booking server — CheckoutModel.OnPost → BookingBUS.Create → BookingDAO.Write.**
   Input request, session draft và access key. Quote tính giá DB; action book
   mới gọi Create, kiểm tra session/ExpectedTotal, khóa request/chuyến, lưu
   customer/booking/invoice/ticket/detail và ghế trong một transaction. Output
   booking ID; cùng payload/khóa có thể trả lại ID cũ. Hủy đi qua CancelJourney
   để hoàn ghế đúng một lần. Luồng SQL này mới xác nhận qua source, chưa chạy
   trên DB test; thiếu BOOKING_WEB_REQUEST hiện ngăn nghiệm thu.
   Xem [Checkout](../src/FlightBooking.Web/Pages/Booking/Checkout.cshtml.cs),
   [BUS](../src/FlightBooking.BUS/BookingBUS.cs), [DAO](../src/FlightBooking.DAO/BookingDAO.cs).

## J. Công việc còn lại và thứ tự tiếp tục

1. Người dùng rà soát diff nhánh hiện tại; chỉ commit/push khi được cho phép.
   Giữ commit e620fd9 làm mốc đối chiếu các sửa lỗi cũ; không reset/clean/stash
   working tree để bắt đầu lại.
2. Chuẩn bị DB test riêng trên server/instance được nhóm cho phép, dữ liệu giả
   hoặc bản sao đã xử lý thông tin nhạy cảm, tài khoản chỉ có quyền trên DB đó.
   Xác minh ServerName/DB_NAME/quyền/schema/FK/index/bảng request và ghi lại
   chuỗi kết nối trước khi cho phép runner ghi. Hậu tố tên phù hợp là điều
   kiện của runner, không tự chứng minh đây là môi trường được phép.
3. Duyệt việc áp dụng script cần thiết **chỉ trên DB test**; không dùng DB chính
   để lấp thiếu BOOKING_WEB_REQUEST. Sau cấu hình/xác minh/cho phép mới chạy
   SQLManagementChecks/SqlChecks, thử CRUD/UI thành công, duplicate/FK, đặt/hủy,
   giá đổi trước xác nhận, gửi lặp, lỗi giữa hai lượt, rollback và tranh chấp
   ghế/discount; kiểm tra bộ đếm và giá lịch sử trước/sau từng ca.
4. Khi Phú có PR khôi phục được nhóm chấp thuận, fetch và so cây source xác
   nhận module thực sự trở lại. Sau khi người dùng duyệt và cho phép, bảo toàn
   thay đổi hiện tại bằng commit rồi mới lập phương án merge. Giải quyết các
   điểm mục G, giữ tests của cả hai, build/test lại; chưa có quyền merge đó
   trong đợt này.
5. Trên DB test, chạy Web đặt/hủy rồi WinForms Tải lại, thử sửa bằng snapshot
   cũ. Thử quy tắc CUSTOMER và DISCOUNT_CODE chung với GUI Phú sau tích hợp.
6. Trên máy có màn hình/DPI phù hợp, thử 1366×768 và 1920×1080 ở 100/125/150%:
   đủ chữ/nút, Lưu/Hủy ngoài vùng cuộn, picker/Esc, dirty prompt và form con.
   Ghi ảnh/kích thước thực tế; chỉ đánh dấu đạt tổ hợp thực sự đã chạy.
7. Nhóm phân công bảng tham số; Quân hoàn thiện/kiểm thử vé, hóa đơn, thống kê
   thu–chi–lợi nhuận/ma trận, xls, PDF. Phú hoàn thiện menu/form con. Không sửa
   thay các module này trong nhánh đang bàn giao.
8. Cả nhóm soạn DOC/PDF cuối kỳ theo W7 mục 14, chụp ảnh sau khi nghiệm thu,
   xác nhận lịch kiểm tra và luyện các luồng mục I bằng tên hàm thực tế.

Các [báo cáo rà soát trước](RaSoat-BookingFlight-2026-11-23.md) và
[đối chiếu tài liệu](DoiChieu-W7-QLSV3Lop.md) được giữ làm lịch sử. Trạng thái
Git, số test và giới hạn nghiệm thu mới nhất dùng bản bàn giao này.
