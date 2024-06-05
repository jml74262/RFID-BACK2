namespace PrinterBackEnd.Models
{
    public class UsbInfo
    {
        public string Vid { get; set; }
        public string Pid { get; set; }
        public string Sid { get; set; }
        public string PortName { get; set; }

        public UsbInfo(string vid, string pid, string sid, string portName)
        {
            Vid = vid;
            Pid = pid;
            Sid = sid;
            PortName = portName;
        }
    }

}
