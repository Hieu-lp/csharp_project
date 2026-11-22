using FlightBooking.DTO;
using System.Collections;
using System.Globalization;

namespace FlightBooking.BUS;

// Bộ lọc chung chạy trong BUS; kết quả ArrayList đáp ứng yêu cầu môn học.
// Các điều kiện được nối lần lượt từ trái sang phải; NOT nghĩa là AND NOT điều kiện hiện tại.
public static class ArrayListSearch
{
    public static ArrayList Filter<T>(
        IEnumerable<T> rows,
        ArrayList conditions,
        Func<T, string, object?> getFieldValue)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(conditions);

        var parsedConditions = new List<SearchConditionDTO>();
        foreach (object? item in conditions)
        {
            if (item is not SearchConditionDTO condition)
                throw new ArgumentException("Mỗi phần tử điều kiện phải là SearchConditionDTO.", nameof(conditions));
            ValidateCondition(condition);
            parsedConditions.Add(condition);
        }

        var results = new ArrayList();
        foreach (T row in rows)
        {
            bool matchesRow = true;
            bool isFirstCondition = true;

            foreach (SearchConditionDTO condition in parsedConditions)
            {
                object? actualValue = getFieldValue(row, condition.Field);
                bool matchesCondition = Compare(actualValue, condition.Operator, condition.Value);

                if (isFirstCondition)
                {
                    matchesRow = matchesCondition;
                    isFirstCondition = false;
                }
                else
                {
                    matchesRow = condition.LogicalOperator.ToUpperInvariant() switch
                    {
                        "AND" => matchesRow && matchesCondition,
                        "OR" => matchesRow || matchesCondition,
                        "NOT" => matchesRow && !matchesCondition,
                        _ => throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(conditions))
                    };
                }
            }

            // Không có điều kiện nghĩa là giữ lại tất cả các dòng.
            if (matchesRow) results.Add(row!);
        }

        return results;
    }

    private static void ValidateCondition(SearchConditionDTO condition)
    {
        string operation = condition.Operator.Trim().ToUpperInvariant();
        if (operation is not ("=" or "<>" or ">" or ">=" or "<" or "<=" or "CONTAINS"))
            throw new ArgumentException("Toán tử so sánh không được hỗ trợ.", nameof(condition));

        string logical = condition.LogicalOperator.Trim().ToUpperInvariant();
        if (logical is not ("AND" or "OR" or "NOT"))
            throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(condition));
    }

    private static bool Compare(object? actualValue, string operationText, string expectedText)
    {
        string operation = operationText.Trim().ToUpperInvariant();
        if (operation == "CONTAINS")
            return actualValue is string text && text.Contains(expectedText, StringComparison.OrdinalIgnoreCase);

        if (actualValue is null) return false;

        int comparison;
        switch (actualValue)
        {
            case int number when int.TryParse(expectedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int expectedNumber):
                comparison = number.CompareTo(expectedNumber);
                break;
            case decimal amount when decimal.TryParse(expectedText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal expectedAmount):
                comparison = amount.CompareTo(expectedAmount);
                break;
            case decimal amount when decimal.TryParse(expectedText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal invariantAmount):
                comparison = amount.CompareTo(invariantAmount);
                break;
            case DateTime date when DateTime.TryParse(expectedText, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime expectedDate):
                comparison = date.CompareTo(expectedDate);
                break;
            default:
                comparison = string.Compare(Convert.ToString(actualValue, CultureInfo.CurrentCulture),
                    expectedText, StringComparison.OrdinalIgnoreCase);
                break;
        }

        return operation switch
        {
            "=" => comparison == 0,
            "<>" => comparison != 0,
            ">" => comparison > 0,
            ">=" => comparison >= 0,
            "<" => comparison < 0,
            "<=" => comparison <= 0,
            _ => false
        };
    }
}
