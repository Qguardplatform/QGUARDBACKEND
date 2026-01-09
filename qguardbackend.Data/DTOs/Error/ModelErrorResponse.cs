namespace qguardbackend.Data.DTOs.Error;

public class ModelErrorResponse
{
    public ModelErrorResponse()
    {
        IsValid = true;
        ValidationMessages = new List<string>();
    }

    public bool IsValid { get; set; }
    public List<string> ValidationMessages { get; set; }
}