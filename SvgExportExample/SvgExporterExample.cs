using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;

using WW.Cad.Base;
using WW.Cad.Drawing;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Objects;
using WW.Cad.Model.Tables;
#if NETCOREAPP
using WW.Drawing.Printing;
#endif
using WW.Math;
using WW.Math.Geometry;

namespace SvgExportExample {
    // This class demonstrates how to export an AutoCAD file to SVG (both model space and paper space layouts).
    public class SvgExporterExample {
        // Exports an AutoCAD file to SVG. For each layout a page in the SVG file is created.
        public static void ExportToSvg(string filename) {
            DxfModel model = CadReader.Read(filename);
            ExportToSvg(model);
        }

        // Exports an AutoCAD file to SVG. For each layout a page in the SVG file is created.
        public static void ExportToSvg(DxfModel model) {
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            // as SVG
            using (FileStream stream = File.Create(Path.Combine(dir, filenameNoExt + ".svg"))) {
                SvgExporter svgExporter = new SvgExporter(stream);

                GraphicsConfig config = (GraphicsConfig)GraphicsConfig.WhiteBackgroundCorrectForBackColor.Clone();
                config.DisplayLineTypeElementShapes = true;
                //config.TryDrawingTextAsText = true;

                AddLayoutToSvgExporter(svgExporter, config, model, null, model.ModelLayout);
            }
        }

        // Exports the specified layout of an AutoCAD file to SVG.
        public static void ExportToSvg(DxfModel model, DxfLayout layout) {
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            // as SVG
            using (FileStream stream = File.Create(Path.Combine(dir, filenameNoExt + "-" + layout.Name + ".svg"))) {
                SvgExporter svgExporter = new SvgExporter(stream);

                GraphicsConfig config = (GraphicsConfig)GraphicsConfig.WhiteBackgroundCorrectForBackColor.Clone();
                config.DisplayLineTypeElementShapes = true;
                //config.TryDrawingTextAsText = true;

                AddLayoutToSvgExporter(svgExporter, config, model, null, layout);
            }
        }

        // For each layout, add a page to the SVG file.
        // Optionally specify a modelView (for model space only).
        // Optionally specify a layout.
        private static void AddLayoutToSvgExporter(
            SvgExporter svgExporter, GraphicsConfig config, DxfModel model, DxfView modelView, DxfLayout layout
        ) {
            Bounds3D bounds;
            const float defaultMargin = 0.5f;
            float margin = 0f;
            PaperSize paperSize = null;
            bool useModelView = false;
            bool emptyLayout = false;
            if (layout == null || !layout.PaperSpace) {
                // Model space.
                BoundsCalculator boundsCalculator = new BoundsCalculator();
                boundsCalculator.GetBounds(model);
                bounds = boundsCalculator.Bounds;
                if (bounds.Initialized) {
                    if (bounds.Delta.X > bounds.Delta.Y) {
                        paperSize = PaperSizes.GetPaperSize(PaperKind.A4Rotated);
                    } else {
                        paperSize = PaperSizes.GetPaperSize(PaperKind.A4);
                    }
                } else {
                    emptyLayout = true;
                }
                margin = defaultMargin;
                useModelView = modelView != null;
            } else {
                // Paper space layout.
                Bounds2D plotAreaBounds = layout.GetPlotAreaBounds();
                bounds = new Bounds3D();
                emptyLayout = !plotAreaBounds.Initialized;
                if (plotAreaBounds.Initialized) {
                    double customScaleFactor = 1d;
                    if ((layout.PlotLayoutFlags & PlotLayoutFlags.UseStandardScale) == 0 && (layout.CustomPrintScaleNumerator != 0d && layout.CustomPrintScaleDenominator != 0d)) {
                        customScaleFactor = layout.CustomPrintScaleNumerator / layout.CustomPrintScaleDenominator;
                    }
                    bounds.Update((Point3D)(Vector3D)((Vector2D)plotAreaBounds.Min / customScaleFactor));
                    bounds.Update((Point3D)(Vector3D)((Vector2D)plotAreaBounds.Max / customScaleFactor));

                    if (layout.PlotArea == PlotArea.LayoutInformation) {
                        switch (layout.PlotPaperUnits) {
                            case PlotPaperUnits.Millimeters:
                                paperSize = new PaperSize(Guid.NewGuid().ToString(), (int)(plotAreaBounds.Delta.X * 100d / 25.4d), (int)(plotAreaBounds.Delta.Y * 100d / 25.4d));
                                break;
                            case PlotPaperUnits.Inches:
                                paperSize = new PaperSize(Guid.NewGuid().ToString(), (int)(plotAreaBounds.Delta.X * 100d), (int)(plotAreaBounds.Delta.Y * 100d));
                                break;
                            case PlotPaperUnits.Pixels:
                                // No physical paper units. Fall back to fitting layout into a known paper size.
                                break;
                        }
                    }

                    if (paperSize == null) {
                        if (bounds.Delta.X > bounds.Delta.Y) {
                            paperSize = PaperSizes.GetPaperSize(PaperKind.A4Rotated);
                        } else {
                            paperSize = PaperSizes.GetPaperSize(PaperKind.A4);
                        }
                        margin = defaultMargin;
                    }
                }
            }

            if (!emptyLayout) {
                svgExporter.PaperSize = paperSize;

                // Lengths in inches.
                float pageWidthInInches = paperSize.Width / 100f;
                float pageHeightInInches = paperSize.Height / 100f;

                double scaleFactor;
                Matrix4D to2DTransform;

                // SvgExporter is in paper mode, so SVG units are in 100ths of inch.
                const double inchToHundredthCm = 2.54 * 100;

                if (useModelView) {
                    to2DTransform = modelView.GetMappingTransform(
                        new Rectangle2D(
                            margin * inchToHundredthCm,
                            margin * inchToHundredthCm,
                            (pageWidthInInches - margin) * inchToHundredthCm,
                            (pageHeightInInches - margin) * inchToHundredthCm),
                        true);
                    scaleFactor = double.NaN; // Not needed for model space.
                } else {
                    to2DTransform = DxfUtil.GetScaleTransform(
                        bounds.Corner1,
                        bounds.Corner2,
                        new Point3D(bounds.Center.X, bounds.Corner2.Y, 0d),
                        new Point3D(new Vector3D(margin, pageHeightInInches - margin, 0d) * inchToHundredthCm),
                        new Point3D(new Vector3D(pageWidthInInches - margin, margin, 0d) * inchToHundredthCm),
                        new Point3D(new Vector3D(pageWidthInInches / 2d, margin, 0d) * inchToHundredthCm),
                        out scaleFactor
                        );
                }
                if (layout == null || !layout.PaperSpace) {
                    svgExporter.Draw(model, config, to2DTransform);
                } else {
                    svgExporter.Draw(model, layout, null, config, to2DTransform, scaleFactor);
                }
            }
        }
    }
}
