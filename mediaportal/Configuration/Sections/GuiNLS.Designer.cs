namespace MediaPortal.Configuration.Sections
{
    partial class GuiNLS
    {
        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mpLabel1 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel2 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel3 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel4 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel5 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel6 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.numNlsCenterZone = new MediaPortal.UserInterface.Controls.MPNumericUpDown();
            this.numNlsZoom = new MediaPortal.UserInterface.Controls.MPNumericUpDown();
            this.numNlsStretchX = new MediaPortal.UserInterface.Controls.MPNumericUpDown();
            this.mpLabel7 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.ctlPreview = new System.Windows.Forms.PictureBox();
            this.numNlsVertPos = new MediaPortal.UserInterface.Controls.MPNumericUpDown();
            this.mpLabel8 = new MediaPortal.UserInterface.Controls.MPLabel();
            this.mpLabel9 = new MediaPortal.UserInterface.Controls.MPLabel();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsCenterZone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsStretchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ctlPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsVertPos)).BeginInit();
            this.SuspendLayout();
            // 
            // mpLabel1
            // 
            this.mpLabel1.Location = new System.Drawing.Point(3, 46);
            this.mpLabel1.Name = "mpLabel1";
            this.mpLabel1.Size = new System.Drawing.Size(269, 32);
            this.mpLabel1.TabIndex = 3;
            this.mpLabel1.Text = "Center zone - this is the portion of the original image that will remain unstretc" +
                "hed";
            // 
            // mpLabel2
            // 
            this.mpLabel2.Location = new System.Drawing.Point(3, 78);
            this.mpLabel2.Name = "mpLabel2";
            this.mpLabel2.Size = new System.Drawing.Size(269, 32);
            this.mpLabel2.TabIndex = 4;
            this.mpLabel2.Text = "Zoom - enlarge the entire image. Reduces non-liear portions and causes cropping. " +
                "";
            // 
            // mpLabel3
            // 
            this.mpLabel3.Location = new System.Drawing.Point(3, 110);
            this.mpLabel3.Name = "mpLabel3";
            this.mpLabel3.Size = new System.Drawing.Size(269, 32);
            this.mpLabel3.TabIndex = 5;
            this.mpLabel3.Text = "X-stretch - add extra horizontal stretch to the image, in order to reduce the non" +
                "-linear portions.";
            // 
            // mpLabel4
            // 
            this.mpLabel4.AutoSize = true;
            this.mpLabel4.Location = new System.Drawing.Point(328, 48);
            this.mpLabel4.Name = "mpLabel4";
            this.mpLabel4.Size = new System.Drawing.Size(15, 13);
            this.mpLabel4.TabIndex = 12;
            this.mpLabel4.Text = "%";
            // 
            // mpLabel5
            // 
            this.mpLabel5.AutoSize = true;
            this.mpLabel5.Location = new System.Drawing.Point(328, 112);
            this.mpLabel5.Name = "mpLabel5";
            this.mpLabel5.Size = new System.Drawing.Size(15, 13);
            this.mpLabel5.TabIndex = 13;
            this.mpLabel5.Text = "%";
            // 
            // mpLabel6
            // 
            this.mpLabel6.AutoSize = true;
            this.mpLabel6.Location = new System.Drawing.Point(328, 80);
            this.mpLabel6.Name = "mpLabel6";
            this.mpLabel6.Size = new System.Drawing.Size(15, 13);
            this.mpLabel6.TabIndex = 14;
            this.mpLabel6.Text = "%";
            // 
            // numNlsCenterZone
            // 
            this.numNlsCenterZone.Location = new System.Drawing.Point(278, 46);
            this.numNlsCenterZone.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.numNlsCenterZone.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numNlsCenterZone.Name = "numNlsCenterZone";
            this.numNlsCenterZone.Size = new System.Drawing.Size(47, 20);
            this.numNlsCenterZone.TabIndex = 15;
            this.numNlsCenterZone.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numNlsCenterZone.ValueChanged += new System.EventHandler(this.numNlsCenterZone_ValueChanged);
            // 
            // numNlsZoom
            // 
            this.numNlsZoom.Location = new System.Drawing.Point(278, 78);
            this.numNlsZoom.Maximum = new decimal(new int[] {
            133,
            0,
            0,
            0});
            this.numNlsZoom.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numNlsZoom.Name = "numNlsZoom";
            this.numNlsZoom.Size = new System.Drawing.Size(47, 20);
            this.numNlsZoom.TabIndex = 16;
            this.numNlsZoom.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numNlsZoom.ValueChanged += new System.EventHandler(this.numNlsZoom_ValueChanged);
            this.numNlsZoom.Validating += new System.ComponentModel.CancelEventHandler(this.numNlsZoom_Validating);
            // 
            // numNlsStretchX
            // 
            this.numNlsStretchX.Location = new System.Drawing.Point(278, 110);
            this.numNlsStretchX.Maximum = new decimal(new int[] {
            133,
            0,
            0,
            0});
            this.numNlsStretchX.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numNlsStretchX.Name = "numNlsStretchX";
            this.numNlsStretchX.Size = new System.Drawing.Size(47, 20);
            this.numNlsStretchX.TabIndex = 17;
            this.numNlsStretchX.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numNlsStretchX.ValueChanged += new System.EventHandler(this.numNlsStretchX_ValueChanged);
            this.numNlsStretchX.Validating += new System.ComponentModel.CancelEventHandler(this.numNlsStretchX_Validating);
            // 
            // mpLabel7
            // 
            this.mpLabel7.Location = new System.Drawing.Point(3, 4);
            this.mpLabel7.Name = "mpLabel7";
            this.mpLabel7.Size = new System.Drawing.Size(340, 39);
            this.mpLabel7.TabIndex = 18;
            this.mpLabel7.Text = "NLS can be used to scale a 4:3 image to a 16:9 screen while retaining most of the" +
                " viewing quality.";
            // 
            // ctlPreview
            // 
            this.ctlPreview.BackColor = System.Drawing.Color.White;
            this.ctlPreview.InitialImage = null;
            this.ctlPreview.Location = new System.Drawing.Point(75, 189);
            this.ctlPreview.Name = "ctlPreview";
            this.ctlPreview.Size = new System.Drawing.Size(200, 150);
            this.ctlPreview.TabIndex = 19;
            this.ctlPreview.TabStop = false;
            this.ctlPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.ctlPreview_Paint);
            // 
            // numNlsVertPos
            // 
            this.numNlsVertPos.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numNlsVertPos.Location = new System.Drawing.Point(278, 142);
            this.numNlsVertPos.Name = "numNlsVertPos";
            this.numNlsVertPos.Size = new System.Drawing.Size(47, 20);
            this.numNlsVertPos.TabIndex = 22;
            this.numNlsVertPos.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numNlsVertPos.ValueChanged += new System.EventHandler(this.numNlsVertPos_ValueChanged);
            // 
            // mpLabel8
            // 
            this.mpLabel8.AutoSize = true;
            this.mpLabel8.Location = new System.Drawing.Point(328, 144);
            this.mpLabel8.Name = "mpLabel8";
            this.mpLabel8.Size = new System.Drawing.Size(15, 13);
            this.mpLabel8.TabIndex = 21;
            this.mpLabel8.Text = "%";
            // 
            // mpLabel9
            // 
            this.mpLabel9.Location = new System.Drawing.Point(3, 142);
            this.mpLabel9.Name = "mpLabel9";
            this.mpLabel9.Size = new System.Drawing.Size(269, 32);
            this.mpLabel9.TabIndex = 20;
            this.mpLabel9.Text = "Vertical position - positions the zoomed image vertically. Use this to control cr" +
                "opping of subtitles.";
            // 
            // GeneralNLS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numNlsVertPos);
            this.Controls.Add(this.mpLabel8);
            this.Controls.Add(this.mpLabel9);
            this.Controls.Add(this.ctlPreview);
            this.Controls.Add(this.mpLabel7);
            this.Controls.Add(this.numNlsStretchX);
            this.Controls.Add(this.numNlsZoom);
            this.Controls.Add(this.numNlsCenterZone);
            this.Controls.Add(this.mpLabel6);
            this.Controls.Add(this.mpLabel5);
            this.Controls.Add(this.mpLabel4);
            this.Controls.Add(this.mpLabel3);
            this.Controls.Add(this.mpLabel2);
            this.Controls.Add(this.mpLabel1);
            this.Name = "GeneralNLS";
            this.Size = new System.Drawing.Size(350, 349);
            ((System.ComponentModel.ISupportInitialize)(this.numNlsCenterZone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsStretchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ctlPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNlsVertPos)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        void numNlsVertPos_ValueChanged(object sender, System.EventArgs e)
        {
            ctlPreview.Invalidate();
        }

        void numNlsCenterZone_ValueChanged(object sender, System.EventArgs e)
        {
            ctlPreview.Invalidate();
        }

        void ctlPreview_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            UpdatePreview(sender, e);
        }

        void numNlsStretchX_ValueChanged(object sender, System.EventArgs e)
        {
            float f = (float)numNlsStretchX.Value / 100.0f * (float)numNlsZoom.Value / 100.0f;
            ctlPreview.Invalidate();
            if (f > 4.0f / 3.0f)
            {
                numNlsStretchX.BackColor = System.Drawing.Color.Red;
            }
            else
            {
                numNlsStretchX.BackColor = System.Drawing.Color.White;
            }
        }

        void numNlsZoom_ValueChanged(object sender, System.EventArgs e)
        {
            ctlPreview.Invalidate();
            float f = (float)numNlsStretchX.Value / 100.0f * (float)numNlsZoom.Value / 100.0f;
            if (f > 4.0f / 3.0f)
            {
                numNlsZoom.BackColor = System.Drawing.Color.Red;
            }
            else
            {
                numNlsZoom.BackColor = System.Drawing.Color.White;
            }
        }

        void numNlsStretchX_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            float f = (float)numNlsStretchX.Value / 100.0f * (float)numNlsZoom.Value / 100.0f;
            if (f > 4.0f / 3.0f)
            {
                e.Cancel = true;
            }
        }

        void numNlsZoom_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            float f = (float)numNlsStretchX.Value/100.0f * (float)numNlsZoom.Value/100.0f;
            if (f > 4.0f / 3.0f)
            {
                e.Cancel = true;
            }
        }

        #endregion

        private MediaPortal.UserInterface.Controls.MPLabel mpLabel1;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel2;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel3;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel4;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel5;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel6;
        private MediaPortal.UserInterface.Controls.MPNumericUpDown numNlsCenterZone;
        private MediaPortal.UserInterface.Controls.MPNumericUpDown numNlsZoom;
        private MediaPortal.UserInterface.Controls.MPNumericUpDown numNlsStretchX;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel7;
        private System.Windows.Forms.PictureBox ctlPreview;
        private MediaPortal.UserInterface.Controls.MPNumericUpDown numNlsVertPos;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel8;
        private MediaPortal.UserInterface.Controls.MPLabel mpLabel9;
    }
}
