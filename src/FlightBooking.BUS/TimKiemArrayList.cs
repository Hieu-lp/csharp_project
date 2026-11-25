using FlightBooking.DTO;
using System.Collections;
using System.Globalization;

namespace FlightBooking.BUS;

// Bộ lọc chung chạy trong BUS; kết quả ArrayList đáp ứng yêu cầu môn học.
// Các điều kiện được nối lần lượt từ trái sang phải; NOT nghĩa là AND NOT điều kiện hiện tại.
public static class TimKiemArrayList
{
    public static ArrayList LocDanhSach<T>(
        IEnumerable<T> danhSach,
        ArrayList cacDieuKien,
        Func<T, string, object?> layGiaTriTruong)
    {
        ArgumentNullException.ThrowIfNull(danhSach);
        ArgumentNullException.ThrowIfNull(cacDieuKien);

        var dieuKienDaDoc = new List<DieuKienTimKiemDTO>();
        foreach (object? phanTu in cacDieuKien)
        {
            if (phanTu is not DieuKienTimKiemDTO dieuKien)
                throw new ArgumentException("Mỗi phần tử điều kiện phải là DieuKienTimKiemDTO.", nameof(cacDieuKien));
            KiemTraDieuKien(dieuKien);
            dieuKienDaDoc.Add(dieuKien);
        }

        var ketQua = new ArrayList();
        foreach (T dong in danhSach)
        {
            bool khopDong = true;
            bool laDieuKienDau = true;

            foreach (DieuKienTimKiemDTO dieuKien in dieuKienDaDoc)
            {
                object? giaTriThuc = layGiaTriTruong(dong, dieuKien.Truong);
                bool khopDieuKien = SoSanh(giaTriThuc, dieuKien.PhepSoSanh, dieuKien.GiaTri);

                if (laDieuKienDau)
                {
                    khopDong = khopDieuKien;
                    laDieuKienDau = false;
                }
                else
                {
                    khopDong = dieuKien.PhepNoi.ToUpperInvariant() switch
                    {
                        "AND" => khopDong && khopDieuKien,
                        "OR" => khopDong || khopDieuKien,
                        "NOT" => khopDong && !khopDieuKien,
                        _ => throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(cacDieuKien))
                    };
                }
            }

            // Không có điều kiện nghĩa là giữ lại tất cả các dòng.
            if (khopDong) ketQua.Add(dong!);
        }

        return ketQua;
    }

    private static void KiemTraDieuKien(DieuKienTimKiemDTO dieuKien)
    {
        string phepSoSanh = dieuKien.PhepSoSanh.Trim().ToUpperInvariant();
        if (phepSoSanh is not ("=" or "<>" or ">" or ">=" or "<" or "<=" or "CONTAINS"))
            throw new ArgumentException("Toán tử so sánh không được hỗ trợ.", nameof(dieuKien));

        string phepNoi = dieuKien.PhepNoi.Trim().ToUpperInvariant();
        if (phepNoi is not ("AND" or "OR" or "NOT"))
            throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(dieuKien));
    }

    private static bool SoSanh(object? giaTriThuc, string chuoiPhepSoSanh, string chuoiCanSoSanh)
    {
        string phepSoSanh = chuoiPhepSoSanh.Trim().ToUpperInvariant();
        if (phepSoSanh == "CONTAINS")
            return giaTriThuc is string chuoi && chuoi.Contains(chuoiCanSoSanh, StringComparison.OrdinalIgnoreCase);

        if (giaTriThuc is null) return false;

        int ketQuaSoSanh;
        switch (giaTriThuc)
        {
            case int so when int.TryParse(chuoiCanSoSanh, NumberStyles.Integer, CultureInfo.InvariantCulture, out int soCanSoSanh):
                ketQuaSoSanh = so.CompareTo(soCanSoSanh);
                break;
            case decimal soTien when decimal.TryParse(chuoiCanSoSanh, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal tienCanSoSanh):
                ketQuaSoSanh = soTien.CompareTo(tienCanSoSanh);
                break;
            case decimal soTien when decimal.TryParse(chuoiCanSoSanh, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal tienQuocTe):
                ketQuaSoSanh = soTien.CompareTo(tienQuocTe);
                break;
            case DateTime ngay when DateTime.TryParse(chuoiCanSoSanh, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime ngayCanSoSanh):
                ketQuaSoSanh = ngay.CompareTo(ngayCanSoSanh);
                break;
            default:
                ketQuaSoSanh = string.Compare(Convert.ToString(giaTriThuc, CultureInfo.CurrentCulture),
                    chuoiCanSoSanh, StringComparison.OrdinalIgnoreCase);
                break;
        }

        return phepSoSanh switch
        {
            "=" => ketQuaSoSanh == 0,
            "<>" => ketQuaSoSanh != 0,
            ">" => ketQuaSoSanh > 0,
            ">=" => ketQuaSoSanh >= 0,
            "<" => ketQuaSoSanh < 0,
            "<=" => ketQuaSoSanh <= 0,
            _ => false
        };
    }
}
