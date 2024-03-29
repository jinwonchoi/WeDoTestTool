using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using Elegant.Ui.Samples.ControlsSample.Sockets;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

namespace Elegant.Ui.Samples.ControlsSample
{
	public partial class MsgrClientPage : SamplePageBase
	{
        private readonly OpenFileDialog _openFileDialog;

        public MsgrClientPage()
		{
			InitializeComponent();

            MainForm.SetMonospaceFont(TextBoxSocketStatus);

            _openFileDialog = new OpenFileDialog();

		    AutoFillListBoxesWithItems();


		    DataTable table = new DataTable("ElegantTable");
		    table.Columns.Add("Color");


		    string[] names = Enum.GetNames(typeof(KnownColor));
		    foreach (string s in names)
            {
                table.Rows.Add(new object[] {s});
            }

            this.GroupBoxMsgrContent.AllowDrop = true;
            this.GroupBoxMsgrContent.DragDrop += MsgrChosenFileDragDrop;
            this.GroupBoxMsgrContent.DragOver += MsgrChosenFileDragOver;
        }

	    private void AutoFillListBoxesWithItems()
	    {
            //FillListBoxWithAutogeneratedItems("Elegant List Box Item Number ", DemoListBox);
            //FillListBoxWithAutogeneratedItems("Elegant Checked List Box Item Number ", DemoCheckedListBox);
	    }


        private int _eventLogCounter;

        private void AddEventsLogMessage(string m)
        {
            _eventLogCounter++;
            string text = TextBoxSocketStatus.Text + _eventLogCounter + " " + m + Environment.NewLine;

            int firstLineEnd = text.IndexOf(Environment.NewLine);
            if (firstLineEnd > 0)
            {
                int newLineCount = 0, currentLineIndex = firstLineEnd + Environment.NewLine.Length;
                while (currentLineIndex > 0)
                {
                    newLineCount += 1;
                    int startIndex = currentLineIndex + Environment.NewLine.Length;
                    if (startIndex >= text.Length - 1)
                        break;

                    currentLineIndex = text.IndexOf(Environment.NewLine, startIndex);
                }

                if (newLineCount == 100)
                {
                    text = text.Remove(0, firstLineEnd + Environment.NewLine.Length);
                }
            }


            TextBoxSocketStatus.Text = text;
            TextBoxSocketStatus.Select(TextBoxSocketStatus.TextLength, 0);
            TextBoxSocketStatus.ScrollToCaret();
        }

        private void ChooseIconButton_Click(object sender, EventArgs e)
        {
            DialogResult result = _openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                FTP_Ready(_openFileDialog.FileName);
            }
        }

        public void FTP_Ready(string fileName)
        {
            mFullFileName = fileName;
            mFileSize = (new FileInfo(fileName)).Length;
            TextBoxFilePath.Text = fileName;
            ProgressBarFileReceiving.Value = 0;
            ProgressBarFileReceiving.Visible = true;
        }

        public void FTP_CancelFile()
        {
            if (ftpClient != null)
            {
                ftpClient.CancelSending();
            }
        }

        public void FTP_SendFile()
        {
            mCurTransferSize = 0;
            try
            {
                if (ftpClient == null)
                {
                    MessageBox.Show("FTP미접속상태");
                    return;
                }
                if (ftpClient.SendFile())
                {
                    Thread_AppendConsoleMsg(string.Format(">파일전송완료[{0}]", mFullFileName));
                }
                else
                {
                    Thread_AppendConsoleMsg(string.Format(">파일전송오류[{0}]", mFullFileName));
                    ftpClient.Close();
                }
            }
            catch (IOException)
            {
            }
        }

        public void FTP_END()
        {
            if (msgClient == null) { MessageBox.Show("미접속상태"); return; }
            msgClient.FinishFile(mFullFileName, mFileSize);
        }

