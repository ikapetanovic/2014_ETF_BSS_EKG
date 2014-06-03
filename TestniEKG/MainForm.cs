using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Collections;

using GraphLib;
using TestniEKG;

namespace TestniEKG
{
    public partial class MainForm : Form
    {

        private static List<double> napon;
        private static List<double> vrijeme;
        
        private static System.Threading.Timer timer;

        private int NumGraphs = 1;        
        private String CurExample = "TILED_VERTICAL";
        private String CurColorSchema = "WHITE";
        private PrecisionTimer.Timer mTimer = null;
        private DateTime lastTimerTick = DateTime.Now;

        public MainForm()
        {            
            InitializeComponent();

            napon = new List<double>();
            vrijeme = new List<double>();

            display.Smoothing = System.Drawing.Drawing2D.SmoothingMode.None;
            CalcDataGraphs();
            display.Refresh();
            UpdateGraphCountMenu();
            UpdateColorSchemaMenu();

            mTimer = new PrecisionTimer.Timer();
            mTimer.Period = 40;                         // 20 fps
            mTimer.Tick += new EventHandler(OnTimerTick);
            lastTimerTick = DateTime.Now;
            mTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            mTimer.Stop();
            mTimer.Dispose();
            base.OnClosed(e);
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (CurExample == "ANIMATED_AUTO")
            {
                try
                {
                    TimeSpan dt = DateTime.Now - lastTimerTick;

                    for (int j = 0; j < NumGraphs; j++)
                    {

                        CalcSinusFunction_3(display.DataSources[j], j, (float)dt.TotalMilliseconds);

                    }

                    this.Invoke(new MethodInvoker(RefreshGraph));
                }
                catch (ObjectDisposedException ex)
                {
                    // we get this on closing of form
                }
                catch (Exception ex)
                {
                    Console.Write("exception invoking refreshgraph(): " + ex.Message);
                }


            }
        }

        void IspisiPodatke(DataSource src, int idx, List<double> vrijeme, List<double> napon)
        {
            // for (int i = 0; i < src.Length; i++)
            for (int i = 0; i < vrijeme.Count; i++)
            {
                src.Samples[i].x = (float)vrijeme[i];
                src.Samples[i].y = (float)napon[i];
            }
            src.OnRenderYAxisLabel = RenderYLabel;
        }                

        private String RenderXLabel(DataSource s, int idx)
        {
            if (s.AutoScaleX)
            {
                //if (idx % 2 == 0)
                {
                    double Value = (s.Samples[idx].x);
                    return "" + Value;
                }
                //return "";
            }
            else
            {
                //int value = (int)(s.Samples[idx].x / 200);
                double value = (s.Samples[idx].x); // /2000
                return String.Format("{0:0}", (float)value);
            }
        }

        private String RenderYLabel(DataSource s, float value)
        {
            return String.Format("{0:0.0}", (float)value/10); // /100
        }

               
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MIT Arrhythmia File (*.txt, *.dat)|*.txt; *.dat";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string filePath = ofd.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                string fileExtension = System.IO.Path.GetExtension(filePath);
                string directoryPath = System.IO.Path.GetDirectoryName(filePath);

                EKGFileType tip = EKGFileType.BINARY;
                if (fileExtension == ".txt" || fileExtension == ".TXT")
                    tip = EKGFileType.TEXT;

                DajVrijeme(filePath, tip);
                DajNapon(filePath, tip);

