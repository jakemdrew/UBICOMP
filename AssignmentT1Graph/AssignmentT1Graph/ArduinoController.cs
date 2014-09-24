using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Linq;
using ZedGraph;

namespace Arduino
{
    public enum ArduinoControllerCmd 
    { 
        hello = 128
      , sampleA0 = 129 
    }

    public class ArduinoController
    {
        public bool ArduinoFound = false;
        public SerialPort ArduinoPort = null;

        //Learning Variables
        const int learnSampleSize = 300;
        Dictionary<double, string> voltSignature = new Dictionary<double, string>();

        //Rolling stats variables
        public Queue<double> rollingAvgVolts;
        public double rollingAvgSum = 0;
        public double rollingAvg = -9999999;
        public long rollCt = 0;

        //ZedGraph Controls
        ZedGraphControl a0Graph;
        GraphPane a0GraphPane;
        IPointListEdit a0PointsList;
        private int yPointer = 0;

        public ArduinoController(ZedGraphControl zgc)
        {
            //Find Arduino's Port and setup Event to read all incoming data
            if (!findArduino())
                throw new Exception("I cannot find Arduino!");

            //Get ZedGraph references
            a0Graph = zgc;
            a0GraphPane = zgc.GraphPane;
            LineItem curve = a0GraphPane.CurveList[0] as LineItem;
            a0PointsList = curve.Points as IPointListEdit;

            //Create stats tracking objects
            rollingAvgVolts = new Queue<double>(learnSampleSize);
        }

        public void sendCmd(ArduinoControllerCmd commandNo, byte arg1 = 0, byte arg2 = 0, SerialPort arduinoPort = null)
        {
            //Create command
            byte[] buffer = new byte[5];
            buffer[0] = 16;
            buffer[1] = (byte)commandNo;
            buffer[2] = arg1;
            buffer[3] = arg2;
            buffer[4] = 4;

            if (arduinoPort == null) 
                arduinoPort = ArduinoPort;

            arduinoPort.Write(buffer, 0, 5);
        }

        /// <summary>
        ///      1.  Loop through all avaiable ports sending an Arduino handshake message.
        ///      2.  Map event handler to the correct port sending back the proper Arduino response.
        /// </summary>
        private bool findArduino()
        {
            SerialPort testPort;
         
            //Check each serial port for Arduino
            foreach (var port in SerialPort.GetPortNames())
            {
                testPort = new SerialPort(port, 9600);
                testPort.Open();
                sendCmd(ArduinoControllerCmd.hello, 0, 0, testPort);
                Thread.Sleep(1000);
                string response = testPort.ReadExisting();

                if (response.Contains("HELLO FROM ARDUINO"))
                {   //Register arduino on the correct serial port
                    ArduinoPort = testPort;
                    ArduinoFound = true;
                    Console.WriteLine("Arduino Found on: " + testPort.PortName);
                    testPort.DataReceived += new SerialDataReceivedEventHandler(processIncomingPortData);
                    break;
                }
            }

            return ArduinoFound;
        }

        /// <summary>
        ///   1. Handles all response data sent from Arduino.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void processIncomingPortData(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort arduinoPort = (SerialPort)sender;

            for (int i = 0; i < arduinoPort.BytesToRead; i++)
            {
                double volts = (double)Convert.ToInt32(arduinoPort.ReadByte()) / 51.0;
                a0PointsList.Add(yPointer, volts);
                if (yPointer == 300) a0PointsList.Clear();
                yPointer = yPointer < 300 ? yPointer + 1 : 0;

                //Track rolling average volts for last learnSampleSize samples
                updateStats(volts);
            }

            updateGraph();
            Thread.Sleep(5);
        }

        public void updateStats(double volt)
        {
            rollingAvgSum += volt;

            if (rollingAvgVolts.Count < learnSampleSize)
            {
                rollingAvgVolts.Enqueue(volt);
            }
            else
            {
                double subtVolt = rollingAvgVolts.Dequeue();
                rollingAvgSum -= subtVolt;
                rollingAvg = rollingAvgSum / learnSampleSize;
            }

            rollCt++;
        }

        public void Learn(string learnName)
        {
            rollCt = 0;

            //Wait till we have captured a big enough sample
            while (rollCt < learnSampleSize)
            {
                Thread.Sleep(10);
            }

            //Save the rolling average volts indexed by volts and by name
            voltSignature.Add(rollingAvg, learnName);
        }

        public string Predict()
        {
            rollCt = 0;
            double halfCt = (double)learnSampleSize / 2;

            //Wait till we have captured a big enough sample
            while (rollCt < halfCt)
            {
                Thread.Sleep(10);
            }

            double closestSignature = (double)NumberFinder.FindClosestTo(voltSignature.Keys, rollingAvg);
            return voltSignature[closestSignature];
        }

        private void updateGraph() //int yValue)
        {
            //a0PointsList.Add(yPointer, yValue);

            try
            {
                // Make sure the Graph gets redrawn
                a0Graph.AxisChange();
                a0Graph.Invalidate();
                //Update the yAxis pointer
                //if (yPointer == 300) a0PointsList.Clear();
                //yPointer = yPointer < 300 ? yPointer + 1 : 0;
            }
            catch (Exception e)
            { 
            
            }
        }

    }

    public static class NumberFinder
    {
        /// <summary>
        ///   http://stackoverflow.com/questions/1988937/find-the-closest-number-in-a-list-of-numbers
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        /// 
        public static double? FindClosestTo(this IEnumerable<double> numbers, double targetNumber)
        {
            var minimumDistance = numbers
                .Select(number => new NumberDistance(targetNumber, number))
                .Min();

            return minimumDistance == null ? (double?)null : minimumDistance.Number;
        }
    }

    /// <summary>
    /// Modified this code to support double:
    /// http://stackoverflow.com/questions/1988937/find-the-closest-number-in-a-list-of-numbers
    /// </summary>
    public class NumberDistance : IComparable<NumberDistance>
    {
        internal NumberDistance(double targetNumber, double number)
        {
            this.Number = number;
            this.Distance = Math.Abs(targetNumber - number);
        }

        internal double Number { get; private set; }

        internal double Distance { get; private set; }

        public int CompareTo(NumberDistance other)
        {
            var comparison = this.Distance.CompareTo(other.Distance);

            if(comparison == 0)
            {
                // When they have the same distance, pick the number closest to zero
                comparison = Math.Abs(this.Number).CompareTo(Math.Abs(other.Number));

                if(comparison == 0)
                {
                    // When they are the same distance from zero, pick the positive number
                    comparison = this.Number.CompareTo(other.Number);
                }
            }

            return comparison;
        }
    }
}
