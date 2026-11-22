using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.BUS;

// BUS kiểm tra luật mã giảm giá trước khi gọi DiscountCodeDAO.
public class DiscountCodeBUS
{
    private readonly DiscountCodeDAO _discountDAO = new();

    public List<DiscountCodeDTO> GetAll() => _discountDAO.GetAll();

    public DiscountCodeDTO? GetById(int discountId) =>
        discountId > 0 ? _discountDAO.GetById(discountId) : null;

    public string? Add(DiscountCodeDTO discount)
    {
        Normalize(discount);
        string? error = Validate(discount, currentUsedCount: 0);
        if (error is not null) return error;
        if (_discountDAO.ExistsByCode(discount.Code)) return "Mã giảm giá đã tồn tại.";

        // Không nhận UsedCount từ form; DAO để database áp dụng mặc định 0.
        return _discountDAO.Insert(discount) ? null : "Không thêm được mã giảm giá.";
    }

    public string? Update(DiscountCodeDTO discount)
    {
        if (discount.DiscountId <= 0) return "Mã giảm giá không hợp lệ.";
        Normalize(discount);

        // Lấy UsedCount hiện tại từ DB để không cho giảm hạn mức thấp hơn số đã dùng.
        DiscountCodeDTO? current = _discountDAO.GetById(discount.DiscountId);
        if (current is null) return "Không tìm thấy mã giảm giá để cập nhật.";

        string? error = Validate(discount, current.UsedCount);
        if (error is not null) return error;
        if (_discountDAO.ExistsByCode(discount.Code, discount.DiscountId)) return "Mã giảm giá đã được mã khác sử dụng.";

        // Dùng số lượt dùng thật từ DB; không lấy số này từ form.
        discount.UsedCount = current.UsedCount;
        return _discountDAO.Update(discount) ? null : "Không cập nhật được mã giảm giá.";
    }

    public string? Delete(int discountId)
    {
        if (discountId <= 0) return "Mã giảm giá không hợp lệ.";
        return _discountDAO.Delete(discountId) ? null : "Không tìm thấy mã giảm giá để xóa.";
    }

    public ArrayList Search(string? keyword)
    {
        string searchText = keyword?.Trim() ?? string.Empty;
        var results = new ArrayList();

        foreach (DiscountCodeDTO discount in _discountDAO.GetAll())
        {
            bool matches = searchText.Length == 0 ||
                Contains(discount.DiscountId.ToString(), searchText) ||
                Contains(discount.Code, searchText) ||
                Contains(discount.Description, searchText) ||
                Contains(discount.DiscountType, searchText) ||
                Contains(discount.DiscountValue.ToString(), searchText) ||
                Contains(discount.Status, searchText) ||
                Contains(discount.StartDate.ToString("yyyy-MM-dd HH:mm:ss"), searchText) ||
                Contains(discount.EndDate.ToString("yyyy-MM-dd HH:mm:ss"), searchText);

            if (matches) results.Add(discount);
        }

        return results;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList SearchAdvanced(ArrayList conditions) =>
        ArrayListSearch.Filter(_discountDAO.GetAll(), conditions, GetFieldValue);

    private static object? GetFieldValue(DiscountCodeDTO discount, string field) =>
        field.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "DISCOUNTID" => discount.DiscountId,
            "CODE" => discount.Code,
            "DESCRIPTION" => discount.Description,
            "DISCOUNTTYPE" => discount.DiscountType,
            "DISCOUNTVALUE" => discount.DiscountValue,
            "MINORDERAMOUNT" => discount.MinOrderAmount,
            "MAXDISCOUNTAMOUNT" => discount.MaxDiscountAmount,
            "STARTDATE" => discount.StartDate,
            "ENDDATE" => discount.EndDate,
            "USAGELIMIT" => discount.UsageLimit,
            "USEDCOUNT" => discount.UsedCount,
            "STATUS" => discount.Status,
            _ => throw new ArgumentException($"Không có trường DISCOUNT_CODE tên '{field}'.", nameof(field))
        };

    private static string? Validate(DiscountCodeDTO discount, int currentUsedCount)
    {
        if (string.IsNullOrWhiteSpace(discount.Code)) return "Mã giảm giá không được để trống.";
        if (discount.Code.Length > 50) return "Mã giảm giá không được dài quá 50 ký tự.";
        if (string.IsNullOrWhiteSpace(discount.Description)) return "Mô tả không được để trống.";
        if (discount.Description.Length > 255) return "Mô tả không được dài quá 255 ký tự.";
        if (discount.DiscountType is not ("PERCENT" or "FIXED")) return "Loại giảm chỉ nhận PERCENT hoặc FIXED.";
        if (discount.DiscountValue <= 0) return "Giá trị giảm phải lớn hơn 0.";
        if (discount.DiscountType == "PERCENT" && discount.DiscountValue > 100) return "Giảm theo phần trăm không được vượt quá 100.";
        if (!FitsDecimal18Scale2(discount.DiscountValue)) return "Giá trị giảm phải có tối đa 16 chữ số nguyên và 2 chữ số thập phân.";
        if (discount.MinOrderAmount < 0 || !FitsDecimal18Scale2(discount.MinOrderAmount)) return "Đơn tối thiểu không hợp lệ.";
        if (discount.MaxDiscountAmount < 0 ||
            (discount.MaxDiscountAmount.HasValue && !FitsDecimal18Scale2(discount.MaxDiscountAmount.Value)))
            return "Mức giảm tối đa không hợp lệ.";
        if (discount.StartDate == default || discount.EndDate <= discount.StartDate) return "Ngày kết thúc phải sau ngày bắt đầu.";
        if (discount.UsageLimit < 0) return "Giới hạn lượt dùng không được âm.";
        if (discount.UsageLimit.HasValue && discount.UsageLimit.Value < currentUsedCount)
            return "Giới hạn lượt dùng không được thấp hơn số lượt đã sử dụng.";
        if (currentUsedCount < 0) return "Số lượt đã dùng không hợp lệ.";
        if (discount.Status is not ("ACTIVE" or "INACTIVE")) return "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.";
        return null;
    }

    private static bool FitsDecimal18Scale2(decimal value) =>
        decimal.Abs(value) <= 9999999999999999.99m && decimal.Round(value, 2) == value;

    private static void Normalize(DiscountCodeDTO discount)
    {
        discount.Code = discount.Code.Trim().ToUpperInvariant();
        discount.Description = discount.Description.Trim();
        discount.DiscountType = discount.DiscountType.Trim().ToUpperInvariant();
        discount.Status = discount.Status.Trim().ToUpperInvariant();
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
