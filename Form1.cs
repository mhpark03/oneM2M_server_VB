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
using System.Xml.Linq;

/*
* 8000번 포트 리스닝을 위한 선행 작업 
* netsh http add urlacl url=http://*:8000/ user=everyone
*/

namespace HttpServer
{
    public partial class Form1 : Form
    {
        HttpWebRequest wReq;
        HttpWebResponse wRes;

        string brkUrl = "https://testbrk.onem2m.uplus.co.kr:443"; // BRK(oneM2M 개발기)       
        string brkUrlL = "https://testbrks.onem2m.uplus.co.kr:8443"; // BRK(LwM2M 개발기)       
        string mefUrl = "https://testmef.onem2m.uplus.co.kr:443"; // MEF(개발기)

        string svrState = "STOP";
        
        ServiceServer svr = new ServiceServer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            svr.enrmtKeyId = string.Empty;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (svrState != "STOP")
            {
                StopHttpServer();
            }
        }

        // MEF Auth
        private void btnMEFAuth_Click(object sender, EventArgs e)
        {
            svr.svcSvrCd = tbSvcSvrCd.Text; // 서비스 서버의 시퀀스
            //LogWrite("svr.svcSvrCd = " + svr.svcSvrCd);
            svr.svcCd = tbSvcCd.Text; // 서비스 서버의 서비스코드
            //LogWrite("svr.svcCd = " + svr.svcCd);
            svr.svcSvrNum = tbSvcSvrNum.Text; // 서비스 서버의 Number
            //LogWrite("svr.svcSvrNum = " + svr.svcSvrNum);

            if (svr.svcCd != string.Empty && svr.svcSvrCd != string.Empty && svr.svcSvrNum != string.Empty)
                RequestMEF();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // CSE Base Get
        private void btnGetCSEBase_Click(object sender, EventArgs e)
        {
            LogWrite("----------CSEBase GET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqCSEBaseGET();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-GET
        private void btnGetRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE GET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEGet();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Create
        private void btnSetRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE SET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSECreate();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Update
        private void btnUpdateRemoteCSE_Click(object sender, EventArgs e)
        {
            // 서비스서버에 대해서는 미지원함
            LogWrite("----------remoteCSE UPDATE----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEUpdate();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Delete
        private void btnDelRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE DEL----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEDEL();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            LogWrite("----------서비스 서버 설정----------");
            if (svr.enrmtKeyId != string.Empty)
            {
                if (svrState == "STOP")
                {
                    StartHttpServer();
                    svrState = "RUN";
                    btnServer.Text = "서버 동작중(중지)";
                }
                else
                {
                    StopHttpServer();
                    svrState = "STOP";
                    btnServer.Text = "서버 시작";
                }
            }
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // 데이터 수신 (oneM2M 플랫폼 DB)
        private void btnDataRetrive_Click(object sender, EventArgs e)
        {
            LogWrite("----------DATA RECIEVE----------");
            if (svr.enrmtKeyId != string.Empty)
                RetriveDataToPlatform();
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // 데이터 송신
        private void btnSendData_Click(object sender, EventArgs e)
        {
            LogWrite("----------DATA SEND----------");
            if (svr.enrmtKeyId != string.Empty)
            {
                string target_comm = "oneM2M";
                SendDataToPlatform(target_comm);
            }
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // 데이터 송신
        private void btnLwM2MData_Click(object sender, EventArgs e)
        {
            LogWrite("----------DATA SEND----------");
            if (svr.enrmtKeyId != string.Empty)
            {
                string target_comm = "LwM2M";
                SendDataToPlatform(target_comm);
            }
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // LwM2M Device Status Check
        private void btnDeviceStatusCheck_Click(object sender, EventArgs e)
        {
            LogWrite("----------DEVICE STATUS CHECK----------");
            if (svr.enrmtKeyId != string.Empty)
            {
                DeviceCheckToPlatform();
            }
            else
                LogWrite("서버인증파라미터 세팅하세요");
        }

        // 1. MEF 인증
        private void RequestMEF()
        {
            ReqHeader header = new ReqHeader();
            header.Url = mefUrl + "/mef/server";
            header.Method = "POST";
            header.ContentType = "application/xml";
            header.X_M2M_RI = string.Empty;
            header.X_M2M_Origin = string.Empty;
            header.X_MEF_TK = string.Empty;
            header.X_MEF_EKI = string.Empty;
            header.X_M2M_NM = string.Empty;

            string packetStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            packetStr += "<auth>";
            packetStr += "<svcSvrCd>" + svr.svcSvrCd + "</svcSvrCd>";
            packetStr += "<svcCd>" + svr.svcCd + "</svcCd>";
            packetStr += "<svcSvrNum>" + svr.svcSvrNum + "</svcSvrNum>";
            packetStr += "</auth>";

            LogWrite("----------MEF 인증----------");
            string retStr = SendHttpRequest(header, packetStr); // xml
            if (retStr != string.Empty)
            {
                ParsingXml(retStr);

                string nameCSR = svr.entityId.Replace("-","");
                lbremoteCSEName.Text = "csr-" + nameCSR;
                svr.remoteCSEName = lbremoteCSEName.Text;
                //LogWrite("svr.remoteCSEName = " + svr.remoteCSEName);
            }
        }

        private void ParsingXml(string xml)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xml);
            //LogWrite(xDoc.OuterXml.ToString());

            XmlNodeList xnList = xDoc.SelectNodes("/authdata/http"); //접근할 노드
            foreach (XmlNode xn in xnList)
            {
                svr.enrmtKey = xn["enrmtKey"].InnerText; // oneM2M 인증 KeyID를 생성하기 위한 Key
                svr.entityId = xn["entityId"].InnerText; // oneM2M에서 사용하는 단말 ID
                svr.token = xn["token"].InnerText; // 인증구간 통신을 위해 발급하는 Token
            }
            //LogWrite("enrmtKey = " + svr.enrmtKey);
            //LogWrite("entityId = " + svr.entityId);
            //LogWrite("token = " + svr.token);
            lbEnrmtKey.Text = svr.enrmtKey;
            lbEntityId.Text = svr.entityId;
            lbToken.Text = svr.token;

            // EKI값 계산하기
            // short uuid구하기
            string suuid = svr.entityId.Substring(10, 10);
            //LogWrite("suuid = " + suuid);

            // KeyData Base64URL Decoding
            string output = svr.enrmtKey;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding

            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break; // One pad char
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(svr.enrmtKey), "Illegal base64url string!");
            }

            var converted = Convert.FromBase64String(output); // Standard base64 decoder

            // keyData로 AES 128비트 비밀키 생성
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();
            AesManaged tdes = new AesManaged();
            tdes.Key = converted;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform crypt = tdes.CreateEncryptor();
            byte[] plain = Encoding.UTF8.GetBytes(suuid);
            byte[] cipher = crypt.TransformFinalBlock(plain, 0, plain.Length);
            String enrmtKeyId = Convert.ToBase64String(cipher);

            enrmtKeyId = enrmtKeyId.Split('=')[0]; // Remove any trailing '='s
            enrmtKeyId = enrmtKeyId.Replace('+', '-'); // 62nd char of encoding
            enrmtKeyId = enrmtKeyId.Replace('/', '_'); // 63rd char of encoding

            lbEnrmtKeyId.Text = enrmtKeyId;
            svr.enrmtKeyId = enrmtKeyId;
            //LogWrite("svr.enrmtKeyId = " + svr.enrmtKeyId);
        }

        // 2. CSEBase-GET : oneM2M 접속 확인
        private void ReqCSEBaseGET()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1";
            header.Method = "GET";
            header.Accept = "application/xml";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "CSEBase";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            string retStr = SendHttpRequest(header, string.Empty);
            //if (retStr != string.Empty)
            //    LogWrite(retStr);
        }

        // 3. RemoteCSE-Get
        private void ReqRemoteCSEGet()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/" + svr.remoteCSEName;
            header.Method = "GET";
            header.Accept = "application/vnd.onem2m-res+xml";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "RemoteCSE_Retrieve";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            string retStr = SendHttpRequest(header, string.Empty);
            //if (retStr != string.Empty)
            //    LogWrite(retStr);
        }

        // 3. RemoteCSE-Create
        private void ReqRemoteCSECreate()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1";
            header.Method = "POST";
            header.Accept = "application/vnd.onem2m-res+xml";
            header.ContentType = "application/vnd.onem2m-res+xml;ty=16";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "RemoteCSE_Create";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = svr.remoteCSEName;

            string packetStr = "<m2m:csr xmlns:m2m=\"http://www.onem2m.org/xml/protocols\">";
            packetStr += "<cst>3</cst>";
            packetStr += "<cb>/" + svr.entityId + "</cb>";
            packetStr += "<csi>/" + svr.entityId + "/cb-1</csi>";
            packetStr += "<rr>true</rr>";
            packetStr += "<poa>" + tbSeverIP.Text + ":" + tbSeverPort.Text + "</poa>";
            packetStr += "</m2m:csr>";

            string retStr = SendHttpRequest(header, packetStr);

            /*
            var obj = new JObject();
            obj.Add("cst", "3");
            obj.Add("cb", "/" + svr.entityId + "/cb-1");
            obj.Add("csi", "/" + svr.entityId);
            obj.Add("rr", "true");
            var arr = new JArray();
            //arr.Add("http://172.17.224.57:8180");
            arr.Add("http://" + tbSeverIP.Text + ":" + tbSeverPort.Text);
            obj.Add("poa", arr);
            //LogWriteobj.ToString());
            string retStr = SendHttpRequest(header, obj.ToString());
            */
            //if (retStr != string.Empty)
            //{
            //    LogWrite(retStr);
            //    // 이미 같은 이름으로 생성되어 있다면 응답 : {"message": "CONFLICT_INVALID_RESOURCE_NAME"}
            //}
        }

        // 3. RemoteCSE-Update
        private void ReqRemoteCSEUpdate()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/"+svr.remoteCSEName;
            header.Method = "PUT";
            header.Accept = "application/vnd.onem2m-res+json";
            header.ContentType = "application/vnd.onem2m-res+json";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "RemoteCSE_Update";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;

            string packetStr = "<m2m:csr xmlns:m2m=\"http://www.onem2m.org/xml/protocols\">";
            packetStr += "<cst>3</cst>";
            packetStr += "<cb>/" + svr.entityId + "</cb>";
            packetStr += "<csi>/" + svr.entityId + "/cb-1</csi>";
            packetStr += "<rr>true</rr>";
            packetStr += "<poa>" + tbSeverIP.Text + ":" + tbSeverPort.Text + "</poa>";
            packetStr += "</m2m:csr>";

            string retStr = SendHttpRequest(header, packetStr);
            //if (retStr != string.Empty)
            //    LogWrite(retStr);
        }

