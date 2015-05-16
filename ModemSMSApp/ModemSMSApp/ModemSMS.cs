using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;


//                    string strCommand = "AT+CMGL=\"ALL\"";

//                    if (this.rbReadAll.Checked)
//                    {
//                          strCommand = "AT+CMGL=\"ALL\"";
//                    }
//                    else if (this.rbReadUnRead.Checked)
//                    {
//                          strCommand = "AT+CMGL=\"REC UNREAD\"";
//                    }
//                    else if (this.rbReadStoreSent.Checked)
//                    {
//                          strCommand = "AT+CMGL=\"STO SENT\"";
//                    }
//                    else if (this.rbReadStoreUnSent.Checked)
//                    {
//                          strCommand = "AT+CMGL=\"STO UNSENT\"";
//                    }

namespace ModemSMSApp
{
    class ModemSMS
    {
        public AutoResetEvent receiveNow;
        SerialPort port = new SerialPort();

        public ModemSMS()
        {

        }

        public bool OpenConnection()
        {

            receiveNow = new AutoResetEvent(false);          

            try
            {
                port.PortName = "COM4";              //COM1
                port.BaudRate = 9600;                //9600
                port.DataBits = 8;                   //8
                port.StopBits = StopBits.One;        //1
                port.Parity = Parity.None;           //None
                port.ReadTimeout = 300;              //300
                port.WriteTimeout = 300;             //300
                port.Encoding = Encoding.GetEncoding("iso-8859-1");
                port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
                port.Open();
                port.DtrEnable = true;
                port.RtsEnable = true;

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }            
        }


        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                {
                    receiveNow.Set();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool CloseConnection()
        {
            try
            {
                port.Close();
                port.DataReceived -= new SerialDataReceivedEventHandler(Port_DataReceived);
                port = null;
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public ShortMessageCollection ReadSMS(string p_strCommand)
        {

            // Set up the phone and read the messages
            ShortMessageCollection messages = null;
            try
            {

                #region Execute Command
                // Check connection
                ExecCommand("AT", 300, "No phone connected");
                // Use message format "Text mode"
                ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                // Read the messages
                string input = ExecCommand(p_strCommand, 500, "Failed to read the messages.");
                #endregion

                #region Parse messages
                messages = ParseMessages(input);
                #endregion

            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (messages != null)
                return messages;
            else
                return null;

        }

        public string ExecCommand(string command, int responseTimeout, string errorMessage)
        {
            try
            {
                Console.WriteLine("Command: " + command);
                port.DiscardOutBuffer();
                port.DiscardInBuffer();
                receiveNow.Reset();
                port.Write(command + "\r");

                string input = ReadResponse(responseTimeout);

                Console.WriteLine("Output: ",input.Replace("\\r\\n",""));
                //if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                //    throw new ApplicationException("No success message was received.");
                return input;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string ReadResponse(int timeout)
        {
            string buffer = string.Empty;
            try
            {
                do
                {
                    if (receiveNow.WaitOne(timeout, false))
                    {
                        string t = port.ReadExisting();
                        buffer += t;
                    }
                    else
                    {
                        if (buffer.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.Contains("ERROR"));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return buffer;
        }

        public ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            try
            {
                Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                Match m = r.Match(input);
                while (m.Success)
                {
                    ShortMessage msg = new ShortMessage();
                    //msg.Index = int.Parse(m.Groups[1].Value);
                    msg.Index = m.Groups[1].Value;
                    msg.Status = m.Groups[2].Value;
                    msg.Sender = m.Groups[3].Value;
                    msg.Alphabet = m.Groups[4].Value;
                    msg.Sent = m.Groups[5].Value;
                    msg.Message = m.Groups[6].Value;
                    messages.Add(msg);

                    m = m.NextMatch();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return messages;
        }
           

        public bool SendSMS(string PhoneNo, string Message)
        {
            bool isSend = false;
            string recievedData = string.Empty;
            try
            {
                recievedData = ExecCommand("AT", 300, "No phone connected");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                String command = "AT^CURC=0";
                ExecCommand(command, 300, "");

                command = "AT+CSCS=\"GSM\"";
                ExecCommand(command,300,"");

                command = "AT+CMGS=\"" + PhoneNo + "\"";
                recievedData = ExecCommand(command, 300, "Failed to accept phoneNo");
                command = Message + char.ConvertFromUtf32(26) + "\r\n";
                recievedData = ExecCommand(command, 50000, "Failed to send message"); //5 seconds
                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isSend = true;
                }
                else if (recievedData.Contains("ERROR"))
                {
                    isSend = false;
                }
                return isSend;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }



        public bool DeleteSMS(string strCommand)
        {           
            try
            {
                #region Execute Command
                string recievedData = ExecCommand("AT", 300, "No phone connected");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                String command = strCommand;
                recievedData = ExecCommand( command, 300, "Failed to delete message");
                #endregion

                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


    }
}
