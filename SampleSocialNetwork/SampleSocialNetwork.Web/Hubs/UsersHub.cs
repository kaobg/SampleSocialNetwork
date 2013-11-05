using Microsoft.AspNet.SignalR;
using SampleSocialNetwork.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;

namespace SampleSocialNetwork.Web.Hubs
{
    public class UsersHub : Hub
    {
        private ApplicationDbContext entities = new ApplicationDbContext();

        public object Initialize(string userId)
        {
            var currentUserId = Context.User.Identity.GetUserId();
            var relation = entities.UserRelations
                .Include("RelatingUser")
                .Include("RelatedUser")
                .FirstOrDefault(x => x.RelatingUser.Id
                    == currentUserId
                    && x.RelatedUser.Id == userId);

            string relationName = null;
            if (relation != null)
            {
                relationName = relation.RelationType.ToString(); 
            }
            else
            {
                relationName = "Neutral";
            }

            var viewModel = new { RelationName = relationName };
            return viewModel;
        }

        public void RequestFriendship()
        {

        }

        protected override void Dispose(bool disposing)
        {
            entities.Dispose();
            base.Dispose(disposing);
        }
    }
}