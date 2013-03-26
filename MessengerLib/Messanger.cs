using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace MessengerLib
{
    public class Messenger : IMessenger
    {
        static List<string> sUserList = new List<string>();
        static Dictionary<string, long> sUserLastAccessDict = new Dictionary<string, long>();
        static Dictionary<Guid, SessionUserContext> sSessionContextDict = new Dictionary<Guid, SessionUserContext>();
        static Queue<AutoResetEvent> sAutoEventQueueForMemberList = new Queue<AutoResetEvent>();
        //static Dictionary<Guid, List<string>> sSessionGroupMember = new Dictionary<Guid, List<string>>();
        static Dictionary<string, Queue<ConnectionRequestData>> sConnReqQueueUser = new Dictionary<string, Queue<ConnectionRequestData>>();
        static Dictionary<string, AutoResetEvent> sConnReqAutoEventUser = new Dictionary<string, AutoResetEvent>();
        static Dictionary<Guid, List<Guid>> sSessionGroupDict = new Dictionary<Guid, List<Guid>>();

        class SessionUserContext
        {
            /*
            public DataQueueAndEvent<MessageData> messageDataQueueAndEvent = null;
            public DataQueueAndEvent<StrokeData> strokeDataQueueAndEvent = null;
            public DataQueueAndEvent<BGImgChunk> imgChunkDataQueueAndEvent = null;
             */
            public DataQueueAndEvent<ContentData> contentDataQueueAndEvent = null;
            //public AutoResetEvent autoEventForConnection = null;
            //public AutoResetEvent autoEventForMemberList = null;
            //public Queue<ConnectionRequestData> connreqQueue = null;
            private string userid = string.Empty;
            public string UserId
            {
                get { return userid; } 
            }
            private Guid groupGuid;
            public Guid GroupGuid
            {
                get { return groupGuid; }
            }
            private Guid userSessionGuid;
            public Guid UserSessionGuid
            {
                get { return userSessionGuid; }
            }

            public SessionUserContext(string userid, Guid groupGuid, Guid userSessionGuid)
            {
                this.userid = userid;
                this.groupGuid = groupGuid;
                this.userSessionGuid = userSessionGuid;
                //this.autoEventForConnection = new AutoResetEvent(false);
                //this.connreqQueue = new Queue<ConnectionRequestData>();
                /*
                this.messageDataQueueAndEvent = new DataQueueAndEvent<MessageData>();
                this.strokeDataQueueAndEvent = new DataQueueAndEvent<StrokeData>();
                this.imgChunkDataQueueAndEvent = new DataQueueAndEvent<BGImgChunk>();
                 */
                this.contentDataQueueAndEvent = new DataQueueAndEvent<ContentData>();
            }
            public class DataQueueAndEvent<T>
            {
                public Queue<T> dataQueue;
                //Dictionary<Guid, AutoResetEvent> autoResetEventForGuid;
                AutoResetEvent autoResetEvent;

                public DataQueueAndEvent()
                {
                    dataQueue = new Queue<T>();
                    //autoResetEventForGuid = new Dictionary<Guid, AutoResetEvent>();
                    autoResetEvent = new AutoResetEvent(false);
                }
                //public void QueueDataAndSetEvent(T data, Guid guid)
                public void QueueDataAndSetEvent(T data)
                {
                    int lastCount = dataQueue.Count;
                    dataQueue.Enqueue(data);
                    Console.WriteLine("QueueDataAndSetEvent after enqueue size={0}", dataQueue.Count);
                    if (lastCount == 0)
                    {
                        Console.WriteLine("QueueDataAndSetEvent Set event");
                        autoResetEvent.Set();
                    }
                }
                //public T GetOrWaitDataInQueue(Guid guid)
                public T GetOrWaitDataInQueue()
                {
                    /*
                    if (autoResetEventForGuid.ContainsKey(guid) == false)
                        autoResetEventForGuid.Add(guid, new AutoResetEvent(false));
                    */
                    if (dataQueue.Count > 0)
                    {
                        Console.WriteLine("GetOrWaitDataInQueue queue size={0} before dequeue, no wait", dataQueue.Count);
                        return dataQueue.Dequeue();
                    }
                    else
                    {
                        Console.WriteLine("GetOrWaitDataInQueue wait");
                        //autoResetEventForGuid[guid].WaitOne();
                        if (autoResetEvent.WaitOne(30000) == false)
                        {
                            Console.WriteLine("GetOrWaitDataInQueue queue empty but timeout, return default");
                            return default(T);
                        }
                        Console.WriteLine("GetOrWaitDataInQueue signaled, queue size={0} before dequeue", dataQueue.Count);
                        if (dataQueue.Count == 0)
                        {
                            Console.WriteLine("GetOrWaitDataInQueue queue empty, return default");
                            return default(T);
                        }
                        return dataQueue.Dequeue();
                    }
                }
            }
        }

        public Messenger()
        {
            //instance = this;
        }

        /*
        private string getClientHostPort()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint =
                prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string host_port = string.Format("{0}:{1}", endpoint.Address, endpoint.Port);
            //convert to ipv4 addr
            System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(endpoint.Address);
            foreach (System.Net.IPAddress ipaddr in host.AddressList)
            {
                if (ipaddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    host_port = string.Format("{0}:{1}", ipaddr.ToString(), endpoint.Port);
                    return host_port;
                }
            }
            return host_port;
        }
         */
        public bool Login(string userid)
        {
            Console.WriteLine("Login called with: \"{0}\"", userid);
            //string host_port = getClientHostPort();
            if (sUserList.Contains(userid))
            {
                return false;
            }
            sUserList.Add(userid);
            long tNow = DateTime.Now.Ticks;
            sUserLastAccessDict.Add(userid, tNow);
            sConnReqQueueUser.Add(userid, new Queue<ConnectionRequestData>());
            sConnReqAutoEventUser.Add(userid, new AutoResetEvent(false));
            while (sAutoEventQueueForMemberList.Count > 0)
            {
                sAutoEventQueueForMemberList.Dequeue().Set();
            }
            return true;
        }
        public void Logout(string userid)
        {
            Console.WriteLine("Logout called with: \"{0}\"", userid);
            //string host_port = getClientHostPort();
            sUserList.Remove(userid);
            sUserLastAccessDict.Remove(userid);
            while (sAutoEventQueueForMemberList.Count > 0)
            {
                sAutoEventQueueForMemberList.Dequeue().Set();
            }
            //autoEventForMessageDictForUser.Remove(userid);
        }
        private void UserListHouseKeeping()
        {
            DateTime dtNow = DateTime.Now;
            string[] keys = new string[sUserLastAccessDict.Count];
            sUserLastAccessDict.Keys.CopyTo(keys, 0);
            foreach (string k in keys)
            {
                if (sUserLastAccessDict.ContainsKey(k))
                {
                    long v = sUserLastAccessDict[k];
                    DateTime dtLast = new DateTime(v);
                    if (dtNow - dtLast > TimeSpan.FromMinutes(3))
                    {
                        Logout(k);
                    }
                }
            }
        }
        public string GetMemberList()
        {
            Console.WriteLine("ReceiveMemberList called ");
            string list = "";
            UserListHouseKeeping();
            foreach (string member in sUserList)
            {
                if (list.Length > 0)
                    list += ",";

                list += member;
            }
            return list;
        }
        public IAsyncResult BeginReceiveMemberList(AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveMemberList called ");
            UserListHouseKeeping();
            AutoResetEvent are = new AutoResetEvent(false);
            sAutoEventQueueForMemberList.Enqueue(are);
            are.WaitOne();
            string list = "";
            foreach (string member in sUserList)
            {
                if (list.Length > 0)
                    list += ",";

                list += member;
            }
            return new CompletedAsyncResult<string>(list);
        }
        public string EndReceiveMemberList(IAsyncResult r)
        {
            CompletedAsyncResult<string> result = r as CompletedAsyncResult<string>;
            Console.WriteLine("EndReceiveMemberList called with: \"{0}\"", result.Data);
            return result.Data;
        }

        public ConnectionResultData RequestConnect(string userFrom, string userTo)
        {
            Console.WriteLine("RequestConnect called with: \"{0}\" -> \"{1}\"", userFrom, userTo);
            sUserLastAccessDict[userFrom] = DateTime.Now.Ticks;
            Guid groupGuid = Guid.NewGuid();
            Guid userSessionGuid = Guid.NewGuid();
            sSessionGroupDict.Add(groupGuid, new List<Guid>());
            sSessionGroupDict[groupGuid].Add(userSessionGuid);

            sSessionContextDict.Add(userSessionGuid, new SessionUserContext(userFrom, groupGuid, userSessionGuid));
            ConnectionRequestData connreq = new ConnectionRequestData();
            connreq.groupGuid = groupGuid;
            connreq.userFrom = userFrom;
            connreq.userTo = userTo;
            //sSessionGroupMember.Add(groupGuid, new List<string>());
            //sSessionGroupMember[groupGuid].Add(userFrom);
            sConnReqQueueUser[userTo].Enqueue(connreq);
            //raise event to notice for the client to be connected
            AutoResetEvent ae = sConnReqAutoEventUser[userTo];
            if (ae != null)
                ae.Set();

            ConnectionResultData res = new ConnectionResultData();
            res.groupGuid = groupGuid;
            res.userSessionGuid = userSessionGuid;
            return res;
        }
        public IAsyncResult BeginReceiveConnection(string userid, AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveConnection called ");
            sUserLastAccessDict[userid] = DateTime.Now.Ticks;
            sConnReqAutoEventUser[userid].WaitOne();
            ConnectionRequestData connreq = sConnReqQueueUser[userid].Dequeue();
            Guid userSessionGuid = Guid.NewGuid();
            connreq.userSessionGuid = userSessionGuid;
            sSessionContextDict.Add(userSessionGuid, new SessionUserContext(connreq.userTo, connreq.groupGuid, userSessionGuid));
            return new CompletedAsyncResult<ConnectionRequestData>(connreq);
        }
        public ConnectionRequestData EndReceiveConnection(IAsyncResult r)
        {
            CompletedAsyncResult<ConnectionRequestData> result = r as CompletedAsyncResult<ConnectionRequestData>;
            Console.WriteLine("EndReceiveConnection called with: \"{0}\"", result.Data);
            sSessionGroupDict[result.Data.groupGuid].Add(result.Data.userSessionGuid);
            return result.Data;
        }
        /*
        #region text message
        public void SendMessage(MessageData data)
        {
            Console.WriteLine("SendMessage called with: {0} \"{1}\"", data.groupGuid, data.message);
            foreach (Guid userSessionGuid in sSessionGroupDict[data.groupGuid])
            {
                if (sSessionContextDict[userSessionGuid].UserId != data.userid)
                    sSessionContextDict[userSessionGuid].messageDataQueueAndEvent.QueueDataAndSetEvent(data);
            }
        }
        public IAsyncResult BeginReceiveMessage(Guid userSessionGuid, AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveMessage called");
            MessageData data = sSessionContextDict[userSessionGuid].messageDataQueueAndEvent.GetOrWaitDataInQueue();
            return new CompletedAsyncResult<MessageData>(data);
        }
        public MessageData EndReceiveMessage(IAsyncResult r)
        {
            CompletedAsyncResult<MessageData> result = r as CompletedAsyncResult<MessageData>;
            Console.WriteLine("EndReceiveMessage called with: \"{0}\"", result.Data);
            return result.Data;
        }
        #endregion
        */
        /*
        #region stroke
        public void SendStroke(StrokeData data)
        {
            Console.WriteLine("SendStroke called with: {0} ", data);
            foreach (Guid userSessionGuid in sSessionGroupDict[data.groupGuid])
            {
                if (sSessionContextDict[userSessionGuid].UserId != data.userid)
                    sSessionContextDict[userSessionGuid].strokeDataQueueAndEvent.QueueDataAndSetEvent(data);
            }
        }
        public IAsyncResult BeginReceiveStroke(Guid userSessionGuid, AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveStroke called");
            StrokeData data = sSessionContextDict[userSessionGuid].strokeDataQueueAndEvent.GetOrWaitDataInQueue();
            return new CompletedAsyncResult<StrokeData>(data);
        }
        public StrokeData EndReceiveStroke(IAsyncResult r)
        {
            CompletedAsyncResult<StrokeData> result = r as CompletedAsyncResult<StrokeData>;
            Console.WriteLine("EndReceiveStroke called with: \"{0}\"", result.Data);
            return result.Data;
        }
        #endregion stroke
         */
        /*
        #region background image
        public void SendBGImgChunk(BGImgChunk data)
        {
            Console.WriteLine("SendBGImgChunk called {0} {1} {2}", data.offset, data.len, data.total);
            foreach (Guid userSessionGuid in sSessionGroupDict[data.groupGuid])
            {
                if (sSessionContextDict[userSessionGuid].UserId != data.userid)
                    sSessionContextDict[userSessionGuid].imgChunkDataQueueAndEvent.QueueDataAndSetEvent(data);
            }
        }
        public IAsyncResult BeginReceiveBGImgChunk(Guid userSessionGuid, AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveBGImgChunk called");
            BGImgChunk data = sSessionContextDict[userSessionGuid].imgChunkDataQueueAndEvent.GetOrWaitDataInQueue();
            return new CompletedAsyncResult<BGImgChunk>(data);
        }
        public BGImgChunk EndReceiveBGImgChunk(IAsyncResult r)
        {
            CompletedAsyncResult<BGImgChunk> result = r as CompletedAsyncResult<BGImgChunk>;
            Console.WriteLine("EndReceiveBGImgChunk called with: {0} {1} {2}", result.Data.offset, result.Data.len, result.Data.total);
            return result.Data;
        }
        #endregion
        */
        #region content data send & receive
        public void SendContentData(ContentData data)
        {
            Console.WriteLine("SendContentData called type {0} {1} {2} {3}", data.type, data.offset, data.len, data.total);
            foreach (Guid userSessionGuid in sSessionGroupDict[data.groupGuid])
            {
                if (sSessionContextDict[userSessionGuid].UserId != data.userid)
                    sSessionContextDict[userSessionGuid].contentDataQueueAndEvent.QueueDataAndSetEvent(data);
            }
            sUserLastAccessDict[data.userid] = DateTime.Now.Ticks;
        }
        public IAsyncResult BeginReceiveContentData(Guid userSessionGuid, AsyncCallback callback, object asyncState)
        {
            Console.WriteLine("BeginReceiveContentData called");
            sUserLastAccessDict[sSessionContextDict[userSessionGuid].UserId] = DateTime.Now.Ticks;
            ContentData data = sSessionContextDict[userSessionGuid].contentDataQueueAndEvent.GetOrWaitDataInQueue();
            return new CompletedAsyncResult<ContentData>(data);
        }
        public ContentData EndReceiveContentData(IAsyncResult r)
        {
            CompletedAsyncResult<ContentData> result = r as CompletedAsyncResult<ContentData>;
            Console.WriteLine("EndReceiveContentData called with: {0} {1} {2} {3}", result.Data.type, result.Data.offset, result.Data.len, result.Data.total);
            return result.Data;
        }
        #endregion
    }

    // Simple async result implementation.
    class CompletedAsyncResult<T> : IAsyncResult
    {
        T data;

        public CompletedAsyncResult(T data)
        { this.data = data; }

        public T Data
        { get { return data; } }

        #region IAsyncResult Members
        public object AsyncState
        { get { return (object)data; } }

        public WaitHandle AsyncWaitHandle
        { get { throw new Exception("The method or operation is not implemented."); } }

        public bool CompletedSynchronously
        { get { return true; } }

        public bool IsCompleted
        { get { return true; } }
        #endregion
    }
}
