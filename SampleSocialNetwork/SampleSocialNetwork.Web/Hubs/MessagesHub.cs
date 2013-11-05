using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using SampleSocialNetwork.Data;
using SampleSocialNetwork.Models;
using SampleSocialNetwork.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Security;

namespace SampleSocialNetwork.Hubs
{
    [Authorize]
    public class MessagesHub : Hub
    {
        private ApplicationDbContext entities = new ApplicationDbContext();

        public string Initialize()
        {
            var user = GetCurrentUser();
            var chatContexts = entities.ChatContexts
                .Include("Initiator")
                .Include("OtherUser")
                .Where(c => c.Initiator.Id == user.Id
                    || c.OtherUser.Id == user.Id)
                .OrderByDescending(c => c.LastInteraction);

            List<object> chatContextViewModels = new List<object>();
            foreach (ChatContext chatContext in chatContexts)
            {
                var otherUser = GetOtherUser(chatContext);
                
                var chatViewModel = new
                {
                    Id = chatContext.Id,
                    OtherUser = new UserProfileBasicViewModel(otherUser)
                    {
                        UserId = otherUser.Id
                    },
                    LastInteraction = chatContext.LastInteraction,
                };
                chatContextViewModels.Add(chatViewModel);
            }

            var viewModel = new
            {
                CurrentUser = new UserProfileBasicViewModel(user)
                {
                    UserId = user.Id
                },
                ChatContexts = chatContextViewModels
            };

            return Serialize(viewModel);
        }

        public object CreateContext(string otherUserName) 
        {
            var otherUser = entities.Users.FirstOrDefault(u => u.UserName == otherUserName);
            if (otherUser.UserName == Context.User.Identity.Name)
            {
                throw new HttpException("Can't create a context with yourself.");
            }

            var chatContext = new ChatContext()
            {
                InitiatorId = Context.User.Identity.GetUserId(),
                OtherUser = otherUser,
                LastInteraction = DateTime.Now
            };

            entities.ChatContexts.Add(chatContext);
            entities.SaveChanges();

            var viewModel = new
            {
                Id = chatContext.Id,
                OtherUser = new UserProfileBasicViewModel(otherUser)
                {
                    UserId = otherUser.Id
                }
            };

            SendNewContextToConnectedClients(chatContext);
            return Serialize(viewModel);
        }

        public string LoadContextHistory(int contextId)
        {
            var context = entities.ChatContexts.FirstOrDefault(c => c.Id == contextId);
            string currentUserId = Context.User.Identity.GetUserId();

            if (context.Initiator.Id != currentUserId && context.OtherUser.Id != currentUserId)
            {
                throw new HttpException(403, "Unauthorized - cannot retrive history");
            }

            var messagesViewModel = context.History.Select(m => new
            {
                Content = m.Content,
                TimeStamp = m.TimeStamp,
                SenderId = m.SenderId
            });

            return Serialize(messagesViewModel);
        }

        public string SendMessage(int chatContextId, string content)
        {
            if (String.IsNullOrEmpty(content))
            {
                throw new HttpException("Message was null or empty!");
            }

            var chatContext = entities.ChatContexts.FirstOrDefault(c => c.Id == chatContextId);
            var userId = Context.User.Identity.GetUserId();
            if (chatContext.Initiator.Id != userId
                && chatContext.OtherUser.Id != userId)
            {
                throw new HttpException(403, "Can't send message to this context.");
            }

            var message = new Message()
            {
                Content = content,
                TimeStamp = DateTime.Now,
                Context = chatContext,
                SenderId = userId,
                IsRead = false
            };

            var messageViewModel = new
            {
                ChatContextId = chatContextId,
                Content = message.Content,
                TimeStamp = message.TimeStamp,
                IsRead = message.IsRead
            };

            chatContext.LastInteraction = DateTime.Now;
            chatContext.History.Add(message);
            entities.SaveChanges();

            SendMessageToConnectedClients(chatContext, messageViewModel);
            return Serialize(messageViewModel);
        }

        public override Task OnConnected()
        {
            string connectionId = Context.ConnectionId;
            var user = GetCurrentUser();

            user.SignalRConnections.Add(new SignalRConnection()
            {
                ConnectionId = connectionId
            });
            entities.SaveChanges();

            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            string connectionId = Context.ConnectionId;
            var user = GetCurrentUser();

            var connection = entities.SignalRConnections.FirstOrDefault(c => c.ConnectionId == connectionId);
            entities.SignalRConnections.Remove(connection);
            entities.SaveChanges();

            return base.OnDisconnected();
        }

        private ApplicationUser GetOtherUser(ChatContext context)
        {
            if (context.Initiator == null)
            {
                context.Initiator = entities.Users
                    .FirstOrDefault(u => u.Id == context.InitiatorId);
            }

            var otherUser = context.Initiator
                    .Id != Context.User.Identity.GetUserId() ?
                        context.Initiator : context.OtherUser;

            return otherUser;
        }

        private void SendNewContextToConnectedClients(ChatContext context)
        {
            var otherUser = GetOtherUser(context);
            var currentUser = GetCurrentUser();

            var viewModel = new
            {
                Id = context.Id,
                OtherUser = new UserProfileBasicViewModel(currentUser)
                {
                    UserId = otherUser.Id
                }
            };

            var otherUserConnections = otherUser.SignalRConnections.Select(c => c.ConnectionId);

            foreach (var connectionId in otherUserConnections)
            {
                Clients.Client(connectionId).newContext(viewModel);
            }
        }

        private void SendMessageToConnectedClients(ChatContext chatContext, object messageViewModel)
        {
            var otherUserConnections = GetOtherUser(chatContext)
                .SignalRConnections.Select(c => c.ConnectionId);

            foreach (var connectionId in otherUserConnections)
            {
                Clients.Client(connectionId).newMessage(messageViewModel);
            }
        }

        protected override void Dispose(bool disposing)
        {
            entities.Dispose();
            base.Dispose(disposing);
        }

        #region Helpers
        public string Serialize(object obj)
        {
            string serialized = JsonConvert.SerializeObject(obj,
                Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return serialized;
        }

        private ApplicationUser GetCurrentUser()
        {
            string userId = Context.User.Identity.GetUserId();
            var user = entities.Users.Find(userId);
            return user;
        }


        #endregion
    }
}