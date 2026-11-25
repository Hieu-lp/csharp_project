namespace FlightBooking.WinForms;

internal enum CheDoChinhSua { Xem, Them, Sua }

// So với dữ liệu ban đầu để phân biệt chỉnh sửa thật với mở form hoặc gõ rồi xóa.
internal sealed class PhienChinhSua
{
    public CheDoChinhSua CheDo { get; private set; }
    private string duLieuBanDau = "";
    public void BatDau(CheDoChinhSua cheDo, string duLieuChup) { CheDo = cheDo; duLieuBanDau = duLieuChup; }
    public bool CoThayDoi(string duLieuChup) => CheDo != CheDoChinhSua.Xem && duLieuChup != duLieuBanDau;
    public bool CoTheLuu(string duLieuChup) => CheDo == CheDoChinhSua.Them || CheDo == CheDoChinhSua.Sua && CoThayDoi(duLieuChup);
}
