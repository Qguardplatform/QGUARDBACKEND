using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace qguardbackend.Api.ServiceExtensions;

public static class ModelStateExtensions
{
    private const string MessageRegex = @"^[0-9.(), a-zA-Z]+$";
    private const string PhoneNumber = @"^([\+]?234[-]?|[0])?[1-9][0-9]{9}$";
    private const string HtmlRegex = "<(\"[^\"]*\"|'[^']*'|()[^'\">])*>";
    private const string AlphaNumericRegex = "^[a-zA-Z0-9]*$";

    public static List<string> GetErrorMessages(this ModelStateDictionary dictionary)
    {
        return dictionary.SelectMany(m => m.Value.Errors)
                         .Select(m => m.ErrorMessage)
                         .ToList();
    }

    public static bool IsValidText(string textValue)
        => Regex.Match(textValue, MessageRegex).Success;

    public static bool IsValidNigerianNumber(string numberValue)
        => Regex.Match(numberValue, PhoneNumber).Success;

    public static bool IsValidHTMLMessage(string htmlText)
        => Regex.Match(htmlText, HtmlRegex).Success;

    public static bool IsDigitsOnly(string str)
        => !string.IsNullOrEmpty(str) && str.All(char.IsDigit);

    public static bool ValidateAntiXSS(string textValue)
    {
        if (string.IsNullOrEmpty(textValue))
            return true;

        // Following regex convers all the js events and html tags mentioned in followng links.
        //https://www.owasp.org/index.php/XSS_Filter_Evasion_Cheat_Sheet                 
        //https://msdn.microsoft.com/en-us/library/ff649310.aspx

        var pattren = new StringBuilder();

        //Checks any js events i.e. onKeyUp(), onBlur(), alerts and custom js functions etc.             
        pattren.Append(@"((alert|on\w+|function\s+\w+)\s*\(\s*(['+\d\w](,?\s*['+\d\w]*)*)*\s*\))");

        //Checks any html tags i.e. <script, <embed, <object etc.
        pattren.Append(@"|(<(script|iframe|embed|frame|frameset|object|img|applet|body|html|style|layer|link|ilayer|meta|bgsound))");

        return !Regex.IsMatch(System.Web.HttpUtility.UrlDecode(textValue), pattren.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public static bool NotContainsQuote(string textValue)
    {
        if (string.IsNullOrEmpty(textValue))
            return true;

        bool status = textValue.Contains("\"");

        return !status;
    }

    public static bool IsAlphaNumeric(string textValue)
        => Regex.Match(textValue, AlphaNumericRegex).Success;
}