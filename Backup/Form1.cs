using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Security.Cryptography;

namespace HttpServer
{
    public partial class Form1 : Form
    {
        string httpServerIp = "106.240.228.18";
        int httpServerPort = 8000;
        
        HttpWebRequest wReq;
        HttpWebResponse wRes;
        //string brkUrl = "http://testbrk.onem2m.uplus.co.kr:80"; // BRK(개발기)
        string brkUrl = "https://brk1.onem2m.uplus.co.kr"; // BRK(상용기)
        //string mefUrl = "http://testmef.onem2m.uplus.co.kr:80"; // MEF(개발기)
        string mefUrl = "https://mef.onem2m.uplus.co.kr"; // MEF(상용기)
        
        ServiceServer svr = null;
        
        /* 초기작업
         * 1. CSEBase-GET : oneM2M 접속 확인
         * 2. MEF 인증 : 서버 Entity ID 및 토큰 가져오기
         * 3. RemoteCSE 생성 및 조회 : 서버주소 및 포트 설정
         */

        string deviceEntityId = "ASN_CSE-D-e857a2c3ed-DMTL"; // 임시(나중에 DB에서 가져와야 함)

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            svr = new ServiceServer();

            //InitServiceServer();
            //StartHttpServer();
        }

        private void InitServiceServer()
        {
            svr = new ServiceServer();
            svr.svcSvrCd = "262"; // 서비스 서버의 시퀀스(개발기)
            //svr.svcSvrCd = "144"; // 서비스 서버의 시퀀스(상용기)
            svr.svcCd = "DMTL"; // 서비스 서버의 서비스코드
            svr.svcSvrNum = "1"; // 서비스 서버의 Number
       
            // MEF 인증하여 DB에 저장
            /*
            if (svr.enrmtKey == string.Empty)
                RequestMEF();
            */
            svr.enrmtKey = "pZd3Bp0782Lz6vi5WrpHqg";
            svr.entityId = "ASN_CSE-S-04a640567a-DMTL";
            svr.token = "aJMmZ2La5lubpuMtQ7NRPcTOMqusozG6yVT6z2g6Eyg";

            // svr.remoteCSEName이 없다면 remoteCSE 생성
            svr.remoteCSEName = "csr-" + svr.svcCd;
            //ReqRemoteCSECreate();

            LogWrite("svr.svcSvrCd = " + svr.svcSvrCd);
            LogWrite("svr.svcCd = " + svr.svcCd);
            LogWrite("svr.svcSvrNum = " + svr.svcSvrNum);
            LogWrite("svr.enrmtKey = " + svr.enrmtKey);
            LogWrite("svr.entityId = " + svr.entityId);
            LogWrite("svr.token = " + svr.token);
            LogWrite("svr.remoteCSEName = " + svr.remoteCSEName);
        }
        
        // CSE Base Get
        private void button1_Click(object sender, EventArgs e)
        {
            ReqCSEBaseGET();
        }

        // RemoteCSE-GET
        private void button2_Click(object sender, EventArgs e)
        {
            ReqRemoteCSEGet();
        }

        // 데이터 송신
        private void button3_Click(object sender, EventArgs e)
        {
            SendDataToPlatform();
        }              

        // RemoteCSE-Create
        private void button4_Click(object sender, EventArgs e)
        {
            ReqRemoteCSECreate();
        }                  

        private void button5_Click(object sender, EventArgs e)
        {
            if (svr.svcCd != string.Empty && svr.svcSvrCd != string.Empty && svr.svcSvrNum != string.Empty)
                RequestMEF();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        static readonly char[] padding = { '=' };
        private void btnTest_Click(object sender, EventArgs e)
        {
            svr.enrmtKey = "pZd3Bp0782Lz6vi5WrpHqg";
            svr.entityId = "ASN_CSE-S-04a640567a-DMTL";

            string enrmtKey = svr.enrmtKey;
            string incoming = enrmtKey.Replace('_', '/').Replace('-', '+');
            switch (enrmtKey.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);
            string keyData = Encoding.ASCII.GetString(bytes);    
            LogWrite("keyData = " + keyData);
          
            string aesKey = EncodingHelper.encryptAES128(keyData);
            LogWrite("aesKey = " + aesKey);

            string shortUuid = svr.entityId.Substring(10, 10);
            LogWrite("shortUuid = " + shortUuid);

            string encryptedData = Encrypt(shortUuid, keyData);
            LogWrite("encryptedData = " + encryptedData);

            string enrmtKeyID = System.Convert.ToBase64String(Encoding.Default.GetBytes(encryptedData)).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
            LogWrite("enrmtKeyID = " + enrmtKeyID);
        }

        public static string Encrypt(string textToEncrypt, string key)
        {

            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;

            rijndaelCipher.Padding = PaddingMode.PKCS7;

 

            rijndaelCipher.KeySize = 128;

            rijndaelCipher.BlockSize = 128;

            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);

            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;

            if (len > keyBytes.Length)

            {

                len = keyBytes.Length;

            }

            Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;

            rijndaelCipher.IV = keyBytes;

            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();

            byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);

