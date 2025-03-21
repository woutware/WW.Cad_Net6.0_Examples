using System;
using System.Collections.Generic;
#if !MULTIPLATFORM
using System.Drawing.Printing;
#endif
using System.IO;

using WW.Cad.Base;
using WW.Cad.Drawing;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Entities;
using WW.Cad.Model.Objects;
using WW.Cad.Model.Tables;
#if MULTIPLATFORM
using WW.Drawing.Printing;
#endif
using WW.Math;
using WW.Math.Geometry;

namespace WW.Cad.Examples {
    // This class demonstrates how to export an AutoCAD file to SVG (both model space and paper space layouts).
    public class SvgExporterExample {
        // Exports an AutoCAD file to SVG. For each layout a page in the SVG file is created.
        public static void ExportToSvg(string filename, SvgExportOptions options = null) {
            DxfModel model = CadReader.Read(filename, true);
            model.LoadExternalReferences();
            ExportToSvg(model, options);
        }

        // Exports the specified layout of an AutoCAD file to SVG.
        public static void ExportToSvg(DxfModel model, SvgExportOptions options = null) {
            if (options == null) {
                options = SvgExportOptions.Default;
            }
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            // as SVG
            string outputFilename = GetOutputFilename(options, dir, filenameNoExt);
            using (FileStream stream = File.Create(outputFilename)) {
                WW.Cad.IO.SvgExporter svgExporter = new WW.Cad.IO.SvgExporter(stream);

                AddLayoutToSvgExporter(svgExporter, model, null, options);
            }
        }

        // For each layout, add a page to the SVG file.
        // Optionally specify a modelView (for model space only).
        // Optionally specify a layout.
        private static void AddLayoutToSvgExporter(
            WW.Cad.IO.SvgExporter svgExporter, DxfModel model, DxfView modelView, SvgExportOptions options = null
        ) {
            if (options == null) {
                options = SvgExportOptions.Default;
            }
            Bounds3D bounds;
            const float defaultMargin = 0.5f;
            float margin = 0f;
            PaperSize paperSize = null;
            bool useModelView = false;
            bool emptyLayout = false;
            DxfLayout layout;
            if (options.Layout != null) {
                layout = options.Layout;
            } else {
                layout = model.Header.ShowModelSpace ? model.ModelLayout : model.ActiveLayout;
            }
            if (!layout.PaperSpace) {
                // Model space.
                BoundsCalculator boundsCalculator = new BoundsCalculator();
                boundsCalculator.GetBounds(model);
                bounds = boundsCalculator.Bounds;
                if (bounds.Initialized) {
                    paperSize = GetPaperSize(bounds, options.ModelSpacePaperKind, options.ModelSpaceOrientation);
                } else {
                    emptyLayout = true;
                }
                margin = defaultMargin;
                useModelView = modelView != null;
            } else {
                // Paper space layout.
                Bounds2D plotAreaBounds = layout.GetPlotAreaBounds(options.GetPlotArea);
                bounds = new Bounds3D();
                emptyLayout = !plotAreaBounds.Initialized;
                if (plotAreaBounds.Initialized) {
                    double customScaleFactor = 1d;
                    if (
                        (layout.PlotLayoutFlags & PlotLayoutFlags.UseStandardScale) == 0 &&
                        (layout.PlotArea == PlotArea.LayoutInformation) &&
                        (layout.CustomPrintScaleNumerator != 0d && layout.CustomPrintScaleDenominator != 0d)
                    ) {
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
                        paperSize = GetPaperSize(bounds, options.PaperSpaceDefaultPaperKind, options.PaperSpaceDefaultOrientation);
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
                    svgExporter.Draw(model, options.GraphicsConfig, to2DTransform);
                } else {
                    svgExporter.Draw(model, layout, null, options.GraphicsConfig, to2DTransform, scaleFactor);
                }
            }
        }

        private static string GetOutputFilename(SvgExportOptions options, string dir, string filenameNoExt) {
            string outputFilename = options.OutputFilename;
            if (string.IsNullOrEmpty(outputFilename)) {
                outputFilename = Path.Combine(dir, filenameNoExt + ".svg");
                options.OutputFilename = outputFilename;
            }
            return outputFilename;
        }

        private static PaperSize GetPaperSize(Bounds3D bounds, PaperKind paperKind, PlotOrientation orientation) {
            PaperSize paperSize = PaperSizes.GetPaperSize(paperKind);
            if (orientation == PlotOrientation.Auto) {
                if (bounds.Delta.X > bounds.Delta.Y) {
                    paperSize = new PaperSize($"{paperSize.PaperName}, rotated", paperSize.Height, paperSize.Width);
                }
            } else if (orientation == PlotOrientation.Portrait) {
                if (paperSize.Width > paperSize.Height) {
                    paperSize = new PaperSize($"{paperSize.PaperName}", paperSize.Height, paperSize.Width);
                }
            } else if (orientation == PlotOrientation.Landscape) {
                if (paperSize.Width < paperSize.Height) {
                    paperSize = new PaperSize($"{paperSize.PaperName}", paperSize.Height, paperSize.Width);
                }
            }
            return paperSize;
        }
    }
}
