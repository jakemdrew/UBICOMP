using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;
using Arduino;

namespace AssignmentT1Graph
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ArduinoController arduinoController;

        private void Form1_Load(object sender, EventArgs e)
        {
            // Get a reference to the GraphPane instance in the ZedGraphControl
            GraphPane myPane = zg1.GraphPane;

            // Set the titles and axis labels
            myPane.Title.Text = "A0 ADC Graph";
            myPane.XAxis.Title.Text = "Sample Timeline";
            myPane.YAxis.Title.Text = "Sample Voltage";

            // Save the last 300 points in this structure. 
            RollingPointPairList list = new RollingPointPairList(300);

            // Initially, a curve is added with no data points (list is empty)
            // Color is blue, and there will be no symbols
            LineItem curve = myPane.AddCurve("A0 Voltage", list, Color.Blue, SymbolType.None);

            // Fill the axis background with a gradient
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            // Just manually control the X axis range so it scrolls continuously
            // instead of discrete step-sized jumps
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 300;
            myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Scale.MajorStep = 5;

            //myPane.YAxis.Scale.Min = 0;
            //myPane.YAxis.Scale.Max = 260;
            //myPane.YAxis.Scale.MinorStep = 1;
            //myPane.YAxis.Scale.MajorStep = 5;

            // Scale the axes
            zg1.AxisChange();

            //Create a reference to my Arduino controller.
            arduinoController = new ArduinoController(zg1);

            //Send command to start getting readings from arduino 
            arduinoController.sendCmd(ArduinoControllerCmd.sampleA0);
        }

        private void btnLearn_Click(object sender, EventArgs e)
        {
            arduinoController.Learn(txtLearnName.Text);
            MessageBox.Show("Learning Completed!");
        }

        private void btnPredict_Click(object sender, EventArgs e)
        {
            MessageBox.Show(arduinoController.Predict(),"Prediction");
        }
    }
}
