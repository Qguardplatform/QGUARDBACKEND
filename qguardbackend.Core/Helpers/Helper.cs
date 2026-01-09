using System.Text;
using ClosedXML.Excel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LS1_Backend.LS1.Shared.Helpers;

public static class Helper
{
    public static DateTime? ForceToUtcKind(DateTime? dateTime)
    {
        return dateTime.HasValue
            ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc)
            : (DateTime?)null;
    }

    public static string GetTodayInDateFormat()
    {
        DateTime today = DateTime.Today; // Gets today at 00:00:00
        DateTimeOffset dateTimeOffset = new DateTimeOffset(today);
        long unixTimeMilliseconds = dateTimeOffset.ToUnixTimeMilliseconds();

        return $"Date({unixTimeMilliseconds})";
    }

    public static decimal ValidateAmount(string amount)
    {
        if (decimal.TryParse(amount, out decimal res))
        {
            return res;
        }
        ;

        return 0;
    }

    public static DateTime ParseSapDate(string sapDate)
    {
        if (string.IsNullOrEmpty(sapDate))
            return DateTime.UtcNow; // or DateTime.MinValue if preferred

        var match = Regex.Match(sapDate ?? "", @"\d+");

        if (match.Success && long.TryParse(match.Value, out long ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }

        return DateTime.UtcNow; // or DateTime.MinValue if preferred
    }

    public static long ToLongOrThrow(string value, string fieldName = "Value")
    {
        if (long.TryParse(value, out long result))
        {
            return result;
        }

        throw new ArgumentException($"{fieldName} must be a valid number.");
    }

    public static string GenerateRandom(int size)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder result = new(size);

        using var rng = RandomNumberGenerator.Create();
        byte[] buffer = new byte[sizeof(uint)];

        for (int i = 0; i < size; i++)
        {
            rng.GetBytes(buffer);
            uint num = BitConverter.ToUInt32(buffer, 0);
            result.Append(chars[(int)(num % chars.Length)]);
        }

        return result.ToString();
    }

    public static MemoryStream ExportToExcel<T>(List<T> data, string worksheetName, string headerText)
    {
        if (data == null || data.Count <= 0)
            throw new ArgumentException("The data list is empty or null.");

        var workbook = new XLWorkbook();
        // Add a worksheet
        //var worksheet = workbook.Worksheets.Add(typeof(T).Name + " Data");
        var worksheet = workbook.Worksheets.Add(worksheetName + "_Data");

        // Get all properties of the object
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create a header with merged cells
        int headerRow = 1;
        int totalColumns = properties.Length;

        // Merge the header row (spanning across the number of properties)
        var headerRange = worksheet.Range(headerRow, 1, headerRow, totalColumns);
        headerRange.Merge();
        headerRange.Value = headerText.ToUpper();
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontSize = 16;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Add column headers for the properties
        for (int i = 0; i < properties.Length; i++)
        {
            var columnHeaderCell = worksheet.Cell(headerRow + 1, i + 1); // Start from row 2 (below merged header)
            columnHeaderCell.Value = properties[i].Name;
            columnHeaderCell.Style.Font.Bold = true;
            columnHeaderCell.Style.Font.FontColor = XLColor.Black;
            columnHeaderCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Insert data into the rows
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = 0; j < properties.Length; j++)
            {
                var value = properties[j].GetValue(data[i], null);
                worksheet.Cell(i + headerRow + 2, j + 1).Value = value?.ToString() ?? ""; // Start from row 3 for data
            }
        }

        // Optionally, auto-adjust column widths
        worksheet.Columns().AdjustToContents();

        // Save to MemoryStream instead of a file
        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0; // Reset the stream's position

        return memoryStream;
    }
    public static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("", "");

        // Normalize whitespace
        var parts = fullName.Trim()
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            // Only one name provided — treat it as first name
            return (parts[0], "");
        }
        else
        {
            // Last word = Last name; others combined = First name
            string lastName = parts[^1]; // last element
            string firstName = string.Join(' ', parts.Take(parts.Length - 1));
            return (firstName, lastName);
        }
    }

    //-------------------password generator


}