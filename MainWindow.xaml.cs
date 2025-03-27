using Markdig;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace MarkdownViewer
{
    public partial class MainWindow : Window
    {
        private WebBrowser _browser;
        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.0;
        private string _htmlTemplate;

        public MainWindow()
        {
            InitializeComponent();

            // Set window properties
            Title = "Markdown Viewer";

            // Create a WebBrowser control
            _browser = new WebBrowser();

            // Set up scaling transform for zooming
            _scaleTransform = new ScaleTransform(1, 1);
            _browser.LayoutTransform = _scaleTransform;

            // Set the content of the window to the browser
            Content = _browser;

            // Handle mouse wheel for zooming
            PreviewMouseWheel += MainWindow_PreviewMouseWheel;

            // Handle print shortcut
            var printCommand = new RoutedCommand();
            printCommand.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(printCommand, ExecutePrint));

            // Load HTML template
            LoadHtmlTemplate();

            // Load window position and size when the window is loaded
            Loaded += MainWindow_Loaded;
            // Save window position and size when the window is closing
            Closing += MainWindow_Closing;
        }

        private void LoadHtmlTemplate()
        {
            try
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "MarkdownTemplate.html");
                if (File.Exists(templatePath))
                {
                    _htmlTemplate = File.ReadAllText(templatePath);
                }
                else
                {
                    // Fallback to embedded template if file doesn't exist
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MarkdownViewer.Resources.MarkdownTemplate.html");
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        _htmlTemplate = reader.ReadToEnd();
                    }
                    else
                    {
                        // If both fail, use a minimal template as fallback
                        _htmlTemplate = "<!DOCTYPE html><html><head><style>body{font-family:sans-serif;}</style></head><body><!-- CONTENT_PLACEHOLDER --></body></html>";
                        System.Windows.MessageBox.Show("Template file not found. Using minimal template.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Fallback to minimal template
                _htmlTemplate = "<!DOCTYPE html><html><head><style>body{font-family:sans-serif;}</style></head><body><!-- CONTENT_PLACEHOLDER --></body></html>";
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Restore window position and size from settings
            var settings = Properties.Settings.Default;

            // Set window dimensions
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;

            // Check if window position is reasonable (on screen)
            bool isOnScreen = IsOnScreen(settings.WindowLeft, settings.WindowTop);

            if (isOnScreen)
            {
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
            else
            {
                // Center on screen if not valid position
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Restore maximized state if it was maximized
            if (settings.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        // Helper method to check if a point is visible on any screen
        private bool IsOnScreen(double left, double top)
        {
            // If this is the first run, the settings will be 0,0
            if (left == 0 && top == 0)
                return false;

            // Check if the position is within the virtual screen bounds
            // (all monitors combined)
            return left > SystemParameters.VirtualScreenLeft - 50 &&
                   top > SystemParameters.VirtualScreenTop - 50 &&
                   left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100 &&
                   top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window position and size
            var settings = Properties.Settings.Default;

            // Only save if window is not maximized to preserve normal dimensions
            if (WindowState == WindowState.Normal)
            {
                settings.WindowLeft = Left;
                settings.WindowTop = Top;
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
            }

            // Save maximized state
            settings.IsMaximized = (WindowState == WindowState.Maximized);

            // Save settings
            settings.Save();
        }

        public void LoadMarkdownFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string markdownText = File.ReadAllText(filePath);

                    // Store the directory of the current file for resolving relative paths
                    string currentDirectory = Path.GetDirectoryName(filePath);

                    // Configure Markdig pipeline with common extensions
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .Build();

                    string html = Markdown.ToHtml(markdownText, pipeline);

                    // Fix relative image paths
                    html = FixRelativeImagePaths(html, currentDirectory);

                    // Replace content placeholder with generated HTML
                    string htmlDocument = _htmlTemplate.Replace("<!-- CONTENT_PLACEHOLDER -->", html);

                    _browser.NavigateToString(htmlDocument);
                    Title = $"Markdown Viewer - {Path.GetFileName(filePath)}";
                }
                else
                {
                    System.Windows.MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Handle zooming with Ctrl+MouseWheel
                if (e.Delta > 0)
                    _zoomFactor = Math.Min(3.0, _zoomFactor + 0.1);
                else
                    _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);

                _scaleTransform.ScaleX = _zoomFactor;
                _scaleTransform.ScaleY = _zoomFactor;

                e.Handled = true;
            }
            // Normal scrolling is handled by the WebBrowser control automatically
        }

        private void ExecutePrint(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                // Use reflection to access the Document COM object
                dynamic document = _browser.Document;
                if (document != null)
                {
                    document.execCommand("Print", true, null);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error printing: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FixRelativeImagePaths(string html, string baseDirectory)
        {
            // This regex looks for <img> tags with src attributes
            var imgRegex = new System.Text.RegularExpressions.Regex(
                @"<img[^>]*src\s*=\s*[""']([^""']+)[""'][^>]*>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return imgRegex.Replace(html, match =>
            {
                string originalTag = match.Value;
                string src = match.Groups[1].Value;

                // Skip URLs that are already absolute
                if (src.StartsWith("http://") ||
                    src.StartsWith("https://") ||
                    src.StartsWith("file://") ||
                    src.StartsWith("data:") ||
                    src.StartsWith("about:") ||
                    src.StartsWith("/"))
                {
                    return originalTag;
                }

                try
                {
                    // Convert the relative path to absolute
                    string absolutePath = Path.Combine(baseDirectory, src);

                    // Ensure the file exists
                    if (File.Exists(absolutePath))
                    {
                        // Replace the src with a file:// URL
                        string fileUrl = "file:///" + absolutePath.Replace('\\', '/');
                        string newTag = originalTag.Replace(src, fileUrl);
                        return newTag;
                    }
                }
                catch
                {
                    // If anything goes wrong, return the original tag
                }

                return originalTag;
            });
        }
    }
}