            return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));

        }        

        // 1. MEF 인증
        private void RequestMEF()
        {
            ReqHeader header = new ReqHeader();
            header.Url = mefUrl + "/mef/server";
            header.Method = "POST";
            header.ContentType = "application/xml";

            string packetStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            packetStr += "<auth>";
            packetStr += "<svcSvrCd>" + svr.svcSvrCd + "</svcSvrCd>";
            packetStr += "<svcCd>" + svr.svcCd + "</svcCd>";
            packetStr += "<svcSvrNum>" + svr.svcSvrNum + "</svcSvrNum>";
            packetStr += "</auth>";

            LogWrite("----------MEF 인증----------");
            string retStr = SendHttpRequest(header, packetStr); // xml
            if (retStr != string.Empty)
                ParsingXml(retStr);
        }

        // 2. CSEBase-GET : oneM2M 접속 확인
        private void ReqCSEBaseGET()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1";
            header.Method = "GET";
            header.Accept = "application/json";
            header.X_M2M_RI = "CSEBase_Retrieve_1";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = "p_YI1FZnZwyPajxwKYv15g";
            string retStr = SendHttpRequest(header, string.Empty);
            if (retStr != string.Empty)
                LogWrite(retStr);
        }

        private void ParsingXml(string xml)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xml);
            XmlNodeList xnList = xDoc.SelectNodes("/authdata/http"); //접근할 노드
            foreach (XmlNode xn in xnList)
            {
                svr.enrmtKey = xn["enrmtKey"].InnerText; // oneM2M 인증 KeyID를 생성하기 위한 Key
                svr.entityId = xn["entityId"].InnerText; // oneM2M에서 사용하는 단말 ID
                svr.token = xn["token"].InnerText; // 인증구간 통신을 위해 발급하는 Token
            }
            LogWrite("enrmtKey = " + svr.enrmtKey);
            LogWrite("entityId = " + svr.entityId);
            LogWrite("token = " + svr.token);
            lbEnrmtKey.Text = svr.enrmtKey;
            lbEntityId.Text = svr.entityId;
            lbToken.Text = svr.token;
        }

        // 3. RemoteCSE-Create
        private void ReqRemoteCSECreate()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1";
            header.Method = "POST";
            header.Accept = "application/vnd.onem2m-res+json";
            header.ContentType = "application/vnd.onem2m-res+json;ty=16";
            header.X_M2M_RI = "RemoteCSE_Create_1";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = "p_YI1FZnZwyPajxwKYv15g";
            header.X_M2M_NM = svr.remoteCSEName;
            var obj = new JObject();
            obj.Add("cst", "3");
            obj.Add("cb", "/" + svr.entityId + "/cb-1");
            obj.Add("csi", "/" + svr.entityId);
            obj.Add("rr", "true");
            var arr = new JArray();
            arr.Add("http://" + httpServerIp + ":" + httpServerPort);
            obj.Add("poa", arr);
            //LogWriteobj.ToString());
            string retStr = SendHttpRequest(header, obj.ToString());
            if (retStr != string.Empty)
            {
                LogWrite(retStr);
                // 이미 같은 이름으로 생성되어 있다면 응답 : {"message": "CONFLICT_INVALID_RESOURCE_NAME"}
            }
        }

        // 3. RemoteCSE-Get
        private void ReqRemoteCSEGet()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/" + svr.remoteCSEName;
            header.Method = "GET";
            header.Accept = "application/vnd.onem2m-res+json";
            header.X_M2M_RI = "RemoteCSE_Retrieve_1";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = "p_YI1FZnZwyPajxwKYv15g";
            string retStr = SendHttpRequest(header, string.Empty);
            if (retStr != string.Empty)
                LogWrite(retStr);
        }

        private void SendDataToPlatform()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/" + deviceEntityId + "/10250/0/1";
            header.Method = "POST";
            header.ContentType = "application/vnd.onem2m-res+xml;ty=4";
            header.X_M2M_RI = "data_send_1";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = "p_YI1FZnZwyPajxwKYv15g";
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(tbData.Text));
            string packetStr = "<m2m:cin xmlns:m2m=\"http://www.onem2m.org/xml/protocols\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.onem2m.org/xml/protocols CDT-contentInstance-v1_0_0.xsd\">";
            packetStr += "<cnf>application/octet-stream</cnf>";
            packetStr += "<con>" + data + "</con>";
            packetStr += "</m2m:cin>";
            string retStr = SendHttpRequest(header, packetStr);
            if (retStr != string.Empty)
                LogWrite(retStr);
        }

        public string SendHttpRequest(ReqHeader header, string data)
        {
            string resResult = string.Empty;

            try
            {
                wReq = (HttpWebRequest)WebRequest.Create(header.Url);
                wReq.Method = header.Method;
                if (header.Accept != string.Empty)
                    wReq.Accept = header.Accept;
                if (header.ContentType != string.Empty)
                    wReq.ContentType = header.ContentType;
                if (header.X_M2M_RI != string.Empty)
                    wReq.Headers.Add("X-M2M-RI", header.X_M2M_RI);
                if (header.X_M2M_Origin != string.Empty)
                    wReq.Headers.Add("X-M2M-Origin", header.X_M2M_Origin);
                if (header.X_M2M_NM != string.Empty)
                    wReq.Headers.Add("X-M2M-NM", header.X_M2M_NM);
                if (header.X_MEF_TK != string.Empty)
                    wReq.Headers.Add("X-MEF-TK", header.X_MEF_TK);
                if (header.X_MEF_EKI != string.Empty)
                    wReq.Headers.Add("X-MEF-EKI", header.X_MEF_EKI);

                // POST 전송일 경우      
                if (header.Method == "POST")
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(data);
                    Stream dataStream = wReq.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                using (wRes = (HttpWebResponse)wReq.GetResponse())
                {
                    for (int i = 0; i < wRes.Headers.Count; ++i)
                        LogWrite("[" + wRes.Headers.Keys[i] + "] " + wRes.Headers[i]);
                    Stream respPostStream = wRes.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                    resResult = readerPost.ReadToEnd();
                }
                return resResult;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    LogWrite("[" + (int)resp.StatusCode + "] " + resp.StatusCode.ToString());
                }
                else
                {
                    LogWrite(ex.ToString());
                }
                return resResult;
            }
        }

        private delegate void AddTextDelegate(string strText);
        private void LogWrite(string data)
        {
            AddTextDelegate addTextDelegate = new AddTextDelegate(tbLogWrite);
            this.Invoke(addTextDelegate, data);
        }

        private void tbLogWrite(string data)
        {
            if (tbLog.TextLength > 5000)
                tbLog.Text = "";

            tbLog.AppendText(Environment.NewLine);
            tbLog.AppendText(DateTime.Now.ToString("HH:mm:ss ") + data);
            tbLog.SelectionStart = tbLog.TextLength;
            tbLog.ScrollToCaret();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            LogWrite("----------서버인증파라미터 세팅-------------");
            svr.svcSvrCd = tbSvcSvrCd.Text; // 서비스 서버의 시퀀스
            svr.svcCd = tbSvcCd.Text; // 서비스 서버의 서비스코드
            svr.svcSvrNum = tbSvcSvrNum.Text; // 서비스 서버의 Number
            LogWrite("svr.svcSvrCd = " + svr.svcSvrCd);
            LogWrite("svr.svcCd = " + svr.svcCd);
            LogWrite("svr.svcSvrNum = " + svr.svcSvrNum);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string keyData = EncodingHelper.Base64Decoding(svr.enrmtKey, null);
            string aesKey = EncodingHelper.encryptAES128(keyData);
            string shortUuid = svr.entityId.Substring(10, 10);
            string encryptedData = Encrypt(shortUuid, keyData);
            LogWrite(encryptedData);
        }      
    }

    public class ServiceServer
    {
        public string svcSvrCd { get; set; } // 서비스 서버의 시퀀스
        public string svcCd { get; set; } // 서비스 서버의 서비스코드
        public string svcSvrNum { get; set; } // 서비스 서버의 Num ber

        public string enrmtKey { get; set; } // oneM2M 인증 KeyID를 생성하기 위한 Key
        public string entityId { get; set; } // oneM2M에서 사용하는 서버 ID
        public string token { get; set; } // 인증구간 통신을 위해 발급하는 Token

        public string enrmtKeyId { get; set; } // MEF 인증 결과를 통해 생성하는 ID

        public string remoteCSEName { get; set; } // RemoteCSE 리소스 이름
    }

    public class ReqHeader
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Accept { get; set; }
        public string ContentType { get; set; }
        public string X_M2M_RI { get; set; } // Request ID(임의 값)
        public string X_M2M_Origin { get; set; } // 서비스서버의 Entity ID
        public string X_MEF_TK { get; set; } // Password : MEF 인증으로 받은 Token 값
        public string X_MEF_EKI { get; set; } // Username(EKI) : MEF 인증으로 받은 Enrollment Key 로 생성한 Enrollment Key ID
        public string X_M2M_NM { get; set; } // 리소스 이름
    }
}
