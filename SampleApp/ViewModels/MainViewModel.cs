using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;

using Reactive.Bindings;

namespace SampleApp.ViewModels;

public class ItemViewModel
{
    public ItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
}

public class BreadcrumbItemViewModel
{
    public BreadcrumbItemViewModel(string text, string path)
    {
        Text = text;
        Path = path;
    }

    public string Text { get; }
    public string Path { get; }
}

public class MainViewModel
{
    public MainViewModel()
    {
        // mock filesystem: richer tree for testing
        _mock = new()
        {
            ["/"] = new[] {
                "/Home",
                "/Projects",
                "/Library",
                "/etc",
                "/opt",
                "/var",
            },

            ["/Home"] = new[] {
                "/Home/Documents",
                "/Home/Pictures",
                "/Home/Music",
                "/Home/.config",
                "/Home/Readme.txt",
                "/Home/todo.md",
            },

            ["/Home/Documents"] = new[] {
                "/Home/Documents/Work",
                "/Home/Documents/Personal",
                "/Home/Documents/Notes.txt",
                "/Home/Documents/Resume.pdf",
            },

            ["/Home/Documents/Work"] = new[] {
                "/Home/Documents/Work/Project1",
                "/Home/Documents/Work/Project2",
                "/Home/Documents/Work/MeetingNotes.docx",
            },

            ["/Home/Documents/Work/Project1"] = new[] {
                "/Home/Documents/Work/Project1/src",
                "/Home/Documents/Work/Project1/docs",
                "/Home/Documents/Work/Project1/README.md",
                "/Home/Documents/Work/Project1/.gitignore",
            },

            ["/Home/Documents/Work/Project1/src"] = new[] {
                "/Home/Documents/Work/Project1/src/main.cs",
                "/Home/Documents/Work/Project1/src/util.cs",
                "/Home/Documents/Work/Project1/src/third_party",
            },

            ["/Home/Documents/Work/Project1/src/third_party"] = new[] {
                "/Home/Documents/Work/Project1/src/third_party/libA",
                "/Home/Documents/Work/Project1/src/third_party/libB",
            },

            ["/Home/Documents/Work/Project2"] = new[] {
                "/Home/Documents/Work/Project2/notes.md",
                "/Home/Documents/Work/Project2/build.log",
            },

            ["/Home/Documents/Personal"] = new[] {
                "/Home/Documents/Personal/Taxes.pdf",
                "/Home/Documents/Personal/Recipes",
                "/Home/Documents/Personal/Journal.txt",
            },

            ["/Home/Pictures"] = new[] {
                "/Home/Pictures/Vacation",
                "/Home/Pictures/Family.jpg",
                "/Home/Pictures/.thumbnails",
            },

            ["/Home/Pictures/Vacation"] = new[] {
                "/Home/Pictures/Vacation/IMG_0001.jpg",
                "/Home/Pictures/Vacation/IMG_0002.jpg",
                "/Home/Pictures/Vacation/Beach.png",
            },

            ["/Home/Music"] = Enumerable.Range(1, 20).Select(i => $"/Home/Music/Track{i:00}.mp3").ToArray(),

            ["/Projects"] = new[] {
                "/Projects/FluentAvalonia",
                "/Projects/Jaya",
                "/Projects/SmallTool",
                "/Projects/OldProject",
            },

            ["/Projects/FluentAvalonia"] = new[] {
                "/Projects/FluentAvalonia/src",
                "/Projects/FluentAvalonia/tests",
                "/Projects/FluentAvalonia/README.md",
            },

            ["/Library"] = new[] {
                "/Library/Fonts",
                "/Library/Application Support",
            },

            ["/Home/.config"] = new[] {
                "/Home/.config/app1",
                "/Home/.config/app2",
            },
        };

        Items = new();
        Breadcrumbs = new();

        // start path
        CurrentPath = new ReactiveProperty<string>("/Home");
        Navigate = new ReactiveCommand<string>().WithSubscribe(p => NavigateTo(p));

        // populate initial
        NavigateTo(CurrentPath.Value);
    }

    private readonly System.Collections.Generic.Dictionary<string, string[]> _mock;

    public ReactiveCollection<ItemViewModel> Items { get; }

    public ReactiveCollection<BreadcrumbItemViewModel> Breadcrumbs { get; }

    public ReactiveProperty<string> CurrentPath { get; }

    public ReactiveCommand<string> Navigate { get; }

    private void NavigateTo(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        CurrentPath.Value = path;

        Items.Clear();
        if (_mock.TryGetValue(path, out var children))
        {
            foreach (var c in children)
            {
                Items.Add(new ItemViewModel(System.IO.Path.GetFileName(c)));
            }
        }

        Breadcrumbs.Clear();
        var segs = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
        var acc = "";
        // root
        Breadcrumbs.Add(new BreadcrumbItemViewModel("/", "/"));
        foreach (var s in segs)
        {
            acc = acc + "/" + s;
            Breadcrumbs.Add(new BreadcrumbItemViewModel(s, acc));
        }
    }

    // Expose children for UI (used by breadcrumb popup)
    public string[] GetChildren(string path)
    {
        if (string.IsNullOrEmpty(path)) return Array.Empty<string>();
        if (_mock.TryGetValue(path, out var children)) return children;
        return Array.Empty<string>();
    }
}
