using System;
using System.Runtime.InteropServices;

namespace SweetPotato {

    [Guid("00000003-0000-0000-C000-000000000046")]
    [InterfaceType(1)]
    [ComConversionLoss]
    [ComImport]
    public interface IMarshal {

        void GetUnmarshalClass([In] ref Guid riid, [In] IntPtr pv, [In] uint dwDestContext, [In] IntPtr pvDestContext, [In] uint MSHLFLAGS, out Guid pCid);
        void GetMarshalSizeMax([In] ref Guid riid, [In] IntPtr pv, [In] uint dwDestContext, [In] IntPtr pvDestContext, [In] uint MSHLFLAGS, out uint pSize);
        void MarshalInterface([MarshalAs(28)] [In] IStream pstm, [In] ref Guid riid, [In] IntPtr pv, [In] uint dwDestContext, [In] IntPtr pvDestContext, [In] uint MSHLFLAGS);
        void UnmarshalInterface([MarshalAs(28)] [In] IStream pstm, [In] ref Guid riid, out IntPtr ppv);
        void ReleaseMarshalData([MarshalAs(28)] [In] IStream pstm);
        void DisconnectObject([In] uint dwReserved);
    }
}
