using FluentValidation;

namespace qguardbackend.Core.Utilities
{
    [Serializable]
    public class ValidationFailure
    {
        public ValidationFailure()
        {

        }
        public ValidationFailure(string propertyName, string errorMessage) : this(propertyName, errorMessage, null)
        {

        }
        public ValidationFailure(string propertyName, string errorMessage, object attemptedValue)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            AttemptedValue = attemptedValue;
        }
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }
        public object AttemptedValue { get; set; }
        public object CustomState { get; set; }
        public Severity Severity { get; set; } = Severity.Error;
        public string ErrorCode { get; set; }
        public Dictionary<string, object> FormattedMessagePlaceholderValues { get; set; }
        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}