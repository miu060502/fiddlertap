using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Fiddler;

[assembly: Fiddler.RequiredVersion("2.2.4.6")]

namespace fiddlerLibraryTap
{
    public delegate void TapPacketHandler(object o, TapPacketEventArgs e);

    public class TapPacketEventArgs : EventArgs
    {
        public readonly Session theSession;

        public TapPacketEventArgs(Session s)
        {
            theSession = s;
        }
    }

    public class fiddlerTap : IAutoTamper2, IFiddlerExtension
    {
        public static event TapPacketHandler EventTapPacket;

        public fiddlerTap(){
            
        }

        public void OnLoad(){
            oPage = new TabPage("Packet Tap");
            layoutFl = new FlowLayoutPanel();
            
            FiddlerApplication.UI.tabsViews.TabPages.Add(oPage);
            oPage.Controls.Add(layoutFl);
            
            layoutFl.Size = new System.Drawing.Size(layoutFl.Parent.Size.Width, 28);
            layoutFl.Dock = System.Windows.Forms.DockStyle.Fill;
            layoutFl.SizeChanged += new EventHandler(LayoutFl_SizeChanged);
            
            lblUrl = new Label();
            lblUrl.Text = "Tap Url:";
            lblUrl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lblUrl.Size = new System.Drawing.Size(50, 25);
            layoutFl.Controls.Add(lblUrl);
            lblUrl.Anchor = AnchorStyles.Right;
            
            tbUrl = new TextBox();
            tbUrl.Text = "http://";
            tbUrl.Anchor = AnchorStyles.Left;
            layoutFl.Controls.Add(tbUrl);
            tbUrl.Size = new System.Drawing.Size(tbUrl.Parent.Size.Width-lblUrl.Size.Width-70, 25);
            //tbUrl.Dock = System.Windows.Forms.DockStyle.Fill;
            
            btnSwitch = new Button();
            btnSwitch.Text = "Start";
            btnSwitch.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            layoutFl.Controls.Add(btnSwitch);
            btnSwitch.Size = new System.Drawing.Size(50, 25);
            layoutFl.SetFlowBreak(btnSwitch, true);
            btnSwitch.Click += new EventHandler(btnSwitch_Click);
            bSwitch = true;
            
            tbMsg = new TextBox();
            tbMsg.Multiline = true;
            tbMsg.ReadOnly = true;
            layoutFl.Controls.Add(tbMsg);
            layoutFl.SetFlowBreak(tbMsg, true);
            tbMsg.Size = new System.Drawing.Size(tbMsg.Parent.Size.Width - 10, tbMsg.Parent.Size.Height-32);
            //tbMsg.Dock = System.Windows.Forms.DockStyle.Fill;
            
            EventTapPacket += new TapPacketHandler(fiddlerTap_EventTapPacket);
        }

        void fiddlerTap_EventTapPacket(object o, TapPacketEventArgs e)
        {
            //mut.WaitOne();
            if (e.theSession != null /*&& e.theSession.requestBodyBytes.Length > 0*/)
            {
                
                // Create a new HttpWebRequest object.
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tbUrl.Text);
                System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                // Set the ContentType property. 
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the Method property to 'POST' to post data to the URI.
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version10;
                
                // Write the data to the request stream.
                string test = string.Format(@"Request Header:
{0}
Request Content:
{1}
Response Header:
{2}
Response Content:
{3}", e.theSession.oRequest.headers.ToString(),
                                enc.GetString(e.theSession.requestBodyBytes),
                                e.theSession.oResponse.headers.ToString(),
                                enc.GetString(e.theSession.responseBodyBytes)            
                    );
                request.ContentLength = test.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(enc.GetBytes(test), 0, test.Length);
                
                // Close the Stream object.
                dataStream.Close();
                request.Abort();
                iRequestBodyLength += test.Length;
                
                tbMsg.Text = string.Format(@"Last Session ID: {0} 
Last Session Request Lenght: {1} 
Total Post Body Length: {2} 
Last Post Time: {3}
Last Request Header:
{4}
Last Request Content:
{5}
Last Response Header:
{6}
Last Response Content:
{7}
",
                                e.theSession.id, e.theSession.requestBodyBytes.Length,
                                iRequestBodyLength, DateTime.Now.ToString(),
                                e.theSession.oRequest.headers.ToString(),
                                enc.GetString(e.theSession.requestBodyBytes),
                                e.theSession.oResponse.headers.ToString(),
                                enc.GetString(e.theSession.responseBodyBytes));
                
            }
            //mut.ReleaseMutex();
        }

        void btnSwitch_Click(object sender, EventArgs e)
        {
            if (bSwitch)
            {
                bSwitch = false;
                tbUrl.Enabled = false;
                btnSwitch.Text = "Stop";
            }
            else
            {
                bSwitch = true;
                tbUrl.Enabled = true;
                btnSwitch.Text = "Start";
            }
        }
        
        void LayoutFl_SizeChanged(object sender, EventArgs e)
        {

            //layoutFl.Size = new System.Drawing.Size(layoutFl.Parent.Size.Width, layoutFl.Parent.Size.Height-10);
            tbUrl.Size = new System.Drawing.Size(tbUrl.Parent.Size.Width - lblUrl.Size.Width - 70, tbUrl.Parent.Size.Height);
            tbMsg.Size = new System.Drawing.Size(tbMsg.Parent.Size.Width - 10, tbMsg.Parent.Size.Height - 32);
        }
        
        public void AutoTamperRequestAfter(Session oSess){
            
        }
        public void AutoTamperRequestBefore(Session oSession)
        {

        }
        public void AutoTamperResponseBefore(Session oSession) { } 
        public void AutoTamperResponseAfter(Session oSess) {
            if (EventTapPacket != null && !bSwitch)
            {
                EventTapPacket(new object(), new TapPacketEventArgs(oSess));
            }
        }
        public void OnBeforeReturningError(Session oSession) { }
        public void OnPeekAtResponseHeaders(Session osses) { }
        public void OnBeforeUnload() { }
        private FlowLayoutPanel layoutFl;
        private TabPage oPage;
        private Label lblUrl;
        private TextBox tbUrl;
        private TextBox tbMsg;
        private Button btnSwitch;
        private bool bSwitch;
        private static int iRequestBodyLength = 0;
        private static Mutex mut = new Mutex();
    }
}
