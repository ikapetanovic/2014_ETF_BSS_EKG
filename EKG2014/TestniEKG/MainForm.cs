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
        private static List<double> vrijeme;
        private static List<double> napon1;
        private static List<double> napon2;
        
        private static System.Threading.Timer timer;

        private int NumGraphs = 2;
        private String CurExample = "STACKED";
        private String CurColorSchema = "WHITE";
        private PrecisionTimer.Timer mTimer = null;
        private DateTime lastTimerTick = DateTime.Now;

        public MainForm()
        {            
            InitializeComponent();
            
            vrijeme = new List<double>();
            napon1 = new List<double>();
            napon2 = new List<double>();

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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DajVrijeme("100.txt", EKGFileType.TEXT);            
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
                        //CalcSinusFunction_3(display.DataSources[j], j, (float)dt.TotalMilliseconds);
                        
                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);
                    }

                    this.Invoke(new MethodInvoker(RefreshGraph));
                }
                catch (ObjectDisposedException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        void IspisiPodatke(DataSource src, int idx, List<double> vrijeme, List<double> napon)
        {
            // for (int i = 0; i < src.Length; i++)

            double granica = vrijeme.Count;
            if (napon.Count < granica)
                granica = napon.Count;

            for (int i = 0; i < granica; i++)
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
                double Value = (s.Samples[idx].x);
                return "" + Value;
            }
        }

        private String RenderYLabel(DataSource s, float value)
        {
            return String.Format("{0:0.0}", (float)value); // /100
        }

               
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            napon1.Clear();
            napon2.Clear();

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
                                
                try
                {
                    DajNapon1(filePath, tip);
                }
                catch (Exception izuzetak)
                {
                    MessageBox.Show(izuzetak.ToString());
                }

                try
                {
                    DajNapon2(filePath, tip);
                }
                catch (Exception izuzetak)
                {
                    MessageBox.Show(izuzetak.ToString());
                }

                if (tip == EKGFileType.TEXT)
                {                    
                    for (int i = 0; i < napon1.Count; i++)
                        napon1[i] = napon1[i] * 1000;                    

                    for (int i = 0; i < napon2.Count; i++)
                        napon2[i] = napon2[i] * 1000;
                }

                CalcDataGraphs();
                
            }
        }

        public void DajVrijeme(string filePath, EKGFileType type)
        {
            timer = new System.Threading.Timer(new TimerCallback(tick0));
            try
            {
                UlazniBuffer.Open(filePath, 0, type);
                timer.Change(0, 60); // 30
                Thread.Sleep(10000);
            }
            catch (Exception izuzetak)
            {
                MessageBox.Show(izuzetak.ToString());
            }
        }

        public void DajNapon1(string filePath, EKGFileType type)
        {
            timer = new System.Threading.Timer(new TimerCallback(tick1));
            try
            {
                UlazniBuffer.Open(filePath, 1, type);
                timer.Change(0, 60); // 30
                Thread.Sleep(10000); //10000
            }
            catch (Exception izuzetak)
            {
                MessageBox.Show(izuzetak.ToString());
            }
        }

        public void DajNapon2(string filePath, EKGFileType type)
        {
            timer = new System.Threading.Timer(new TimerCallback(tick2));
            try
            {
                UlazniBuffer.Open(filePath, 2, type);
                timer.Change(0, 60); // 30
                Thread.Sleep(10000); //10000
            }
            catch (Exception izuzetak)
            {
                MessageBox.Show(izuzetak.ToString());
            }
        }

        public void tick0(object o) //static
        {
            
            //System.Console.WriteLine("tick");
            double[] signal = new double[40000]; //10
            for (int i = 0; i < 40000; i++) // 10
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
            } 
        }

        public void tick1(object o) //static
        {

            //System.Console.WriteLine("tick");
            double[] signal = new double[40000]; //10
            for (int i = 0; i < 40000; i++) // 10
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

                napon1.Add(signal[i]); // 1000
            }
        }

        public void tick2(object o) //static
        {

            //System.Console.WriteLine("tick");
            double[] signal = new double[40000]; //10
            for (int i = 0; i < 40000; i++) // 10
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

                napon2.Add(signal[i]);
            }
        }

        public static void tickmany(object o)
        {
            //System.Console.WriteLine("tick");
            double[] signal = new double[10];
            while (!UlazniBuffer.ReadMany(out signal, 1000)) ;
            for (int i = 0; i < 1000; i++)
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
            
            display.SetDisplayRangeX(0, 3);
            display.SetGridDistanceX((float)0.2);


            for (int j = 0; j < NumGraphs; j++)
            {
                display.DataSources.Add(new DataSource());

                display.DataSources[j].Name = "Graph " + (j + 1);
                //if (j == 0) display.DataSources[j].Name = "MLII";
                //if (j == 1) display.DataSources[j].Name = "V5";

                display.DataSources[j].OnRenderXAxisLabel += RenderXLabel;

                switch (CurExample)
                {
                    case "NORMAL":
                        this.Text = "Normal Graph";
                        display.DataSources[j].Length = vrijeme.Count;
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.NORMAL;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].OnRenderYAxisLabel = RenderYLabel;
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);                        

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "NORMAL_AUTO":
                        this.Text = "Normal Graph Autoscaled";
                        display.DataSources[j].Length = vrijeme.Count;
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.NORMAL;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].OnRenderYAxisLabel = RenderYLabel;
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);


                        break;

                    case "STACKED":
                        this.Text = "Stacked Graph";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.STACKED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "VERTICAL_ALIGNED":
                        this.Text = "Vertical aligned Graph";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.VERTICAL_ARRANGED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "VERTICAL_ALIGNED_AUTO":
                        this.Text = "Vertical aligned Graph autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.VERTICAL_ARRANGED;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "TILED_VERTICAL":
                        this.Text = "Tiled Vertical Graphs";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_VER;
                        display.DataSources[j].Length = vrijeme.Count; // 60
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "TILED_VERTICAL_AUTO":
                        this.Text = "Tiled Vertical Graphs autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_VER;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "TILED_HORIZONTAL":
                        this.Text = "Tiled Horizontal Graphs";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "TILED_HORIZONTAL_AUTO":
                        this.Text = "Tiled Horizontal Graphs autoscaled";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = true;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;

                    case "ANIMATED_AUTO":

                        this.Text = "Animated graphs fixed x range";
                        display.PanelLayout = PlotterGraphPaneEx.LayoutMode.TILES_HOR;
                        display.DataSources[j].Length = vrijeme.Count;
                        display.DataSources[j].AutoScaleY = false;
                        display.DataSources[j].AutoScaleX = true;
                        display.DataSources[j].SetDisplayRangeY(-3, 3);
                        display.DataSources[j].SetGridDistanceY((float)1);
                        display.DataSources[j].XAutoScaleOffset = 50;
                        //CalcSinusFunction_3(display.DataSources[j], j, 0);
                        //IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);

                        if (j == 0)
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon1);
                        else
                            IspisiPodatke(display.DataSources[j], j, vrijeme, napon2);

                        break;
                }
            }

            ApplyColorSchema();

            this.ResumeLayout();
            //display.Refresh();

        }

        

        protected override void OnClosing(CancelEventArgs e)
        {
            display.Dispose();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            mTimer.Stop();
            mTimer.Dispose();
            base.OnClosed(e);
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

