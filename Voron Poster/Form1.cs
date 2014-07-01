﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Web;

namespace Voron_Poster
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var pos = this.PointToScreen(label1.Location);
            pos = progressBar1.PointToClient(pos);
        }

        enum ForumEngine { Unknown, SMF }

        struct FESearchExpression
        {
            public string SearchExpression;
            public int Value;

            public FESearchExpression(string newSearchExpression, int newValue)
            {
                SearchExpression = newSearchExpression;
                Value = newValue;
            }
        }

        private int SearchForExpressions(string Html, FESearchExpression[] Expressions)
        {
            int ResultMatch = 0;
            foreach (FESearchExpression CurrExpression in Expressions)
            {
                if (Html.IndexOf(CurrExpression.SearchExpression.ToLower()) >= 0)
                    ResultMatch += CurrExpression.Value;
            }
            return ResultMatch;
        }

        private ForumEngine DetectForumEngine(string Html)
        {
            int[] Match = new int[Enum.GetNames(typeof(ForumEngine))
                     .Length];
            Html = Html.ToLower();
            Match[(int)ForumEngine.Unknown] = 9;

            Match[(int)ForumEngine.SMF] += SearchForExpressions(Html, new FESearchExpression[] {
            new FESearchExpression("Powered by SMF", 20),
            new FESearchExpression("Simple Machines Forum", 20),
            new FESearchExpression("http://www.simplemachines.org/about/copyright.php", 10),
            new FESearchExpression("http://www.simplemachines.org/", 10),
            new FESearchExpression("Simple Machines", 10)});

            ForumEngine PossibleEngine = ForumEngine.Unknown;
            for (int i = 0; i < Match.Length; i++)
            {
                if (Match[i] > Match[(int)PossibleEngine])
                    PossibleEngine = (ForumEngine)i;
            }
            return PossibleEngine;
        }

        abstract class Forum
        {

            protected HttpClient Client;
            HttpClientHandler ClientHandler;
            public int ReqTimeout;
            public List<string> Log;
            public int Progress;
            public Uri MainPage;
            protected CookieContainer Cookies;
            public CancellationTokenSource Cancel;
            public Forum()
            {
                Log = new List<string>();
                Progress = 0;
                Cancel = new CancellationTokenSource();
                Cookies = new CookieContainer();
                ClientHandler = new HttpClientHandler() { CookieContainer = Cookies };
                Client = new HttpClient(ClientHandler);
            }

            ~Forum()
            {
                ClientHandler.Dispose();
                Client.Dispose();
            }

            public abstract Task<bool> Login(string Username, string Password);
            public abstract Task<bool> PostMessage(Uri TargetBoard, string Subject, string BBText);

        }

        class ForumSMF : Forum
        {

            private Uri CaptchaUri;
            public string h;
            string CurrSessionID;
            string AnotherID;
            public ForumSMF() : base() { }

            private static string SHA1HashStringForUTF8String(string s)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s);

                var sha1 = SHA1.Create();
                byte[] hashBytes = sha1.ComputeHash(bytes);

                return HexStringFromBytes(hashBytes);
            }

            private static string HexStringFromBytes(byte[] bytes)
            {
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }
                return sb.ToString();
            }

            private static string hashLoginPassword(string Username, string Password, string cur_session_id)
            {
                return SHA1HashStringForUTF8String(SHA1HashStringForUTF8String(Username.ToLower() + Password) + cur_session_id);
            }

            private string GetBetweenStrAfterStr(string Html, string After, string Beg, string End)
            {
                int b = Html.IndexOf(After);
                if (b < 0 || b + After.Length >= Html.Length) return "";
                b = Html.IndexOf(Beg, b + After.Length);
                if (b < 0 || b + Beg.Length >= Html.Length) return "";
                int e = Html.IndexOf(End, b + Beg.Length);
                if (e > 0)
                    return Html.Substring(b + Beg.Length, e - b - Beg.Length);
                return "";
            }

            public override async Task<bool> Login(string Username, string Password)
            {
                lock (Log) { Log.Add("Cоединение с сервером"); }
                try
                {
                    HttpResponseMessage RespMes = await Client.GetAsync(MainPage.AbsoluteUri + "index.php?action=login", Cancel.Token);
                    Progress++;
                    string Html = await RespMes.Content.ReadAsStringAsync();
                    lock (Log) { Log.Add("Авторизация"); Progress++; }
                    Html = Html.ToLower();
                    CurrSessionID = GetBetweenStrAfterStr(Html, "hashloginpassword", "'", "'");
                    string HashPswd = hashLoginPassword(Username, Password, CurrSessionID);
                    AnotherID = GetBetweenStrAfterStr(Html, "value=\"" + CurrSessionID + "\"", "\"", "\"");
                    var PostData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("user", Username.ToLower()),
                        new KeyValuePair<string, string>("cookielength", "-1"),
                        new KeyValuePair<string, string>("passwrd", Password),        
                        new KeyValuePair<string, string>("hash_passwrd", HashPswd),   
                        new KeyValuePair<string, string>(AnotherID, CurrSessionID)
                     });
                    Progress++;
                    RespMes = await Client.PostAsync(MainPage.AbsoluteUri + "index.php?action=login2", PostData, Cancel.Token);
                    Progress++;
                    Html = await RespMes.Content.ReadAsStringAsync();
                    if (Html.ToLower().IndexOf("index.php?action=logout") >= 0)
                    {
                        lock (Log) { Log.Add("Успешно авторизирован"); Progress++; }
                        return true;
                    }
                    lock (Log) { Log.Add("Ошибка при авторизации"); }
                    return false;
                }
                catch (Exception e)
                {
                    lock (Log) { Log.Add("Ошибка: " + e.Message); }
                    return false;
                }
            }

            //private bool TryGetStartBoard(string BoardUri, out string Start, out string Board)
            //{
            //    BoardUri = BoardUri.ToLower();
            //    Start = String.Empty;
            //    Board = String.Empty;
            //    int b = BoardUri.LastIndexOf("board=");
            //    if (b < 0 || b + 6 >= BoardUri.Length) return false;
            //    b += 6;
            //    int p = BoardUri.IndexOf('.', b);
            //    if (p <= 0) return false;
            //    for (int i = p + 1; i < BoardUri.Length; i++) Start += BoardUri[i].ToString();
            //    for (int i = b; i < p; i++) Board += BoardUri[i].ToString();
            //    return true;
            //}

            //private bool TryGetStartBoard(string BoardUri, out string StartBoard)
            //{
            //    BoardUri = BoardUri.ToLower();
            //    StartBoard = "start=";
            //    int b = BoardUri.LastIndexOf("board=");
            //    if (b < 0 || b + 6 >= BoardUri.Length) return false;
            //    b += 6;
            //    int p = BoardUri.IndexOf('.', b);
            //    if (p <= 0) return false;
            //    for (int i = p + 1; i < BoardUri.Length; i++) StartBoard += BoardUri[i].ToString();
            //    StartBoard += ";board=";
            //    for (int i = b + 1; i < p; i++) StartBoard += BoardUri[i].ToString();
            //    return true;
            //}

            private bool TryGetPostUrl(string Html, out Uri PostUri)
            {
                PostUri = null;
                int b = Html.IndexOf("index.php?action=post2");
                if (b < 0) return false;
                int e = Html.IndexOf("\"", b + 1);
                if (e < 0) return false;
                b = Html.LastIndexOf("\"", b);
                if (b < 0) return false;
                if (Uri.TryCreate(Html.Substring(b + 1, e - b - 1), UriKind.Absolute,
                    out PostUri) && PostUri.Scheme == Uri.UriSchemeHttp)
                    return true;
                else return false;
            }


            private async Task<bool> GetCaptcha(CaptchaForm CaptchaForm)
            {
                CaptchaForm.button3.Enabled = false;
                Bitmap Captcha = new Bitmap(10, 10);
                try
                {
                    HttpResponseMessage RespMes = await Client.GetAsync(CaptchaUri, Cancel.Token);
                    lock (Log) { Log.Add("Загружаю каптчу"); };
                    CaptchaForm.pictureBox1.Image = new Bitmap(await RespMes.Content.ReadAsStreamAsync());
                    CaptchaForm.ClientSize = CaptchaForm.ClientSize - CaptchaForm.pictureBox1.Size + CaptchaForm.pictureBox1.Image.Size;
                    //Random r = new Random();
                    //string NewRand = String.Empty;
                    //int n = HttpUtility.ParseQueryString(CaptchaUri.Query).Get("rand").Length;
                    //for (int i = 0; i < n; i++)
                    //{
                    //    int ran = r.Next(15);
                    //    if (ran > 9)
                    //        NewRand += ((char)(ran + 87)).ToString();
                    //    else NewRand += ((char)ran).ToString();
                    //}
                    //HttpUtility.ParseQueryString(CaptchaUri.Query).Set("rand", NewRand);
                    return true;
                }
                catch (Exception e)
                {
                    lock (Log) { Log.Add("Ошибка: " + e.Message); }
                    return false;
                }
                finally
                {
                    CaptchaForm.button3.Enabled = true;
                }
            }

            public override async Task<bool> PostMessage(Uri TargetBoard, string Subject, string BBText)
            {
                CaptchaForm CaptchaForm = null;
                try
                {
                    HttpResponseMessage RespMes = await Client.GetAsync(MainPage.AbsoluteUri
                        + "index.php" + TargetBoard.Query + "&action=post", Cancel.Token);
                    Progress++;
                    string Html = await RespMes.Content.ReadAsStringAsync();
                    lock (Log) { Log.Add("Подготовка"); Progress++; }
                    Html = Html.ToLower();
                    string Topic = HttpUtility.ParseQueryString(TargetBoard.Query.Replace(';', '&')).Get("topic");
                    if (Topic == null) Topic = "0";
                    if (!TryGetPostUrl(Html, out TargetBoard))
                    {
                        lock (Log) { Log.Add("Ошибка не удалось извлечь ссылку для публикации"); }
                        return false;
                    }
                    string SeqNum = GetBetweenStrAfterStr(Html, "name=\"seqnum\"", "value=\"", "\"");
                    if (Uri.TryCreate(GetBetweenStrAfterStr(Html, "class=\"verification_control\"", "src=\"", "\"").Replace(';', '&'),
                        UriKind.Absolute, out CaptchaUri) && CaptchaUri.Scheme == Uri.UriSchemeHttp)
                    {
                        CaptchaForm = new CaptchaForm();
                        CaptchaForm.func = GetCaptcha;
                        CaptchaForm.button2.Click += new System.EventHandler((object o, EventArgs e) => { Cancel.Cancel(); });
                        await GetCaptcha(CaptchaForm);
                        Progress++;
                        CaptchaForm.ShowDialog();
                    }
                    else Progress++;
                    lock (Log) { Log.Add("Публикация"); }
                    using (var FormData = new MultipartFormDataContent())
                    {
                        FormData.Add(new StringContent(Topic), "topic");
                        FormData.Add(new StringContent(Subject), "subject");
                        FormData.Add(new StringContent(BBText), "message");
                        if (CaptchaForm != null)
                            FormData.Add(new StringContent(CaptchaForm.textBox1.Text), "post_vv[code]");
                        FormData.Add(new StringContent(SeqNum), "seqnum");
                        FormData.Add(new StringContent("0"), "message_mode");
                        FormData.Add(new StringContent(CurrSessionID), AnotherID);

                        FormData.Add(new StringContent("0"), "additional_options");
                        FormData.Add(new StringContent("0"), "lock");
                        FormData.Add(new StringContent("0"), "notify");
                        //FormData.Add(new StringContent(""), "sel_color");
                        //FormData.Add(new StringContent(""), "sel_size");
                        //FormData.Add(new StringContent(""), "sel_face");
                        //FormData.Add(new StringContent("xx"), "icon");

                        RespMes = await Client.PostAsync(TargetBoard.AbsoluteUri, FormData, Cancel.Token);
                        Progress++;
                        Html = await RespMes.Content.ReadAsStringAsync();
                        Html = Html.ToLower();
                        if (Html.IndexOf("errorbox") > 0 || Html.IndexOf(Subject) < 0)
                        {
                            lock (Log) { Log.Add("Ошибка"); Progress++; }
                            return false;
                        }
                        else
                        {
                            lock (Log) { Log.Add("Тема создана"); Progress++; }
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    lock (Log) { Log.Add("Ошибка: " + e.Message); }
                    return false;
                }
                finally
                {
                    CaptchaForm.Dispose();
                }
            }
        }

        private void RenderHtml(string Html)
        {
            Form b = new Form();
            WebBrowser wb = new WebBrowser();
            wb.Parent = b;
            wb.DocumentText = Html;
            wb.Refresh();
            wb.Dock = DockStyle.Fill;
            b.Show();
        }

        ForumSMF f;
        private async void button1_Click(object sender, EventArgs e)
        {
            //WebRequest Request = WebRequest.Create(textBox1.Text);
            //WebResponse Response = Request.GetResponse();
            //Stream dataStream = Response.GetResponseStream();
            //// Open the stream using a StreamReader for easy access.
            //StreamReader Reader = new StreamReader(dataStream);
            //// Read the content.
            //string responseFromServer = Reader.ReadToEnd();
            //// Display the content.
            //// Clean up the streams and the response.
            //Reader.Close();
            //Response.Close();
            //textBox1.Text = responseFromServer;
            progressBar1.Parent = this;
            progressBar1.Text = "test";
            progressBar1.Maximum = 15;


            f = new ForumSMF();
            f.ReqTimeout = 3000;
            f.MainPage = new Uri("http://www.simplemachines.org/community/");
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 100;
            t.Tick += new EventHandler((a, b) => { progressBar1.Value = f.Progress; });
            t.Start();
            await f.Login("Voron", "LEVEL2index");
            textBox1.Lines = f.Log.ToArray();
            textBox2.Text = f.h;
            //  RenderHtml(f.h);
            // this.Text = hashLoginPassword(textBox1.Lines[0], textBox1.Lines[1], textBox1.Lines[2]);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            f.Cancel.Cancel();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await f.PostMessage(new Uri("http://www.simplemachines.org/community/index.php?topic=524612.0"), "test3", textBox2.Text);
            textBox1.Lines = f.Log.ToArray();
            textBox2.Text += f.h;
            RenderHtml(f.h);
        }




        class TaskGui
        {
            TableLayoutPanel Parent;
            CheckBox Selected;
            LinkLabel Name;
            Label Status;
            PictureBox StatusIcon;
            ProgressBar Progress;
            Button StartStop;
            Button Properties;
            Button Delete;

            private void InitializeComponent()
            {
                this.Selected = new System.Windows.Forms.CheckBox();
                this.Name = new System.Windows.Forms.LinkLabel();
                this.Status = new System.Windows.Forms.Label();
                this.StatusIcon = new System.Windows.Forms.PictureBox();
                this.Progress = new System.Windows.Forms.ProgressBar();
                this.StartStop = new System.Windows.Forms.Button();
                this.Properties = new System.Windows.Forms.Button();
                this.Delete = new System.Windows.Forms.Button();
                // 
                // GTSelected
                // 
                Selected.AutoSize = false;
                Selected.Dock = System.Windows.Forms.DockStyle.Fill;
                Selected.Anchor = System.Windows.Forms.AnchorStyles.Top;
                Selected.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
                Selected.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
                Selected.Location = new System.Drawing.Point(1, 1);
                Selected.Margin = new System.Windows.Forms.Padding(0);
                Selected.MaximumSize = new System.Drawing.Size(24, 24);
                Selected.MinimumSize = new System.Drawing.Size(24, 24);
                Selected.Name = "GTSelected";
                Selected.Size = new System.Drawing.Size(24, 24);
                Selected.TabIndex = 0;
                Selected.UseVisualStyleBackColor = true;
                // 
                // GTName
                // 
                Name.AutoSize = false;
                Name.Dock = System.Windows.Forms.DockStyle.Fill;
                Name.Location = new System.Drawing.Point(28, 1);
                Name.Name = "GTName";
                Name.Size = new System.Drawing.Size(377, 24);
                Name.MaximumSize = new System.Drawing.Size(0, 24);
                Name.MinimumSize = new System.Drawing.Size(0, 24);
                Name.TabIndex = 3;
                Name.Text = "Тема/Раздел";
                Name.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                // 
                // GTStatus
                // 
                Status.AutoSize = false;
                Status.Dock = System.Windows.Forms.DockStyle.Fill;
                Status.Location = new System.Drawing.Point(412, 1);
                Status.Name = "GTStatus";
                Status.Size = new System.Drawing.Size(153, 24);
                Status.MaximumSize = new System.Drawing.Size(0, 24);
                Status.MinimumSize = new System.Drawing.Size(0, 24);
                Status.TabIndex = 4;
                Status.Text = "Состояние";
                Status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                // 
                // GTStatusIcon
                // 
                StatusIcon.Dock = System.Windows.Forms.DockStyle.Fill;
                StatusIcon.Image = global::Voron_Poster.Properties.Resources.StatusAnnotations_Stop_16xLG;
                StatusIcon.Location = new System.Drawing.Point(569, 1);
                StatusIcon.Margin = new System.Windows.Forms.Padding(0);
                StatusIcon.MaximumSize = new System.Drawing.Size(24, 24);
                StatusIcon.MinimumSize = new System.Drawing.Size(24, 24);
                StatusIcon.Name = "GTStatusIcon";
                StatusIcon.Size = new System.Drawing.Size(24, 24);
                StatusIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
                StatusIcon.TabIndex = 9;
                StatusIcon.TabStop = false;
                // 
                // GTProgress
                // 
                Progress.Dock = System.Windows.Forms.DockStyle.Fill;
                Progress.Location = new System.Drawing.Point(597, 4);
                Progress.Name = "GTProgress";
                Progress.Size = new System.Drawing.Size(69, 18);
                Progress.MaximumSize = new System.Drawing.Size(0, 18);
                Progress.MinimumSize = new System.Drawing.Size(0, 18);
                Progress.TabIndex = 2;
                // 
                // GTStartStop
                // 
                StartStop.AutoSize = false;
                StartStop.Dock = System.Windows.Forms.DockStyle.Fill;
                StartStop.Image = global::Voron_Poster.Properties.Resources.arrow_run_16xLG;
                StartStop.Location = new System.Drawing.Point(670, 1);
                StartStop.Margin = new System.Windows.Forms.Padding(0);
                StartStop.MaximumSize = new System.Drawing.Size(24, 24);
                StartStop.MinimumSize = new System.Drawing.Size(24, 24);
                StartStop.Name = "GTStartStop";
                StartStop.Size = new System.Drawing.Size(24, 24);
                StartStop.TabIndex = 8;
                StartStop.UseVisualStyleBackColor = true;
                // 
                // GTPropeties
                // 
                Properties.AutoSize = false;
                Properties.Dock = System.Windows.Forms.DockStyle.Fill;
                Properties.Image = global::Voron_Poster.Properties.Resources.gear_16xLG;
                Properties.Location = new System.Drawing.Point(695, 1);
                Properties.Margin = new System.Windows.Forms.Padding(0);
                Properties.MaximumSize = new System.Drawing.Size(24, 24);
                Properties.MinimumSize = new System.Drawing.Size(24, 24);
                Properties.Name = "GTPropeties";
                Properties.Size = new System.Drawing.Size(24, 24);
                Properties.TabIndex = 7;
                Properties.UseVisualStyleBackColor = true;
                // 
                // GTDelete
                // 
                Delete.AutoSize = false;
                Delete.Dock = System.Windows.Forms.DockStyle.Fill;
                Delete.Image = global::Voron_Poster.Properties.Resources.action_Cancel_16xLG;
                Delete.Location = new System.Drawing.Point(720, 1);
                Delete.Margin = new System.Windows.Forms.Padding(0);
                Delete.MaximumSize = new System.Drawing.Size(24, 24);
                Delete.MinimumSize = new System.Drawing.Size(24, 24);
                Delete.Name = "GTDelete";
                Delete.Size = new System.Drawing.Size(24, 24);
                Delete.TabIndex = 10;
                Delete.UseVisualStyleBackColor = true;
            }

            public TaskGui(TableLayoutPanel ParentPanel)
            {
                Parent = ParentPanel;
                InitializeComponent();
                Control[] Ctrls = new Control[] { Selected, Name, Status, StatusIcon, Progress, StartStop, Properties, Delete };
                Parent.RowCount = Parent.RowCount + 1;
                for (int i = 0; i < Ctrls.Length; i++)
                {
                    Parent.Controls.Add(Ctrls[i], i, Parent.RowCount - 1);
                    Parent.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
                }
            }
        }

        private void TasksGuiTable_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            if (e.Row == 0)
            {
                Graphics g = e.Graphics;
                Rectangle r = e.CellBounds;
                g.FillRectangle(SystemBrushes.Control, r);
            }
        }
    }
}
