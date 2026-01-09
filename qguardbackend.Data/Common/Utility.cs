using qguardbackend.Data.DTOs.Results;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace qguardbackend.Data.Common
{
    public static class Utility
    {
        public static string CleanupString(string value)
        {
            return string.IsNullOrEmpty(value) ? "n/a" : value.Replace("\"", "").Replace("\\", "").Replace("\n", "").Replace("\\r\\n", "");
        }

        public static DateTime AddTimeSpan(DateTime date)
        {
            var tm = TimeSpan.Parse("23:59:59");
            DateTime endDate = date + tm;
            return endDate;
        }
        public static DateTime EndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1); // gives 23:59:59.9999999
        }

        public static string GetGradeWithRemark(decimal score)
        {
            return score switch
            {
                >= 70 => "A - Excellent",
                >= 60 => "B - Very Good",
                >= 50 => "C - Good",
                >= 45 => "D - Fair",
                >= 40 => "E - Pass",
                _ => "F - Fail"
            };
        }

        public static DateTime? ToUtc(DateTime? input)
        {
            if (!input.HasValue)
                return null;

            var value = input.Value;

            return value.Kind switch
            {
                DateTimeKind.Utc => value, // already UTC
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
                _ => value
            };
        }

        public static DateTime? ToLocal(DateTime? input)
        {
            if (!input.HasValue)
                return null;

            return input.Value.Kind == DateTimeKind.Utc
                ? input.Value.ToLocalTime()
                : input.Value;
        }

        public static int GetDurationInMinutes(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null)
                return 0;

            return (int)(endTime.Value - startTime).TotalMinutes;
        }

        public static string GetFormattedTime(DateTime time)
        {
            return time.ToString("hh:mm tt"); // 12-hour format with AM/PM
        }

        public static PaginatedResult<T> ToPageList<T>(this IEnumerable<T> query, int pageNumber, int pageSize)
        {
            var count = query.Count();
            int offset = (pageNumber - 1) * pageSize;
            var items = query.Skip(offset).Take(pageSize).ToArray();
            return new PaginatedResult<T>(items, count, pageNumber, pageSize);
        }
        public static PaginatedResult<T> NoPaginate<T>(this IEnumerable<T> query, int pageNumber, int pageSize)
        {
            var count = query.Count();
            var items = query;
            return new PaginatedResult<T>(items.ToArray(), count, pageNumber, pageSize);
        }
        public static string GenerateOTP(int length)
        {
            if (length <= 0) throw new ArgumentException("Length must be greater than zero.", nameof(length));

            byte[] buffer = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            StringBuilder result = new StringBuilder(length);
            foreach (byte b in buffer)
            {
                result.Append((b % 10).ToString()); // keep it between 0-9
            }

            return result.ToString();
        }
        public static bool IsValidDate(this object d)
        {
            try
            {
                string s = Convert.ToString((DateTime)d);
                DateTime p = DateTime.MinValue;
                if (DateTime.TryParse(s, out p)) return true;
                return false;
            }
            catch (Exception) { return default; }
        }

        public static string ToStringItems<T>(this IEnumerable<T> items, string separator = ",")
        {
            return items != null ? string.Join(separator, items) : null;
        }

        public static string ToDelimitedString<T>(this IEnumerable<T> items, string separator = ",")
        {
            return items.ToDelimitedString(p => p, separator);
        }

        public static string ToDelimitedString<S, T>(this IEnumerable<S> items, Func<S, T> selector, string separator = ",")
        {
            return string.Join(separator, items.Select(selector));
        }

        public static int ToInt(this object val)
        {
            try
            {
                var b = int.TryParse(val.ToString(), out int x);
                return x;
            }
            catch (Exception) { return default; }
        }

        public static long ToLong(this object val)
        {
            try
            {
                var b = long.TryParse(val.ToString(), out long x);
                return x;
            }
            catch (Exception) { return default; }
        }

        public static decimal ToDecimal(this object val)
        {
            try
            {
                var b = decimal.TryParse(val.ToString(), out decimal x);
                return x;
            }
            catch (Exception) { return default; }
        }

        public static float ToFloat(this object val)
        {
            try
            {
                var b = float.TryParse(val.ToString(), out float x);
                return x;
            }
            catch (Exception) { return default; }
        }

        public static bool ToBool(this object val)
        {
            try
            {
                var b = bool.TryParse(val.ToString(), out bool x);
                return x;
            }
            catch (Exception) { return default; }
        }

        public static DateTime? ToDate(this string d, string dateFormat = null)
        {
            if (string.IsNullOrEmpty(d)) return null;
            try
            {
                DateTime p;
                if (dateFormat == null)
                {
                    var formats = new string[] { "dd-MM-yyyy", "dd/MM/yyyy", "dd-MMM-yyyy", "MM/dd/yyyy", "MM-dd-yyyy", "yyyy-MM-dd", "yyyy-MM-dd", "yyyy-MM-ddT00:00:00.000" };
                    if (DateTime.TryParseExact(d.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out p))
                      return p;
                }
                else
                {
                    if (DateTime.TryParseExact(d.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out p))
                        return p;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }       

        public static DateTime? ToDate(this string d, string dateFormat, CultureInfo culture_info)
        {
            if (string.IsNullOrEmpty(d)) return null;
            try
            {
                if (DateTime.TryParseExact(d.Trim(), dateFormat, culture_info, DateTimeStyles.None, out DateTime p))                              
                    return p;
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string AllowedXters(this string value, int allowed_length)
        {
            string trim_val = String.Empty;

            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length > allowed_length)
                {
                    trim_val = value.Substring(0, allowed_length - 1);
                }
                else
                {
                    trim_val = value;
                }
            }

            return trim_val;
        }

        public static string Timestamp()
        {
            TimeSpan span = (System.DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            return Math.Round(Convert.ToDouble(span.TotalSeconds), 0).ToString();
        }
        public static string ToDashFormat(this DateTime date)
        {
            var newDate = date.ToString("dd-MM-yyyy");

            return newDate;
        }

        public static Guid UniqueId()
        {
            return Guid.NewGuid();
        }

        public static string MapMonthNumberToShortName(int monthId)
        {
            string[] monthlist = { "Jan","Feb", "March", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};

            return monthlist[monthId - 1];
        }

        public static int GetWeekNumber(DateTime value)
        {
            decimal d = value.Day / 7;

            return (int)Math.Floor(d) + 1;
        }

        public static int Quarter(DateTime dateTime)
        {
            return Convert.ToInt16((dateTime.Month - 1) / 3) + 1;
        }

        public static string Encrypt(this string valueToEncrypt)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = new byte[32] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
                aes.IV = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16 };
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Encrypt the string to a byte array
                byte[] encrypted = EncryptStringToBytes(valueToEncrypt, aes.Key, aes.IV);
                return Convert.ToBase64String(encrypted);
            }
        }

        public static string Decrypt(this string valueToDecrypt)
        {
            byte[] buffer = Convert.FromBase64String(valueToDecrypt);
            using (Aes aes = Aes.Create())
            {
                aes.Key = new byte[32] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
                aes.IV = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16 };
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Decrypt the byte array back to a string
                string decrypted = DecryptStringFromBytes(buffer, aes.Key, aes.IV);
                return decrypted;
            }
        }

        private static byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] iv)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var msEncrypt = new MemoryStream())
                {
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
        public static bool IsBase64Encrypted(string encryptedPassword)
        {
            try
            {
                // Attempt to decode the message from Base64
                byte[] bytes = Convert.FromBase64String(encryptedPassword);

                // Check if the decoded bytes have the expected format
                // In this case, assuming the format is salt (16 bytes) + IV (16 bytes) + ciphertext
                // Adjust this logic based on your actual format if needed
                if (bytes.Length < 16)
                    return false; // At least salt and IV should be present

                // Other validation logic can be added if necessary

                // If no exception is thrown and format seems valid, return true
                return true;
            }
            catch (FormatException)
            {
                // If decoding from Base64 fails, return false
                return false;
            }
        }
        public static string CreateRandomPasswordWithRandomLength()
        {
            string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            Random random = new Random();

            int size = random.Next(16, validChars.Length);

            char[] chars = new char[size];
            for (int i = 0; i < size; i++)
            {
                chars[i] = validChars[random.Next(0, validChars.Length)];
            }

            return new string(chars);
        }

        public static string GetFormattedDate(DateTime date)
        {
            int day = date.Day;
            string daySuffix = GetDaySuffix(day);
            string month = date.ToString("MMMM");
            int year = date.Year;

            return $"{day}{daySuffix} of {month}, {year}";
        }

        public static string GetDaySuffix(int day)
        {
            if (day >= 11 && day <= 13)
                return "th";

            return (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        }
    }
}