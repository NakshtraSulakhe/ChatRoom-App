using System.Collections.Concurrent;
<<<<<<< HEAD
using ChatRoom_API.Interfecae;
=======
using ChatRoom_API.Interface;
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)

namespace ChatRoom_API.Service
{
    public class OnlineUserService : IOnlineUserService
    {
        // username → set of connectionIds
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _connectionToUser = new(StringComparer.OrdinalIgnoreCase);

<<<<<<< HEAD
        private readonly ConcurrentBag<string> _typingUsers = new(); // thread-safe typing list
=======
        // Use ConcurrentDictionary for thread-safe typing users management
        private readonly ConcurrentDictionary<string, DateTime> _typingUsers = new(StringComparer.OrdinalIgnoreCase);
        
        private readonly ILogger<OnlineUserService> _logger;

        public OnlineUserService(ILogger<OnlineUserService> logger)
        {
            _logger = logger;
        }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)

        public void AddUser(string username, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(connectionId))
<<<<<<< HEAD
                return;

            _userConnections.AddOrUpdate(username,
                _ => new HashSet<string> { connectionId },
                (_, existingSet) =>
                {
                    lock (existingSet) existingSet.Add(connectionId);
                    return existingSet;
                });

            _connectionToUser[connectionId] = username;
=======
            {
                _logger.LogWarning("Attempted to add user with empty username or connectionId");
                return;
            }

            try
            {
                _userConnections.AddOrUpdate(username,
                    _ => new HashSet<string> { connectionId },
                    (_, existingSet) =>
                    {
                        lock (existingSet) existingSet.Add(connectionId);
                        return existingSet;
                    });

                _connectionToUser[connectionId] = username;
                _logger.LogInformation("User {Username} added with connection {ConnectionId}", username, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username} with connection {ConnectionId}", username, connectionId);
            }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
        }

        public void RemoveUser(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
<<<<<<< HEAD
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
=======
            {
                _logger.LogWarning("Attempted to remove user with empty connectionId");
                return;
            }

            try
            {
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
                                _logger.LogInformation("User {Username} completely removed (no more connections)", username);
                            }
                            else
                            {
                                _logger.LogInformation("Connection {ConnectionId} removed for user {Username}", connectionId, username);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Connection {ConnectionId} not found in connection mapping", connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user with connection {ConnectionId}", connectionId);
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
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
<<<<<<< HEAD
            if (!_typingUsers.Contains(username))
            {
                _typingUsers.Add(username);
=======
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Attempted to add typing user with empty username");
                return;
            }

            try
            {
                _typingUsers[username] = DateTime.UtcNow;
                _logger.LogDebug("User {Username} started typing", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding typing user {Username}", username);
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
            }
        }

        public void RemoveTypingUser(string username)
        {
<<<<<<< HEAD
            lock (_typingUsers)
            {
                var updated = _typingUsers.Except(new[] { username }).ToList();
                while (!_typingUsers.IsEmpty)
                    _typingUsers.TryTake(out _);

                foreach (var user in updated)
                    _typingUsers.Add(user);
=======
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Attempted to remove typing user with empty username");
                return;
            }

            try
            {
                if (_typingUsers.TryRemove(username, out _))
                {
                    _logger.LogDebug("User {Username} stopped typing", username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing typing user {Username}", username);
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
            }
        }

        public List<string> GetTypingUsers()
        {
<<<<<<< HEAD
            return _typingUsers.ToList();
=======
            try
            {
                // Remove users who have been typing for more than 5 seconds (stale entries)
                var cutoffTime = DateTime.UtcNow.AddSeconds(-5);
                var staleUsers = _typingUsers
                    .Where(kvp => kvp.Value < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var staleUser in staleUsers)
                {
                    _typingUsers.TryRemove(staleUser, out _);
                }

                return _typingUsers.Keys.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting typing users");
                return new List<string>();
            }
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
        }
    }
}
