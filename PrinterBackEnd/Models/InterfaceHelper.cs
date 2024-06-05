namespace PrinterBackEnd.Models
{
    public abstract class InterfaceHelper
    {
        protected bool isPermanent = false;
        protected const int BufSize = 1048576;
        protected byte[] buffer = new byte[BufSize];
        protected IAsyncResult Gar = null;

        public Action<byte[]> fOnReceiveCallBack = null;

        public InterfaceHelper(bool PermanentConnect)
        {
            isPermanent = PermanentConnect;
        }

        public abstract void Open(string name, object data, int timeout, int whichInterface);
        public abstract void Open(string name, object data, int timeout);
        public abstract void Close();
        public abstract void CBReceive(IAsyncResult ar);
        public abstract byte[] Send(byte[] data, int ReplyCnt);
    }
}
