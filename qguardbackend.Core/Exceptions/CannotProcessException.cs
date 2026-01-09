namespace qguardbackend.Core.Exceptions
{
    public class CannotProcessException : Exception
    {
        public CannotProcessException() : base("Cannot process your request, kindly try again")
        { }
    }
}
