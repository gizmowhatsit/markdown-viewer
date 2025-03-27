using Markdig;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using MessageBox = System.Windows.MessageBox;

namespace MarkdownViewer
{
    public partial class MainWindow : Window
    {
        private System.Windows.Controls.WebBrowser _browser;
        private ScaleTransform _scaleTransform;
        private double _zoomFactor = 1.0;

        public MainWindow()
        {
            InitializeComponent();

            // Set window properties
            Title = "Markdown Viewer";

            // Create a WebBrowser control
            _browser = new System.Windows.Controls.WebBrowser();

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

            // Load window position and size when the window is loaded
            Loaded += MainWindow_Loaded;
            // Save window position and size when the window is closing
            Closing += MainWindow_Closing;
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

                    // Configure Markdig pipeline with common extensions
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .Build();

                    string html = Markdown.ToHtml(markdownText, pipeline);

                    // Create a complete HTML document with CSS for styling (dark mode)
                    string htmlDocument = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1'>
                        <style>
                            body {{ 
                                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
                                line-height: 1.6;
                                color: #e0e0e0;
                                background-color: #121212;
                                margin: 0 auto;
                                padding: 20px;
                            }}
                            h1 {{ 
                                font-size: 2em; 
                                border-bottom: 1px solid #333; 
                                padding-bottom: .3em; 
                                color: #ffffff;
                            }}
                            h2 {{ 
                                font-size: 1.5em; 
                                border-bottom: 1px solid #333; 
                                padding-bottom: .3em; 
                                color: #ffffff;
                            }}
                            h3, h4, h5, h6 {{
                                color: #ffffff;
                            }}
                            a {{
                                color: #58a6ff;
                                text-decoration: none;
                            }}
                            a:hover {{
                                text-decoration: underline;
                            }}
                            pre {{ 
                                background-color: #1e1e1e;
                                border-radius: 3px;
                                padding: 16px;
                                overflow: auto;
                                font-family: Consolas, Monaco, monospace;
                                color: #d4d4d4;
                                border: 1px solid #333;
                            }}
                            code {{
                                font-family: Consolas, Monaco, monospace;
                                background-color: #2d2d2d;
                                padding: 0.2em 0.4em;
                                border-radius: 3px;
                                color: #d4d4d4;
                            }}
                            img {{ max-width: 100%; }}
                            blockquote {{
                                border-left: 4px solid #444;
                                padding: 0 1em;
                                color: #a0a0a0;
                                margin: 0 0 16px;
                                background-color: #1a1a1a;
                                border-radius: 0 3px 3px 0;
                            }}
                            table {{
                                border-collapse: collapse;
                                width: 100%;
                                margin-bottom: 16px;
                            }}
                            table, th, td {{
                                border: 1px solid #333;
                            }}
                            th {{
                                background-color: #252525;
                                color: #ffffff;
                                padding: 8px 13px;
                            }}
                            td {{
                                padding: 6px 13px;
                            }}
                            tr:nth-child(even) {{
                                background-color: #1a1a1a;
                            }}
                            hr {{
                                border: 0;
                                height: 1px;
                                background-color: #333;
                                margin: 24px 0;
                            }}
                            /* Adjust list styling */
                            ul, ol {{
                                color: #e0e0e0;
                            }}
                            /* Style for print - revert to light theme when printing */
                            @media print {{
                                body {{
                                    color: #000;
                                    background-color: #fff;
                                }}
                                pre, code {{
                                    background-color: #f6f8fa;
                                    color: #000;
                                    border: 1px solid #ddd;
                                }}
                                h1, h2, h3, h4, h5, h6 {{
                                    color: #000;
                                }}
                                blockquote {{
                                    border-left: 4px solid #ddd;
                                    color: #555;
                                    background-color: #f8f8f8;
                                }}
                                table, th, td {{
                                    border: 1px solid #ddd;
                                }}
                                th {{
                                    background-color: #f2f2f2;
                                    color: #000;
                                }}
                                a {{
                                    color: #0366d6;
                                }}
                            }}
                        </style>
                    </head>
                    <body>
                        {html}
                    </body>
                    </html>";

                    _browser.NavigateToString(htmlDocument);
                    Title = $"Markdown Viewer - {Path.GetFileName(filePath)}";
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}