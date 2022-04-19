using System;
using System.IO;
using System.Text;

namespace SweetPotato
{

    internal class RemoteObjRef
    {

        [Flags]
        enum Type : uint
        {
            Standard = 0x1,
            Handler = 0x2,
            Custom = 0x4
        }

        const uint Signature = 0x574f454d;
        public readonly Guid Guid;
        public readonly Standard StandardObjRef;

        public RemoteObjRef(Guid guid, Standard standardObjRef)
        {
            Guid = guid;
            StandardObjRef = standardObjRef;
        }

        public RemoteObjRef(byte[] objRefBytes)
        {

            BinaryReader br = new BinaryReader(new MemoryStream(objRefBytes), Encoding.Unicode);

            if (br.ReadUInt32() != Signature)
            {
                throw new InvalidDataException("Does not look like an OBJREF stream");
            }

            uint flags = br.ReadUInt32();
            Guid = new Guid(br.ReadBytes(16));

            if ((Type)flags == Type.Standard)
            {
                StandardObjRef = new Standard(br);
            }
        }

        public byte[] GetBytes()
        {
            BinaryWriter bw = new BinaryWriter(new MemoryStream());

            bw.Write(Signature);
            bw.Write((uint)1);
            bw.Write(Guid.ToByteArray());

            StandardObjRef.Save(bw);

            return ((MemoryStream)bw.BaseStream).ToArray();
        }

        internal class SecurityBinding
        {

            public readonly ushort AuthnSvc;
            public readonly ushort AuthzSvc;
            public readonly string PrincipalName;

            public SecurityBinding(ushort authnSvc, ushort authzSnc, string principalName)
            {
                AuthnSvc = authnSvc;
                AuthzSvc = authzSnc;
                PrincipalName = principalName;
            }

            public SecurityBinding(BinaryReader br)
            {

                AuthnSvc = br.ReadUInt16();
                AuthzSvc = br.ReadUInt16();
                char character;
                string principalName = "";

                while ((character = br.ReadChar()) != 0)
                {
                    principalName += character;
                }

                br.ReadChar();
            }


            public byte[] GetBytes()
            {
                BinaryWriter bw = new BinaryWriter(new MemoryStream(), Encoding.Unicode);

                bw.Write(AuthnSvc);
                bw.Write(AuthzSvc);

                if (PrincipalName != null && PrincipalName.Length > 0)
                    bw.Write(Encoding.Unicode.GetBytes(PrincipalName));

                bw.Write((char)0);
                bw.Write((char)0);

                return ((MemoryStream)bw.BaseStream).ToArray();
            }
        }

        internal class StringBinding
        {
            public readonly TowerProtocol TowerID;
            public readonly string NetworkAddress;

            public StringBinding(TowerProtocol towerID, string networkAddress)
            {
                TowerID = towerID;
                NetworkAddress = networkAddress;
            }

            public StringBinding(BinaryReader br)
            {
                TowerID = (TowerProtocol)br.ReadUInt16();
                char character;
                string networkAddress = "";

                while ((character = br.ReadChar()) != 0)
                {
                    networkAddress += character;
                }

                br.ReadChar();
                NetworkAddress = networkAddress;
            }

            internal byte[] GetBytes()
            {
                BinaryWriter bw = new BinaryWriter(new MemoryStream(), Encoding.Unicode);

                bw.Write((ushort)TowerID);
                bw.Write(Encoding.Unicode.GetBytes(NetworkAddress));
                bw.Write((char)0);
                bw.Write((char)0);
                return ((MemoryStream)bw.BaseStream).ToArray();
            }
        }

        internal class DualStringArray
        {
            private readonly ushort NumEntries;
            private readonly ushort SecurityOffset;
            public readonly StringBinding StringBinding;
            public readonly SecurityBinding SecurityBinding;

            public DualStringArray(StringBinding stringBinding, SecurityBinding securityBinding)
            {
                NumEntries = (ushort)((stringBinding.GetBytes().Length + securityBinding.GetBytes().Length) / 2);
                SecurityOffset = (ushort)(stringBinding.GetBytes().Length / 2);

                StringBinding = stringBinding;
                SecurityBinding = securityBinding;
            }

            public DualStringArray(BinaryReader br)
            {
                NumEntries = br.ReadUInt16();
                SecurityOffset = br.ReadUInt16();

                StringBinding = new StringBinding(br);
                SecurityBinding = new SecurityBinding(br);
            }

            internal void Save(BinaryWriter bw)
            {

                byte[] stringBinding = StringBinding.GetBytes();
                byte[] securityBinding = SecurityBinding.GetBytes();

                bw.Write((ushort)((stringBinding.Length + securityBinding.Length) / 2));
                bw.Write((ushort)(stringBinding.Length / 2));
                bw.Write(stringBinding);
                bw.Write(securityBinding);
            }
        }

        internal class Standard
        {

            public readonly uint Flags;
            public readonly uint PublicRefs;
            public readonly Guid IPID;
            public readonly DualStringArray DualStringArray;

            public Standard(uint flags, uint publicRefs, DualStringArray dualStringArray)
            {
                Flags = flags;
                PublicRefs = publicRefs;
                DualStringArray = dualStringArray;
            }

            public Standard(BinaryReader br)
            {
                Flags = br.ReadUInt32();
                PublicRefs = br.ReadUInt32();

                DualStringArray = new DualStringArray(br);
            }

            internal void Save(BinaryWriter bw)
            {
                bw.Write(Flags);
                bw.Write(PublicRefs);
                bw.Write(Guid.NewGuid().ToByteArray());
                bw.Write(Guid.NewGuid().ToByteArray());
                DualStringArray.Save(bw);
            }
        }
    }
}
