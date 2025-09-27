namespace ChatRoom_API.Interface
{
    public interface IOnlineUserService
    {
        void AddUser(string username, string connectionId);
        void RemoveUser(string connectionId);
        void RemoveUserByUsername(string username);
        string? GetUsernameByConnectionId(string connectionId);
        List<string> GetOnlineUsers();
        List<string> GetConnectionsByUsername(string username);
        void AddTypingUser(string username);
        void RemoveTypingUser(string username);
        List<string> GetTypingUsers();
    }
}
