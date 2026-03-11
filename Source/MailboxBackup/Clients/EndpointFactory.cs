namespace MailboxBackup.Clients
{
    class EndpointFactory
    {
        public IRemoteEndpoint Create(ArgumentValues argumentValues)
        {
            return ImapEndpoint.Create(argumentValues);
        }
    }
}