        public bool FTP_Init(string ipAddress, int port)
        {
            if (ftpClient != null && ftpClient.IsConnected())
            {
                ftpClient.Close();
            }
            ftpClient = new FtpClientManager(ipAddress, port, "ftp_cli");
                //AppendConsoleMsg("[SERVER_CONNECT]Socket Already Inited.");

            ftpClient.SocStatusChanged += SocStatusChanged;

            if (!ftpClient.IsConnected())
            {
                if (ftpClient.Connect())
                    AppendConsoleMsg(">[SERVER_CONNECT]Server Connected.");
                else
                {
                    AppendConsoleMsg(">[SERVER_CONNECT]Server Not Connected.");
                    return false;
                }
            }
            else
                AppendConsoleMsg(">[SERVER_CONNECT]Server Already Connected.");

            string file = Utils.GetFileName(mFullFileName);
            string path = Utils.GetPath(mFullFileName);

            ftpClient.setFileName(file);
            ftpClient.setFilePath(path);
            return true;
        }

        MsgrClientManager msgClient;
        FtpClientManager ftpClient;
        //ISocketStatus socStatus;
        //public delegate void delOnChange(SocHandlerStatus status);
        delegate void SetThreadSocMsgLabelCallback(object sender, SocStatusEventArgs e);
        delegate void SetThreadSocMsgConsoleCallback(object sender, SocStatusEventArgs e, string msg);
        delegate void SetThreadSocMsgCallback(object sender, SocStatusEventArgs e);
        delegate void SetThreadSocMsgButtonCallback(object sender, SocStatusEventArgs e);


        string mFullFileName;
        long mFileSize = 0;
        long mCurTransferSize = 0;
        string mHostName = SocConst.LOCAL_HOST;
        int mPort = 1100;

        public void TCP_Connect(string ipAddress, int port)
        {
            if (msgClient == null || !msgClient.IsConnected())
                msgClient = new MsgrClientManager(ipAddress, port, "tcp_cli");
            else
                AppendConsoleMsg(">[SERVER_CONNECT]Socket Already Inited.");

            msgClient.SocStatusChanged += SocStatusChanged;

            if (!msgClient.IsConnected())
                msgClient.Connect();
            else
                AppendConsoleMsg(">[SERVER_CONNECT]Server Already Connected.");


            if (msgClient.IsConnected())
            {
                AppendConsoleMsg(">[SERVER_CONNECT]Server Connected.");
            }
            else
            {
                AppendConsoleMsg(">[SERVER_CONNECT]Server Not Connected.");
            }
        }

        private void ConfigServerInfo()
        {
            if (RadoiButtonLocalhost.Checked)
            {
                mHostName = SocConst.LOCAL_HOST;
            }
            else
            {
                mHostName = Utils.getIpAddress(TextBoxIP1.Text, TextBoxIP2.Text, TextBoxIP3.Text, TextBoxIP4.Text);
            }

            try
            {
                mPort = Convert.ToInt32(TextBoxPortNum.Text);
            }
            catch (Exception ex) { mPort = 0; }
        }

        private void ButtonMsgrConnect_Click(object sender, EventArgs e)
        {
            ConfigServerInfo();
            if (mHostName != "" && mPort != 0)
                TCP_Connect(mHostName, mPort);
            else
                MessageBox.Show("잘못된 서버정보!!");
        }

        private void ButtonMsgrClose_Click(object sender, EventArgs e)
        {
            if (msgClient == null) { MessageBox.Show("미접속상태"); return; }
            msgClient.Close();
        }

        /**
         * 1. Cli A Send File Noti , Wait for Ack
         * 2. Svr B Run FTPListener
         * 3. Svr B Send Info | Nack
         * 6. Cli A Run FTPClient
         * 7. Cli A Done
         * 8. Cli A BYE
         */

        private void AppendConsoleMsg(string msg)
        {
            RichTextBoxLog.AppendText(msg + "\n");
            RichTextBoxLog.ScrollToCaret();
        }

