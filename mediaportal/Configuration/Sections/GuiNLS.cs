using MediaPortal.Profile;
using System;
using System.Drawing;

namespace MediaPortal.Configuration.Sections
{
    public partial class GuiNLS : SectionSettings
    {
        #region ctor

        public GuiNLS()
            : this("Non-linear stretch")
        {
        }

        public GuiNLS(string name)
            : base("Non-linear stretch")
        {
            InitializeComponent();
        }

        #endregion

        #region Persistance

        public override void LoadSettings()
        {
            using (Settings xmlreader = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                numNlsCenterZone.Value = Convert.ToInt16(xmlreader.GetValueAsInt("nls", "center zone", 50));
                numNlsZoom.Value = Convert.ToInt16(xmlreader.GetValueAsInt("nls", "zoom", 115));
                numNlsStretchX.Value = Convert.ToInt16(xmlreader.GetValueAsInt("nls", "stretchx", 103));
                numNlsVertPos.Value = Convert.ToInt16(xmlreader.GetValueAsInt("nls", "vertpos", 30));
            }
        }

        public override void SaveSettings()
        {
            using (Settings xmlreader = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                xmlreader.SetValue("nls", "center zone", numNlsCenterZone.Value);
                xmlreader.SetValue("nls", "zoom", numNlsZoom.Value);
                xmlreader.SetValue("nls", "stretchx", numNlsStretchX.Value);
                xmlreader.SetValue("nls", "vertpos", numNlsVertPos.Value);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void UpdatePreview(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            int w = ctlPreview.Width;
            int h = ctlPreview.Height;
            float margX = (float)w / 10.0f;
            float margY = (float)h / 10.0f;
            float w16 = (float)w * 0.8f;
            float h16 = w16 / 16.0f * 9.0f;
            float w4 = h16 / 3.0f * 4.0f * (float)numNlsZoom.Value / 100.0f;
            float h4 = h16 * (float)numNlsZoom.Value / 100.0f;
            float wCenter4 = w4 * (float)numNlsCenterZone.Value / 100.0f ;
            float wCenterOrig4 = h16 / 3.0f * 4.0f * (float)numNlsCenterZone.Value / 100.0f;
            float wCenter16 = w4 * (float)numNlsCenterZone.Value / 100.0f * (float)numNlsStretchX.Value / 100.0f;
            float vertOff = (h4 - h16) * ((float)numNlsVertPos.Value /100.0f - 0.5f);

            e.Graphics.Clear(Color.White);

            PointF[] p = new PointF[4];

            // draw gradients

            p[0] = new PointF((w / 2 - w16 / 2), (h / 2 - h16 / 2));
            p[1] = new PointF((w / 2 - wCenter16 / 2), (h / 2 - h16 / 2));
            p[2] = new PointF((w / 2 - wCenter16 / 2), (h / 2 + h16 / 2));
            p[3] = new PointF((w / 2 - w16 / 2), (h / 2 + h16 / 2));

            System.Drawing.Drawing2D.LinearGradientBrush brOrangeGradientL = 
                new System.Drawing.Drawing2D.LinearGradientBrush(
                    new RectangleF(p[1], new SizeF((w16 - wCenterOrig4)/2, p[2].Y - p[0].Y)), 
                    Color.Orange, Color.White, 180, false);
            e.Graphics.FillPolygon(brOrangeGradientL, p);

            p[0] = new PointF((w / 2 + w16 / 2), (h / 2 - h16 / 2));
            p[1] = new PointF((w / 2 + wCenter16 / 2), (h / 2 - h16 / 2));
            p[2] = new PointF((w / 2 + wCenter16 / 2), (h / 2 + h16 / 2));
            p[3] = new PointF((w / 2 + w16 / 2), (h / 2 + h16 / 2));

            System.Drawing.Drawing2D.LinearGradientBrush brOrangeGradientR =
                new System.Drawing.Drawing2D.LinearGradientBrush(
                    new RectangleF(p[1], new SizeF((w16 - wCenterOrig4) / 2, p[2].Y - p[0].Y)),
                    Color.Orange, Color.White, 0, false);
            e.Graphics.FillPolygon(brOrangeGradientR, p);

            // draw 16:9 center zone

            p[0] = new PointF((w / 2 - wCenter16 / 2), (h / 2 - h16 / 2));
            p[1] = new PointF((w / 2 + wCenter16 / 2), (h / 2 - h16 / 2));
            p[2] = new PointF((w / 2 + wCenter16 / 2), (h / 2 + h16 / 2));
            p[3] = new PointF((w / 2 - wCenter16 / 2), (h / 2 + h16 / 2));

            e.Graphics.FillPolygon(Brushes.Orange, p);
            e.Graphics.DrawPolygon(Pens.Gold, p);

            // draw 4:3 center zone

            p[0] = new PointF((w / 2 - wCenter4 / 2), (h / 2 - h4 / 2) + vertOff);
            p[1] = new PointF((w / 2 + wCenter4 / 2), (h / 2 - h4 / 2) + vertOff);
            p[2] = new PointF((w / 2 + wCenter4 / 2), (h / 2 + h4 / 2) + vertOff);
            p[3] = new PointF((w / 2 - wCenter4 / 2), (h / 2 + h4 / 2) + vertOff);

            System.Drawing.Drawing2D.HatchBrush brGrayHatch =
                new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.BackwardDiagonal, Color.LightGray, Color.Transparent);
            e.Graphics.FillPolygon(brGrayHatch, p);

            // draw 4:3 rect

            p[0] = new PointF((w / 2 - w4 / 2), (h / 2 - h4 / 2) + vertOff);
            p[1] = new PointF((w / 2 + w4 / 2), (h / 2 - h4 / 2) + vertOff);
            p[2] = new PointF((w / 2 + w4 / 2), (h / 2 + h4 / 2) + vertOff);
            p[3] = new PointF((w / 2 - w4 / 2), (h / 2 + h4 / 2) + vertOff);

            e.Graphics.DrawPolygon(Pens.LightGray, p);

            // draw 16:9 rect

            p[0] = new PointF((w / 2 - w16 / 2), (h / 2 - h16 / 2));
            p[1] = new PointF((w / 2 + w16 / 2), (h / 2 - h16 / 2));
            p[2] = new PointF((w / 2 + w16 / 2), (h / 2 + h16 / 2));
            p[3] = new PointF((w / 2 - w16 / 2), (h / 2 + h16 / 2));

            e.Graphics.DrawPolygon(Pens.Red, p);

        }
    }
}
