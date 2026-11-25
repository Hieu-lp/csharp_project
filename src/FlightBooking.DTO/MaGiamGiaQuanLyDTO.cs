using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

// DTO mang dữ liệu của một mã giảm giá.
public sealed record MaGiamGiaQuanLyDTO
{
    // Khóa chính IDENTITY do SQL Server tạo.
    [JsonPropertyName("DiscountId")]
    public int MaGiamGia { get; set; }
    [JsonPropertyName("Code")]
    public string Ma { get; set; } = string.Empty;
    [JsonPropertyName("Description")]
    public string MoTa { get; set; } = string.Empty;
    [JsonPropertyName("DiscountType")]
    public string LoaiGiam { get; set; } = string.Empty;
    [JsonPropertyName("DiscountValue")]
    public decimal GiaTriGiam { get; set; }
    [JsonPropertyName("MinOrderAmount")]
    public decimal DonToiThieu { get; set; }

    // Hai trường này nullable theo schema: không giới hạn thì để null.
    [JsonPropertyName("MaxDiscountAmount")]
    public decimal? GiamToiDa { get; set; }
    [JsonPropertyName("StartDate")]
    public DateTime NgayBatDau { get; set; }
    [JsonPropertyName("EndDate")]
    public DateTime NgayKetThuc { get; set; }
    [JsonPropertyName("UsageLimit")]
    public int? GioiHanLuotDung { get; set; }

    // used_count do nghiệp vụ đặt vé tăng; không nên để form nhập tùy ý.
    [JsonPropertyName("UsedCount")]
    public int SoLuotDaDung { get; set; }
    [JsonPropertyName("Status")]
    public string TrangThai { get; set; } = "ACTIVE";

    // Giữ alias Type/Value của DTO quản trị cũ; DTO báo giá Booking vẫn là MaGiamGiaDTO riêng.
    public string Type
    {
        get => LoaiGiam;
        init => LoaiGiam = value;
    }

    public decimal Value
    {
        get => GiaTriGiam;
        init => GiaTriGiam = value;
    }

    public MaGiamGiaQuanLyDTO() { }

    // Constructor giữ tương thích với code booking và các check hiện có.
    public MaGiamGiaQuanLyDTO(
        int maGiamGia,
        string ma,
        string kieu,
        decimal giaTri,
        decimal donToiThieu,
        decimal? giamToiDa,
        DateTime ngayBatDau,
        DateTime ngayKetThuc,
        int? gioiHanLuotDung,
        int soLuotDaDung,
        string trangThai)
    {
        MaGiamGia = maGiamGia;
        Ma = ma;
        LoaiGiam = kieu;
        GiaTriGiam = giaTri;
        DonToiThieu = donToiThieu;
        GiamToiDa = giamToiDa;
        NgayBatDau = ngayBatDau;
        NgayKetThuc = ngayKetThuc;
        GioiHanLuotDung = gioiHanLuotDung;
        SoLuotDaDung = soLuotDaDung;
        TrangThai = trangThai;
    }
}
