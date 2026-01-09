namespace qguardbackend.Core.Utilities
{
    [Serializable]
    public class ValidationResult
    {
        private List<ValidationFailure> _errors;

        public virtual bool IsValid => Errors.Count == 0;

        public List<ValidationFailure> Errors
        {
            get => _errors;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _errors = value.Where(failure => failure != null).ToList();
            }
        }

        public string[] RuleSetsExecuted { get; set; }

        public ValidationResult()
        {
            _errors = new List<ValidationFailure>();
        }


        public ValidationResult(IEnumerable<ValidationFailure> failures)
        {
            _errors = failures.Where(failure => failure != null).ToList();
        }

        public ValidationResult(IEnumerable<ValidationResult> otherResults)
        {
            _errors = otherResults.SelectMany(x => x.Errors).ToList();
            RuleSetsExecuted = otherResults.Where(x => x.RuleSetsExecuted != null).SelectMany(x => x.RuleSetsExecuted).Distinct().ToArray();
        }

        internal ValidationResult(List<ValidationFailure> errors)
        {
            _errors = errors;
        }

        public override string ToString()
        {
            return ToString(Environment.NewLine);
        }

        public string ToString(string separator)
        {
            return string.Join(separator, _errors.Select(failure => failure.ErrorMessage));
        }

        public IDictionary<string, string[]> ToDictionary()
        {
            return Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );
        }
    }
}
