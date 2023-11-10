using NewLife.Messaging;
using NewLife.Model;
using NewLife.Remoting;//注意，早期的Newlie.Core版本包含了ApiServer,最新引用NewLife.Remoting

namespace CSharpTest
{
    [TestClass]
    public class ApiServerTest
    {
        public ApiServerTest()
        {
        }
        [TestMethod]
        public void Test()
        {
            //看此案例可以先看看石头写的几篇入门教程，我这个只是细化到具体的实现
            //  RPC框架NewLife.ApiServer  https://newlifex.com/blood/apiserver
            //  网络客户端ISocketClient    https://newlifex.com/core/socket_client
            //  网络服务器NetServer        https://newlifex.com/core/netserver
            //  数据包编码器PacketCodec    https://newlifex.com/core/packet_codec


            Console.WriteLine("正在启动服务...");

            var server = new NewLife.Remoting.ApiServer(8400);

            server.Register<MyController>();
            server.Register<SecurityController>();

            server.Start();

            ObjectContainer.Current.AddSingleton(typeof(ApiServer), server);//使用默认的DI，推荐使用官方的


            Console.WriteLine("正在启动客户端...");
            var client = new MyApiClient("tcp://127.0.0.1:8400");

            //client.LoginAsync(); //废弃，这个需在MyApiClient重写 其实还是通过接口校验返回Token

            //var tokenData = client.Invoke<ReturnModel>("Security/Login", new { UserName = "Lobster", Pwd = "123" });


            ReturnModel rm = null;

            try
            {
                rm = client.Invoke<ReturnModel>("My/Add", new { x = 1, y = 2 });
            }
            catch (Exception ex)
            { }

            //别直接返回String，Newlife的Json序列化应该有bug
            var tokenData = client.Invoke<ReturnModel>("Security/Login", new { UserName = "Lobster", Pwd = "123" });

            if (tokenData.Code == 1)
                client.Token = tokenData.Data.ToString();

            rm = client.Invoke<ReturnModel>("My/Add", new { x = 1, y = 2 });

            //模拟服务端发送分发信息
            Task.Factory.StartNew(() =>
            {
                Task.Delay(60 * 1000);//等待客户端连接

                var targetServer = ObjectContainer.Provider.GetService<ApiServer>();

                if (targetServer != null)
                {
                    foreach (var session in targetServer.Server.AllSessions)
                    {
                        //session.Items.Add("aa", "cc"); //Item可以保存各种上报的临时信息，如果是IM可以保存用户名，如果是采集可以保存客户端功能，譬如采集资讯、图片
                        if (!string.IsNullOrEmpty(session.Token))
                        {
                            session.InvokeOneWay("work", new { Name = " 靓仔", Work = "Codeing" });
                        }
                    }

                    //主要是为了测试Pack的解包
                    foreach (var session in targetServer.Server.AllSessions)
                    {
                        session.InvokeOneWay("playgame", new { Name = " 靓仔", Work = "Fun" });
                    }
                }

            });

            Thread.Sleep(60 * 1000);
        }
    }

    class MyApiClient : NewLife.Remoting.ApiClient
    {
        public MyApiClient(string uri) : base(uri)
        {

        }

        protected override void OnReceive(IMessage message, ApiReceivedEventArgs e)
        {
            if (message != null && !message.Reply)//服务器推送的消息
            {
                var data = message?.Payload.ToArray();

                var pts = new PackToStr(data);

                base.OnReceive(message, e);
            }
        }
    }

    /// <summary>自定义控制器。包含多个服务</summary>
    //[Api("My")]
    class MyController : IApi, IActionFilter //继承IApi后遍可以获得Session和当前会话，之前还想着靠注入获得服务
    {

        //private ApiServer _apiServer = null;


        //public MyController(NewLife.Remoting.ApiServer apiServer)
        //{
        //    //this._apiServer = apiServer;
        //    //apiServer.Server.AllSessions[0].InvokeOneWay("")
        //    //apiServer.Server.AllSessions[0].
        //}

        public IApiSession Session { get; set; }

        /// <summary>添加，标准业务服务，走Json序列化</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public ReturnModel Add(Int32 x, Int32 y)
        {
            var ctx = ControllerContext.Current;
            var session = Session;
            return new ReturnModel { Code = 1, Msg = "成功", Data = x + y };
        }

        public void OnActionExecuted(ControllerContext filterContext)
        {
            //throw new NotImplementedException();
        }

        public void OnActionExecuting(ControllerContext filterContext)
        {
            // 请求参数
            var ps = filterContext.Parameters;

            if (!ps.ContainsKey("Token"))
            {
                filterContext.Result = new ReturnModel { Code = 0, Msg = "未登录" };
                //filterContext.Session.
                //throw new NotImplementedException();
            }

            //这里可以做Token校验，譬如Jwt令牌校验
        }
    }

    class SecurityController
    {
        public ReturnModel Login(string userName, string pwd)
        {
            //这里校验登录逻辑
            return new ReturnModel { Code = 1, Msg = "成功", Data = "KRY-CODE" };//这里返回正确校验后的令牌
        }
    }

    public class ReturnModel
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public object Data { get; set; }
    }

    public class PackToStr
    {
        public string Action { get; protected set; }
        public string Data { get; protected set; }

        public PackToStr(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            { return; }
            var len = buffer[0];
            var spiltLen = 4;

            Action = System.Text.Encoding.UTF8.GetString(buffer, 1, len);
            Data = System.Text.Encoding.UTF8.GetString(buffer, len + spiltLen + 1, buffer.Length - (len + spiltLen + 1));
        }
    }

}

