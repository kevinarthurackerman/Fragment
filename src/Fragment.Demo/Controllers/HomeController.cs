using Fragment.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace Fragment.Demo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => FragmentedResult.Page();

        public IActionResult Privacy() => FragmentedResult.Page();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Test()
        {
            return FragmentedResult
                .WithFragment(new JavascriptFragment { FilePath = "js/site.js" })
                .WithFragment(new JavascriptFragment { RawContent = "window.setTimeout(() => document.querySelectorAll('.home-partial').forEach(e => e.classList.add('text-success')), 4000)" })
                .WithFragment(new PartialFragment { ViewName = "_Partial", Model = "A", Selector = ".x", ContentPosition = ContentPositions.BeforeElement })
                .WithFragment(new PartialFragment { ViewName = "_Partial", Model = "B", Selector = ".x", Delay = TimeSpan.FromMilliseconds(500), ContentPosition = ContentPositions.AfterElement })
                .WithFragment(new PartialFragment { ViewName = "_Partial", Model = "C", Selector = ".x", Delay = TimeSpan.FromMilliseconds(1000), ContentPosition = ContentPositions.BeforeContent })
                .WithFragment(new PartialFragment { ViewName = "_Partial", Model = "D", Selector = ".x", Delay = TimeSpan.FromMilliseconds(1500), ContentPosition = ContentPositions.AfterContent })
                .WithFragment(new PartialFragment { ViewName = "_Partial", Model = "E", Selector = ".x", Delay = TimeSpan.FromMilliseconds(2000), ContentPosition = ContentPositions.ReplaceContent })
                .WithFragment(new PartialFragment { ViewName = "../Test/_Partial", Model = "F", Selector = ".x", Delay = TimeSpan.FromMilliseconds(2500), ContentPosition = ContentPositions.ReplaceElement })
                .WithFragment(new PartialFragment { Selector = ".r1", Delay = TimeSpan.FromMilliseconds(3000), ContentPosition = ContentPositions.RemoveElement })
                .WithFragment(new PartialFragment { Selector = ".r2", Delay = TimeSpan.FromMilliseconds(3500), ContentPosition = ContentPositions.RemoveContent });
        }
    }
}
