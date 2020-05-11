using System;
using System.Collections.Generic;
using System.IO.Pipes;
using NtApiDotNet;
using NtApiDotNet.Win32.Rpc;
using NtApiDotNet.Win32.Rpc.Transport;
using NtApiDotNet.Ndr.Marshal;
using NtApiDotNet;
using NtApiDotNet.Ndr;

namespace SweetPotato.NtApiDotNet.Win32.Rpc.Transport {

    public class RpcNamedPipeTransport : IRpcClientTransport {

        public bool Connected { get; }
        public string Endpoint { get; }
        public string ProtocolSequence { get; }
        public int CallId { get; private set; }

        private NamedPipeClientStream _client;

        public RpcNamedPipeTransport(string path, SecurityQualityOfService security_quality_of_service) {

            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentException("Must specify a path to connect to");
            }

            if (path.Contains(@"\")) {
                path = path.Substring(path.LastIndexOf(@"\") + 1);
            }

            _client = new NamedPipeClientStream(path);
            _client.Connect();

            Endpoint = path;
        }

        public void Bind(Guid interface_id, Version interface_version, Guid transfer_syntax_id, Version transfer_syntax_version)
        {
            if (transfer_syntax_id != NdrNativeUtils.DCE_TransferSyntax || transfer_syntax_version != new Version(2, 0)) {
                throw new ArgumentException("Only supports DCE transfer syntax");
            }

            CallId = 1;
            BindInterface(interface_id, interface_version);
        }

        private void BindInterface(Guid interface_id, Version interface_version) {
            var bind_msg = new AlpcMessageType<LRPC_BIND_MESSAGE>(new LRPC_BIND_MESSAGE(interface_id, interface_version));
            var recv_msg = new AlpcMessageRaw(0x1000);

            using (var recv_attr = new AlpcReceiveMessageAttributes()) {

                //bind_msg.ToSafeBuffer().


                //_client.Write();

                /*
                _client.SendReceive(AlpcMessageFlags.SyncRequest, bind_msg, null, recv_msg, recv_attr, NtWaitTimeout.Infinite);
                using (var buffer = recv_msg.Data.ToBuffer()) {
                    CheckForFault(buffer, LRPC_MESSAGE_TYPE.lmtBind);
                    var value = buffer.Read<LRPC_BIND_MESSAGE>(0);
                    if (value.RpcStatus != 0) {
                        throw new NtException(NtObjectUtils.MapDosErrorToStatus(value.RpcStatus));
                    }
                }
                */
            }
        }

        public RpcClientResponse SendReceive(int proc_num, Guid objuuid, NdrDataRepresentation data_representation, byte[] ndr_buffer,
            IReadOnlyCollection<NtObject> handles)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }
        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}
