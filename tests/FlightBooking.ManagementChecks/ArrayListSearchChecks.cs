using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class ArrayListSearchChecks
{
    public static void Run()
    {
        var customers = new List<CustomerDTO>
        {
            new() { CustomerId = 1, FullName = "Lan Nguyen", Phone = "0901" },
            new() { CustomerId = 2, FullName = "An Tran", Phone = "0902" },
            new() { CustomerId = 3, FullName = "Binh Le", Phone = "0903" }
        };

        var containsName = new ArrayList
        {
            new SearchConditionDTO
            {
                Field = "FullName",
                Operator = "CONTAINS",
                Value = "an",
                LogicalOperator = "AND"
            }
        };
        ArrayList nameResults = ArrayListSearch.Filter(customers, containsName, GetCustomerField);
        CheckAssert.True(nameResults.Count == 2, "ArrayList search matches text without case sensitivity");

        var combinedConditions = new ArrayList
        {
            new SearchConditionDTO
            {
                Field = "CustomerId",
                Operator = ">=",
                Value = "2",
                LogicalOperator = "AND"
            },
            new SearchConditionDTO
            {
                Field = "FullName",
                Operator = "CONTAINS",
                Value = "Binh",
                LogicalOperator = "AND"
            }
        };
        ArrayList combinedResults = ArrayListSearch.Filter(customers, combinedConditions, GetCustomerField);
        CheckAssert.True(combinedResults.Count == 1 && ((CustomerDTO)combinedResults[0]!).CustomerId == 3,
            "ArrayList search combines numeric and text conditions");
    }

    private static object? GetCustomerField(CustomerDTO customer, string field) =>
        field.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "CUSTOMERID" => customer.CustomerId,
            "FULLNAME" => customer.FullName,
            "PHONE" => customer.Phone,
            _ => throw new ArgumentException($"Unknown test field '{field}'.", nameof(field))
        };
}
