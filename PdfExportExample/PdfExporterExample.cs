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
using WW.Drawing.Printing;

#if NETCORE
using WW.Drawing.Printing;
#endif
using WW.Math;
using WW.Math.Geometry;

namespace WW.Cad.Examples {
    // This class demonstrates how to export an AutoCAD file to PDF (both model space and paper space layouts).
    public class PdfExporterExample {
        // Exports an AutoCAD file to PDF. For each layout a page in the PDF file is created.
        public static void ExportToPdf(string filename, PlotOptions options = null) {
            DxfModel model = CadReader.Read(filename);
            model.LoadExternalReferences();
            ExportToPdf(model, options);
        }

        // Exports an AutoCAD file to PDF. For each layout a page in the PDF file is created.
        public static void ExportToPdf(DxfModel model, PlotOptions options = null) {
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            // as PDF
            using (FileStream stream = File.Create(Path.Combine(dir, filenameNoExt + ".pdf"))) {
                PdfExporter pdfExporter = new PdfExporter(stream);
                pdfExporter.EmbedFonts = true;

                GraphicsConfig config = GetGraphicsConfig(options);

                foreach (DxfLayout layout in model.OrderedLayouts) {
                    AddLayoutToPdfExporter(pdfExporter, options, config, model, layout);
                }
                pdfExporter.EndDocument();
            }
        }

        // Exports the specified layout of an AutoCAD file to PDF.
        public static void ExportToPdf(DxfModel model, DxfLayout layout, PlotOptions options = null) {
            string filename = Path.GetFileName(model.Filename);
            string dir = Path.GetDirectoryName(model.Filename);
            string filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            // as PDF
            using (FileStream stream = File.Create(Path.Combine(dir, filenameNoExt + "-" + layout.Name + ".pdf"))) {
                PdfExporter pdfExporter = new PdfExporter(stream);
                pdfExporter.EmbedFonts = true;

                GraphicsConfig config = GetGraphicsConfig(options);

                AddLayoutToPdfExporter(pdfExporter, options, config, model, layout);

                pdfExporter.EndDocument();
            }
        }

