using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModemSMSApp
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                ModemSMS sms = new ModemSMS();
                sms.OpenConnection();
                ShortMessageCollection smsData = sms.ReadSMS("AT+CMGL=\"ALL\"");
                Dictionary<string, string> smsList = new Dictionary<string, string>();
                foreach (ShortMessage aMessage in smsData)
                {
                    smsList.Add(aMessage.Index, aMessage.Message);
                    Console.WriteLine(aMessage.Message);

                }


                if (smsData.Count != 0)
                {
                    foreach (ShortMessage aMessage in smsData)
                    {
                        sms.DeleteSMS("AT+CMGD=" + aMessage.Index + ",0");
                        sms.SendSMS(aMessage.Sender, "Your SMS Received...");
                        Console.WriteLine("Message Sent to: " + aMessage.Sender);
                    }
                }
                sms.CloseConnection();
                System.Threading.Thread.Sleep(4000);
            }

            //sms.DeleteSMS("AT+CMGD=<index>,0");
            //sms.DeleteSMS("AT+CMGD=1,4");
            //String command = "AT^CURC=0";
            //sms.ExecCommand(command,300,"Error");

            //System.Threading.Thread.Sleep(2000);
            //while (true)
            //{
            //    smsList = new Dictionary<string, string>();
            //    smsData = sms.ReadSMS("AT + CMGL =\"ALL\"");
            //    if(smsData.Count != 0)
            //    {
            //        foreach(ShortMessage aMessage in smsData)
            //{
            //sms.DeleteSMS("AT+CMGD="+aMessage.Index+",0");
            //sms.SendSMS(aMessage.Sender, "Your SMS Received...");
            //Console.WriteLine("Message Sent to: " + aMessage.Sender);
            //        }


            //    }
            //    Console.WriteLine(smsData.Count);
            //    System.Threading.Thread.Sleep(2000);
            //}

            //bool send = sms.SendSMS("+8801926662274", "Dear Atique Reza Chowdhury, This is an auto generated sms. :)");

            Console.ReadKey();
        }
    }
}
