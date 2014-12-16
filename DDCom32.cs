using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace HyPDM
{
    public sealed class DDCom32
    {
        //const int c_maxbufsize = 1024;
        const int c_maxbufsize = 2048;
        private static readonly DDCom32 instance = new DDCom32();
        //private bool isInitilized = false;

        [DllImport("ddcom32.dll")]
        private static extern int ddinit(string host, short user);

        [DllImport("ddcom32.dll")]
        private static extern int ddsend(string data, ref short dberr);

        [DllImport("ddcom32.dll")]
        private static extern int ddreceive(IntPtr data, ref short dberr);

        [DllImport("ddcom32.dll")]
        private static extern long ddmessagetype(long user);

        [DllImport("ddcom32.dll")]
        private static extern int ddgetfile(string remotefile, string localfile);

        [DllImport("ddcom32.dll")]
        private static extern int ddminoffline(int sec);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

        private Object lockObj = new Object();

        public static DDCom32 Instance
        {
            get
            {
                return instance;
            }
        }

        private DDCom32() {}

        public int Init(string host, short user)
        {
            int result = -1;
            try
            {
                result = ddinit(host, user);
            }
            catch (Exception e)
            {
                //логирование ошибки
            }
            return result;
        }

        public int Send(string data, ref short dberr, long threadUser = 0)
        {
            int result = -1;
            try
            {
                if (threadUser > 0)
                    lock (lockObj)
                    {
                        ddmessagetype(threadUser);
                        result = ddsend(data, ref dberr);
                    }
                else
                    result = ddsend(data, ref dberr);
            }
            catch (Exception e)
            {
                //логирование ошибки
            }
            return result;
        }

        public int Receive(ref string data, ref short dberr, long threadUser = 0)
        {
            int result = -1;
            IntPtr data_ptr = Marshal.AllocHGlobal(c_maxbufsize * sizeof(byte));
            
            try
            {
                FillMemory(data_ptr, c_maxbufsize, 0);
                if (threadUser > 0)
                    lock (lockObj)
                    {
                        ddmessagetype(threadUser);
                        result = ddreceive(data_ptr, ref dberr);
                    }
                else
                    result = ddreceive(data_ptr, ref dberr);
                if (result == 0)
                    data = Marshal.PtrToStringAnsi(data_ptr);
            }
            catch (Exception e)
            {
                //логирование ошибки
            }
            finally
            {
                Marshal.FreeHGlobal(data_ptr);
            }

            return result;
        }

        public int GetFile(string remotefile, string localfile)
        {
            return ddgetfile(remotefile, localfile);
        }

        public int MinOffline(int sec)
        {
            return ddminoffline(sec);
        }

        public int DoCommand(ref string data, bool withInit = true, int timeOut = 1000, string host = "", short user = 0, long threadUser = 0)
        {
            int result;
            short error = 0;
            //string partData;

            if (withInit)
                result = Init(host, user);
            else
                result = 0;
            if (result == 0)
            {
                result = Send(data, ref error, threadUser);
                if (result == 0)
                {
                    do
                    {
                        result = Receive(ref data, ref error, threadUser);
                        if (result == 13)
                            Thread.Sleep(timeOut);

                    } while (result == 13);
                }
            }
            return result;
        }

        public string GetErrorDescription(int ddcomRes)
        {
            string result = string.Empty;
            switch (ddcomRes)
            {
                case 1:
                    result = string.Concat("Ddcom32 error: ", " 1. Initialization of Windows sockets incorrect (ddinit() only)");
                    break;
                case 2:
                    result = string.Concat("Ddcom32 error: ", " 2. No port found for “HYDRA file server“ (ddinit() only)");
                    break;
                case 3:
                    result = string.Concat("Ddcom32 error: ", " 3. No port found for a HYDRA interface (e.g. “HYDRA LANT-DD-Server 1“) (ddinit() only)");
                    break;
                case 4:
                    result = string.Concat("Ddcom32 error: ", " 4. Unable to build up a connection");
                    break;
                case 5:
                    result = string.Concat("Ddcom32 error: ", " 5. Dispatch with error cancelled");
                    break;
                case 6:
                    result = string.Concat("Ddcom32 error: ", " 6. Reception with error cancelled");
                    break;
                case 7:
                    result = string.Concat("Ddcom32 error: ", " 7. Connected to wrong interface");
                    break;
                case 8:
                    result = string.Concat("Ddcom32 error: ", " 8. (Local) interface offline at the moment");
                    break;
                case 9:
                    result = string.Concat("Ddcom32 error: ", " 9. Error in opening a file via the \"HYDRA file server\"");
                    break;
                case 10:
                    result = string.Concat("Ddcom32 error: ", " 10. Error in reading a file via the \"HYDRA file server\"");
                    break;
                case 11:
                    result = string.Concat("Ddcom32 error: ", " 11. Error in writing in a file via the \"HYDRA file server\"");
                    break;
                case 12:
                    result = string.Concat("Ddcom32 error: ", " 12. Error in closing a file via the \"HYDRA file server\"");
                    break;
                case 13:
                    result = string.Concat("Ddcom32 error: ", " 13. Data is not yet available");
                    break;
                case 23:
                    result = string.Concat("Ddcom32 error: ", " 23. Interface has not yet been initialized successfully");
                    break;
                case 24:
                    result = string.Concat("Ddcom32 error: ", " 24. Synchronization with server failed (automatic re-initialization)");
                    break;
            }

            return result;
        }

        private void WriteLog(string filename, string message)
        {
            using (StreamWriter sw = new StreamWriter(filename, true))
            {
                sw.WriteLine(message);
                sw.Close();
            }
        }
    }
}
