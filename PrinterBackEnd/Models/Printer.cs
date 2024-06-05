using System;
using System.Text;
using System.Threading;

namespace PrinterBackEnd.Models
{
    public class Printer
    {
        public enum InterfaceType
        {
            TCPIP,
            USB,
            COM,
            LPT
        }

        public class Status
        {
            public bool IsOnline { get; set; }
            public bool IsError { get; set; }
            public string Description { get; set; }
            public string State { get; set; }
            public string Code { get; set; }
            public int Buffer { get; set; }
            public string JobName { get; set; }
            public string JobID { get; set; }
            public string Raw { get; set; }
        }

        private InterfaceHelper interfaceConnection;
        public InterfaceType? Interface { get; set; }
        public string TCPIPAddress { get; set; }
        public string TCPIPPort { get; set; }
        public int Timeout { get; set; }

        public Printer()
        {
            interfaceConnection = new SocketHelper(); // Utiliza el constructor predeterminado
        }

        public void OpenConnection()
        {
            if (Interface == InterfaceType.TCPIP)
            {
                interfaceConnection.Open(TCPIPAddress, TCPIPPort, Timeout, 0);
            }
            else
            {
                throw new Exception("Unsupported interface type");
            }
        }

        public void CloseConnection()
        {
            interfaceConnection.Close();
        }

        public void Send(byte[] data)
        {
            interfaceConnection.Send(data, 0);
        }

        public Status GetPrinterStatus()
        {
            byte[] data = new byte[] { 5 };
            OpenConnection();
            byte[] response = interfaceConnection.Send(data, 1);
            CloseConnection();

            if (response != null)
            {
                string text = Encoding.UTF8.GetString(response);
                if (text.IndexOf('\u0002') >= 0 && text.IndexOf('\u0003') >= 0)
                {
                    char value = '\u0002';
                    Status ps = new Status();
                    ps.JobID = text.Substring(text.IndexOf(value) + 1, 2).Trim();
                    ps.Code = text.Substring(text.IndexOf(value) + 3, 1);
                    ps.Buffer = int.Parse(text.Substring(text.IndexOf(value) + 4, 6));
                    if (text.Substring(text.IndexOf(value) + 1).Length > 10)
                    {
                        ps.JobName = text.Substring(text.IndexOf(value) + 10, 16).Trim();
                    }

                    ps.Raw = text;
                    AssignStatusState(ref ps);
                    return ps;
                }
            }

            return null;
        }

        private void AssignStatusState(ref Status ps)
        {
            switch (ps.Code)
            {
                case "A":
                    ps.IsOnline = true;
                    ps.IsError = false;
                    ps.Description = "NO ERROR";
                    ps.State = "WAIT TO RECEIVE";
                    break;
                // Additional status code mappings...
                default:
                    ps.IsOnline = false;
                    ps.IsError = false;
                    ps.Description = null;
                    ps.State = null;
                    break;
            }
        }
    }
}
