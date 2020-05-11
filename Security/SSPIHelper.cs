using System;
using System.Runtime.InteropServices;

namespace SweetPotato {
    internal class SSPIHelper {

        public enum SecBufferType {
            SECBUFFER_VERSION = 0,
            SECBUFFER_EMPTY = 0,
            SECBUFFER_DATA = 1,
            SECBUFFER_TOKEN = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecBuffer : IDisposable {
            private int cbBuffer;
            private int bufferType;
            private IntPtr pvBuffer;

            public SecBuffer(int bufferSize) {
                cbBuffer = bufferSize;
                bufferType = (int)SecBufferType.SECBUFFER_TOKEN;
                if (bufferSize > 0) {
                    pvBuffer = Marshal.AllocHGlobal(bufferSize);
                } else {
                    pvBuffer = IntPtr.Zero;
                }
            }

            public SecBuffer(byte[] secBufferBytes) {
                cbBuffer = secBufferBytes.Length;
                bufferType = (int)SecBufferType.SECBUFFER_TOKEN;
                pvBuffer = Marshal.AllocHGlobal(cbBuffer);
                Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
            }

            public SecBuffer(byte[] secBufferBytes, SecBufferType bufferType) {
                cbBuffer = secBufferBytes.Length;
                this.bufferType = (int)bufferType;
                pvBuffer = Marshal.AllocHGlobal(cbBuffer);
                Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
            }

            public void Dispose() {
                if (pvBuffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pvBuffer);
                    pvBuffer = IntPtr.Zero;
                }
            }

            public byte[] GetBytes() {
                byte[] buffer = null;
                if (cbBuffer > 0) {
                    buffer = new byte[cbBuffer];
                    Marshal.Copy(pvBuffer, buffer, 0, cbBuffer);
                }
                return buffer;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecBufferDesc : IDisposable {
            public int ulVersion;
            public int cBuffers;
            public IntPtr pBuffers; //Point to SecBuffer

            public SecBufferDesc(int bufferSize) {
                ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
                cBuffers = 1;
                SecBuffer secBuffer = new SecBuffer(bufferSize);
                pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));
                Marshal.StructureToPtr(secBuffer, pBuffers, false);

            }

            public SecBufferDesc(byte[] secBufferBytes) {
                ulVersion = (int)SecBufferType.SECBUFFER_VERSION;
                cBuffers = 1;
                SecBuffer secBuffer = new SecBuffer(secBufferBytes);
                pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));
                Marshal.StructureToPtr(secBuffer, pBuffers, false);
            }

            public void Dispose() {
                if (pBuffers != IntPtr.Zero) {
                    SecBuffer secBuffer = (SecBuffer)Marshal.PtrToStructure(pBuffers, typeof(SecBuffer));
                    secBuffer.Dispose();
                    Marshal.FreeHGlobal(pBuffers);
                    pBuffers = IntPtr.Zero;
                }
            }

            public SecBuffer GetSecBuffer() {
                if (pBuffers == IntPtr.Zero)
                    throw new ObjectDisposedException("SecBufferDesc");
                SecBuffer secBuffer = (SecBuffer)Marshal.PtrToStructure(pBuffers, typeof(SecBuffer));
                return secBuffer;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class TimeStamp {
            public uint LowPart;
            public int HighPart;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class CredHandle {
            IntPtr LowPart;
            IntPtr HighPart;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal class CtxHandle {
            IntPtr LowPart;
            IntPtr HighPart;
        };


        [DllImport("secur32.dll", SetLastError = true)]
        public static extern int AcquireCredentialsHandle(
            string pszPrincipal, //SEC_CHAR*
            string pszPackage, //SEC_CHAR* //"Kerberos","NTLM","Negotiative"
            int fCredentialUse,
            IntPtr PAuthenticationID,//_LUID AuthenticationID,//pvLogonID, //PLUID
            IntPtr pAuthData,//PVOID
            IntPtr pGetKeyFn, //SEC_GET_KEY_FN
            IntPtr pvGetKeyArgument, //PVOID
            CredHandle phCredential, //SecHandle //PCtxtHandle ref
            TimeStamp ptsExpiry); //PTimeStamp //TimeStamp ref

        [DllImport("secur32.dll", SetLastError = true)]
        public static extern int AcceptSecurityContext(CredHandle phCredential, CtxHandle phContext,
                            ref SecBufferDesc pInput,
                            uint fContextReq,
                            uint TargetDataRep,
                            CtxHandle phNewContext,
                            out SecBufferDesc pOutput,
                            out uint pfContextAttr,    //managed ulong == 64 bits!!!
                            TimeStamp ptsTimeStamp);

        [DllImport("secur32.dll", SetLastError = true)]
        public static extern int QuerySecurityContextToken(CtxHandle phContext, out IntPtr hToken);

        public const int TOKEN_QUERY = 0x00008;
        public const int SEC_E_OK = 0;
        public const int SEC_I_CONTINUE_NEEDED = 0x00090312;
        public const int SECPKG_CRED_OUTBOUND = 2;
        public const int SECURITY_NATIVE_DREP = 0x10;
        public const int SECPKG_CRED_INBOUND = 1;
        public const int MAX_TOKEN_SIZE = 12288;
    }
}