        // 3. RemoteCSE-Delete
        private void ReqRemoteCSEDEL()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/" + svr.remoteCSEName;
            header.Method = "DELETE";
            header.Accept = "application/vnd.onem2m-res+xml";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "RemoteCSE_Delete";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            string retStr = SendHttpRequest(header, string.Empty);
            //if (retStr != string.Empty)
            //    LogWrite(retStr);
        }

        private void RetriveDataToPlatform()
        {
            ReqHeader header = new ReqHeader();
            //header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/csr-m2m_01222990847/cnt-TEMP/la";
            header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/csr-m2m_" + tbDeviceCTN.Text + "/cnt-" + tbContainer.Text +"/la";
            header.Method = "GET";
            header.X_M2M_Origin = svr.entityId;
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "data_retrive";
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            header.Accept = "application/xml";
            header.ContentType = string.Empty;

            string retStr = SendHttpRequest(header, string.Empty);
            if (retStr != string.Empty)
            {
                string format = string.Empty;
                string value = string.Empty;

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(retStr);
                //LogWrite(xDoc.OuterXml.ToString());

                XmlNodeList xnList = xDoc.SelectNodes("/*"); //접근할 노드
                foreach (XmlNode xn in xnList)
                {
                    format = xn["cnf"].InnerText; // data format
                    value = xn["con"].InnerText; // data value
                }
                //LogWrite("value = " + value);
                //LogWrite("format = " + format);

                if (format == "application/octet-stream")
                    lboneM2MRxData.Text = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                else
                    lboneM2MRxData.Text = value;
            }
        }

        private void SendDataToPlatform(string target_comm)
        {
            ReqHeader header = new ReqHeader();
            string txData;

            if(target_comm == "oneM2M")
            {
                //header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/csr-m2m_01222990847/cnt-TEMP";
                header.Url = brkUrl + "/IN_CSE-BASE-1/cb-1/csr-m2m_" + tbDeviceCTN.Text + "/cnt-" + tbContainer.Text;
                txData = tbData.Text;
            }
            else
            {
                header.Url = brkUrlL + "/" + tbLwM2MEntityID.Text + "/10250/0/1";
                //header.Url = brkUrlL + "/IN_CSE-BASE-1/cb-1/" + deviceEntityId + "/10250/0/1";
                txData = tbLwM2MData.Text;
            }
            header.Method = "POST";
            header.X_M2M_Origin = svr.entityId;
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "data_send";
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            header.Accept = "application/vnd.onem2m-res+xml";
            header.ContentType = "application/vnd.onem2m-res+xml;ty=4";

            string packetStr = "<m2m:cin xmlns:m2m=\"http://www.onem2m.org/xml/protocols\">";
            packetStr += "<cnf>text/plain</cnf>";
            packetStr += "<con>" + txData + "</con>";
            packetStr += "</m2m:cin>";
            string retStr = SendHttpRequest(header, packetStr);
            //if (retStr != string.Empty)
            //    LogWrite(retStr);
        }

        private void DeviceCheckToPlatform()
        {
            ReqHeader header = new ReqHeader();
            header.Url = brkUrlL + "/" + tbLwM2MEntityID.Text + "/10250/0/0";
            header.Method = "GET";
            header.X_M2M_Origin = svr.entityId;
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "device_status";
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            header.X_M2M_NM = string.Empty;
            header.Accept = "application/vnd.onem2m-res+xml";
            header.ContentType = "application/vnd.onem2m-res+xml;ty=4";

            string retStr = SendHttpRequest(header, string.Empty);
            if (retStr != string.Empty)
            {
                string format = string.Empty;
                string value = string.Empty;

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(retStr);
                //LogWrite(xDoc.OuterXml.ToString());

                XmlNodeList xnList = xDoc.SelectNodes("/*"); //접근할 노드
                foreach (XmlNode xn in xnList)
                {
                    format = xn["cnf"].InnerText; // data format
                    value = xn["con"].InnerText; // data value
                }
                //LogWrite("value = " + value);
                //LogWrite("format = " + format);

                if (format == "application/octet-stream")
                    lbLwM2MRxData.Text = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                else
                    lbLwM2MRxData.Text = value;
            }
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

                LogWriteNoTime(wReq.Method + " " + wReq.RequestUri + " HTTP/1.1");
                LogWriteNoTime("");
                for (int i = 0; i < wReq.Headers.Count; ++i)
                    LogWriteNoTime(wReq.Headers.Keys[i] + ": " + wReq.Headers[i]);
                LogWriteNoTime("");
                LogWriteNoTime(data);
                LogWriteNoTime("");

                // POST 전송일 경우      
                if (header.Method == "POST")
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(data);
                    Stream dataStream = wReq.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }

                LogWrite("----------Response from oneM2M----------");
                wReq.Timeout = 20000;          // 서버 응답을 20초동안 기다림
                using (wRes = (HttpWebResponse)wReq.GetResponse())
                {
                    LogWriteNoTime("HTTP/1.1 " + (int)wRes.StatusCode + " " + wRes.StatusCode.ToString());
                    LogWriteNoTime("");
                    for (int i = 0; i < wRes.Headers.Count; ++i)
                        LogWriteNoTime("[" + wRes.Headers.Keys[i] + "] " + wRes.Headers[i]);
                    LogWriteNoTime("");

                    Stream respPostStream = wRes.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                    resResult = readerPost.ReadToEnd();
                    LogWriteNoTime(resResult);
                    LogWriteNoTime("");
                }
                return resResult;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    LogWriteNoTime("HTTP/1.1 " + (int)resp.StatusCode + " " + resp.StatusCode.ToString());
                    LogWriteNoTime("");
                    for (int i = 0; i < resp.Headers.Count; ++i)
                        LogWriteNoTime(" " + resp.Headers.Keys[i] + ": " + resp.Headers[i]);
                    LogWriteNoTime("");

                    Stream respPostStream = resp.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                    string resError = readerPost.ReadToEnd();
                    LogWriteNoTime(resError);
                    LogWriteNoTime("");
                    //LogWrite("[" + (int)resp.StatusCode + "] " + resp.StatusCode.ToString());
                }
                else
                {
                    LogWrite(ex.ToString());
                }
                return resResult;
            }
        }

        private void LogWrite(string data)
        {
            BeginInvoke(new Action(() =>
            {
                if (tbLog.TextLength > 5000)
                    tbLog.Text = string.Empty;
                tbLog.AppendText(Environment.NewLine);
                tbLog.AppendText(DateTime.Now.ToString("MM/dd HH:mm:ss ") + data);
                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.ScrollToCaret();
            }));
        }

        private void LogWriteNoTime(string data)
        {
            BeginInvoke(new Action(() =>
            {
                if (tbLog.TextLength > 5000)
                    tbLog.Text = string.Empty;
                tbLog.AppendText(Environment.NewLine);
                data = data.Replace("><", ">" + Environment.NewLine + "<");         // xml tag 위치에 줄바꿈 삽입
                tbLog.AppendText(" " + data);
                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.ScrollToCaret();
            }));
        }

        /***** 아래부터는 Http Server 관련 *****/
        HttpListener listener;
        private void StartHttpServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://+:" + tbSeverPort.Text + "/");
            listener.Start();
            listener.BeginGetContext(this.OnRequested, this.listener);
            LogWrite("StartHttpServer");
        }

        private void StopHttpServer()
        {
            this.listener.Close();
        }

        private void OnRequested(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            if (!listener.IsListening)
            {
                LogWrite("listening finished.");
                return;
            }

            LogWrite("OnRequested start.");
            LogWrite("OnRequested result.IsCompleted " + result.IsCompleted);
            LogWrite("OnRequested result.CompletedSynchronously " + result.CompletedSynchronously);
            LogWrite("OnRequested listener.IsListening " + listener.IsListening);

            HttpListenerContext ctx = listener.EndGetContext(result);
            HttpListenerRequest req = null;
            HttpListenerResponse res = null;
            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                req = ctx.Request;
                res = ctx.Response;

                DisplayWebHeaderCollection(req);

                reader = new StreamReader(req.InputStream);
                writer = new StreamWriter(res.OutputStream);

                string received = reader.ReadToEnd();
                if (received != string.Empty)
                {
                    LogWrite("[ 수신 데이터 ]");
                    LogWrite(received);
                    ParsingJson(received, req.Url.AbsolutePath);
                }
                //writer.Write(received);
                //writer.Flush();       

                //res.StatusCode = (int)HttpStatusCode.NotFound;
                res.Headers.Add("X-M2M-RI", "response_1");
                res.Headers.Add("X-M2M-RSC", "2000");
                res.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
            finally
            {
                try
                {
                    if (null != writer) writer.Close();
                    if (null != reader) reader.Close();
                    if (null != res) res.Close();  // close할 때 응답이 완료됨..
                }
                catch (Exception ex)
                {
                    LogWrite(ex.ToString());
                }
            }

            listener.BeginGetContext(this.OnRequested, listener);
        }

        public void DisplayWebHeaderCollection(HttpListenerRequest request)
        {
            LogWrite("[ request.Url.AbsolutePath ]");
            LogWrite("   " + request.Url.AbsolutePath);

            System.Collections.Specialized.NameValueCollection headers = request.Headers;

            foreach (string key in headers.AllKeys)
            {
                string[] values = headers.GetValues(key);
                if (values.Length > 0)
                {
                    LogWrite("[ " + key + " ]");
                    foreach (string value in values)
                    {
                        LogWrite("   " + value);
                    }
                }
                else
                    LogWrite("There is no value associated with the header.");
            }
        }

        private void ParsingJson(string jsonStr, string path)
        {
            try
            {
                JObject obj = JObject.Parse(jsonStr);
                if (path == "/" + svr.entityId + "/10250/0/0") // 데이터 수신
                {
                    string temp = obj["nev"]["rep"]["m2m:cin"]["con"].ToString();
                    string data = Encoding.UTF8.GetString(Convert.FromBase64String(temp));
                    string deviceEntityId = obj["cr"].ToString();
                    if (data != string.Empty)
                        LogWrite("[" + deviceEntityId + "][데이터 수신]" + data);
                }
                else if (path == "/" + svr.entityId + "/bs") // 부트스트랩
                {
                    string deviceEntityId = obj["cr"].ToString();
                    LogWrite("[" + deviceEntityId + "] Bootstrap 요청 수신");
                }
                else if (path == "/" + svr.entityId + "/rd") // 레지스터
                {
                    string deviceEntityId = obj["cr"].ToString();
                    LogWrite("[" + deviceEntityId + "] Registration 요청 수신");
                }
                else
                {
                    LogWrite("[ParsingJson] path = " + path);
                }
            }
            catch (Exception ex)
            {
                LogWrite(ex.ToString());
            }
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
