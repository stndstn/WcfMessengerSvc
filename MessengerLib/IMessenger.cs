using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace MessengerLib
{
    [ServiceContract]
    public interface IMessenger
    {
        [OperationContract]
        bool Login(string userid);
        [OperationContract]
        void Logout(string userid);
        [OperationContract]
        string GetMemberList();
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveMemberList(AsyncCallback callback, object asyncState);
        string EndReceiveMemberList(IAsyncResult result);

        [OperationContract]
        ConnectionResultData RequestConnect(string userFrom, string userTo);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveConnection(string userid, AsyncCallback callback, object asyncState);
        ConnectionRequestData EndReceiveConnection(IAsyncResult result);
        /*
        [OperationContract]
        void SendMessage(MessageData msg);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveMessage(Guid userSessionGuid, AsyncCallback callback, object asyncState);
        MessageData EndReceiveMessage(IAsyncResult result);

        [OperationContract]
        void SendStroke(StrokeData data);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveStroke(Guid userSessionGuid, AsyncCallback callback, object asyncState);
        StrokeData EndReceiveStroke(IAsyncResult result);

        [OperationContract]
        void SendBGImgChunk(BGImgChunk data);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveBGImgChunk(Guid userSessionGuid, AsyncCallback callback, object asyncState);
        BGImgChunk EndReceiveBGImgChunk(IAsyncResult result);
        */
        [OperationContract]
        void SendContentData(ContentData data);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReceiveContentData(Guid userSessionGuid, AsyncCallback callback, object asyncState);
        ContentData EndReceiveContentData(IAsyncResult result);
    }

    [DataContract]
    public struct ConnectionRequestData
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public Guid userSessionGuid;
        [DataMember]
        public string userFrom;
        [DataMember]
        public string userTo;
    }
    [DataContract]
    public struct ConnectionResultData
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public Guid userSessionGuid;
    }
    /*
    [DataContract]
    public struct MessageData
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public string message;
        [DataMember]
        public string userid;
    }
    [DataContract]
    public struct StrokeData
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public int[] x;
        [DataMember]
        public int[] y;
        [DataMember]
        public string userid;
    }
    [DataContract]
    public struct BGImgChunk
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public byte[] data;
        [DataMember]
        public int offset;
        [DataMember]
        public int len;
        [DataMember]
        public int total;
        [DataMember]
        public string userid;
    }
     * */
    [DataContract]
    public enum DataType {
        [EnumMember(Value = "Message")]
        MessageData,
        [EnumMember(Value = "Stroke")]
        StrokeData,
        [EnumMember(Value = "BGImgCk")]
        BGImgChunk,
        NotASerializableEnumeration
    }
    [DataContract]
    public struct ContentData
    {
        [DataMember]
        public Guid groupGuid;
        [DataMember]
        public string userid;
        [DataMember]
        public DataType type;
        //MessageData
        [DataMember]
        public string message;
        //StrokeData
        [DataMember]
        public int[] x;
        [DataMember]
        public int[] y;
        //BGImgChunk
        [DataMember]
        public byte[] data;
        [DataMember]
        public int offset;
        [DataMember]
        public int len;
        [DataMember]
        public int total;
    }
}
