#  旧Http接口改双工Socket通讯

## 老式的接口服务
最近要重新整理一些旧的SPI，发现很多SPI通讯都是客户端根据Http轮询，有些特定接口其实采用轮询的方式是很耗时且不高效，所以得想想办法优化优化。刚开始开发的时候并未想太多，后来随着客户端的请求次数增多，到后期，每个客户端都得以每秒的方式轮询是否需要处理服务端发起的任务，这效率就很低了。可能一天得请求个几千次，又或许一个客户端请求了一天也没有自己什么事。这就导致了大量的空跑，所以得想想怎么解决这低效的交互。想想Socket通讯应该是首选，成熟的框架有Websocket、SignalR、protobuf等。但是就几个接口就用上这么重的框架感觉有点小题大做，不单学习成本高，而且Dll的引用贼多。所以最后还是决定使用Newlife组件，不到200行的代码就可以实现所有需求，增加的Dll也就两个，如果使用旧版，一个Dll就可搞定。

在看本示例之前，你必须对Newlife网络通讯组件有所了解，这些是我之前参考的教程案例：

- RPC框架NewLife.ApiServer  https://newlifex.com/blood/apiserver
- 网络客户端ISocketClient    https://newlifex.com/core/socket_client
- 网络服务器NetServer        https://newlifex.com/core/netserver
- 数据包编码器PacketCodec    https://newlifex.com/core/packet_codec


这是Newlife网络组件一个测试案例，主要实现了:

- 简单的客户端验证，之前的教程都是缺少验证这块，在这里作为教程的补充
- 简单的服务推送及分发案例，对Pack包解码，方便客户端后期对服务下发命令的处理
- 简单的演示了Session的获取和处理，也是对原教程的一些补充
- 可以根据自己的需求，编写各种Control

其实代码都非常简单，都是Newlife教程里面有的，只是通过这个测试将很多实用逻辑具体化，也是对原来教程的一个补充

具体代码可参照 [ApiServerTest](ApiServerTest.cs)


Enjoy～  ^_^