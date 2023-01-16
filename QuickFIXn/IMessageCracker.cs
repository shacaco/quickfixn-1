namespace QuickFix
{
    public interface IMessageCracker
    {
        void Crack(Message message, SessionID sessionID);
    }
}