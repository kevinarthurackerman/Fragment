using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Fragment
{
    public interface IFragment
    {
        public string Selector { get; }
        public ContentPositions? ContentPosition { get; }
        public int? Delay { get; }
        public string ContentType { get; }
        public Stream Content { get; }
        public string FilePath { get; }
        public Controller Controller { get; }
        public string ViewName { get; }
        public object Model { get; }
    }

    public class ViewFragment : IFragment
    {
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public int? Delay { get; set; }
        public string ContentType { get; } = "text/html";
        public Stream Content { get; } = null;
        public string FilePath { get; } = null;
        public Controller Controller { get; set; }
        public string ViewName { get; set; }
        public object Model { get; set; }
    }

    public class HtmlFragment : IFragment
    {
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public int? Delay { get; set; }
        public string ContentType { get; } = "text/html";
        public Stream Content { get; set; }
        public string FilePath { get; set; }
        public Controller Controller { get; } = null;
        public string ViewName { get; } = null;
        public object Model { get; } = null;
    }

    public class ScriptFragment : IFragment
    {
        public string Selector { get; } = null;
        public ContentPositions? ContentPosition { get; } = null;
        public int? Delay { get; } = null;
        public string ContentType { get; } = "text/javascript";
        public Stream Content { get; set; }
        public string FilePath { get; set; }
        public Controller Controller { get; } = null;
        public string ViewName { get; } = null;
        public object Model { get; } = null;
    }
}
