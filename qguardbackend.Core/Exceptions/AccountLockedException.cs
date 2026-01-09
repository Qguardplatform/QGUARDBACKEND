namespace qguardbackend.Core.Exceptions
{
    public class AccountLockedException : Exception
    {
        public AccountLockedException() : base("This account has been locked, contact the administrator")

        { }
    }
}
