namespace MailboxBackup
{
    public readonly struct ValidationError
    {
        public ValidationError(ValidationErrorType errorType, string value, string key)
        {
            ErrorType = errorType;
            Value = value;
            Key = key;
        }

        public ValidationErrorType ErrorType { get; }
        public string Value { get; }
        public string Key { get; }
    }
}