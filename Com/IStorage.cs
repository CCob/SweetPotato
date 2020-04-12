using System;
using System.Runtime.InteropServices;

namespace SweetPotato {

    [InterfaceType(1)]
    [ComConversionLoss]
    [Guid("0000000B-0000-0000-C000-000000000046")]
    [ComImport]
    public interface IStorage {
        void CreateStream([MarshalAs(21)] [In] string pwcsName, [In] uint grfMode, [In] uint reserved1, [In] uint reserved2, [MarshalAs(28)] out IStream ppstm);
        void OpenStream([MarshalAs(21)] [In] string pwcsName, [In] IntPtr reserved1, [In] uint grfMode, [In] uint reserved2, [MarshalAs(28)] out IStream ppstm);
        void CreateStorage([MarshalAs(21)] [In] string pwcsName, [In] uint grfMode, [In] uint reserved1, [In] uint reserved2, [MarshalAs(28)] out IStorage ppstg);
        void OpenStorage([MarshalAs(21)] [In] string pwcsName, [MarshalAs(28)] [In] IStorage pstgPriority, [In] uint grfMode, [In] IntPtr snbExclude, [In] uint reserved, [MarshalAs(28)] out IStorage ppstg);
        void CopyTo([In] uint ciidExclude, [MarshalAs(42, SizeParamIndex = 0)] [In] Guid[] rgiidExclude, [In] IntPtr snbExclude, [MarshalAs(28)] [In] IStorage pstgDest);
        void MoveElementTo([MarshalAs(21)] [In] string pwcsName, [MarshalAs(28)] [In] IStorage pstgDest, [MarshalAs(21)] [In] string pwcsNewName, [In] uint grfFlags);
        void Commit([In] uint grfCommitFlags);
        void Revert();
        void EnumElements([In] uint reserved1, [In] IntPtr reserved2, [In] uint reserved3, [MarshalAs(28)] out IEnumSTATSTG ppEnum);
        void DestroyElement([MarshalAs(21)] [In] string pwcsName);
        void RenameElement([MarshalAs(21)] [In] string pwcsOldName, [MarshalAs(21)] [In] string pwcsNewName);
        void SetElementTimes([MarshalAs(21)] [In] string pwcsName, [MarshalAs(42)] [In] FILETIME[] pctime, [MarshalAs(42)] [In] FILETIME[] patime, [MarshalAs(42)] [In] FILETIME[] pmtime);
        void SetClass([In] ref Guid clsid);
        void SetStateBits([In] uint grfStateBits, [In] uint grfMask);
        void Stat([MarshalAs(42)] [Out] STATSTG[] pstatstg, [In] uint grfStatFlag);
    }
}

