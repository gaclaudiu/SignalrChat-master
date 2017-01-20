using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ChatR.Models;
using ChatR.Utilities;
using Microsoft.AspNet.SignalR.Hubs;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;

namespace ChatR.Hubs
{
    public class ChatHub : Hub
    {
        private InMemoryRepository _repository;
        private string serviceName = "Chat App";

        public ChatHub()
        {
            _repository = InMemoryRepository.GetInstance();
        }

        #region IDisconnect and IConnected event handlers implementation

        /// <summary>
        /// Fired when a client disconnects from the system. The user associated with the client ID gets deleted from the list of currently connected users.
        /// </summary>
        /// <returns></returns>
        
        public override Task OnDisconnected(bool stopCall)
        {
            string userId = _repository.GetUserByConnectionId(Context.ConnectionId);
            if (userId != null)
            {
                ChatUser user = _repository.Users.Where(u => u.Id == userId).FirstOrDefault();
                if (user != null)
                {
                    _repository.Remove(user);
                    return Clients.All.leaves(user.Id, user.Username, DateTime.Now);
                }
            }

            return base.OnDisconnected(stopCall);
        }

        public override Task OnConnected()
        {
            string data = HttpContext.Current.Request.QueryString["browser"];
            EventLog.WriteEntry(serviceName, "CONNECTED : Browser:"+ data + "ConnID: " + Context.ConnectionId, EventLogEntryType.Information);
            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }

        #endregion

        #region Chat event handlers

        /// <summary>
        /// Fired when a client pushes a message to the server.
        /// </summary>
        /// <param name="message"></param>
        public void Send(ChatMessage message)
        {
            if (!string.IsNullOrEmpty(message.Content))
            {
                EventLog.WriteEntry(serviceName, "SEND:" + message.Content + "ConnID: " + Context.ConnectionId, EventLogEntryType.Information);
                // Sanitize input
                message.Content = HttpUtility.HtmlEncode(message.Content);
                // Process URLs: Extract any URL and process rich content (e.g. Youtube links)
                HashSet<string> extractedURLs;
                message.Content = TextParser.TransformAndExtractUrls(message.Content, out extractedURLs);
                message.Timestamp = DateTime.Now;
                Clients.All.onMessageReceived(message);
            }
        }

        /// <summary>
        /// Fired when a client joins the chat. Here round trip state is available and we can register the user in the list
        /// </summary>
        public void Joined()
        {
            ChatUser user = new ChatUser()
            {
                //Id = Context.ConnectionId,                
                Id = Guid.NewGuid().ToString(),
                Username = Clients.Caller.username
            };

            EventLog.WriteEntry(serviceName, "Joinned: " + user.Username + " connId:" + user.Id, EventLogEntryType.Information);
            _repository.Add(user);
            _repository.AddMapping(Context.ConnectionId, user.Id);
            Clients.All.joins(user.Id, Clients.Caller.username, DateTime.Now);
        }

        /// <summary>
        /// Invoked when a client connects. Retrieves the list of all currently connected users
        /// </summary>
        /// <returns></returns>
        public ICollection<ChatUser> GetConnectedUsers()
        {
            return _repository.Users.ToList<ChatUser>();
        }

        #endregion
    }
}