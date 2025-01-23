using System;

using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Entities;
using WW.Math;

namespace ReadExample {        
    class Program {
        static void Main(string[] args) {
            // To get the trial software working, please open file MyWWLicense.cs
            // in the root directory and follow the instructions.
            WW.MyWWLicense.Set();

            CreateAndWriteCadDrawing();
            ReadCadDrawing();

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

            DwgWriter.Write(@"test.dwg", model);
        }

        // Read the AutoCAD drawing that was just created.
        private static void ReadCadDrawing() {
            DxfModel model = CadReader.Read("test.dwg");
            Console.WriteLine($"Read file successfully, filename: {model.Filename}, number of entities: {model.Entities.Count}");
        }
    }
}
