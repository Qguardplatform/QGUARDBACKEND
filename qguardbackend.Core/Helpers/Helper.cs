using System.Text;
using ClosedXML.Excel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace qguardbackend.Shared.Helpers;

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
    public const int MaxFileSizeInMb = 1;
    private const int BytesPerMb = 1024 * 1024;

    public static bool BeValidBase64(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return false;

        Span<byte> buffer = new Span<byte>(new byte[base64String.Length]);
        return Convert.TryFromBase64String(base64String, buffer, out _);
    }

    public static string GenerateRef(long fromStoreId, long toStoreId, long fromStoreSectionId, long toStoreSectionId, decimal quantity)
    {
        return $"{fromStoreId}{toStoreId}{fromStoreSectionId}{toStoreSectionId}{quantity}{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    //public static bool IsCustomer(string? role)
    //{
    //    return !string.IsNullOrWhiteSpace(role) &&
    //            string.Equals(role, UserRole.Customer.GetEnumText(), StringComparison.OrdinalIgnoreCase);
    //}
    //public static bool IsDriver(string? role)
    //{
    //    return !string.IsNullOrWhiteSpace(role) &&
    //            string.Equals(role, UserRole.Driver.GetEnumText(), StringComparison.OrdinalIgnoreCase);
    //}
    //public static bool IsTransporter(string? role)
    //{
    //    return !string.IsNullOrWhiteSpace(role) &&
    //            string.Equals(role, UserRole.Transporter.GetEnumText(), StringComparison.OrdinalIgnoreCase);
    //}
    //public static bool IsAdmin(string? role)
    //{
    //    return !string.IsNullOrWhiteSpace(role) &&
    //            string.Equals(role, UserRole.Admin.GetEnumText(), StringComparison.OrdinalIgnoreCase);
    //}

    public static bool BeUnderMaxFileSize(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return false;

        try
        {
            int commaIndex = base64String.IndexOf(',');

            if (commaIndex >= 0)
                base64String = base64String.Substring(commaIndex + 1);

            byte[] decodedBytes = Convert.FromBase64String(base64String);
            var fileLength = decodedBytes.Length;
            return fileLength <= MaxFileSizeInMb * BytesPerMb;
        }
        catch
        {
            return false;
        }
    }


    //public static string MapSapOrderStatus(string delvst, string wbstk)
    //{
    //    var pb = delvst?.ToUpperInvariant() ?? "";
    //    var ws = wbstk?.ToUpperInvariant() ?? "";

    //    return (ws, pb) switch
    //    {
    //        ("A", "A") => SapOrderTrackingStatus.Pending.GetEnumText(),
    //        ("C", "A") => SapOrderTrackingStatus.Active.GetEnumText(),
    //        ("C", "C") => SapOrderTrackingStatus.Completed.GetEnumText(),
    //        _ => SapOrderTrackingStatus.Pending.GetEnumText()
    //    };
    //}

    public static string MapSapDeliveryStatus(string delvst, string wbstk)
    {
        var pb = delvst?.ToUpperInvariant() ?? "";
        var ws = wbstk?.ToUpperInvariant() ?? "";

        return (ws, pb) switch
        {
            ("A", "A") => "PENDING",
            ("C", "A") => "INTRANSIT",
            ("C", "C") => "COMPLETED",
            _ => "PENDING"
        };
    }

    //public static string GenerateRandom(int size)
    //{
    //    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    //    StringBuilder result = new(size);

    //    using var rng = RandomNumberGenerator.Create();
    //    byte[] buffer = new byte[sizeof(uint)];

    //    for (int i = 0; i < size; i++)
    //    {
    //        rng.GetBytes(buffer);
    //        uint num = BitConverter.ToUInt32(buffer, 0);
    //        result.Append(chars[(int)(num % chars.Length)]);
    //    }

    //    return result.ToString();
    //}

    //public static MemoryStream ExportToExcel<T>(List<T> data, string worksheetName, string headerText)
    //{
    //    if (data == null || data.Count <= 0)
    //        throw new ArgumentException("The data list is empty or null.");

    //    var workbook = new XLWorkbook();
    //    // Add a worksheet
    //    //var worksheet = workbook.Worksheets.Add(typeof(T).Name + " Data");
    //    var worksheet = workbook.Worksheets.Add(worksheetName + "_Data");

    //    // Get all properties of the object
    //    PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    //    // Create a header with merged cells
    //    int headerRow = 1;
    //    int totalColumns = properties.Length;

    //    // Merge the header row (spanning across the number of properties)
    //    var headerRange = worksheet.Range(headerRow, 1, headerRow, totalColumns);
    //    headerRange.Merge();
    //    headerRange.Value = headerText.ToUpper();
    //    headerRange.Style.Font.Bold = true;
    //    headerRange.Style.Font.FontSize = 16;
    //    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    //    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

    //    // Add column headers for the properties
    //    for (int i = 0; i < properties.Length; i++)
    //    {
    //        var columnHeaderCell = worksheet.Cell(headerRow + 1, i + 1); // Start from row 2 (below merged header)
    //        columnHeaderCell.Value = properties[i].Name;
    //        columnHeaderCell.Style.Font.Bold = true;
    //        columnHeaderCell.Style.Font.FontColor = XLColor.Black;
    //        columnHeaderCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
    //    }

    //    // Insert data into the rows
    //    for (int i = 0; i < data.Count; i++)
    //    {
    //        for (int j = 0; j < properties.Length; j++)
    //        {
    //            var value = properties[j].GetValue(data[i], null);
    //            worksheet.Cell(i + headerRow + 2, j + 1).Value = value?.ToString() ?? ""; // Start from row 3 for data
    //        }
    //    }

    //    // Optionally, auto-adjust column widths
    //    worksheet.Columns().AdjustToContents();

    //    // Save to MemoryStream instead of a file
    //    var memoryStream = new MemoryStream();
    //    workbook.SaveAs(memoryStream);
    //    memoryStream.Position = 0; // Reset the stream's position

    //    return memoryStream;
    //}

    /// <summary>
    /// Extracts the MIME type from a base64 data URI.
    /// Example: "data:image/png;base64,...." -> "image/png"
    /// </summary>
    public static string ExtractMimeType(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            throw new ArgumentException("Base64 string is null or empty.");

        var match = Regex.Match(base64String, @"data:(?<type>.+?);base64,", RegexOptions.IgnoreCase);

        if (!match.Success)
            throw new FormatException("Invalid base64 data URI format.");

        return match.Groups["type"].Value;
    }

    /// <summary>
    /// Maps a MIME type to a file extension.
    /// </summary>
    public static string GetFileExtension(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            _ => ""
        };
    }

    /// <summary>
    /// Extracts the MIME type and maps it to a file extension.
    /// </summary>
    public static (string MimeType, string Extension) ExtractMimeAndExtension(string base64String)
    {
        var mimeType = ExtractMimeType(base64String);
        var extension = GetFileExtension(mimeType);

        if (string.IsNullOrEmpty(extension))
            throw new NotSupportedException($"Unsupported MIME type: {mimeType}");

        return (mimeType, extension);
    }


}