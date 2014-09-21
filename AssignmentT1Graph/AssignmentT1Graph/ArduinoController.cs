using System;
using System.IO.Ports;
using System.Threading;
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

                //Console.WriteLine( Convert.ToString(arduinoPort.ReadByte()) );

                //add the latest reading from arduino
                //updateGraph((Convert.ToInt32(arduinoPort.ReadByte())));
                //Thread.Sleep(2);
            }

            updateGraph();
            Thread.Sleep(5);
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
}
