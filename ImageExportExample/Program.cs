using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using WW.Cad.Drawing;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Entities;
using WW.Math;

namespace ImageExportExample
{
    class Program {
        static void Main(string[] args) {
            // To get the trial software working, please open file MyWWLicense.cs
            // in the root directory and follow the instructions.
            WW.MyWWLicense.Set();

            CreateAndWriteCadDrawing();
            var model = CadReader.Read(@"test.dwg");
            var bitmap = ImageExporter.CreateAutoSizedBitmap<Bgra32>(model, Matrix4D.Identity, GraphicsConfig.AcadLikeWithBlackBackground, new Size(600, 500));
            string filename = @"test.png";
            using (Stream stream = File.Create(filename)) {
                bitmap.SaveAsPng(stream);
            }

            Console.WriteLine($"Written image file to {Environment.CurrentDirectory}\\{filename}.");
            Console.WriteLine("Press enter.");
            Console.ReadLine();
        }

        // Create a very simple AutoCAD drawing.
        private static void CreateAndWriteCadDrawing() {
            DxfModel model = new DxfModel();

            model.Entities.Add(new DxfLine(new Point2D(1, 0), new Point2D(3, 2)));
            model.Entities.Add(
                new DxfDimension.Aligned(model.CurrentDimensionStyle) {
                    DimensionLineLocation = new Point3D(2, 4, 0),
                    ExtensionLine1StartPoint = new Point3D(1, 0, 0),
                    ExtensionLine2StartPoint = new Point3D(3, 2, 0)
                }
            );
            model.Entities.Add(
                new DxfMText("This is some sample text", new Point3D(0, -1, 0), 0.4d) {
                    Color = EntityColors.Blue
                }
            );

            DwgWriter.Write(@"test.dwg", model);
        }
    }
}
