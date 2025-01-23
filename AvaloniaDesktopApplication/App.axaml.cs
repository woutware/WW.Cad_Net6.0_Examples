using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

using WW.Cad.Avalonia.Views;
using WW.Cad.IO;

namespace AvaloniaDesktopApplication {
    public partial class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                var window = new MainWindow();
                CadView? view = window.Content as CadView;
                if (view != null) {
                    var topLevel = TopLevel.GetTopLevel(window);

                    var task = topLevel.StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions {
                            FileTypeFilter = new[] { 
                                new FilePickerFileType("AutoCAD drawings") {
                                    Patterns = new[] { "*.dwg", "*.dxf" }
                                } 
                            },
                            Title = "Open AutoCAD File",
                            AllowMultiple = false
                        }
                    );
                    Task.WaitAll(task);
                    var files = task.Result;

                    if (files.Count >= 1) {
                        string filename = files[0].Path.LocalPath;
                        var model = CadReader.Read(filename);
                        view.SetCadModel(model, model.Header.ShowModelSpace ? null : model.ActiveLayout);
                    }
                }

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}