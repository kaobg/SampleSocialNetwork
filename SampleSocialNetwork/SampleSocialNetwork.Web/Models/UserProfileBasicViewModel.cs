using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SampleSocialNetwork.Models;

namespace SampleSocialNetwork.Web.Models
{
    public class UserProfileBasicViewModel
    {
        private const string ImgFolder = "/Content/UserImages/";
        private const string DefaultAvatarURL = "default.jpg";

        public UserProfileBasicViewModel() { }

        public UserProfileBasicViewModel(ApplicationUser profile)
        {
            this.UserName = profile.UserName;

            if (String.IsNullOrEmpty(profile.FirstName) || String.IsNullOrEmpty(profile.LastName))
            {
                this.DisplayName = profile.UserName;
            }
            else
            {
                this.DisplayName = profile.FirstName + " " + profile.LastName;
            }

            if (profile.AvatarURL != null)
            {
                this.AvatarURL = ImgFolder + profile.AvatarURL;
            }
            else
            {
                this.AvatarURL = ImgFolder + DefaultAvatarURL;
            }
        }

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string AvatarURL { get; set; }
    }
}