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
using Avalonia.VisualTree;
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
        Console.WriteLine($"\n=== CHEVRON CLICK ===");
        Console.WriteLine($"Chevron clicked for path: {target}");
        
        // Find the parent BreadcrumbBarItem to align with the full item height
        Control targetControl = btn;
        var item = btn.FindAncestorOfType<BreadcrumbBarItem>();
        if (item != null)
        {
            targetControl = item;
            Console.WriteLine($"Found parent BreadcrumbBarItem. Height: {item.Bounds.Height}");
        }
        else
        {
            Console.WriteLine("Could not find parent BreadcrumbBarItem, falling back to Button");
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

        // Create a simple ListBox for the children
        var items = children.Select(c => System.IO.Path.GetFileName(c)).ToArray();
        var listBox = new ListBox 
        { 
            ItemsSource = items,
            Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)),
            MinWidth = 240,
        };

        // Create a borderless, transparent window for the popup
        var border = new Border 
        { 
            Child = listBox, 
            Padding = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
        };
        
        // Add box shadow using the BoxShadows property
        //var shadow = new BoxShadow { Blur = 8, Spread = 0, OffsetX = 0, OffsetY = 2, Color = Color.FromArgb(128, 0, 0, 0) };
        //border.BoxShadow = new BoxShadows(shadow);
        
        var popupWindow = new Window
        {
            Content = border,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            Topmost = true,
            Width = 240,
            Height = Math.Min(300, 28 * items.Length + 2),
        };

        listBox.SelectionChanged += (_, __) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                var selected = children[listBox.SelectedIndex];
                Console.WriteLine($"Menu item selected, navigating to: {selected}");
                vm.Navigate.Execute(selected);
                popupWindow?.Close();
            }
        };

        // Position the window below the target control (BreadcrumbBarItem or Button)
        try
        {
            // 1. Get the bottom-left corner of the target control in its local coordinates
            var targetBottomLeft = new Point(0, targetControl.Bounds.Height);

            // 2. Translate that point to the Window's coordinate space
            var targetBottomLeftInWindow = targetControl.TranslatePoint(targetBottomLeft, this);

            if (targetBottomLeftInWindow.HasValue)
            {
                // 3. Convert the Window-relative point to Screen coordinates (PixelPoint)
                var screenPoint = this.PointToScreen(targetBottomLeftInWindow.Value);
                
                // 4. Set the popup position with a small vertical gap (4px) to ensure no overlap
                popupWindow.Position = new PixelPoint(screenPoint.X, screenPoint.Y + 4);

                Console.WriteLine($"=== ROBUST POSITIONING (Target: {targetControl.GetType().Name}) ===");
                Console.WriteLine($"Target Height: {targetControl.Bounds.Height}");
                Console.WriteLine($"Target Bottom-Left (Local): {targetBottomLeft}");
                Console.WriteLine($"Target Bottom-Left (Window): {targetBottomLeftInWindow.Value}");
                Console.WriteLine($"Screen Point (PixelPoint): {screenPoint}");
                Console.WriteLine($"Final Popup Position: {popupWindow.Position}");
                Console.WriteLine($"==========================");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error positioning popup: {ex.Message}");
        }

        Console.WriteLine($"Opening popup window with {items.Length} items");
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
