using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using NewLife;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace CSharpTest
{
    //本加密是基于Netcore版的，NetFramework版多少有些出入，当然基于Bouncy Castle组件可以做到一致，但网上资料偏少
    //代码主要是看DemoClient和DemoWebApi，下边的测试案例主要是列举市面上比较常用的hash和加解密
    //如果查看代码会觉得有点画蛇添足，一个简单的事复杂化，这是因为矛和盾你都知道了，实际是客户端（ios、android）代码需要so层加密混淆和隐藏一些敏感的公钥和字符串
    //其实本案例可以不用引用Bouncy Castle组件，只是为了Rsa能生成密钥串，还有方便老平台升级到最新的Pkcs#8
    [TestClass]
    public class SecurityTest
    {
        private string _apiKey = "com.newlife.app".MD5_16();//举个例子，可以写得复杂点
        private string _iv= "BBBBBBBB";

        public SecurityTest()
        {
           

        }

        //这是模拟客户端提交，实际应该是android或者ios编写
        [TestMethod]
        public void DemoClient()
        {
            //客户端
            //  1.设计几个参与计算签名的参数，时间戳肯定要加，有利于签名每次提交不一样
            //  2.对核心参数进行加密（排除参与签名计算），使用对称和非对称也行，非对称每次加密的结果也不一样，防止抓包查看提交值
            //  3.android最好把密钥和加密方法写在so层，密钥最好也使用其他工具base64一下，不要明文变量

            var publicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3c1ohhCSfMr1TFizwg0e
teLHoZIG8saYSyOgGkhdO5jK7pLWDDq3B41UvsovSyVBhq8vpfofHmSrM88gyA4U
d8DBvdNLithpuim8nuMbGTtS+SxboXWVHTwgSkrMT4JaJYfpJjWUMw7N4dtjLtRE
4ZdyME6y6KN9CP3RCp/cH463AlXiweSWEnaiLM8jOHBIHeudiHVpPZlIH5UKEtS2
l/o0dI/z30j6yykkTcL0xIRs2mmBX+GYFE6lyBk29acbSzJICS5c9QAOxgHxPzc/
ASZfxuj1Pjs77rcupXXp/aQ7UqMMo9JEd2LFGaLXcLG6eEGLFtmaUQYwTnZ8uzyV
JwIDAQAB
-----END PUBLIC KEY-----";

            //只模拟get，post都是同理
            var sb = new System.Text.StringBuilder();

            DateTime now = DateTime.UtcNow;
            long ts = now.Ticks / TimeSpan.TicksPerSecond;
            //使用非对称加密
            var secretKey = "「设备号」".MD5().Substring(0, 24);//可以用户名、设备号、新闻Id等等

            var queryData = "{\"nid\":116}";
            var qdEncryptData = EncryptUtilis.TripleDESEncrypt(queryData.GetBytes(), secretKey.GetBytes(), CipherMode.CBC, _iv.GetBytes()).ToBase64();
            var rsaEncryptInB64 = NewLife.Security.RSAHelper.Encrypt(secretKey.GetBytes(), publicKey).ToBase64();

            var sign = $"api_key={_apiKey}&ts={ts}##abc".MD5();//##abc是干扰字符串，一般都是拿到参数再按字典排序


            sb.Append($"api_key={_apiKey}");
            sb.Append($"&ts={ts}");
            sb.Append($"&sk={System.Web.HttpUtility.UrlEncode(rsaEncryptInB64)}");
            sb.Append($"&qd={System.Web.HttpUtility.UrlEncode(qdEncryptData)}");//这个放在post参数会更合理
            sb.Append($"&sig={sign}");

            //可以添加更多，可以加入到sign值计算
            //隐藏点可以添加到请求Head，不过都是程序员为难逆向

        }

        [TestMethod]
        public void DemoWebApi()
        {
            
            var privateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDdzWiGEJJ8yvVM
WLPCDR614sehkgbyxphLI6AaSF07mMruktYMOrcHjVS+yi9LJUGGry+l+h8eZKsz
zyDIDhR3wMG900uK2Gm6Kbye4xsZO1L5LFuhdZUdPCBKSsxPglolh+kmNZQzDs3h
22Mu1EThl3IwTrLoo30I/dEKn9wfjrcCVeLB5JYSdqIszyM4cEgd652IdWk9mUgf
lQoS1LaX+jR0j/PfSPrLKSRNwvTEhGzaaYFf4ZgUTqXIGTb1pxtLMkgJLlz1AA7G
AfE/Nz8BJl/G6PU+Ozvuty6lden9pDtSowyj0kR3YsUZotdwsbp4QYsW2ZpRBjBO
dny7PJUnAgMBAAECggEAYGFnsAn3fZ675McOpZ4J4EOBN+Y6erhtaQk8Au+7A7Cr
Tewkcv/4lKGHV7iHwIGZ7aqma9s6NnzMICE7P3NO1ZK/HXt5cgYEO07zlZ9eISK0
NW5fCtQLTb7Y0S+bhFlCTti2KVJS6bTfJeutN6YpoFWs2uz3tTuFk6inc5RvlI4i
C0jMtatKLq/bZj8egNq1IAv/9djwtGdDIQ7J5fvu7Szm4xOGQj1+bbEhTTEq6KwX
VjZgGwY3ac5Z/RrZJOpm21VKgzySOT3hvg2iIkbX5ESmXNyePl6Lb0exSxfCXC/r
QyOjemdKMkaxXH9W4I5fvdyy7enZrfGE+ZRj0XXbcQKBgQD5pKCoggSjbF8gIVLE
XFbg9TFhUii9NHfiWKSyDLX3apx05YFtXtgSqXEHuBHs7ZppwgqvCYwKw3eFvbVx
2ZwmsZs+xNmJDgk+o/Jmoek2ns/VewW+Mb3VooIBVd+Q+y3QVrXia8iUs6cize4X
PvNynOuJ+n0dmpXZb1UY70k0iwKBgQDjc0r5Enp1ewzdH9599ptoZJGaYuot4meJ
euhDRPm4Y6VNpTiMPTPc3cgDksZVpzUXaJOM8cs8vBFlcY/+3ArZgiBXc7IbR5li
6ZhN79YvQyPVmJOF6bGcnPnuhZqmSG76yXOtOt8gGZd2wiTfQzmz0TpYiUB2Pfz2
EFywdafJVQKBgGTbIuERLiiMBt5nOBYGrD7UMG/+DmFqSijS4S7hvb5Ifw8nDaQP
FkJr3DNfJTbQQ3sInuJafA55K9eHbj+hx9lGFc9FHhGl7ww0liDqttqPTK4VtptB
Y01lCxrZA6qCH980uOTR4MZa0mJYSiFwGFCsnugun8+O/Y1L0lhxi+TDAoGAF+mF
Fk51BWjcX1r8Xy3QBNed3ydLC2vkCrYbOQdFYbdIJ7OZEFVW4H+Iiaeiplqf4Egk
SwsYnNgA1DNuOhMUKO3fTJJjRS7v11BLrNXsJKfgWpJh4BBDlf2C4Sq2qYiv8jm/
qZSo5I0MOXYLvlUo+dijU6+KUWQO9ieeNwcHjskCgYB8e0lg3eVz1uzR3PcRKONb
ZiCtTaY8SulRnAt0iuBIDAGYmVbmY1o2F5+mIuIIFwMZO7nGQZYEFmLuQnZYvaRE
UepinAVpd0oKLOXgoC27QrWSHmKloyTdXCNRtVZy/l5pvmAklJ9kewMu09luLyg2
wTPGcyZmj8KXAeS0KjTSxw==
-----END PRIVATE KEY-----";

            //固定样本
            var query = "api_key=F1036BC5B36EE20A&ts=63835477793&sk=EiTHc4M0l4NEbf6A4oi1D2x6QhXKUPAqGU92OhvQ36XgHobrbSF4%2bgHDrIh6xcrHlqEftssYiWFAfKKe2cI%2fuinTaroyN1pcN7V2Q2XNvJFwjGYnxjcCxof%2fw%2b10kulRb7HZKuMOIjyLfJeEbPgwTLXmko1mxGjkIlDUSQQcLGHrjoIvMAY77QYNtraXzBnLIJTDXbMzAycgWWd039UAjMMLALiNn9pUjGwfA1Bf57kYkFSVQkfz%2fut%2bwsNF3VBeSz6%2bIvMM0HEWEo9OENA7Y7Swzn53g6CYEAkje9eCkKTF064ixyKAVE5shlpcunGkpinTGYbcFMe9NbOWKYeckA%3d%3d&qd=HRsDM4xaVkjHs7c1NZgDvA%3d%3d&sig=5A157D0B133B57CE61941A5B1C5705B6";

            var ql = query.Split("&");

            var apiKey = ql.Find(x => { return x.StartsWith("api_key"); }).Split("=")[1];
            var ts = ql.Find(x => { return x.StartsWith("ts"); }).Split("=")[1];
            var sig= ql.Find(x => { return x.StartsWith("sig"); }).Split("=")[1];


            ////可以校验一下ts时间戳是否是很久之前的，预防别人固定一个ts值，达到sig值不会变
            //if (ts.ToDateTime() < DateTime.Now.AddDays(-1))
            //{
            //    //返回报错
            //}

            //先校验签名值
            var sign = $"api_key={apiKey}&ts={ts}##abc".MD5();


            if (sig == sign)
            {
                var skEncrypt= ql.Find(x => { return x.StartsWith("sk"); }).Split("=")[1];
                var qdEncrypt= ql.Find(x => { return x.StartsWith("qd"); }).Split("=")[1];

                skEncrypt = System.Web.HttpUtility.UrlDecode(skEncrypt);
                qdEncrypt = System.Web.HttpUtility.UrlDecode(qdEncrypt);

                //解密请求参数
                var secretKey = NewLife.Security.RSAHelper.Decrypt(skEncrypt.ToBase64(), privateKey).ToStr();
                var queryData = EncryptUtilis.TripleDESDecrypt(qdEncrypt.ToBase64(), secretKey.GetBytes(), CipherMode.CBC, _iv.GetBytes()).ToStr();

                //结果为	queryData	"{\"nid\":116}"
                //继续写返回数据的逻辑

            }



        }


        #region >Hash<
        //第三方哈希值在线工具    https://emn178.github.io/online-tools/
        [TestMethod]
        public void TestHash()
        {
            var testParams = "name=lobster&age=18";

            //需using NewLife;
            var signForMd5U32 = testParams.MD5();
            var signForMd5U16 = testParams.MD5_16();

            var signForSha256NoKey = SHA256(testParams).ToHex();
            //HMAC-sha256
            var signForSha256NullKey = testParams.GetBytes().SHA256("".GetBytes()).ToHex();
            //如果复杂点可以制作16位key值
            var key = "AAAAAAAABBBBBB";//可以自定义十六个字节长度字符窜或者byte[16]
            var signForSha256WithKey = testParams.GetBytes().SHA256(key.GetBytes()).ToHex();

            //更多的SHA1、SHA384、SHA512就不举例了，根SHA256一样，如果要用SHA224，那就得引用BouncyCastle库了 下面注释bouncyCastleSHA224可以有大概的实现

            //小众的
            var signForCrc32 = testParams.GetBytes().Crc().ToString();
        }


        public static byte[] SHA256(string data)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            return System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);
        }
        /** 
        * Bouncy Castle实现sha-224 
        */
        //public static String bouncyCastleSHA224(String src)
        //{
        //    var bytes = Encoding.UTF8.GetBytes(src);
        //    var digest = new Org.BouncyCastle.Crypto.Digests.Sha224Digest();
        //    digest.BlockUpdate(bytes, 0, bytes.Length);
        //    byte[] sha224Bytes = new byte[digest.GetDigestSize()];
        //    digest.DoFinal(sha224Bytes, 0);
        //    return String.Concat(sha224Bytes.Select(x => x.ToString("x2")));
        //}
        #endregion

        #region >对称加密<
        //第三方加解密在线工具  http://tool.chacuo.net/cryptdes  
        [TestMethod]
        public void TestSymmetricEncrypt()
        {
            //Rijndael (pronounced rain-dahl) is an Advanced Encryption Standard (AES) algorithm.
            //  It replaced the older and weaker Data Encryption Standard (DES)
            //  when it was selected as the standard symmetric key encryption algorithm by the National Institute of Standards and Technology (NIST).
            //对称加密 3des、aes、des 推荐3des > aes > des
            //  加密模式推荐ECB和CBC就行了，注意，ECB不需要提供IV
            //  如需把密文变成base64编码时要注意 加密时的编码 UTF8和GB2312最终的base64的密文不一样，如果不注意这点很容易导致客户端解密失败

            var text = "我是一段明文";

            var secretKey = "AAAAAAAAAAAASSSSSSSSSSSS"; //24个字节字符串，方便保存，可以是其他自定义，总之最后是变成byte[24],要注意编码！
            var iv = "BBBBBBBB";// 8个字节字符串，方便保存，可以是其他自定义，总之最后是变成byte[24],要注意编码！(ECB 模式不需要)

            //3des加密
            //这里GetBytes是使用UTF8编码
            var encryptDataInB64 = EncryptUtilis.TripleDESEncrypt(text.GetBytes(), secretKey.GetBytes(), CipherMode.CBC, iv.GetBytes()).ToBase64();

            //3des解密
            var decryptText = EncryptUtilis.TripleDESDecrypt(encryptDataInB64.ToBase64(), secretKey.GetBytes(), CipherMode.CBC, iv.GetBytes()).ToStr();
            Assert.AreEqual<string>(text, decryptText);

            var aesSecretKey = "AAAAAAAASSSSSSS";//aes密钥长度是16
            var aesIv = "2222222266666666"; //aes向量长度和密钥长度一样
            //aes加密
            var aesEncryptDataInB64 = EncryptUtilis.AESEncrypt(text.GetBytes(), secretKey.GetBytes(), CipherMode.CBC, aesIv.GetBytes()).ToBase64();

            //aes解密
            var aesDecryptText = EncryptUtilis.AESDecrypt(aesEncryptDataInB64.ToBase64(), secretKey.GetBytes(), CipherMode.CBC, aesIv.GetBytes()).ToStr();
            Assert.AreEqual<string>(text, aesDecryptText);



            secretKey = "AAAAAAAA";//des的密钥长度只有8位
            //des加密
            var desEncryptDataInB64 = EncryptUtilis.DESEncrypt(text.GetBytes(), secretKey.GetBytes(), CipherMode.CBC, iv.GetBytes()).ToBase64();

            //des解密
            var desDecryptText = EncryptUtilis.DESDecrypt(desEncryptDataInB64.ToBase64(), secretKey.GetBytes(), CipherMode.CBC, iv.GetBytes()).ToStr();
            Assert.AreEqual<string>(text, desDecryptText);


            //总结(不太严谨，自己考究)：net对称加密比较乱，早期的Aes加密不支持128位以上的加密，所以出了个RijndaelManaged作为补充，现在netcore又回归到Aes类
            //  所以使用XXXCryptoServiceProvider和RijndaelManaged都是提示过时的
            //  对称加密一般用于文本加密，特别是ECB模式，加密速度比起非对称有天然的优势，大文本加密优先使用对称加密
            //  对称加密一般都是很容易逆向，只能防抓包，并不能百分百做到可靠性防御

        }

        #endregion

        #region >非对称加密<
        [TestMethod]
        public void TestAsymmetricEncrypt()
        {
            //这里只演示常规非对称加密的使用，所以使用CA证书加密校验可以不用看。证书都是程序自动生成的，或者使用OpenSSL生成的
            //  这里的证书格式推荐使用pem格式，系统自动生成的xml除非是C#写的客户端，不然android使用得转一遍
            //  默认一般是公钥加密私钥解密 私钥签名公钥匙验证，如果非得反着来也是可行的，只不过存在被破解密文的风险
            //  非对称加密之所以有难度，第一密钥的格式有好几种，第二，每次同一个字符串加密后的密文都不一样的，增加了跟客户端核对的成本，特别在字符串编码上
            //  非对称加密一般用户短字符加密，譬如密码和aes的key值，不适合长文本加密，最长现在主流的2048，也大概支持214字节左右长度的明文，大于这个还得切分成多片
            //  下面演示加密 aes的key值

            //以下加密参数 keySize=2048 字符编码UTF-8
            //生成密钥对 密钥对每次生成都不一样的，需要保存起来

            var rsa = System.Security.Cryptography.RSA.Create(2048);

            var xmlPrivateKey = rsa.ToXmlString(true);//私钥已包含公钥信息（不推荐使用）
            var xmlPublicKey = rsa.ToXmlString(false);
            //基于BouncyCastle组件转换
            var demPublicKey = RsaKeyConvert.PublicKeyXmlToPem(xmlPublicKey);//Pkcs#1和Pkcs#8的公钥是一样的，只是私钥不一样

            var demPkcs1PrivateKey = RsaKeyConvert.PrivateKeyXmlToPkcs1(xmlPrivateKey);//推荐使用Pkcs#8
            var demPkcs8PrivateKey = RsaKeyConvert.PrivateKeyXmlToPkcs8(xmlPrivateKey);//记得保存一下，每次值都不一样

            var secretKey = "AAAAAAAAAAAASSSSSSSSSSSS"; //aes密钥， 流程是客户端随机生成24字节的aes密钥，然后将提交的Header或者Post参数加密 这样可以隐藏aes的明文密钥

            //使用newlife组件加密
            var nlEncrypt = NewLife.Security.RSAHelper.Encrypt(secretKey.GetBytes(), demPublicKey).ToBase64();
            //使用newlife组件解密
            var nlDecrypt = NewLife.Security.RSAHelper.Decrypt(nlEncrypt.ToBase64(), demPkcs8PrivateKey).ToStr();

            //使用BouncyCastle
            var bcEncrypt = BouncyCastleUtilis.RsaEncrypt(secretKey.GetBytes(), demPublicKey).ToBase64();
            var bcDecrypt = BouncyCastleUtilis.RsaDecrypt(bcEncrypt.ToBase64(), demPkcs8PrivateKey).ToStr();

            //注意，密文跟密钥对是一起的，不然随便生成的密钥对是解不了上一个密钥对的密码





        }


        #endregion
    }

    public class EncryptUtilis
    {
        //3des加密
        public static byte[] TripleDESEncrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var tDes = System.Security.Cryptography.TripleDES.Create();//NetFramework是TripleDESCryptoServiceProvider类
            return SymmetricEncrypt(tDes, data, pass, mode, iv);
        }
        //3des解密
        public static byte[] TripleDESDecrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var tDes = System.Security.Cryptography.TripleDES.Create();//NetFramework是TripleDESCryptoServiceProvider类
            return SymmetricDecrypt(tDes, data, pass, mode, iv);
        }

        //aes加密
        public static byte[] AESEncrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var aes = System.Security.Cryptography.Aes.Create();//NetFramework是TripleDESCryptoServiceProvider类
            return SymmetricEncrypt(aes, data, pass, mode, iv);
        }
        //aes解密
        public static byte[] AESDecrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var aes = System.Security.Cryptography.Aes.Create();//NetFramework是TripleDESCryptoServiceProvider类
            return SymmetricDecrypt(aes, data, pass, mode, iv);
        }

        //des加密
        public static byte[] DESEncrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var des = System.Security.Cryptography.DES.Create();
            return SymmetricEncrypt(des, data, pass, mode, iv);
        }
        public static byte[] DESDecrypt(byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            var des = System.Security.Cryptography.DES.Create();
            return SymmetricEncrypt(des, data, pass, mode, iv);
        }


        #region >对称加密核心<

        private static byte[] SymmetricEncrypt(SymmetricAlgorithm algorithm, byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            algorithm.Key = pass;
            algorithm.Mode = mode;

            if (mode == CipherMode.CBC)
            {
                algorithm.IV = iv;
            }

            algorithm.Padding = PaddingMode.PKCS7; //填充一般都是PKCS7，如果有特殊需求可以传参

            var transform = algorithm.CreateEncryptor();
            return transform.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] SymmetricDecrypt(SymmetricAlgorithm algorithm, byte[] data, byte[] pass, CipherMode mode = CipherMode.ECB, byte[] iv = null)
        {
            algorithm.Key = pass;
            algorithm.Mode = mode;

            if (mode == CipherMode.CBC)
            {
                algorithm.IV = iv;
            }

            algorithm.Padding = PaddingMode.PKCS7; //填充一般都是PKCS7，如果有特殊需求可以传参

            var transform = algorithm.CreateDecryptor();
            return transform.TransformFinalBlock(data, 0, data.Length);
        }

        #endregion

    }

    public class BouncyCastleUtilis
    {
        public static String SHA224(String src)
        {
            var bytes = Encoding.UTF8.GetBytes(src);
            var digest = new Org.BouncyCastle.Crypto.Digests.Sha224Digest();
            digest.BlockUpdate(bytes, 0, bytes.Length);
            var sha224Bytes = new byte[digest.GetDigestSize()];
            digest.DoFinal(sha224Bytes, 0);
            return String.Concat(sha224Bytes.Select(x => x.ToString("x2")));//ToHex
        }
        //只支持dem格式，如果是xml可以转一遍再加密
        public static Byte[] RsaEncrypt(Byte[] data, string pubKey)
        {
            var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(2048);
            rsa.ImportParameters(CreateRsapFromPublicKey(pubKey));
            return rsa.Encrypt(data, true);
        }

        private static RSAParameters CreateRsapFromPublicKey(string puKey)
        {
            puKey = RsaPemFormatHelper.PublicKeyFormatRemove(puKey);
            RsaKeyParameters publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(puKey));
            var rsap = new RSAParameters();
            rsap.Modulus = publicKeyParam.Modulus.ToByteArrayUnsigned();
            rsap.Exponent = publicKeyParam.Exponent.ToByteArrayUnsigned();
            return rsap;
        }

        public static Byte[] RsaDecrypt(Byte[] data, string priKey)
        {
            var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(2048);
            rsa.ImportParameters(CreateRsapFromPrivateKey(priKey));
            return rsa.Decrypt(data, true);
        }

        private static  RSAParameters CreateRsapFromPrivateKey(string priKey)
        {
            priKey = RsaPemFormatHelper.Pkcs8PrivateKeyFormatRemove(priKey);
            RsaPrivateCrtKeyParameters privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(priKey));

            var rsap = new RSAParameters();
            rsap.Modulus = privateKeyParam.Modulus.ToByteArrayUnsigned();
            rsap.Exponent = privateKeyParam.PublicExponent.ToByteArrayUnsigned();
            rsap.P = privateKeyParam.P.ToByteArrayUnsigned();
            rsap.Q = privateKeyParam.Q.ToByteArrayUnsigned();
            rsap.DP = privateKeyParam.DP.ToByteArrayUnsigned();
            rsap.DQ = privateKeyParam.DQ.ToByteArrayUnsigned();
            rsap.InverseQ = privateKeyParam.QInv.ToByteArrayUnsigned();
            rsap.D = privateKeyParam.Exponent.ToByteArrayUnsigned();

            return rsap;
        }

    }

    /// <summary>
    /// RSA Key Convert Class
    /// Author:Zhiqiang Li
    /// </summary>
    public class RsaKeyConvert
    {
        /// <summary>
        /// Public Key Convert pem->xml
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static string PublicKeyPemToXml(string publicKey)
        {
            publicKey = RsaPemFormatHelper.PublicKeyFormat(publicKey);

            PemReader pr = new PemReader(new StringReader(publicKey));
            var obj = pr.ReadObject();
            if (!(obj is RsaKeyParameters rsaKey))
            {
                throw new Exception("Public key format is incorrect");
            }

            XElement publicElement = new XElement("RSAKeyValue");
            //Modulus
            XElement pubmodulus = new XElement("Modulus", Convert.ToBase64String(rsaKey.Modulus.ToByteArrayUnsigned()));
            //Exponent
            XElement pubexponent = new XElement("Exponent", Convert.ToBase64String(rsaKey.Exponent.ToByteArrayUnsigned()));

            publicElement.Add(pubmodulus);
            publicElement.Add(pubexponent);
            return publicElement.ToString();
        }

        /// <summary>
        /// Public Key Convert xml->pem
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static string PublicKeyXmlToPem(string publicKey)
        {
            XElement root = XElement.Parse(publicKey);
            //Modulus
            var modulus = root.Element("Modulus");
            //Exponent
            var exponent = root.Element("Exponent");

            RsaKeyParameters rsaKeyParameters = new RsaKeyParameters(false, new BigInteger(1, Convert.FromBase64String(modulus.Value)), new BigInteger(1, Convert.FromBase64String(exponent.Value)));

            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            pWrt.WriteObject(rsaKeyParameters);
            pWrt.Writer.Close();
            return sw.ToString();
        }

        /// <summary>
        /// Private Key Convert Pkcs1->xml
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs1ToXml(string privateKey)
        {
            privateKey = RsaPemFormatHelper.Pkcs1PrivateKeyFormat(privateKey);

            PemReader pr = new PemReader(new StringReader(privateKey));
            if (!(pr.ReadObject() is AsymmetricCipherKeyPair asymmetricCipherKeyPair))
            {
                throw new Exception("Private key format is incorrect");
            }
            RsaPrivateCrtKeyParameters rsaPrivateCrtKeyParameters =
                (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(
                    PrivateKeyInfoFactory.CreatePrivateKeyInfo(asymmetricCipherKeyPair.Private));

            XElement privatElement = new XElement("RSAKeyValue");
            //Modulus
            XElement primodulus = new XElement("Modulus", Convert.ToBase64String(rsaPrivateCrtKeyParameters.Modulus.ToByteArrayUnsigned()));
            //Exponent
            XElement priexponent = new XElement("Exponent", Convert.ToBase64String(rsaPrivateCrtKeyParameters.PublicExponent.ToByteArrayUnsigned()));
            //P
            XElement prip = new XElement("P", Convert.ToBase64String(rsaPrivateCrtKeyParameters.P.ToByteArrayUnsigned()));
            //Q
            XElement priq = new XElement("Q", Convert.ToBase64String(rsaPrivateCrtKeyParameters.Q.ToByteArrayUnsigned()));
            //DP
            XElement pridp = new XElement("DP", Convert.ToBase64String(rsaPrivateCrtKeyParameters.DP.ToByteArrayUnsigned()));
            //DQ
            XElement pridq = new XElement("DQ", Convert.ToBase64String(rsaPrivateCrtKeyParameters.DQ.ToByteArrayUnsigned()));
            //InverseQ
            XElement priinverseQ = new XElement("InverseQ", Convert.ToBase64String(rsaPrivateCrtKeyParameters.QInv.ToByteArrayUnsigned()));
            //D
            XElement prid = new XElement("D", Convert.ToBase64String(rsaPrivateCrtKeyParameters.Exponent.ToByteArrayUnsigned()));

            privatElement.Add(primodulus);
            privatElement.Add(priexponent);
            privatElement.Add(prip);
            privatElement.Add(priq);
            privatElement.Add(pridp);
            privatElement.Add(pridq);
            privatElement.Add(priinverseQ);
            privatElement.Add(prid);

            return privatElement.ToString();
        }

        /// <summary>
        /// Private Key Convert xml->Pkcs1
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyXmlToPkcs1(string privateKey)
        {
            XElement root = XElement.Parse(privateKey);
            //Modulus
            var modulus = root.Element("Modulus");
            //Exponent
            var exponent = root.Element("Exponent");
            //P
            var p = root.Element("P");
            //Q
            var q = root.Element("Q");
            //DP
            var dp = root.Element("DP");
            //DQ
            var dq = root.Element("DQ");
            //InverseQ
            var inverseQ = root.Element("InverseQ");
            //D
            var d = root.Element("D");

            RsaPrivateCrtKeyParameters rsaPrivateCrtKeyParameters = new RsaPrivateCrtKeyParameters(
                new BigInteger(1, Convert.FromBase64String(modulus.Value)),
                new BigInteger(1, Convert.FromBase64String(exponent.Value)),
                new BigInteger(1, Convert.FromBase64String(d.Value)),
                new BigInteger(1, Convert.FromBase64String(p.Value)),
                new BigInteger(1, Convert.FromBase64String(q.Value)),
                new BigInteger(1, Convert.FromBase64String(dp.Value)),
                new BigInteger(1, Convert.FromBase64String(dq.Value)),
                new BigInteger(1, Convert.FromBase64String(inverseQ.Value)));

            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            pWrt.WriteObject(rsaPrivateCrtKeyParameters);
            pWrt.Writer.Close();
            return sw.ToString();

        }


        /// <summary>
        /// Private Key Convert Pkcs8->xml
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs8ToXml(string privateKey)
        {
            privateKey = RsaPemFormatHelper.Pkcs8PrivateKeyFormatRemove(privateKey);
            RsaPrivateCrtKeyParameters privateKeyParam =
                (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));

            XElement privatElement = new XElement("RSAKeyValue");
            //Modulus
            XElement primodulus = new XElement("Modulus", Convert.ToBase64String(privateKeyParam.Modulus.ToByteArrayUnsigned()));
            //Exponent
            XElement priexponent = new XElement("Exponent", Convert.ToBase64String(privateKeyParam.PublicExponent.ToByteArrayUnsigned()));
            //P
            XElement prip = new XElement("P", Convert.ToBase64String(privateKeyParam.P.ToByteArrayUnsigned()));
            //Q
            XElement priq = new XElement("Q", Convert.ToBase64String(privateKeyParam.Q.ToByteArrayUnsigned()));
            //DP
            XElement pridp = new XElement("DP", Convert.ToBase64String(privateKeyParam.DP.ToByteArrayUnsigned()));
            //DQ
            XElement pridq = new XElement("DQ", Convert.ToBase64String(privateKeyParam.DQ.ToByteArrayUnsigned()));
            //InverseQ
            XElement priinverseQ = new XElement("InverseQ", Convert.ToBase64String(privateKeyParam.QInv.ToByteArrayUnsigned()));
            //D
            XElement prid = new XElement("D", Convert.ToBase64String(privateKeyParam.Exponent.ToByteArrayUnsigned()));

            privatElement.Add(primodulus);
            privatElement.Add(priexponent);
            privatElement.Add(prip);
            privatElement.Add(priq);
            privatElement.Add(pridp);
            privatElement.Add(pridq);
            privatElement.Add(priinverseQ);
            privatElement.Add(prid);

            return privatElement.ToString();
        }

        /// <summary>
        /// Private Key Convert xml->Pkcs8
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyXmlToPkcs8(string privateKey)
        {
            XElement root = XElement.Parse(privateKey);
            //Modulus
            var modulus = root.Element("Modulus");
            //Exponent
            var exponent = root.Element("Exponent");
            //P
            var p = root.Element("P");
            //Q
            var q = root.Element("Q");
            //DP
            var dp = root.Element("DP");
            //DQ
            var dq = root.Element("DQ");
            //InverseQ
            var inverseQ = root.Element("InverseQ");
            //D
            var d = root.Element("D");

            RsaPrivateCrtKeyParameters rsaPrivateCrtKeyParameters = new RsaPrivateCrtKeyParameters(
                new BigInteger(1, Convert.FromBase64String(modulus.Value)),
                new BigInteger(1, Convert.FromBase64String(exponent.Value)),
                new BigInteger(1, Convert.FromBase64String(d.Value)),
                new BigInteger(1, Convert.FromBase64String(p.Value)),
                new BigInteger(1, Convert.FromBase64String(q.Value)),
                new BigInteger(1, Convert.FromBase64String(dp.Value)),
                new BigInteger(1, Convert.FromBase64String(dq.Value)),
                new BigInteger(1, Convert.FromBase64String(inverseQ.Value)));

            StringWriter swpri = new StringWriter();
            PemWriter pWrtpri = new PemWriter(swpri);
            Pkcs8Generator pkcs8 = new Pkcs8Generator(rsaPrivateCrtKeyParameters);
            pWrtpri.WriteObject(pkcs8);
            pWrtpri.Writer.Close();
            return swpri.ToString();

        }

        /// <summary>
        /// Private Key Convert Pkcs1->Pkcs8
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs1ToPkcs8(string privateKey)
        {
            privateKey = RsaPemFormatHelper.Pkcs1PrivateKeyFormat(privateKey);
            PemReader pr = new PemReader(new StringReader(privateKey));

            AsymmetricCipherKeyPair kp = pr.ReadObject() as AsymmetricCipherKeyPair;
            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            Pkcs8Generator pkcs8 = new Pkcs8Generator(kp.Private);
            pWrt.WriteObject(pkcs8);
            pWrt.Writer.Close();
            string result = sw.ToString();
            return result;
        }

        /// <summary>
        /// Private Key Convert Pkcs8->Pkcs1
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs8ToPkcs1(string privateKey)
        {
            privateKey = RsaPemFormatHelper.Pkcs8PrivateKeyFormat(privateKey);
            PemReader pr = new PemReader(new StringReader(privateKey));

            RsaPrivateCrtKeyParameters kp = pr.ReadObject() as RsaPrivateCrtKeyParameters;

            var keyParameter = PrivateKeyFactory.CreateKey(PrivateKeyInfoFactory.CreatePrivateKeyInfo(kp));

            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            pWrt.WriteObject(keyParameter);
            pWrt.Writer.Close();
            string result = sw.ToString();
            return result;
        }
    }

    public class RsaPemFormatHelper
    {
        /// <summary>
        /// Format Pkcs1 format private key
        /// Author:Zhiqiang Li
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs1PrivateKeyFormat(string str)
        {
            if (str.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
            {
                return str;
            }

            List<string> res = new List<string>();
            res.Add("-----BEGIN RSA PRIVATE KEY-----");

            int pos = 0;

            while (pos < str.Length)
            {
                var count = str.Length - pos < 64 ? str.Length - pos : 64;
                res.Add(str.Substring(pos, count));
                pos += count;
            }

            res.Add("-----END RSA PRIVATE KEY-----");
            var resStr = string.Join(Environment.NewLine, res);
            return resStr;
        }

        /// <summary>
        /// Remove the Pkcs1 format private key format
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs1PrivateKeyFormatRemove(string str)
        {
            if (!str.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
            {
                return str;
            }
            return str.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "");
        }

        /// <summary>
        /// Format Pkcs8 format private key
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs8PrivateKeyFormat(string str)
        {
            if (str.StartsWith("-----BEGIN PRIVATE KEY-----"))
            {
                return str;
            }
            List<string> res = new List<string>();
            res.Add("-----BEGIN PRIVATE KEY-----");

            int pos = 0;

            while (pos < str.Length)
            {
                var count = str.Length - pos < 64 ? str.Length - pos : 64;
                res.Add(str.Substring(pos, count));
                pos += count;
            }

            res.Add("-----END PRIVATE KEY-----");
            var resStr = string.Join(Environment.NewLine, res);
            return resStr;
        }

        /// <summary>
        /// Remove the Pkcs8 format private key format
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs8PrivateKeyFormatRemove(string str)
        {
            if (!str.StartsWith("-----BEGIN PRIVATE KEY-----"))
            {
                return str;
            }
            return str.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "");
        }

        /// <summary>
        /// Format public key
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string PublicKeyFormat(string str)
        {
            if (str.StartsWith("-----BEGIN PUBLIC KEY-----"))
            {
                return str;
            }
            List<string> res = new List<string>();
            res.Add("-----BEGIN PUBLIC KEY-----");
            int pos = 0;

            while (pos < str.Length)
            {
                var count = str.Length - pos < 64 ? str.Length - pos : 64;
                res.Add(str.Substring(pos, count));
                pos += count;
            }
            res.Add("-----END PUBLIC KEY-----");
            var resStr = string.Join(Environment.NewLine, res);
            return resStr;
        }

        /// <summary>
        /// Public key format removed
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string PublicKeyFormatRemove(string str)
        {
            if (!str.StartsWith("-----BEGIN PUBLIC KEY-----"))
            {
                return str;
            }
            return str.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "")
                .Replace(Environment.NewLine, "");
        }
    }
}

