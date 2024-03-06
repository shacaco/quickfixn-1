namespace QuickFix
{
    public interface IMessageCracker
    {
        void Crack(Message.Message message, SessionID sessionID);
    }
}