        // For each layout, add a page to the PDF file.
        // Optionally specify a layout.
        // Parameter options may be null, in which case PlotOptions.Default is used.
        public static void AddLayoutToPdfExporter(
            PdfExporter pdfExporter,
            PlotOptions options,
            GraphicsConfig config,
            DxfModel model,
            DxfLayout layout
        ) {
            Bounds3D bounds = null;
            const float defaultMargin = 0.5f;
            float margin = 0f;
            PaperSize paperSize = null;
            bool useModelView = false;
            bool emptyLayout = false;
            Matrix4D modelTransform = Matrix4D.Identity;
            DxfVPort activeVPort = null;

            if (options == null) {
                options = PlotOptions.Default;
            }
            if (layout == null) {
                layout = model.ModelLayout;
            }
            var modelSpacePaperSize = options.ModelSpacePaperSize;
            if (!layout.PaperSpace) {
                // Model space.
                activeVPort = model.VPorts.GetActiveVPort();

                if (activeVPort != null && options.WhenHasActiveVportFitBoundsToPaperSize) {
                    modelTransform = activeVPort.GetTransform(new Size2D(1, 1));
                    activeVPort = null;
                }
                if (activeVPort != null) {
                    paperSize = modelSpacePaperSize;
                } else {
                    BoundsCalculator boundsCalculator = new BoundsCalculator();
                    boundsCalculator.GetBounds(model, modelTransform);
                    bounds = boundsCalculator.Bounds;

                    if (bounds.Initialized) {
                        // If modelSpacePaperSize is not set use paper size information in the model layout if present.
                        if (modelSpacePaperSize == null && layout.PlotPaperSize != Size2D.Zero) {
                            switch (layout.PlotRotation) {
                                case PlotRotation.None:
                                case PlotRotation.Half:
                                    modelSpacePaperSize = new PaperSize(
                                        layout.PaperSizeName,
                                        (int)System.Math.Round(layout.PlotPaperSize.X * 100d / 25.4d),
                                        (int)System.Math.Round(layout.PlotPaperSize.Y * 100d / 25.4d)
                                    );
                                    break;
                                default:
                                    modelSpacePaperSize = new PaperSize(
                                        layout.PaperSizeName,
                                        (int)System.Math.Round(layout.PlotPaperSize.Y * 100d / 25.4d),
                                        (int)System.Math.Round(layout.PlotPaperSize.X * 100d / 25.4d)
                                    );
                                    break;
                            }
                            paperSize = modelSpacePaperSize;
                        } else {
                            if (modelSpacePaperSize == null) {
                                modelSpacePaperSize = PaperSizes.GetPaperSize(PaperKind.A4);
                            }
                            if (bounds.Delta.X > bounds.Delta.Y) {
                                paperSize = modelSpacePaperSize;
                            } else {
                                paperSize = new PaperSize(modelSpacePaperSize.PaperName, modelSpacePaperSize.Height, modelSpacePaperSize.Width);
                            }
                        }
                    } else {
                        emptyLayout = true;
                    }
                }

                margin = defaultMargin;
                useModelView = options.ModelView != null;
            } else {
                if (layout.PlotPaperSize == Size2D.Zero) {
                    layout.PlotPaperSize = new Size2D(297, 210); // A4 size in mm.
                }
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
                // Lengths in inches.
                float pageWidthInInches = paperSize.Width / 100f;
                float pageHeightInInches = paperSize.Height / 100f;

                double scaleFactor;
                Matrix4D to2DTransform;
                if (useModelView) {
                    to2DTransform = options.ModelView.GetMappingTransform(
                        new Rectangle2D(
                            margin * PdfExporter.InchToPixel,
                            margin * PdfExporter.InchToPixel,
                            (pageWidthInInches - margin) * PdfExporter.InchToPixel,
                            (pageHeightInInches - margin) * PdfExporter.InchToPixel),
                        false) * modelTransform;
                    scaleFactor = double.NaN; // Not needed for model space.
                } else if (activeVPort != null) {
                    to2DTransform = activeVPort.GetTransform(
                        new Size2D(pageWidthInInches * PdfExporter.InchToPixel, pageHeightInInches * PdfExporter.InchToPixel),
                        margin * PdfExporter.InchToPixel);
                    scaleFactor = double.NaN; // Not needed for model space.
                } else {
                    to2DTransform = DxfUtil.GetScaleTransform(
                        bounds.Corner1,
                        bounds.Corner2,
                        new Point3D(bounds.Center.X, bounds.Corner2.Y, 0d),
                        new Point3D(new Vector3D(margin, margin, 0d) * PdfExporter.InchToPixel),
                        new Point3D(new Vector3D(pageWidthInInches - margin, pageHeightInInches - margin, 0d) * PdfExporter.InchToPixel),
                        new Point3D(new Vector3D(pageWidthInInches / 2d, pageHeightInInches - margin, 0d) * PdfExporter.InchToPixel),
                        out scaleFactor
                    ) * modelTransform;
                }
                if (layout == null || !layout.PaperSpace) {
                    pdfExporter.DrawPage(model, config, to2DTransform, paperSize, ReportProgress);
                } else {
                    pdfExporter.DrawPage(model, config, to2DTransform, scaleFactor, layout, null, paperSize, ReportProgress);
                }
            }
        }

        private static GraphicsConfig GetGraphicsConfig(PlotOptions options) {
            GraphicsConfig config = (GraphicsConfig)GraphicsConfig.WhiteBackgroundCorrectForBackColor.Clone();
            config.DisplayLineTypeElementShapes = true;
            config.TryDrawingTextAsText = true;
            if (options != null) {
                config.PlotStyleManager = options.PlotStyleProvider;
            }
            return config;
        }

        private static void ReportProgress(object sender, ProgressEventArgs e) {
            Console.WriteLine("{0}%", (int)System.Math.Round(e.Progress * 100d));
        }
    }
}