        private void Thread_AppendConsoleMsg(string msg)
        {

            StateObject stateObj = new StateObject();
            AppendConsoleMsg(this, new SocStatusEventArgs(stateObj), "Thread:" + msg);
        }

        private void AppendConsoleMsg(object sender, SocStatusEventArgs e, string msg)
        {
            if (this.RichTextBoxLog.InvokeRequired)
            {
                SetThreadSocMsgConsoleCallback d = new SetThreadSocMsgConsoleCallback(AppendConsoleMsg);
                this.Invoke(d, new object[] { sender, e, msg });
            }
            else
            {
                if (e.Status.socMessage != null && e.Status.socMessage.Trim() != "")
                    RichTextBoxLog.AppendText(">" + e.Status.socMessage + "\n");
                if (msg != null && msg.Trim() != "")
                    RichTextBoxLog.AppendText(">" + msg + "\n");

                if (e.Status.exception != null)
                {
                    if (e.Status.exception is SocketException)
                        RichTextBoxLog.AppendText(string.Format(">Error: {0} : {1}",
                            ((SocketException)e.Status.exception).ErrorCode,
                            ((SocketException)e.Status.exception).Message));
                    else
                        RichTextBoxLog.AppendText(string.Format(">Error: {0}", e.Status.exception.Message));
                }
                RichTextBoxLog.ScrollToCaret();
            }
        }

        private void SocStatusChanged(object sender, SocStatusEventArgs e)
        {
            UpdateMsgStatus(sender, e);

            if (e.Status.status == SocHandlerStatus.FTP_SENDING)
            {
                UpdateFTPStatus(sender, e);
            }
            else if (e.Status.status == SocHandlerStatus.FTP_END
                || e.Status.status == SocHandlerStatus.FTP_CANCELED
                || e.Status.status == SocHandlerStatus.FTP_SERVER_CANCELED)
            {
                UpdateButtonSendingText(sender, e);
            }

            AppendConsoleMsg(sender, e, "");
        }

        private void ReceiveMsg()
        {
            string msg = msgClient.Receive();
            Thread_AppendConsoleMsg(">" + msg);
        }

        private void UpdateButtonSendingText(object sender, SocStatusEventArgs e)
        {

            if (this.ButtonFileSend.InvokeRequired)
            {
                SetThreadSocMsgButtonCallback d = new SetThreadSocMsgButtonCallback(UpdateButtonSendingText);
                this.Invoke(d, new object[] { sender, e });
            }
            else
            {
                ButtonFileSend.Text = "파일전송";
                mIsSending = false;

                FTP_END();
            }
        }


        private void UpdateFTPStatus(object sender, SocStatusEventArgs e)
        {

            if (this.ProgressBarFileReceiving.InvokeRequired)
            {
                SetThreadSocMsgCallback d = new SetThreadSocMsgCallback(UpdateFTPStatus);
                this.Invoke(d, new object[] { sender, e });
            }
            else
            {
                mCurTransferSize += e.Status.bufferSize;
                ProgressBarFileReceiving.Value = (int)(mCurTransferSize * (long)100 / mFileSize);
            }
        }

        private void UpdateMsgStatus(object sender, SocStatusEventArgs e)
        {

            if (this.TextBoxSocketStatus.InvokeRequired)
            {
                SetThreadSocMsgLabelCallback d = new SetThreadSocMsgLabelCallback(UpdateMsgStatus);
                this.Invoke(d, new object[] { sender, e });
            }
            else
            {
                string strStatus = "";
                switch (e.Status.status)
                {
                    case SocHandlerStatus.UNINIT:
                        strStatus = "UNINIT";
                        break;
                    case SocHandlerStatus.CONNECTED:
                        strStatus = "CONNECTED";
                        break;
                    case SocHandlerStatus.DISCONNECTED:
                        strStatus = "DISCONNECTED";
                        break;
                    case SocHandlerStatus.ERROR:
                        strStatus = "ERROR";
                        break;
                    case SocHandlerStatus.RECEIVING:
                        strStatus = "RECEIVING";
                        break;
                    case SocHandlerStatus.SENDING:
                        strStatus = "SENDING";
                        break;
                }
                AddEventsLogMessage(strStatus);
            }
        }

