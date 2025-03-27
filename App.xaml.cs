using System;
using System.Windows;

namespace MarkdownViewer
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var mainWindow = new MainWindow();
            
            // Check for command line arguments
            if (e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                mainWindow.LoadMarkdownFile(filePath);
            }
            else
            {
                // No file provided, show a message and exit
                System.Windows.MessageBox.Show("Please open a markdown file from Windows Explorer or provide a file path as a command line argument.", 
                    "No File Provided", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            
            mainWindow.Show();
        }
    }
}
