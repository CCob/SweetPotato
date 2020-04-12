using System;
using System.Runtime.InteropServices;

namespace SweetPotato {
    public static class Ole32 {

        [Flags]
        public enum CLSCTX : uint {
            CLSCTX_INPROC_SERVER = 0x1,
            CLSCTX_INPROC_HANDLER = 0x2,
            CLSCTX_LOCAL_SERVER = 0x4,
            CLSCTX_INPROC_SERVER16 = 0x8,
            CLSCTX_REMOTE_SERVER = 0x10,
            CLSCTX_INPROC_HANDLER16 = 0x20,
            CLSCTX_RESERVED1 = 0x40,
            CLSCTX_RESERVED2 = 0x80,
            CLSCTX_RESERVED3 = 0x100,
            CLSCTX_RESERVED4 = 0x200,
            CLSCTX_NO_CODE_DOWNLOAD = 0x400,
            CLSCTX_RESERVED5 = 0x800,
            CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,
            CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,
            CLSCTX_NO_FAILURE_LOG = 0x4000,
            CLSCTX_DISABLE_AAA = 0x8000,
            CLSCTX_ENABLE_AAA = 0x10000,
            CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,
            CLSCTX_ACTIVATE_32_BIT_SERVER = 0x40000,
            CLSCTX_ACTIVATE_64_BIT_SERVER = 0x80000,
            CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER,
            CLSCTX_SERVER = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER,
            CLSCTX_ALL = CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
        }

        [Flags]
        public enum STGM : int {
            DIRECT = 0x00000000,
            TRANSACTED = 0x00010000,
            SIMPLE = 0x08000000,
            READ = 0x00000000,
            WRITE = 0x00000001,
            READWRITE = 0x00000002,
            SHARE_DENY_NONE = 0x00000040,
            SHARE_DENY_READ = 0x00000030,
            SHARE_DENY_WRITE = 0x00000020,
            SHARE_EXCLUSIVE = 0x00000010,
            PRIORITY = 0x00040000,
            DELETEONRELEASE = 0x04000000,
            NOSCRATCH = 0x00100000,
            CREATE = 0x00001000,
            CONVERT = 0x00020000,
            FAILIFTHERE = 0x00000000,
            NOSNAPSHOT = 0x00200000,
            DIRECT_SWMR = 0x00400000,
        }

        public static IntPtr GuidToPointer(Guid g) {
            IntPtr ret = Marshal.AllocCoTaskMem(16);
            Marshal.Copy(g.ToByteArray(), 0, ret, 16);
            return ret;
        }

        public static Guid IID_IUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");
        public static IntPtr IID_IUnknownPtr = GuidToPointer(IID_IUnknown);

        [StructLayout(LayoutKind.Sequential)]
        public struct MULTI_QI {
            public IntPtr pIID;
            [MarshalAs(UnmanagedType.Interface)]
            public object pItf;
            public int hr;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class COSERVERINFO {
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwszName;
            public IntPtr pAuthInfo;
            public uint dwReserved2;
        }

        [DllImport("ole32.dll", PreserveSig = false, ExactSpelling = true)]
        public static extern int CreateILockBytesOnHGlobal(
                 IntPtr hGlobal,
                 [MarshalAs(UnmanagedType.Bool)] bool fDeleteOnRelease,
                 out ILockBytes ppLkbyt);

        [DllImport("ole32.dll", PreserveSig = false, ExactSpelling = true)]
        public static extern int StgCreateDocfileOnILockBytes(
                   ILockBytes plkbyt,
                   STGM grfMode,
                   uint reserved,
                   out IStorage ppstgOpen);

        [DllImport("ole32.dll", PreserveSig = false, ExactSpelling = true)]
        public static extern int CoGetInstanceFromIStorage(COSERVERINFO pServerInfo, ref Guid pclsid,
                   [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, CLSCTX dwClsCtx,
                   IStorage pstg, uint cmq, [In, Out] MULTI_QI[] rgmqResults);
    }
}
