using Fragment.Demo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace Fragment.Demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(IWebHostEnvironment hostingEnvironment) {
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index() => Request.IsAjaxRequest()
            ? (IActionResult)new FragmentedResult(this, true, new ViewFragment())
            : (IActionResult)View();

        public IActionResult Privacy() => Request.IsAjaxRequest()
            ? (IActionResult)new FragmentedResult(this, true, new ViewFragment())
            : (IActionResult)View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Test()
        {
            return new FragmentedResult(
                new ScriptFragment { FilePath = "js/site.js" },
                new ViewFragment { ViewName = "_Partial", Model = "A", Selector = ".x", ContentPosition = ContentPositions.BeforeElement },
                new ViewFragment { ViewName = "_Partial", Model = "B", Selector = ".x", Delay = 500, ContentPosition = ContentPositions.AfterElement },
                new ViewFragment { ViewName = "_Partial", Model = "C", Selector = ".x", Delay = 1000, ContentPosition = ContentPositions.BeforeContent },
                new ViewFragment { ViewName = "_Partial", Model = "D", Selector = ".x", Delay = 1500, ContentPosition = ContentPositions.AfterContent },
                new ViewFragment { ViewName = "_Partial", Model = "E", Selector = ".x", Delay = 2000, ContentPosition = ContentPositions.ReplaceContent },
                new ViewFragment { ViewName = "../Test/_Partial", Model = "F", Selector = ".x", Delay = 2500, ContentPosition = ContentPositions.ReplaceElement },
                new ViewFragment { Selector = ".r1", Delay = 3000, ContentPosition = ContentPositions.RemoveElement },
                new ViewFragment { Selector = ".r2", Delay = 3500, ContentPosition = ContentPositions.RemoveContent }
            );
        }
    }
}
