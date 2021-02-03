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
        string logUrl = "http://106.103.228.184/api/v1"; // oneM2M log(개발기)

        string svrState = "STOP";
        
        ServiceServer svr = new ServiceServer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            svr.enrmtKeyId = string.Empty;
            svr.entityId = string.Empty;
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
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // CSE Base Get
        private void btnGetCSEBase_Click(object sender, EventArgs e)
        {
            LogWrite("----------CSEBase GET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqCSEBaseGET();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-GET
        private void btnGetRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE GET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEGet();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Create
        private void btnSetRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE SET----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSECreate();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Update
        private void btnUpdateRemoteCSE_Click(object sender, EventArgs e)
        {
            // 서비스서버에 대해서는 미지원함
            LogWrite("----------remoteCSE UPDATE----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEUpdate();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // RemoteCSE-Delete
        private void btnDelRemoteCSE_Click(object sender, EventArgs e)
        {
            LogWrite("----------remoteCSE DEL----------");
            if (svr.enrmtKeyId != string.Empty)
                ReqRemoteCSEDEL();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
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
                MessageBox.Show("서버인증파라미터 세팅하세요");
        }

        // 데이터 수신 (oneM2M 플랫폼 DB)
        private void btnDataRetrive_Click(object sender, EventArgs e)
        {
            LogWrite("----------DATA RECIEVE----------");
            if (svr.enrmtKeyId != string.Empty)
                RetriveDataToPlatform();
            else
                MessageBox.Show("서버인증파라미터 세팅하세요");
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
                MessageBox.Show("서버인증파라미터 세팅하세요");
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
                MessageBox.Show("서버인증파라미터 세팅하세요");
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
                MessageBox.Show("서버인증파라미터 세팅하세요");
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

            string packetStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            packetStr += "<m2m:csr xmlns:m2m=\"http://www.onem2m.org/xml/protocols\">";
            packetStr += "<cst>3</cst>";
            packetStr += "<csi>/" + svr.entityId + "</csi>";
            packetStr += "<cb>/" + svr.entityId + "/cb-1</cb>";
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

            string packetStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            packetStr += "<m2m:cin xmlns:m2m=\"http://www.onem2m.org/xml/protocols\">";
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
            header.Url = brkUrlL + "/" + tbLwM2MEntityID.Text + "/10250/0/1";
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

                string minified = string.Empty;
                if (data.StartsWith("<?xml", System.StringComparison.CurrentCultureIgnoreCase))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    minified = IndentedPrint(doc);
                }
                else if (data.StartsWith("{", System.StringComparison.CurrentCultureIgnoreCase))
                {
                    JToken parsedJson = JToken.Parse(data);
                    minified = parsedJson.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                else
                    minified = data;

                tbLog.AppendText(" " + minified);
                tbLog.SelectionStart = tbLog.TextLength;
                tbLog.ScrollToCaret();
            }));
        }

        public static string IndentedPrint(XmlDocument doc)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode))
                {
                    xmlTextWriter.Formatting = System.Xml.Formatting.Indented;

                    doc.WriteContentTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    memoryStream.Flush();

                    memoryStream.Position = 0;

                    using (StreamReader sr = new StreamReader(memoryStream))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
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

        private void button2_Click(object sender, EventArgs e)
        {
            string kind = "type=lwm2m";
            if (textBox1.TextLength > 8)
                kind += "&ctn=" + textBox1.Text;
            getSvrLoglists(kind);
        }

        private void getSvrLoglists(string kind)
        {
            ReqHeader header = new ReqHeader();
            header.Url = logUrl + "/logs?" + kind;
            header.Method = "GET";
            header.ContentType = "application/json";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "LogList";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            string retStr = GetHttpLog(header, string.Empty);

            if (retStr != string.Empty)
            {
                //LogWriteNoTime(retStr);
                try
                {
                    JArray jarr = JArray.Parse(retStr); //json 객체로

                    listBox1.Items.Clear();
                    listBox2.Items.Clear();
                    listBox3.Items.Clear();
                    foreach (JObject jobj in jarr)
                    {
                        string time = jobj["logTime"].ToString();
                        string logtime = time.Substring(8, 2) + ":" + time.Substring(10, 2) + ":" + time.Substring(12, 2);
                        var pathInfo = jobj["pathInfo"] ?? "NULL";
                        var trgAddr = jobj["trgAddr"] ?? "NULL";
                        string path = pathInfo.ToString();
                        if (path == "NULL")
                            path = jobj["resType"].ToString() + " : " + trgAddr.ToString();

                        listBox1.Items.Add(logtime + "\t" + jobj["logId"].ToString() + "\t" + jobj["resultCode"].ToString() + "\t   " + jobj["resultCodeName"].ToString() + " (" + path + ")");
                    }

                    if (listBox1.Items.Count != 0)
                    {
                        listBox1.SelectedIndex = 0;
                        //getSvrEventLog(listBox1.SelectedItem.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public string GetHttpLog(ReqHeader header, string data)
        {
            string resResult = string.Empty;

            try
            {
                wReq = (HttpWebRequest)WebRequest.Create(header.Url);
                wReq.Method = header.Method;
                if (header.ContentType != string.Empty)
                    wReq.ContentType = header.ContentType;
                /*
                if (header.X_M2M_RI != string.Empty)
                    wReq.Headers.Add("X-M2M-RI", header.X_M2M_RI);
                if (header.X_M2M_Origin != string.Empty)
                    wReq.Headers.Add("X-M2M-Origin", header.X_M2M_Origin);
                if (header.X_MEF_TK != string.Empty)
                    wReq.Headers.Add("X-MEF-TK", header.X_MEF_TK);
                if (header.X_MEF_EKI != string.Empty)
                    wReq.Headers.Add("X-MEF-EKI", header.X_MEF_EKI);
                */

                LogWrite(wReq.Method + " " + wReq.RequestUri + " HTTP/1.1");
                Console.WriteLine(wReq.Method + " " + wReq.RequestUri + " HTTP/1.1");
                Console.WriteLine("");
                for (int i = 0; i < wReq.Headers.Count; ++i)
                    Console.WriteLine(wReq.Headers.Keys[i] + ": " + wReq.Headers[i]);
                Console.WriteLine("");
                Console.WriteLine(data);
                Console.WriteLine("");

                wReq.Timeout = 20000;          // 서버 응답을 20초동안 기다림
                using (wRes = (HttpWebResponse)wReq.GetResponse())
                {
                    Console.WriteLine("HTTP/1.1 " + (int)wRes.StatusCode + " " + wRes.StatusCode.ToString());
                    Console.WriteLine("");
                    for (int i = 0; i < wRes.Headers.Count; ++i)
                        Console.WriteLine("[" + wRes.Headers.Keys[i] + "] " + wRes.Headers[i]);
                    Console.WriteLine("");

                    Stream respPostStream = wRes.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                    resResult = readerPost.ReadToEnd();
                    Console.WriteLine(resResult);
                    Console.WriteLine("");
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    Console.WriteLine("HTTP/1.1 " + (int)resp.StatusCode + " " + resp.StatusCode.ToString());
                    Console.WriteLine("");
                    for (int i = 0; i < resp.Headers.Count; ++i)
                        Console.WriteLine(" " + resp.Headers.Keys[i] + ": " + resp.Headers[i]);
                    Console.WriteLine("");

                    Stream respPostStream = resp.GetResponseStream();
                    StreamReader readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("UTF-8"), true);
                    string resError = readerPost.ReadToEnd();
                    Console.WriteLine(resError);
                    Console.WriteLine("");
                    Console.WriteLine("[" + (int)resp.StatusCode + "] " + resp.StatusCode.ToString());
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return resResult;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string kind = "type=onem2m";
            if (tbDeviceCTN.TextLength > 8)
                kind += "&ctn=" + tbDeviceCTN.Text;
            getSvrLoglists(kind);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            getSvrEventLog(listBox1.SelectedItem.ToString());
        }

        private void getSvrEventLog(string selected_msg)
        {
            string[] values = selected_msg.Split('\t');    // 수신한 데이터를 한 문장씩 나누어 array에 저장

            tBResultCode.Text = values[2];
            label21.Text = "서버로그 ID : " + values[1] + " 상세내역";

            // oneM2M log server 응답 확인 (resultcode)
            ReqHeader header = new ReqHeader();
            header.Url = logUrl + "/apilog?logId=" + values[1];
            //header.Url = logUrl + "/apilog?Id=61";
            header.Method = "GET";
            header.ContentType = "application/json";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "LogDetail";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            string retStr = GetHttpLog(header, string.Empty);

            listBox2.Items.Clear();
            listBox3.Items.Clear();
            if (retStr != string.Empty)
            {
                //LogWriteNoTime(retStr);
                try
                {
                    JArray jarr = JArray.Parse(retStr); //json 객체로

                    foreach (JObject jobj in jarr)
                    {
                        string time = jobj["logTime"].ToString();
                        string logtime = time.Substring(8, 2) + ":" + time.Substring(10, 2) + ":" + time.Substring(12, 2);
                        var pathInfo = jobj["pathInfo"] ?? "NULL";
                        var trgAddr = jobj["trgAddr"] ?? "NULL";
                        var logType = jobj["logType"] ?? " ";

                        string path = pathInfo.ToString();
                        if (path == "NULL")
                            path = jobj["resType"].ToString() + " : " + trgAddr.ToString();

                        listBox2.Items.Add(logtime + "\t" + jobj["logId"].ToString() + "\t" + jobj["resultCode"].ToString() + "\t   " + jobj["resultCodeName"].ToString() + " (" + logType.ToString() + " => " + path + ")");
                    }

                    if (listBox2.Items.Count != 0)
                    {
                        listBox2.SelectedIndex = 0;
                        //getSvrDetailLog(listBox2.SelectedItem.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ReqHeader header = new ReqHeader();
            header.Url = logUrl + "/resultCode?value=" + tBResultCode.Text;
            header.Method = "GET";
            header.ContentType = "application/json";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "ResultCode";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            string retStr = GetHttpLog(header, string.Empty);
            if (retStr != string.Empty)
            {
                //LogWriteNoTime(retStr);
                try
                {
                    JObject obj = JObject.Parse(retStr);

                    var resultCode = obj["resultCode"] ?? tBResultCode.Text;
                    var codeName = obj["codeName"] ?? "NULL";
                    var desc = obj["desc"] ?? "NULL";

                    MessageBox.Show("message = " + codeName.ToString() + "\ndescription = " + desc.ToString(), "Resultcode=" + resultCode.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
                MessageBox.Show("message = " + "Unknown" + "\ndescription = " + "Resultcode 값이 존재하지 않습니다.", "Resultcode=" + tBResultCode.Text);
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            getSvrDetailLog(listBox2.SelectedItem.ToString());
        }

        private void getSvrDetailLog(string selected_msg)
        {
            string[] values = selected_msg.Split('\t');    // 수신한 데이터를 한 문장씩 나누어 array에 저장

            tBResultCode.Text = values[2];
            label22.Text = "ID : " + values[1] + " 상세내역";

            // oneM2M log server 응답 확인 (resultcode)
            ReqHeader header = new ReqHeader();
            header.Url = logUrl + "/log?logId=" + values[1];
            header.Method = "GET";
            header.ContentType = "application/json";
            header.X_M2M_RI = DateTime.Now.ToString("yyyyMMddHHmmss") + "LogDetail";
            header.X_M2M_Origin = svr.entityId;
            header.X_MEF_TK = svr.token;
            header.X_MEF_EKI = svr.enrmtKeyId;
            string retStr = GetHttpLog(header, string.Empty);

            listBox3.Items.Clear();
            //listBox3.Items.Add(DateTime.Now.ToString("hh:mm:ss.fff") + " : " + values[1]);

            if (retStr != string.Empty)
            {
                //LogWriteNoTime(retStr);

                try
                {
                    JArray jarr = JArray.Parse(retStr); //json 객체로

                    foreach (JObject jobj in jarr)
                    {
                        var methodName = jobj["methodName"] ?? " ";
                        var logType = jobj["logType"] ?? " ";
                        var svrType = jobj["svrType"] ?? " ";

                        string message = " \t ";

                        string logtype = logType.ToString();
                        if (logtype == "COAP")
                        {
                            var coapType = jobj["coapType"] ?? " ";
                            message = coapType.ToString() + " (";


                            var code = jobj["code"] ?? " ";
                            message += code.ToString();

                            var uriPath = jobj["uriPath"] ?? "";
                            string path = uriPath.ToString();
                            if (path != "")
                                message += " " + path;

                            message += ")\t ";

                            var coapPayload = jobj["coapPayload"] ?? "";
                            if (coapPayload.ToString() != "")
                            {
                                var uriQuery = jobj["uriQuery"] ?? " ";
                                if (uriQuery.ToString() == " ")
                                    message += coapPayload.ToString();
                                else
                                {
                                    if (path.StartsWith("rd/", System.StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        message += coapPayload.ToString();
                                    }
                                    else
                                    {
                                        message += uriQuery.ToString() + "\n";

                                        if (uriPath.ToString() == "rd")
                                        {
                                            var others = jobj["others"] ?? "";
                                            if (others.ToString() == "")
                                            {
                                                message += "\n2048(EKI), 2049(TOKEN) 정보가 없습니다\n";
                                            }
                                        }
                                        message += "\n" + coapPayload.ToString();
                                    }
                                }
                            }
                        }
                        else if (logtype == "API_LOG")            //  서버 API LOG
                        {
                            logtype = "API";
                            var resultCode = jobj["resultCode"] ?? " ";
                            var trgAddr = jobj["trgAddr"] ?? " ";
                            var prtcType = jobj["prtcType"] ?? " ";
                            //if (resultCode.ToString() != " ")
                            //    tBResultCode.Text = resultCode.ToString();

                            message = resultCode.ToString() + " (" + prtcType.ToString() + ")\t" + trgAddr.ToString();
                        }
                        else if (logtype == "HTTP")
                        {
                            var httpMethod = jobj["httpMethod"] ?? " ";
                            var uri = jobj["uri"] ?? " ";
                            var body = jobj["body"] ?? " ";
                            var responseBody = jobj["responseBody"] ?? " ";

                            string decode = " ";
                            string bodymsg = body.ToString();
                            bodymsg = bodymsg.Replace("\t", "");
                            Console.WriteLine(bodymsg);

                            if (bodymsg.StartsWith("{", System.StringComparison.CurrentCultureIgnoreCase))
                            {
                                try
                                {
                                    JObject obj = JObject.Parse(bodymsg);

                                    string format = obj["cnf"].ToString(); // data format
                                    string value = obj["con"].ToString(); // data value

                                    if (format == "application/octet-stream")
                                    {
                                        string hexOutput = string.Empty;
                                        string ascii = "YES";
                                        byte[] orgBytes = Convert.FromBase64String(value);
                                        char[] orgChars = System.Text.Encoding.ASCII.GetString(orgBytes).ToCharArray();
                                        foreach (char _eachChar in orgChars)
                                        {
                                            // Get the integral value of the character.
                                            int intvalue = Convert.ToInt32(_eachChar);
                                            // Convert the decimal value to a hexadecimal value in string form.
                                            if (intvalue < 16)
                                            {
                                                hexOutput += "0";
                                                ascii = "NO";
                                            }
                                            else if (intvalue < 32)
                                            {
                                                ascii = "NO";
                                            }
                                            hexOutput += String.Format("{0:X}", intvalue);
                                        }
                                        //logPrintInTextBox(hexOutput, "");

                                        if (hexOutput != string.Empty)
                                        {
                                            decode = "\n\n( HEX DATA : " + hexOutput;

                                            if (ascii == "YES")
                                            {
                                                string asciidata = Encoding.UTF8.GetString(orgBytes);
                                                decode += "\nASCII DATA : " + asciidata;
                                            }
                                            decode += ")";
                                        }
                                    }
                                    else
                                    {
                                        decode = "\n\n( DATA : " + value + " )";
                                    }
                                    //LogWrite("decode = " + decode);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                            else if (bodymsg.StartsWith("<?xml", System.StringComparison.CurrentCultureIgnoreCase))
                            {
                                string format = string.Empty;
                                string value = string.Empty;

                                //bodymsg = bodymsg.Replace("\\t", "");
                                XmlDocument xDoc = new XmlDocument();
                                xDoc.LoadXml(bodymsg);
                                Console.WriteLine(xDoc.OuterXml.ToString());

                                XmlNodeList xnList = xDoc.SelectNodes("/*"); //접근할 노드
                                foreach (XmlNode xn in xnList)
                                {
                                    try
                                    {
                                        if (xn["cnf"] != null)
                                            format = xn["cnf"].InnerText; // data format
                                        if (xn["con"] != null)
                                            value = xn["con"].InnerText; // data value

                                        if (xn["nev"] != null)
                                        {
                                            if (xn["nev"]["rep"]["m2m:cin"]["cnf"] != null)
                                                format = xn["nev"]["rep"]["m2m:cin"]["cnf"].InnerText; // data format
                                            if (xn["nev"]["rep"]["m2m:cin"]["con"] != null)
                                                value = xn["nev"]["rep"]["m2m:cin"]["con"].InnerText; // data value
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                }
                                //LogWrite("value = " + value);
                                //LogWrite("format = " + format);

                                if (format == "application/octet-stream")
                                {
                                    string hexOutput = string.Empty;
                                    string ascii = "YES";
                                    byte[] orgBytes = Convert.FromBase64String(value);
                                    char[] orgChars = System.Text.Encoding.ASCII.GetString(orgBytes).ToCharArray();
                                    foreach (char _eachChar in orgChars)
                                    {
                                        // Get the integral value of the character.
                                        int intvalue = Convert.ToInt32(_eachChar);
                                        // Convert the decimal value to a hexadecimal value in string form.
                                        if (intvalue < 16)
                                        {
                                            hexOutput += "0";
                                            ascii = "NO";
                                        }
                                        else if (intvalue < 32)
                                        {
                                            ascii = "NO";
                                        }
                                        hexOutput += String.Format("{0:X}", intvalue);
                                    }
                                    //logPrintInTextBox(hexOutput, "");

                                    if (hexOutput != string.Empty)
                                    {
                                        decode = "\n\n( HEX DATA : " + hexOutput;

                                        if (ascii == "YES")
                                        {
                                            string asciidata = Encoding.UTF8.GetString(orgBytes);
                                            decode += "\nASCII DATA : " + asciidata;
                                        }
                                        decode += ")";
                                    }
                                }
                                else if (value != string.Empty)
                                {
                                    decode = "\n\n( DATA : " + value + " )";
                                }
                                //LogWrite("decode = " + decode);
                            }
                            else if (bodymsg.StartsWith("<m2m", System.StringComparison.CurrentCultureIgnoreCase))
                            {
                                string format = string.Empty;
                                string value = string.Empty;

                                //bodymsg = bodymsg.Replace("\\t", "");
                                XmlDocument xDoc = new XmlDocument();
                                xDoc.LoadXml(bodymsg);
                                Console.WriteLine(xDoc.OuterXml.ToString());

                                XmlNodeList xnList = xDoc.SelectNodes("/*"); //접근할 노드
                                foreach (XmlNode xn in xnList)
                                {
                                    try
                                    {
                                        if (xn["cnf"] != null)
                                            format = xn["cnf"].InnerText; // data format
                                        if (xn["con"] != null)
                                            value = xn["con"].InnerText; // data value
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                }
                                //LogWrite("value = " + value);
                                //LogWrite("format = " + format);

                                if (format == "application/octet-stream")
                                {
                                    string hexOutput = string.Empty;
                                    string ascii = "YES";
                                    byte[] orgBytes = Convert.FromBase64String(value);
                                    char[] orgChars = System.Text.Encoding.ASCII.GetString(orgBytes).ToCharArray();
                                    foreach (char _eachChar in orgChars)
                                    {
                                        // Get the integral value of the character.
                                        int intvalue = Convert.ToInt32(_eachChar);
                                        // Convert the decimal value to a hexadecimal value in string form.
                                        if (intvalue < 16)
                                        {
                                            hexOutput += "0";
                                            ascii = "NO";
                                        }
                                        else if (intvalue < 32)
                                        {
                                            ascii = "NO";
                                        }
                                        hexOutput += String.Format("{0:X}", intvalue);
                                    }
                                    //logPrintInTextBox(hexOutput, "");

                                    if (hexOutput != string.Empty)
                                    {
                                        decode = "\n\n( HEX DATA : " + hexOutput;

                                        if (ascii == "YES")
                                        {
                                            string asciidata = Encoding.UTF8.GetString(orgBytes);
                                            decode += "\nASCII DATA : " + asciidata;
                                        }
                                        decode += ")";
                                    }
                                }
                                else if (value != string.Empty)
                                {
                                    decode = "\n\n( DATA : " + value + " )";
                                }
                                //LogWrite("decode = " + decode);
                            }

                            message = httpMethod.ToString() + " " + uri.ToString() + "\tREQUEST\n" + bodymsg + decode + "\n\nRESPONSE\n" + responseBody;
                        }
                        else if (logtype == "HTTP_CLIENT")
                        {
                            logtype = "CLIENT";
                            var responseCode = jobj["responseCode"] ?? " ";
                            string resp = responseCode.ToString();

                            var uri = jobj["uri"] ?? " ";
                            var reqheader = jobj["header"] ?? " ";
                            var responseHeader = jobj["responseHeader"] ?? " ";

                            if (responseHeader.ToString() != " ")
                            {
                                JObject obj = JObject.Parse(responseHeader.ToString());
                                var rsc = obj["X-M2M-RSC"] ?? " ";
                                resp += "/" + rsc.ToString();
                                var resultcode = obj["X-LGU-RSC"] ?? " ";
                                if (resultcode.ToString() != " ")
                                    tBResultCode.Text = resultcode.ToString();
                            }

                            message = resp + " (" + uri.ToString() + ")\tREQUEST\n" + reqheader + "\n\nRESPONSE\n" + responseHeader;
                        }
                        else if (logtype == "RUNTIME_LOG")
                        {
                            logtype = "RUN";
                            var topicOrEntityId = jobj["topicOrEntityId"] ?? " ";
                            var requestEntity = jobj["requestEntity"] ?? " ";
                            var responseEntity = jobj["responseEntity"] ?? " ";

                            message = topicOrEntityId.ToString() + "\tREQUEST\n" + requestEntity + "\n\nRESPONSE\n" + responseEntity;
                        }

                        string svrtype = svrType.ToString();
                        if (svrtype == "CSE-NB01")
                            svrtype = "CSNB01";

                        string method = methodName.ToString();
                        if (method == "httpClientRuntimeLog")
                            method = "httpClientRuntime";

                        if (method.Length < 8)
                            method += "         ";

                        listBox3.Items.Add(svrtype + "\t" + logtype + "\t" + method + "\t" + message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected_msg = listBox3.SelectedItem.ToString();
            string[] values = selected_msg.Split('\t');    // 수신한 데이터를 한 문장씩 나누어 array에 저장

            if (values[4] != " ")
                MessageBox.Show(values[4], "전문 상세내역");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (svr.entityId != string.Empty)
            {
                string kind = string.Empty;
                kind += "entityId=" + svr.entityId;
                getSvrLoglists(kind);
            }
            else
               MessageBox.Show("서버인증파라미터 세팅하세요");
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
