using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

using FluentAvalonia.Styling;
using FluentAvalonia.BreadcrumbBar.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace SampleApp.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        BreadcrumbBar.ItemClicked += BreadcrumbBar_ItemClicked;
    }

    private void Chevron_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!(sender is Control btn)) return;
        if (DataContext is not SampleApp.ViewModels.MainViewModel vm) return;

        if (btn.DataContext is not SampleApp.ViewModels.BreadcrumbItemViewModel bvm) return;

        var target = bvm.Path;
        Console.WriteLine($"Chevron clicked for path: {target}");

        // Debug: print bounds information for the clicked elements to help diagnose alignment
        try
        {
            Console.WriteLine($"BreadcrumbBar.Bounds (local): {BreadcrumbBar.Bounds}");
            Console.WriteLine($"Button.Bounds (local): {btn.Bounds}");
            if (btn is Button b2 && b2.Content is TextBlock tb2)
            {
                Console.WriteLine($"TextBlock.Bounds (local): {tb2.Bounds}");
                var tbWindow = tb2.TranslatePoint(new Point(0, 0), this) ?? new Point();
                Console.WriteLine($"TextBlock top-left (window coords): {tbWindow}");
                var tbScreen = this.PointToScreen(tbWindow);
                Console.WriteLine($"TextBlock top-left (screen coords): {tbScreen}");
            }
            var barTopLeft = BreadcrumbBar.TranslatePoint(new Point(0,0), this) ?? new Point();
            var barScreen = this.PointToScreen(barTopLeft);
            Console.WriteLine($"BreadcrumbBar top-left (window coords): {barTopLeft}, screen coords: {barScreen}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while logging bounds: {ex}");
        }

        var children = vm.GetChildren(target).Where(c => !c.Contains('.')).ToArray();
        Console.WriteLine($"Children count for '{target}': {children.Length}");
        if (children.Length <= 1)
        {
            if (children.Length == 1)
            {
                vm.Navigate.Execute(children[0]);
            }
            return;
        }

        var popupWindow = new Window
        {
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Width = 240,
            Height = Math.Min(300, 28 * Math.Max(1, children.Length)),
        };

        var items = children.Select(c => System.IO.Path.GetFileName(c)).ToArray();
        var listBox = new ListBox { ItemsSource = items };
        listBox.SelectionChanged += (_, __) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                var selected = children[listBox.SelectedIndex];
                Console.WriteLine($"Popup item selected, navigating to: {selected}");
                vm.Navigate.Execute(selected);
                try { popupWindow.Close(); } catch { }
            }
        };

        popupWindow.Content = new Border { Child = listBox, Padding = new Thickness(6) };

        try
        {
            // If the sender is a Button whose Content is a TextBlock, anchor to that TextBlock's left edge
            if (btn is Button b && b.Content is TextBlock tb)
            {
                Console.WriteLine($"Anchoring popup to TextBlock. Text='{tb.Text}', FontSize={tb.FontSize}");
                // Make ListBox use same font settings so text aligns
                listBox.FontSize = tb.FontSize;
                listBox.FontFamily = tb.FontFamily;

                var localTopLeft = new Point(0, 0);
                var translated = tb.TranslatePoint(localTopLeft, this) ?? localTopLeft;
                var screen = this.PointToScreen(translated);
                var popupX = (int)screen.X; // left aligned to text
                var popupY = (int)(screen.Y + tb.Bounds.Height);
                popupWindow.Position = new PixelPoint(popupX, popupY);
                Console.WriteLine($"Opening text-anchored popup at {popupWindow.Position}");
            }
            else
            {
                // fallback to chevron bounds
                var localTopLeft = new Point(0, 0);
                var translated = btn.TranslatePoint(localTopLeft, this) ?? localTopLeft;
                var screen = this.PointToScreen(translated);
                var popupX = (int)screen.X; // left aligned
                var popupY = (int)(screen.Y + btn.Bounds.Height);
                popupWindow.Position = new PixelPoint(popupX, popupY);
                Console.WriteLine($"Opening left-aligned popup at {popupWindow.Position} (fallback)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to anchor popup to chevron: {ex}");
            var mainScreen = this.PointToScreen(new Point(0, 0));
            popupWindow.Position = new PixelPoint((int)mainScreen.X + 50, (int)mainScreen.Y + 50);
        }

        popupWindow.Show(this);
    }

    private async void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (DataContext is SampleApp.ViewModels.MainViewModel vm)
        {
            if (args.Index >= 0 && args.Index < vm.Breadcrumbs.Count)
            {
                Console.WriteLine($"Breadcrumb body clicked: index={args.Index}");
                var target = vm.Breadcrumbs[args.Index].Path;
                Console.WriteLine($"Direct navigate to '{target}' from breadcrumb body click");
                vm.Navigate.Execute(target);
            }
        }
    }
}