        private void ButtonTextSend_Click(object sender, EventArgs e)
        {
            string msg = (TextBoxTextMessage.Text.Trim() != "") ? TextBoxTextMessage.Text : "test msg";

            if (msgClient == null) { MessageBox.Show("미접속상태"); return; }

            msgClient.SendMsg(msg);
            AppendConsoleMsg(">Sent:" + msg);
            Thread thMsgReader = new Thread(new ThreadStart(ReceiveMsg));
            thMsgReader.Start();
        }
        bool mIsSending = false;
        private void ButtonFileSend_Click(object sender, EventArgs e)
        {
            if (mIsSending)
            {
                FTP_CancelFile();
                mIsSending = false;
                return;
            }

            if (mFullFileName == null || mFullFileName.Trim() == "")
            {
                Utils.showMsgBox(this, "파일을 선택하세요.", "경고");
                return;
            }

            if (msgClient != null && msgClient.SendFile(mFullFileName, mFileSize))
            {
                FTP_Init(msgClient.getFtpHostName(), msgClient.getFtpPort());
                Thread thServer = new Thread(new ThreadStart(FTP_SendFile));
                thServer.Start();
                ButtonFileSend.Text = "전송중지";
                mIsSending = true;
            }
        }


        private void MsgrChosenFileDragDrop(object sender, DragEventArgs e)
        {
            string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            for (int i = 0; i < fileNames.Length; i++)
            {
                Logger.info("Dropped" + fileNames[i]);
                TextBoxFilePath.Text = fileNames[i];
            }
            FTP_Ready(TextBoxFilePath.Text);
        }

        private void MsgrChosenFileDragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void BottonLogErase_Click(object sender, EventArgs e)
        {
            RichTextBoxLog.Clear();
        }

        private void ComboBoxLogLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.setLogLevel((LOGLEVEL)ComboBoxLogLevel.SelectedIndex);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue  != 13) return;
            label1.Text = textBox1.Text;
            toolTip1.SetToolTip(label1, label1.Text);
            //Regex reg = new Regex(@"\d\d\d\d\d\d");
            //foreach (Match m in reg.Matches(textBox1.Text))
            //{
            //    TextBoxSocketStatus.AppendText(m.Value + Environment.NewLine);
            //}

        }

        //private void treeView1_DragDrop(object sender, DragEventArgs e)
        //{
        //    Debug.WriteLine("treeView1 DragDrop:" + sender.ToString() + ":" + treeView1.SelectedNode.Text);


        //    string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop, false);
        //    string tempFilename = "";
        //    for (int i = 0; i < fileNames.Length; i++)
        //    {
        //        tempFilename = fileNames[i].Substring(fileNames[i].LastIndexOf("\\") + 1, fileNames[i].Length - (fileNames[i].LastIndexOf("\\") + 1));
        //        Debug.WriteLine("Dropped" + fileNames[i]);
        //        chatBox.AppendText("sendfile to " + treeView1.SelectedNode.Text + ":" + tempFilename + "\n");

        //        mPanelSender.addEntry(FileTransferPanel.JobMode.Send, fileNames[i]);
        //        mPanelReceiver2.addEntry(FileTransferPanel.JobMode.Receive, fileNames[i]);
        //    }
        //}

        //private void treeView1_DragOver(object sender, DragEventArgs e)
        //{
        //    Point pos = treeView1.PointToClient(new Point(e.X, e.Y));
        //    TreeNode node = (TreeNode)treeView1.GetNodeAt(pos);
        //    treeView1.HideSelection = false;
        //    treeView1.SelectedNode = node;

        //    if (!node.IsExpanded)
        //    {
        //        node.Expand();
        //    }
        //    e.Effect = DragDropEffects.All;
        //}

	}
}