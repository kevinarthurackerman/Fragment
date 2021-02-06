using Microsoft.AspNetCore.Mvc;
using System;

namespace Fragment
{
    public static class FragmentedResultExtensions
    {
        public static FragmentedResult WithPage(this FragmentedResult fragmentedResult, IActionResult pageFragment)
        {
            if (fragmentedResult.PageFragment != null)
                throw new InvalidOperationException($"Cannot create a {nameof(FragmentedResult)} with multiple page fragments.");

            fragmentedResult.PageFragment = pageFragment;
            fragmentedResult.PageUri ??= new Uri(String.Empty, UriKind.Relative);

            return fragmentedResult;
        }

        public static FragmentedResult WithFragment(this FragmentedResult fragmentedResult, IActionResult viewFragment)
        {
            fragmentedResult.ViewFragments.Add(viewFragment);
            return fragmentedResult;
        }
    }

    public partial class FragmentedResult
    {
        public static FragmentedResult Page() => Page(new ViewResult());

        public static FragmentedResult Page(IActionResult pageFragment)
        {
            var fragmentedResult = new FragmentedResult
            {
                PageFragment = pageFragment,
                PageUri = new Uri(String.Empty, UriKind.Relative)
            };
            fragmentedResult.ViewFragments.Add(pageFragment);
            return fragmentedResult;
        }

        public static FragmentedResult WithPage(IActionResult pageFragment) =>
            new FragmentedResult
            {
                PageFragment = pageFragment,
                PageUri = new Uri(String.Empty, UriKind.Relative)
            };

        public static FragmentedResult WithFragment(IActionResult viewFragment)
        {
            var fragmentedResult = new FragmentedResult();
            fragmentedResult.ViewFragments.Add(viewFragment);
            return fragmentedResult;
        }
    }
}
