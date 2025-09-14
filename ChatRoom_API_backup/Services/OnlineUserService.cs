using System.Collections.Concurrent;
using ChatRoom_API.Interfecae;

namespace ChatRoom_API.Service
{
    public class OnlineUserService : IOnlineUserService
    {
        // username → set of connectionIds
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _connectionToUser = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentBag<string> _typingUsers = new(); // thread-safe typing list

        public void AddUser(string username, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(connectionId))
                return;

            _userConnections.AddOrUpdate(username,
                _ => new HashSet<string> { connectionId },
                (_, existingSet) =>
                {
                    lock (existingSet) existingSet.Add(connectionId);
                    return existingSet;
                });

            _connectionToUser[connectionId] = username;
        }

        public void RemoveUser(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                return;

            if (_connectionToUser.TryRemove(connectionId, out var username))
            {
                if (_userConnections.TryGetValue(username, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(username, out _);
                        }
                    }
                }
            }
        }

        public void RemoveUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;

            if (_userConnections.TryRemove(username, out var connections))
            {
                foreach (var conn in connections)
                {
                    _connectionToUser.TryRemove(conn, out _);
                }
            }
        }

        public string? GetUsernameByConnectionId(string connectionId)
        {
            return _connectionToUser.TryGetValue(connectionId, out var username) ? username : null;
        }

        public List<string> GetOnlineUsers()
        {
            // ✅ Always return distinct usernames
            return _userConnections.Keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public List<string> GetConnectionsByUsername(string username)
        {
            return _userConnections.TryGetValue(username, out var connections)
                ? connections.ToList()
                : new List<string>();
        }

        // ===== Typing indicator =====
        public void AddTypingUser(string username)
        {
            if (!_typingUsers.Contains(username))
            {
                _typingUsers.Add(username);
            }
        }

        public void RemoveTypingUser(string username)
        {
            lock (_typingUsers)
            {
                var updated = _typingUsers.Except(new[] { username }).ToList();
                while (!_typingUsers.IsEmpty)
                    _typingUsers.TryTake(out _);

                foreach (var user in updated)
                    _typingUsers.Add(user);
            }
        }

        public List<string> GetTypingUsers()
        {
            return _typingUsers.ToList();
        }
    }
}
