﻿using System;
using System.Collections.Generic;

using System.Drawing.Printing;
using WW.Cad.Drawing;
using WW.Drawing.Printing;

namespace WW.Cad.Examples {
    public class SvgExportOptions : PlotOptions {
        public static readonly new SvgExportOptions Default = new SvgExportOptions();

        #region optional options
        public string OutputFilename { get; set; }

        public string LayoutName { get; set; }
        public int LayoutIndex { get; set; } = -1;

        public PaperKind ModelSpacePaperKind { get; set; } = PaperKind.Letter;
        public Orientation ModelSpaceOrientation { get; set; } = Orientation.Auto;

        // Paper space paper kind/orientation in case not defined by the paper space layout itself.
        // Normally never used.
        public PaperKind PaperSpaceDefaultPaperKind { get; set; } = PaperKind.Letter;
        public Orientation PaperSpaceDefaultOrientation { get; set; } = Orientation.Auto;
        #endregion
    }
}