                CalcDataGraphs();
            }
        }

        public void DajVrijeme(string filePath, EKGFileType type)
        {
            timer = new System.Threading.Timer(new TimerCallback(tick0));
            try
            {
                UlazniBuffer.Open(filePath, 0, type);
                timer.Change(0, 30);
                Thread.Sleep(10000);
            }
            catch (Exception izuzetak)
            {
                MessageBox.Show(izuzetak.ToString());
            }
        }

        public void DajNapon(string filePath, EKGFileType type)
        {
            timer = new System.Threading.Timer(new TimerCallback(tick1));
            try
            {
                UlazniBuffer.Open(filePath, 1, type);
                timer.Change(0, 30);
                Thread.Sleep(10000);
            }
            catch (Exception izuzetak)
            {
                MessageBox.Show(izuzetak.ToString());
            }
        }

        public void tick0(object o) //static
        {
            
            //System.Console.WriteLine("tick");
            double[] signal = new double[100]; //10
            for (int i = 0; i < 100; i++) // 10
            {
                while (!UlazniBuffer.ReadOne(out signal[i])) ;
                //Console.Write("3");

                if (signal[i] == Double.PositiveInfinity)
                {
                    //stop the program
                    timer.Change(Timeout.Infinite, 1);
                    UlazniBuffer.Clear();
                    //System.Console.WriteLine("Exiting");
                    return;
                }

                vrijeme.Add(signal[i]*1000);
                //napon.Add(signal[i]*1000);
            } 
        }

        public void tick1(object o) //static
        {

            //System.Console.WriteLine("tick");
            double[] signal = new double[100]; //10
            for (int i = 0; i < 100; i++) // 10
            {
                while (!UlazniBuffer.ReadOne(out signal[i])) ;
                //Console.Write("3");

                if (signal[i] == Double.PositiveInfinity)
                {
                    //stop the program
                    timer.Change(Timeout.Infinite, 1);
                    UlazniBuffer.Clear();
                    //System.Console.WriteLine("Exiting");
                    return;
                }

                napon.Add(signal[i] * 1000);
            }
        }

        public static void tickmany(object o)
        {
            //System.Console.WriteLine("tick");
            double[] signal = new double[10];
            while (!UlazniBuffer.ReadMany(out signal, 10)) ;
            for (int i = 0; i < 10; i++)
            {
                //Console.Write("3");
                if (signal[i] == Double.PositiveInfinity)
                {
                    //stop the program
                    timer.Change(Timeout.Infinite, 1);
                    UlazniBuffer.Clear();
                    //System.Console.WriteLine("Exiting");
                    return;
                }
                else
                    MessageBox.Show(signal[i].ToString());
            }
        }

        protected void CalcDataGraphs()
        {

            this.SuspendLayout();

            display.DataSources.Clear();
            //display.SetDisplayRangeX(0, 2000);
            display.SetDisplayRangeX(0, 3);
            display.SetGridDistanceX((float)0.5);

            for (int j = 0; j < NumGraphs; j++)
            {
                display.DataSources.Add(new DataSource());

                //display.DataSources[j].Name = "Graph " + (j + 1);
                if (j == 0) display.DataSources[j].Name = "MLII";
                if (j == 1) display.DataSources[j].Name = "V5";

                //display.DataSources[j].OnRenderXAxisLabel += RenderXLabel;

                switch (CurExample)
                {
                    case "NORMAL":
                        this.Text = "Normal Graph";
                        display.DataSources[j].Length = vrijeme.Count;
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.NORMAL;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].OnRenderYAxisLabel = RenderYLabel;
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "NORMAL_AUTO":
                        this.Text = "Normal Graph Autoscaled";
                        display.DataSources[j].Length = vrijeme.Count;
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.NORMAL;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].OnRenderYAxisLabel = RenderYLabel;
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "STACKED":
                        this.Text = "Stacked Graph";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.STACKED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "VERTICAL_ALIGNED":
                        this.Text = "Vertical aligned Graph";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.VERTICAL_ARRANGED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "VERTICAL_ALIGNED_AUTO":
                        this.Text = "Vertical aligned Graph autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.VERTICAL_ARRANGED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "TILED_VERTICAL":
                        this.Text = "Tiled Graphs (vertical prefered)";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_VER;
                        display.DataSources[j].Length = vrijeme.Count; // 60
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);

                        break;

                    case "TILED_VERTICAL_AUTO":
                        this.Text = "Tiled Graphs (vertical prefered) autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_VER;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "TILED_HORIZONTAL":
                        this.Text = "Tiled Graphs (horizontal prefered)";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "TILED_HORIZONTAL_AUTO":
                        this.Text = "Tiled Graphs (horizontal prefered) autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;

                    case "ANIMATED_AUTO":

                        this.Text = "Animated graphs fixed x range";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].AutoScaleX = true;
                        display.DataSources[j].SetDisplayRangeY(-2, 2);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].XAutoScaleOffset = 50;
                        CalcSinusFunction_3(display.DataSources[j], j, 0);
                        IspisiPodatke(display.DataSources[j], j, vrijeme, napon);
                        break;
                }
            }

            ApplyColorSchema();

            this.ResumeLayout();
            //display.Refresh();

        }









        

        // Obrisati:


        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            display.Smoothing = System.Drawing.Drawing2D.SmoothingMode.None;
        }

        private void antiAliasedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            display.Smoothing = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        }

        private void highQualityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            display.Smoothing = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        }

        private void highSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            display.Smoothing = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
        }


        protected void CalcSinusFunction_0(DataSource src, int idx)
        {
            for (int i = 0; i < src.Length; i++)
            {
                src.Samples[i].x = i;
                src.Samples[i].y = (float)(((float)200 * Math.Sin((idx + 1) * (i + 1.0) * 48 / src.Length)));
            }
        }

        protected void CalcSinusFunction_1(DataSource src, int idx)
        {
            for (int i = 0; i < src.Length; i++)
            {
                src.Samples[i].x = i;

                src.Samples[i].y = (float)(((float)20 *
                                            Math.Sin(20 * (idx + 1) * (i + 1) * 3.141592 / src.Length)) *
                                            Math.Sin(40 * (idx + 1) * (i + 1) * 3.141592 / src.Length)) +
                                            (float)(((float)200 *
                                            Math.Sin(200 * (idx + 1) * (i + 1) * 3.141592 / src.Length)));
            }
            src.OnRenderYAxisLabel = RenderYLabel;
        }

        protected void CalcSinusFunction_2(DataSource src, int idx)
        {
            for (int i = 0; i < src.Length; i++)
            {
                /*
                src.Samples[i].x = i;

                src.Samples[i].y = (float)(((float)20 *
                                            Math.Sin(40 * (idx + 1) * (i + 1) * 3.141592 / src.Length)) *
                                            Math.Sin(160 * (idx + 1) * (i + 1) * 3.141592 / src.Length)) +
                                            (float)(((float)200 *
                                            Math.Sin(4 * (idx + 1) * (i + 1) * 3.141592 / src.Length)));
                */
                src.Samples[i].x = i;
                src.Samples[i].y = i;
            }
            src.OnRenderYAxisLabel = RenderYLabel;
        }

        protected void CalcSinusFunction_3(DataSource ds, int idx, float time)
        {
            cPoint[] src = ds.Samples;
            for (int i = 0; i < src.Length; i++)
            {
                src[i].x = i;
                src[i].y = 200 + (float)((200 * Math.Sin((idx + 1) * (time + i * 100) / 8000.0))) +
                                +(float)((40 * Math.Sin((idx + 1) * (time + i * 200) / 2000.0)));
                /**
                            (float)( 4* Math.Sin( ((time + (i+8) * 100) / 900.0)))+
                            (float)(28 * Math.Sin(((time + (i + 8) * 100) / 290.0))); */
            }

        }





        protected override void OnClosing(CancelEventArgs e)
        {
            display.Dispose();

            base.OnClosing(e);
        }

        private void ApplyColorSchema()
        {
            switch (CurColorSchema)
            {
                case "DARK_GREEN":
                    {
                        Color[] cols = { Color.FromArgb(0,255,0), 
                                         Color.FromArgb(0,255,0),
                                         Color.FromArgb(0,255,0), 
                                         Color.FromArgb(0,255,0), 
                                         Color.FromArgb(0,255,0) ,
                                         Color.FromArgb(0,255,0),                              
                                         Color.FromArgb(0,255,0) };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.FromArgb(0, 64, 0);
                        display.BackgroundColorBot = Color.FromArgb(0, 64, 0);
                        display.SolidGridColor = Color.FromArgb(0, 128, 0);
                        display.DashedGridColor = Color.FromArgb(0, 128, 0);
                    }
                    break;
                case "WHITE":
                    {
                        Color[] cols = { Color.DarkRed, 
                                         Color.DarkSlateGray,
                                         Color.DarkCyan, 
                                         Color.DarkGreen, 
                                         Color.DarkBlue ,
                                         Color.DarkMagenta,                              
                                         Color.DeepPink };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.White;
                        display.BackgroundColorBot = Color.White;
                        display.SolidGridColor = Color.LightGray;
                        display.DashedGridColor = Color.LightGray;
                    }
                    break;

                case "BLUE":
                    {
                        Color[] cols = { Color.Red, 
                                         Color.Orange,
                                         Color.Yellow, 
                                         Color.LightGreen, 
                                         Color.Blue ,
                                         Color.DarkSalmon,                              
                                         Color.LightPink };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.Navy;
                        display.BackgroundColorBot = Color.FromArgb(0, 0, 64);
                        display.SolidGridColor = Color.Blue;
                        display.DashedGridColor = Color.Blue;
                    }
                    break;

                case "GRAY":
                    {
                        Color[] cols = { Color.DarkRed, 
                                         Color.DarkSlateGray,
                                         Color.DarkCyan, 
                                         Color.DarkGreen, 
                                         Color.DarkBlue ,
                                         Color.DarkMagenta,                              
                                         Color.DeepPink };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.White;
                        display.BackgroundColorBot = Color.LightGray;
                        display.SolidGridColor = Color.LightGray;
                        display.DashedGridColor = Color.LightGray;
                    }
                    break;

                case "RED":
                    {
                        Color[] cols = { Color.DarkCyan, 
                                         Color.Yellow,
                                         Color.DarkCyan, 
                                         Color.DarkGreen, 
                                         Color.DarkBlue ,
                                         Color.DarkMagenta,                              
                                         Color.DeepPink };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.DarkRed;
                        display.BackgroundColorBot = Color.Black;
                        display.SolidGridColor = Color.Red;
                        display.DashedGridColor = Color.Red;
                    }
                    break;

                case "LIGHT_BLUE":
                    {
                        Color[] cols = { Color.DarkRed, 
                                         Color.DarkSlateGray,
                                         Color.DarkCyan, 
                                         Color.DarkGreen, 
                                         Color.DarkBlue ,
                                         Color.DarkMagenta,                              
                                         Color.DeepPink };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.White;
                        display.BackgroundColorBot = Color.FromArgb(183, 183, 255);
                        display.SolidGridColor = Color.Blue;
                        display.DashedGridColor = Color.Blue;
                    }
                    break;

                case "BLACK":
                    {
                        Color[] cols = { Color.FromArgb(255,0,0), 
                                         Color.FromArgb(0,255,0),
                                         Color.FromArgb(255,255,0), 
                                         Color.FromArgb(64,64,255), 
                                         Color.FromArgb(0,255,255) ,
                                         Color.FromArgb(255,0,255),                              
                                         Color.FromArgb(255,128,0) };

                        for (int j = 0; j < NumGraphs; j++)
                        {
                            display.DataSources[j].GraphColor = cols[j % 7];
                        }

                        display.BackgroundColorTop = Color.Black;
                        display.BackgroundColorBot = Color.Black;
                        display.SolidGridColor = Color.DarkGray;
                        display.DashedGridColor = Color.DarkGray;
                    }
                    break;
            }

        }

        private void RefreshGraph()
        {
            display.Refresh();
        }


        private void UpdateColorSchemaMenu()
        {
            blueToolStripMenuItem.Checked = false;
            whiteToolStripMenuItem.Checked = false;
            grayToolStripMenuItem.Checked = false;
            lightBlueToolStripMenuItem.Checked = false;
            blackToolStripMenuItem.Checked = false;
            redToolStripMenuItem.Checked = false;

            if (CurColorSchema == "WHITE") whiteToolStripMenuItem.Checked = true;
            if (CurColorSchema == "BLUE") blueToolStripMenuItem.Checked = true;
            if (CurColorSchema == "GRAY") grayToolStripMenuItem.Checked = true;
            if (CurColorSchema == "LIGHT_BLUE") lightBlueToolStripMenuItem.Checked = true;
            if (CurColorSchema == "BLACK") blackToolStripMenuItem.Checked = true;
            if (CurColorSchema == "RED") redToolStripMenuItem.Checked = true;
            if (CurColorSchema == "DARK_GREEN") greenToolStripMenuItem.Checked = true;
        }

        private void UpdateGraphCountMenu()
        {
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;

            switch (NumGraphs)
            {
                case 1: toolStripMenuItem2.Checked = true; break;
                case 2: toolStripMenuItem3.Checked = true; break;
            }
        }


        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "NORMAL";
            CalcDataGraphs();
        }

        private void normalAutoscaledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "NORMAL_AUTO";
            CalcDataGraphs();
        }

        private void stackedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "STACKED";
            CalcDataGraphs();
        }

        private void verticallyAlignedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "VERTICAL_ALIGNED";
            CalcDataGraphs();
        }

        private void verticallyAlignedAutoscaledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "VERTICAL_ALIGNED_AUTO";
            CalcDataGraphs();
        }

        private void tiledVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "TILED_VERTICAL";
            CalcDataGraphs();
        }

        private void tiledVerticalAutoscaledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "TILED_VERTICAL_AUTO";
            CalcDataGraphs();
        }

        private void tiledHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "TILED_HORIZONTAL";
            CalcDataGraphs();
        }

        private void tiledHorizontalAutoscaledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "TILED_HORIZONTAL_AUTO";
            CalcDataGraphs();
        }

        private void animatedGraphDemoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurExample = "ANIMATED_AUTO";
            CalcDataGraphs();
        }



        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "BLUE";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void whiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "WHITE";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void grayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "GRAY";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void lightBlueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "LIGHT_BLUE";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void blackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "BLACK";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "RED";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurColorSchema = "DARK_GREEN";
            CalcDataGraphs();
            UpdateColorSchemaMenu();
        }


        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            NumGraphs = 1;
            CalcDataGraphs();
            UpdateGraphCountMenu();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            NumGraphs = 2;
            CalcDataGraphs();
            UpdateGraphCountMenu();
        }

    
    }
}        

