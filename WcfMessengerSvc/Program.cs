using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description; 

namespace WcfMessengerSvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            /*
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new Service1() 
			};
            ServiceBase.Run(ServicesToRun);
             */
            // Step 1 Create a URI to serve as the base address.
            Uri baseAddress = new Uri("http://localhost:8000/");

            // Step 2 Create a ServiceHost instance
            ServiceHost selfHost = new ServiceHost(typeof(MessengerLib.Messenger), baseAddress);

            try
            {
                // Step 3 Add a service endpoint.
                selfHost.AddServiceEndpoint(typeof(MessengerLib.IMessenger), new WSHttpBinding(SecurityMode.None), "Messenger");

                // Step 4 Enable metadata exchange.
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                selfHost.Description.Behaviors.Add(smb);

                // Step 5 Start the service.
                selfHost.Open();
                Console.WriteLine("The service is ready.");
                Console.WriteLine("Press ZZZ<ENTER> to terminate service.");
                Console.WriteLine();
                string readbuf = "";
                do
                {
                    readbuf = Console.ReadLine();
                } while (readbuf != "ZZZ");

                // Close the ServiceHostBase to shutdown the service.
                selfHost.Close();
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                selfHost.Abort();
            }
        }
    }
}
