using SampleSocialNetwork.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SampleSocialNetwork.Web.Controllers
{
    public class UsersController : Controller
    {
        private ApplicationDbContext context = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ViewProfile(string username)
        {
            var user = context.Users.FirstOrDefault(u => u.UserName == username);
            return View(user);
        }

        protected override void Dispose(bool disposing)
        {
            context.Dispose();
            base.Dispose(disposing);
        }
	}
}