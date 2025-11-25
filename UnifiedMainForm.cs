using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace Game
{
    public class UserInfo
    {
        public string Password { get; set; }
        public string Nickname { get; set; }
        public DateTime RegDate { get; set; }
    }

    public partial class UnifiedMainForm : Form
    {
        private Dictionary<string, UserInfo> _userDatabase = new Dictionary<string, UserInfo>();
        private UserInfo _currentUser = null;
        private string _currentUserEmail = "";

        private enum Lang { KR, EN, JP }
        private Lang _currentLang = Lang.KR;

        private readonly Color ColorBg = Color.FromArgb(245, 247, 250);
        private readonly Color ColorWood = Color.FromArgb(220, 180, 130);
        private readonly Color ColorAccentBlue = Color.FromArgb(66, 133, 244);
        private readonly Color ColorAccentRed = Color.FromArgb(234, 67, 53);

        private CustomMenuStrip _menuStrip;
        private Panel _pnlIntro;
        private Panel _pnlLogin;
        private Panel _pnlGameLoading;
        private Panel _pnlGame;
        private PausePanel _pausePanel;
        private PausePanel _myInfoPanel;

        private enum IntroSeq { LogoFadeIn, LogoStay, LogoFadeOut, LoadingBar, LoadingWait, ShowButtons }
        private IntroSeq _introSeq = IntroSeq.LogoFadeIn;
        private Timer _introTimer;
        private double _introElapsed = 0;
        private float _logoAlpha = 0f;
        private Image _logoImage;
        private ZenLoadingBar _introProgressBar;
        private Label _introLoadingLbl;
        private DiagonalSlideButton _btnLocal, _btnAI, _btnExit;
        private Image _mainMenuBgImage;

        private ModernTextBox _inputEmail, _inputPwd, _inputPwdConfirm, _inputNick;
        private RoundedActionButton _btnLoginAction;
        private Label _lblLoginTitle, _lblSwitchMode;
        private bool _isSignupMode = false;

        private Timer _gameLoadTimer;
        private float _gameLoadProgress = 0;
        private ZenLoadingBar _gameLoadBar;
        private Label _gameLoadLbl;
        private PictureBox _gifBox;

        private BoardPanel _boardPanel;
        private Label _lblP1, _lblP2;
        private RoundProgressBar _barP1, _barP2;
        private GhostButton _btnGameMenu;

        private const int BOARD_SIZE = 19;
        private int[,] _boardState = new int[BOARD_SIZE, BOARD_SIZE];
        private int[,] _forbiddenSpots = new int[BOARD_SIZE, BOARD_SIZE];
        private int _currentPlayer = 1;
        private int _lastMoveX = -1;
        private int _lastMoveY = -1;
        private bool _isAiMode = false;

        private OmokAI _ai;

        private Timer _turnTimer;
        private const int TURN_TIME = 30;
        private int _timeRemaining = TURN_TIME;


        private Label _lblPauseTitle;
        private SimpleRoundedButton _btnPauseResume, _btnPauseRetry, _btnPauseQuit;

        public UnifiedMainForm()
        {
            this.DoubleBuffered = true;
            this.Text = "오목";
            this.ClientSize = new Size(600, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = ColorBg;

            try
            {
                string[] paths = {
                    "AppIcon.ico",
                    @"Resources\AppIcon.ico",
                    @"IntroImages\company_logo.png"
                };

                foreach (var p in paths)
                {
                    string fullPath = Path.Combine(Application.StartupPath, p);
                    if (File.Exists(fullPath))
                    {
                        if (p.EndsWith(".ico")) this.Icon = new Icon(fullPath);
                        else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                        break;
                    }
                }
            }
            catch { }

            InitMenuStrip();
            InitIntroScreen();
            InitLoginScreen();
            InitMyInfoPanel();
            InitGameLoadingScreen();
            InitGameScreen();
            InitPausePanel();

            _menuStrip.Visible = false;
            _pnlIntro.Visible = true;
            _pnlIntro.BringToFront();

            StartIntroAnimation();
        }

        //=================================================================
        // 메뉴바 & 언어 시스템
        //=================================================================
        private void InitMenuStrip()
        {
            _menuStrip = new CustomMenuStrip();
            _menuStrip.Dock = DockStyle.Top;

            var menuFile = new ToolStripMenuItem("메뉴");
            var itemMyInfo = new ToolStripMenuItem("내 정보");
            var itemLang = new ToolStripMenuItem("언어");

            var itemKr = new ToolStripMenuItem("한국어");
            var itemEn = new ToolStripMenuItem("English");
            var itemJp = new ToolStripMenuItem("日本語");

            itemKr.Click += (s, e) => ChangeLanguage(Lang.KR);
            itemEn.Click += (s, e) => ChangeLanguage(Lang.EN);
            itemJp.Click += (s, e) => ChangeLanguage(Lang.JP);

            itemLang.DropDownItems.AddRange(new ToolStripItem[] { itemKr, itemEn, itemJp });
            itemMyInfo.Click += (s, e) => ShowMyInfo();

            menuFile.DropDownItems.Add(itemMyInfo);
            menuFile.DropDownItems.Add(itemLang);

            var menuHelp = new ToolStripMenuItem("도움말");
            var itemRule = new ToolStripMenuItem("게임 규칙");
            var itemCreator = new ToolStripMenuItem("제작자");

            itemRule.Click += (s, e) => ShowMessage(GetText("RuleContent"), "Rules");
            itemCreator.Click += (s, e) =>
            {
                string msg =
                    "【 제작자 정보 】\n" +
                    "\n" +
                    "제작자 : 윈도우프로그래밍 4조\n" +
                    "\n" +
                    "이 오목 게임은 C# Windows Forms를 기반으로,\n" +
                    "로그인 시스템, 금수 판정, AI 대전,\n" +
                    "턴 타이머 등 다양한 기능을 직접 구현하며\n" +
                    "공부 목적으로 제작되었습니다.\n" +
                    "\n" +
                    "플레이해 주셔서 감사합니다!";

                ShowMessage(msg, "제작자");
            };

            menuHelp.DropDownItems.Add(itemRule);
            menuHelp.DropDownItems.Add(itemCreator);

            _menuStrip.Items.Add(menuFile);
            _menuStrip.Items.Add(menuHelp);

            this.Controls.Add(_menuStrip);
        }

        private void ChangeLanguage(Lang lang)
        {
            _currentLang = lang;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (_btnLocal != null) _btnLocal.Title = GetText("Start_Local");
            if (_btnAI != null) _btnAI.Title = GetText("Start_AI");
            if (_btnExit != null) _btnExit.Title = GetText("Start_Exit");

            if (_lblPauseTitle != null) _lblPauseTitle.Text = GetText("Pause_Title");
            if (_btnPauseResume != null) _btnPauseResume.Text = GetText("Pause_Resume");
            if (_btnPauseRetry != null) _btnPauseRetry.Text = GetText("Pause_Retry");
            if (_btnPauseQuit != null) _btnPauseQuit.Text = GetText("Pause_Quit");

            _menuStrip.Items[0].Text = GetText("Menu_Menu");
            ((ToolStripMenuItem)_menuStrip.Items[0]).DropDownItems[0].Text = GetText("Menu_MyInfo");
            ((ToolStripMenuItem)_menuStrip.Items[0]).DropDownItems[1].Text = GetText("Menu_Lang");

            _menuStrip.Items[1].Text = GetText("Menu_Help");
            ((ToolStripMenuItem)_menuStrip.Items[1]).DropDownItems[0].Text = GetText("Menu_Rule");
            ((ToolStripMenuItem)_menuStrip.Items[1]).DropDownItems[1].Text = GetText("Menu_Creator");

            UpdateLoginUIText();
            if (_lblP2 != null)
            {
                _lblP2.Text = _isAiMode ? "AI" : GetText("Opponent");
            }
            Refresh();
        }

        private void UpdateLoginUIText()
        {
            if (_lblLoginTitle == null) return;
            if (_isSignupMode)
            {
                _lblLoginTitle.Text = "SIGN UP";
                _btnLoginAction.Text = GetText("Btn_Signup");
                _lblSwitchMode.Text = GetText("Link_GoLogin");
            }
            else
            {
                _lblLoginTitle.Text = "LOGIN";
                _btnLoginAction.Text = GetText("Btn_Login");
                _lblSwitchMode.Text = GetText("Link_GoSignup");
            }
            _inputEmail.PlaceholderText = GetText("Placeholder_Email");
            _inputPwd.PlaceholderText = GetText("Placeholder_Pwd");
            _inputPwdConfirm.PlaceholderText = GetText("Placeholder_PwdConfirm");
            _inputNick.PlaceholderText = GetText("Placeholder_Nick");
        }

        private string GetText(string key)
        {
            switch (key)
            {
                case "Start_Local": return _currentLang == Lang.KR ? "로컬 대전" : (_currentLang == Lang.JP ? "ローカル対戦" : "Local Game");
                case "Start_AI": return _currentLang == Lang.KR ? "AI 대전" : (_currentLang == Lang.JP ? "AI対戦" : "AI Match");
                case "Start_Exit": return _currentLang == Lang.KR ? "게임 종료" : (_currentLang == Lang.JP ? "終了" : "Exit");

                case "Pause_Title": return _currentLang == Lang.KR ? "일시정지" : (_currentLang == Lang.JP ? "一時停止" : "PAUSE");
                case "Pause_Resume": return _currentLang == Lang.KR ? "계속하기" : (_currentLang == Lang.JP ? "再開" : "Resume");
                case "Pause_Retry": return _currentLang == Lang.KR ? "처음부터" : (_currentLang == Lang.JP ? "最初から" : "Restart");
                case "Pause_Quit": return _currentLang == Lang.KR ? "나가기" : (_currentLang == Lang.JP ? "出る" : "Quit");

                case "Menu_Menu": return _currentLang == Lang.KR ? "메뉴" : (_currentLang == Lang.JP ? "メニュー" : "Menu");
                case "Menu_MyInfo": return _currentLang == Lang.KR ? "내 정보" : (_currentLang == Lang.JP ? "マイ情報" : "My Info");
                case "Menu_Lang": return _currentLang == Lang.KR ? "언어" : (_currentLang == Lang.JP ? "言語" : "Language");
                case "Menu_Help": return _currentLang == Lang.KR ? "도움말" : (_currentLang == Lang.JP ? "ヘルプ" : "Help");
                case "Menu_Rule": return _currentLang == Lang.KR ? "게임 규칙" : (_currentLang == Lang.JP ? "ルール" : "Rules");
                case "Menu_Creator": return _currentLang == Lang.KR ? "제작자" : (_currentLang == Lang.JP ? "作成者" : "Creator");

                case "RuleContent":
                    if (_currentLang == Lang.KR)
                    {
                        return
                            "【 오목 규칙 】\n" +
                            "\n" +
                            "1. 흑(선)과 백(후)이 번갈아 한 수씩 둡니다.\n" +
                            "2. 가로, 세로, 또는 대각선으로\n" +
                            "   같은 색 돌이 5개 연속이면 승리입니다.\n" +
                            "3. 흑에게는 금수 규칙이 적용됩니다.\n" +
                            "   - 3·3 금수 : 동시에 두 개의 열린 3이 되는 수\n" +
                            "   - 4·4 금수 : 동시에 두 개의 열린 4가 되는 수\n" +
                            "   - 6목 금수 : 같은 색 돌이 6개 이상 일직선이 되는 수\n" +
                            "\n" +
                            "위 규칙을 기준으로 게임이 진행됩니다.";
                    }
                    else if (_currentLang == Lang.JP)
                    {
                        return
                            "【 オモクのルール 】\n\n" +
                            "1. 黒と白が交互に一手ずつ打ちます。\n" +
                            "2. 横・縦・斜めに同じ色の石が\n" +
                            "   5つ連続すると勝ちです。\n" +
                            "3. 黒には禁じ手(三々・四々・六目)が適用されます。";
                    }
                    else
                    {
                        return
                            "[ Omok Rules ]\n\n" +
                            "1. Black plays first and players\n" +
                            "   alternate placing stones.\n" +
                            "2. The player who makes 5 stones in a row\n" +
                            "   (horizontal, vertical, or diagonal) wins.\n" +
                            "3. For Black, forbidden moves are applied:\n" +
                            "   - Double three\n" +
                            "   - Double four\n" +
                            "   - Overline (6 or more in a row).";
                    }
                case "Btn_Login": return _currentLang == Lang.KR ? "로그인" : (_currentLang == Lang.JP ? "ログイン" : "Login");
                case "Btn_Signup": return _currentLang == Lang.KR ? "가입하기" : (_currentLang == Lang.JP ? "登録" : "Sign Up");
                case "Link_GoSignup": return _currentLang == Lang.KR ? "회원가입" : (_currentLang == Lang.JP ? "会員登録" : "Sign Up");
                case "Link_GoLogin": return _currentLang == Lang.KR ? "로그인 화면으로" : (_currentLang == Lang.JP ? "ログインへ" : "Back to Login");

                case "Placeholder_Email": return "Email / ID";
                case "Placeholder_Pwd": return "Password";
                case "Placeholder_PwdConfirm": return "Confirm Password";
                case "Placeholder_Nick": return "Nickname";
                case "Opponent": return _currentLang == Lang.KR ? "상대" : (_currentLang == Lang.JP ? "相手" : "Opponent");

                case "Msg_AI_Ready": return _currentLang == Lang.KR ? "AI 모드는 준비 중입니다." : "AI Mode Not Ready.";
                case "Msg_Input_Err": return _currentLang == Lang.KR ? "모든 정보를 입력해주세요." : "Check input.";
                case "Msg_Pwd_Mismatch": return _currentLang == Lang.KR ? "비밀번호가 일치하지 않습니다." : "Passwords do not match.";
                case "Msg_Email_Exist": return _currentLang == Lang.KR ? "이미 존재하는 계정입니다." : "Account exists.";
                case "Msg_Signup_Ok": return _currentLang == Lang.KR ? "가입 완료! 로그인해주세요." : "Signup Complete!";
                case "Msg_Login_Fail": return _currentLang == Lang.KR ? "계정 정보가 틀렸습니다." : "Login Failed.";
            }
            return key;
        }

        // =================================================================
        // 스플래시 이미지
        // =================================================================
        private void InitIntroScreen()
        {
            _pnlIntro = new IntroPanel { Dock = DockStyle.Fill, Visible = false };
            _pnlIntro.Paint += PnlIntro_Paint;
            this.Controls.Add(_pnlIntro);

            _logoImage = LoadImageSafe(@"IntroImages\company_logo.png");

            _mainMenuBgImage = LoadImageSafe(@"IntroImages\main_menu_bg.png");

            _introLoadingLbl = new Label
            {
                Text = "GAME LOADING...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                Size = new Size(200, 20),
                Visible = false,
                BackColor = Color.Transparent
            };

            _introProgressBar = new ZenLoadingBar
            {
                Size = new Size(260, 8),
                BarColor = Color.FromArgb(50, 50, 50),
                TrackColor = Color.FromArgb(220, 220, 220),
                Visible = false
            };

            _btnLocal = new DiagonalSlideButton { Title = "로컬 대전", Size = new Size(300, 70), Visible = false };
            _btnAI = new DiagonalSlideButton { Title = "AI 대전", Size = new Size(300, 70), Visible = false };
            _btnExit = new DiagonalSlideButton { Title = "게임 종료", Size = new Size(300, 70), Visible = false };

            _btnLocal.Click += (s, e) =>
            {
                _isAiMode = false;
                StartGameLoadingSequence();
            };
            _btnAI.Click += (s, e) =>
            {
                _isAiMode = true;
                StartGameLoadingSequence();
            };
            _btnExit.Click += (s, e) => Application.Exit();

            _pnlIntro.Controls.Add(_introLoadingLbl);
            _pnlIntro.Controls.Add(_introProgressBar);
            _pnlIntro.Controls.Add(_btnLocal);
            _pnlIntro.Controls.Add(_btnAI);
            _pnlIntro.Controls.Add(_btnExit);

            _pnlIntro.Resize += (s, e) => LayoutIntro();
        }

        private void IntroTimer_Tick(object sender, EventArgs e)
        {
            _introElapsed += 0.03;
            switch (_introSeq)
            {
                case IntroSeq.LogoFadeIn:
                    if (_introElapsed < 1.5) _logoAlpha = (float)(_introElapsed / 1.5);
                    else { _logoAlpha = 1f; _introElapsed = 0; _introSeq = IntroSeq.LogoStay; }
                    _pnlIntro.Invalidate(); break;

                case IntroSeq.LogoStay:
                    if (_introElapsed >= 1.0) { _introElapsed = 0; _introSeq = IntroSeq.LogoFadeOut; }
                    break;

                case IntroSeq.LogoFadeOut:
                    if (_introElapsed < 1.0) _logoAlpha = 1f - (float)(_introElapsed / 1.0);
                    else
                    {
                        _logoAlpha = 0f; _introElapsed = 0; _introSeq = IntroSeq.LoadingBar;
                        _pnlIntro.Invalidate(); _introLoadingLbl.Visible = true; _introProgressBar.Visible = true;
                    }
                    _pnlIntro.Invalidate(); break;

                case IntroSeq.LoadingBar:
                    if (_introElapsed < 4.0)
                    {
                        int dotCount = (int)(_introElapsed * 3) % 3 + 1;
                        _introLoadingLbl.Text = "GAME LOADING" + new string('.', dotCount);

                        _introProgressBar.Value = (int)((_introElapsed / 4.0) * 100);
                        _introProgressBar.Invalidate();
                    }
                    else
                    {
                        _introProgressBar.Value = 100; _introProgressBar.Invalidate();
                        _introElapsed = 0; _introSeq = IntroSeq.LoadingWait;
                    }
                    break;

                case IntroSeq.LoadingWait:
                    if (_introElapsed >= 1.0)
                    {
                        _introLoadingLbl.Visible = false; _introProgressBar.Visible = false;
                        _introTimer.Stop();
                        ShowScreen(_pnlLogin);
                    }
                    break;
            }
        }

        private void LayoutIntro()
        {
            int cx = _pnlIntro.Width / 2, cy = _pnlIntro.Height / 2;
            _introProgressBar.Location = new Point(cx - _introProgressBar.Width / 2, cy + 60);
            _introLoadingLbl.Location = new Point(cx - _introLoadingLbl.Width / 2, _introProgressBar.Top - 30);

            int gap = 25, startY = cy + 20;
            _btnLocal.Location = new Point(cx - _btnLocal.Width / 2, startY);
            _btnAI.Location = new Point(cx - _btnAI.Width / 2, startY + _btnLocal.Height + gap);
            _btnExit.Location = new Point(cx - _btnExit.Width / 2, startY + (_btnLocal.Height + gap) * 2);
        }

        private void StartIntroAnimation()
        {
            _introSeq = IntroSeq.LogoFadeIn; _introElapsed = 0; _logoAlpha = 0;

            _introLoadingLbl.Visible = false; _introProgressBar.Visible = false;
            _btnLocal.Visible = false; _btnAI.Visible = false; _btnExit.Visible = false;

            _introTimer = new Timer { Interval = 30 };
            _introTimer.Tick += IntroTimer_Tick;
            _introTimer.Start();
        }

        private void IntroTimer_MainTick(object sender, EventArgs e)
        {
            _introElapsed += 0.03;
            switch (_introSeq)
            {
                case IntroSeq.LogoFadeIn:
                    if (_introElapsed < 1.5) _logoAlpha = (float)(_introElapsed / 1.5);
                    else { _logoAlpha = 1f; _introElapsed = 0; _introSeq = IntroSeq.LogoStay; }
                    _pnlIntro.Invalidate(); break;

                case IntroSeq.LogoStay:
                    if (_introElapsed >= 1.0) { _introElapsed = 0; _introSeq = IntroSeq.LogoFadeOut; }
                    break;

                case IntroSeq.LogoFadeOut:
                    if (_introElapsed < 1.0) _logoAlpha = 1f - (float)(_introElapsed / 1.0);
                    else
                    {
                        _logoAlpha = 0f; _introElapsed = 0; _introSeq = IntroSeq.LoadingBar;
                        _pnlIntro.Invalidate(); _introLoadingLbl.Visible = true; _introProgressBar.Visible = true;
                    }
                    _pnlIntro.Invalidate(); break;

                case IntroSeq.LoadingBar:
                    if (_introElapsed < 4.0)
                    {
                        _introProgressBar.Value = (int)((_introElapsed / 4.0) * 100);
                        _introProgressBar.Invalidate();
                    }
                    else
                    {
                        _introProgressBar.Value = 100; _introProgressBar.Invalidate();
                        _introElapsed = 0; _introSeq = IntroSeq.LoadingWait;
                    }
                    break;

                case IntroSeq.LoadingWait:
                    if (_introElapsed >= 1.0)
                    {
                        _introLoadingLbl.Visible = false; _introProgressBar.Visible = false;
                        _introTimer.Stop();
                        ShowScreen(_pnlLogin);
                    }
                    break;
            }
        }

        private void PnlIntro_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            Rectangle rect = _pnlIntro.ClientRectangle;

            if (_introSeq == IntroSeq.ShowButtons && _mainMenuBgImage != null)
            {
                Image img = _mainMenuBgImage;

                g.Clear(Color.White);

                float imgRatio = (float)img.Width / img.Height;
                float rectRatio = (float)rect.Width / rect.Height;

                Rectangle src;
                Rectangle dest = rect;

                if (rectRatio > imgRatio)
                {
                    int cropH = (int)(img.Width / rectRatio);
                    int y = (img.Height - cropH) / 2;
                    src = new Rectangle(0, y, img.Width, cropH);
                }
                else
                {
                    int cropW = (int)(img.Height * rectRatio);
                    int x = (img.Width - cropW) / 2;
                    src = new Rectangle(x, 0, cropW, img.Height);
                }

                g.DrawImage(img, dest, src, GraphicsUnit.Pixel);
            }
            else
            {
                using (LinearGradientBrush br =
                    new LinearGradientBrush(rect,
                        Color.FromArgb(245, 247, 250),
                        Color.FromArgb(223, 228, 234),
                        60F))
                {
                    g.FillRectangle(br, rect);
                }

                if ((_introSeq == IntroSeq.LogoFadeIn ||
                     _introSeq == IntroSeq.LogoStay ||
                     _introSeq == IntroSeq.LogoFadeOut) && _logoImage != null)
                {
                    int maxW = 200;
                    float ratio = (float)_logoImage.Height / _logoImage.Width;
                    int dw = Math.Min(_logoImage.Width, maxW);
                    int dh = (int)(dw * ratio);

                    int x = (_pnlIntro.Width - dw) / 2;
                    int y = (_pnlIntro.Height - dh) / 2;

                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        ColorMatrix matrix = new ColorMatrix { Matrix33 = _logoAlpha };
                        attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        g.DrawImage(_logoImage,
                            new Rectangle(x, y, dw, dh),
                            0, 0, _logoImage.Width, _logoImage.Height,
                            GraphicsUnit.Pixel, attr);
                    }
                }
            }
        }



        // =================================================================
        // 로그인 / 회원가입 화면
        // =================================================================
        private void InitLoginScreen()
        {
            _pnlLogin = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = ColorBg };
            this.Controls.Add(_pnlLogin);

            var loginBox = new RoundedPanel { Size = new Size(380, 520), BackColor = Color.White };
            _pnlLogin.Controls.Add(loginBox);

            _lblLoginTitle = new Label
            {
                Text = "LOGIN",
                Dock = DockStyle.Top,
                Height = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            _inputEmail = new ModernTextBox { PlaceholderText = "아이디 또는 이메일", Size = new Size(300, 45) };
            _inputNick = new ModernTextBox { PlaceholderText = "닉네임", Size = new Size(300, 45), Visible = false };
            _inputPwd = new ModernTextBox { PlaceholderText = "비밀번호", Size = new Size(300, 45), IsPassword = true };
            _inputPwdConfirm = new ModernTextBox { PlaceholderText = "비밀번호 확인", Size = new Size(300, 45), IsPassword = true, Visible = false };

            _btnLoginAction = new RoundedActionButton
            {
                Text = "로그인",
                Size = new Size(300, 50),
                BaseColor = ColorAccentBlue,
                HoverColor = Color.FromArgb(50, 110, 220)
            };

            _lblSwitchMode = new Label
            {
                Text = "회원가입",
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = new Font("맑은 고딕", 9, FontStyle.Underline),
                ForeColor = Color.Gray
            };

            _btnLoginAction.Click += (s, e) => ProcessLoginOrSignup();
            _lblSwitchMode.Click += (s, e) => ToggleLoginMode();

            loginBox.Controls.Add(_lblSwitchMode);
            loginBox.Controls.Add(_btnLoginAction);
            loginBox.Controls.Add(_inputPwdConfirm);
            loginBox.Controls.Add(_inputPwd);
            loginBox.Controls.Add(_inputNick);
            loginBox.Controls.Add(_inputEmail);
            loginBox.Controls.Add(_lblLoginTitle);

            _pnlLogin.Resize += (s, e) =>
            {
                loginBox.Location = new Point((_pnlLogin.Width - loginBox.Width) / 2, (_pnlLogin.Height - loginBox.Height) / 2);
                LayoutLoginBox(loginBox);
            };

            LayoutLoginBox(loginBox);
        }

        private void LayoutLoginBox(Panel box)
        {
            int startY = 120, gap = 15, x = (box.Width - 300) / 2;

            _inputEmail.Location = new Point(x, startY);

            if (_isSignupMode)
            {
                _inputNick.Location = new Point(x, _inputEmail.Bottom + gap);
                _inputPwd.Location = new Point(x, _inputNick.Bottom + gap);
                _inputPwdConfirm.Location = new Point(x, _inputPwd.Bottom + gap);
                _btnLoginAction.Location = new Point(x, _inputPwdConfirm.Bottom + 30);
            }
            else
            {
                _inputPwd.Location = new Point(x, _inputEmail.Bottom + gap);
                _btnLoginAction.Location = new Point(x, _inputPwd.Bottom + 30);
            }

            _lblSwitchMode.Location = new Point((box.Width - _lblSwitchMode.Width) / 2, _btnLoginAction.Bottom + 20);
        }

        private void ToggleLoginMode()
        {
            _isSignupMode = !_isSignupMode;

            _inputEmail.Clear(); _inputPwd.Clear(); _inputNick.Clear(); _inputPwdConfirm.Clear();

            _inputNick.Visible = _isSignupMode;
            _inputPwdConfirm.Visible = _isSignupMode;

            UpdateLoginUIText();

            if (_pnlLogin.Controls.Count > 0) LayoutLoginBox((Panel)_pnlLogin.Controls[0]);
        }

        private void ProcessLoginOrSignup()
        {
            string email = _inputEmail.Text.Trim();
            string pwd = _inputPwd.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pwd))
            {
                ShowMessage(GetText("Msg_Input_Err"), "Error"); return;
            }

            if (_isSignupMode)
            {
                string nick = _inputNick.Text.Trim();
                string pwdConf = _inputPwdConfirm.Text.Trim();

                if (string.IsNullOrEmpty(nick)) { ShowMessage(GetText("Msg_Input_Err"), "Error"); return; }
                if (pwd != pwdConf) { ShowMessage(GetText("Msg_Pwd_Mismatch"), "Error"); return; }

                if (_userDatabase.ContainsKey(email))
                {
                    ShowMessage(GetText("Msg_Email_Exist"), "Error");
                }
                else
                {
                    _userDatabase.Add(email, new UserInfo { Password = pwd, Nickname = nick, RegDate = DateTime.Now });
                    ShowMessage(GetText("Msg_Signup_Ok"), "Success");
                    ToggleLoginMode();
                }
            }
            else
            {
                if (_userDatabase.ContainsKey(email) && _userDatabase[email].Password == pwd)
                {
                    _currentUser = _userDatabase[email];
                    _currentUserEmail = email;

                    ShowScreen(_pnlIntro);
                    _btnLocal.Visible = true;
                    _btnAI.Visible = true; 
                    _btnExit.Visible = true;
                    _menuStrip.Visible = true;

                    _introSeq = IntroSeq.ShowButtons;
                    _introLoadingLbl.Visible = false;
                    _introProgressBar.Visible = false;
                    _pnlIntro.Invalidate();
                    ApplyLanguage();
                }
                else
                {
                    ShowMessage(GetText("Msg_Login_Fail"), "Error");
                }
            }
        }

        // =================================================================
        // 내 정보창 (오버레이)
        // =================================================================
        private void InitMyInfoPanel()
        {
            _myInfoPanel = new PausePanel { Dock = DockStyle.Fill, Visible = false };
            this.Controls.Add(_myInfoPanel);

            var box = new RoundedPanel { Size = new Size(320, 250), BackColor = Color.White };
            _myInfoPanel.Controls.Add(box);

            var title = new Label { Text = "내 정보", Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 18, FontStyle.Bold) };
            var infoLbl = new Label
            {
                Text = "",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 11)
            };
            var btnClose = new RoundedActionButton { Text = "닫기", Size = new Size(100, 40), BaseColor = Color.Gray, HoverColor = Color.DarkGray };
            btnClose.Click += (s, e) => _myInfoPanel.Visible = false;
            btnClose.Location = new Point((box.Width - btnClose.Width) / 2, 190);

            box.Controls.Add(btnClose);
            box.Controls.Add(infoLbl);
            box.Controls.Add(title);

            _myInfoPanel.Tag = infoLbl;
            _myInfoPanel.Resize += (s, e) => box.Location = new Point((_myInfoPanel.Width - box.Width) / 2, (_myInfoPanel.Height - box.Height) / 2);
        }

        private void ShowMyInfo()
        {
            if (_currentUser == null) return;
            if (_myInfoPanel.Tag is Label lbl)
            {
                lbl.Text = $"ID: {_currentUserEmail}\n" +
                           $"닉네임: {_currentUser.Nickname}\n" +
                           $"가입일: {_currentUser.RegDate:yyyy-MM-dd}";
            }
            _myInfoPanel.Visible = true; _myInfoPanel.BringToFront();
        }

        // =================================================================
        // 게임 로딩 화면
        // =================================================================
        private void InitGameLoadingScreen()
        {
            _pnlGameLoading = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.White };
            this.Controls.Add(_pnlGameLoading);

            _gifBox = new PictureBox { Size = new Size(80, 80), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            try { string p = Path.Combine(Application.StartupPath, @"IntroImages\main_loading.gif"); if (File.Exists(p)) _gifBox.Image = Image.FromFile(p); } catch { }

            _gameLoadLbl = new Label { Text = "Initializing...", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 11), Size = new Size(300, 30) };
            _gameLoadBar = new ZenLoadingBar { Size = new Size(300, 8), BarColor = ColorAccentBlue };

            _pnlGameLoading.Controls.Add(_gifBox);
            _pnlGameLoading.Controls.Add(_gameLoadLbl);
            _pnlGameLoading.Controls.Add(_gameLoadBar);

            _pnlGameLoading.Resize += (s, e) =>
            {
                int cx = _pnlGameLoading.Width / 2, cy = _pnlGameLoading.Height / 2;
                _gifBox.Location = new Point(cx - 40, cy - 80);
                _gameLoadLbl.Location = new Point(cx - 150, cy + 20);
                _gameLoadBar.Location = new Point(cx - 150, cy + 60);
            };
            _gameLoadTimer = new Timer { Interval = 50 };
            _gameLoadTimer.Tick += GameLoadTimer_Tick;
        }

        private void StartGameLoadingSequence()
        {
            _pnlIntro.Visible = false; _menuStrip.Visible = false;
            _pnlGameLoading.Visible = true; _pnlGameLoading.BringToFront();
            _gameLoadProgress = 0; _gameLoadBar.Value = 0; _gameLoadTimer.Start();
        }

        private void GameLoadTimer_Tick(object sender, EventArgs e)
        {
            _gameLoadProgress += 0.9f;
            if (_gameLoadProgress > 100) _gameLoadProgress = 100;
            _gameLoadBar.Value = (int)_gameLoadProgress;
            _gameLoadBar.Invalidate();

            if (_gameLoadProgress < 30) _gameLoadLbl.Text = "리소스 초기화 중...";
            else if (_gameLoadProgress < 60) _gameLoadLbl.Text = "게임 리소스 로딩 중...";
            else if (_gameLoadProgress < 90) _gameLoadLbl.Text = "플레이어 데이터 동기화...";
            else _gameLoadLbl.Text = "준비 완료!";

            if (_gameLoadProgress >= 100)
            {
                _gameLoadTimer.Stop();
                Timer wait = new Timer { Interval = 800 };
                wait.Tick += (s, arg) =>
                {
                    wait.Stop();
                    _pnlGameLoading.Visible = false;
                    _pnlGame.Visible = true;
                    _pnlGame.BringToFront();
                    _menuStrip.Visible = true;

                    if (_currentUser != null) _lblP1.Text = _currentUser.Nickname;
                    _lblP2.Text = _isAiMode ? "AI" : GetText("Opponent");

                    ResetGameState();
                    StartTurnTimer();
                };
                wait.Start();
            }
        }

        // =================================================================
        // 게임 화면
        // =================================================================
        private void InitGameScreen()
        {
            _pnlGame = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = ColorBg };
            this.Controls.Add(_pnlGame);

            var font = new Font("맑은 고딕", 14, FontStyle.Bold);

            _lblP2 = new Label { Text = GetText("Opponent"), Font = font, AutoSize = true, BackColor = Color.Transparent };
            _barP2 = new RoundProgressBar { Height = 12, Value = 100, BarColor = ColorAccentRed };

            _lblP1 = new Label { Text = "Me", Font = font, AutoSize = true, BackColor = Color.Transparent };
            _barP1 = new RoundProgressBar { Height = 12, Value = 100, BarColor = ColorAccentBlue };

            _boardPanel = new BoardPanel { BoardColor = ColorWood };
            _boardPanel.BoardState = _boardState;
            _boardPanel.ForbiddenSpots = _forbiddenSpots;
            _boardPanel.MouseClick += BoardPanel_MouseClick;

            _btnGameMenu = new GhostButton { Text = "MENU", Size = new Size(90, 36) };
            _btnGameMenu.Click += (s, e) => ShowPauseMenu();

            _pnlGame.Controls.Add(_lblP2); _pnlGame.Controls.Add(_barP2);
            _pnlGame.Controls.Add(_lblP1); _pnlGame.Controls.Add(_barP1);
            _pnlGame.Controls.Add(_boardPanel); _pnlGame.Controls.Add(_btnGameMenu);

            _pnlGame.Resize += (s, e) => LayoutGame();

            _turnTimer = new Timer { Interval = 1000 };
            _turnTimer.Tick += TurnTimer_Tick;
        }

        private void LayoutGame()
        {
            int w = _pnlGame.Width, h = _pnlGame.Height, m = 30;

            _lblP2.Location = new Point(w - m - _lblP2.Width, m + 30);
            _barP2.SetBounds(m, _lblP2.Bottom + 10, w - m * 2, 12);

            _barP1.SetBounds(m, h - m - 12, w - m * 2, 12);

            int btnY = _barP1.Top - 15 - _btnGameMenu.Height;
            _btnGameMenu.Location = new Point(w - m - _btnGameMenu.Width, btnY);

            int labelY = _btnGameMenu.Top + (_btnGameMenu.Height / 2) - (_lblP1.Height / 2);
            _lblP1.Location = new Point(m, labelY);

            int topY = _barP2.Bottom + 20;
            int botY = _btnGameMenu.Top - 20;
            int availH = botY - topY;
            int size = Math.Min(w - m * 2, availH); if (size < 100) size = 100;

            _boardPanel.SetBounds((w - size) / 2, topY + (availH - size) / 2, size, size);
        }

        // =================================================================
        // 턴 타이머 로직
        // =================================================================
        private void StartTurnTimer()
        {
            _timeRemaining = TURN_TIME;
            UpdateTimeBars();
            _turnTimer.Start();
        }

        private void StopTurnTimer()
        {
            _turnTimer?.Stop();
        }

        private void TurnTimer_Tick(object sender, EventArgs e)
        {
            _timeRemaining--;
            if (_timeRemaining < 0) _timeRemaining = 0;

            UpdateTimeBars();

            if (_timeRemaining == 0)
            {
                HandleTimeOver();
            }
        }

        private void UpdateTimeBars()
        {
            int pct = (int)Math.Round(_timeRemaining * 100.0 / TURN_TIME);
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;

            if (_currentPlayer == 1)
            {
                _barP1.Value = pct;
                _barP2.Value = 100;
            }
            else
            {
                _barP2.Value = pct;
                _barP1.Value = 100;
            }

            _barP1.Invalidate();
            _barP2.Invalidate();
        }

        private void AutoMoveOnTimeout()
        {
            bool hasEmpty = false;
            for (int x = 0; x < BOARD_SIZE && !hasEmpty; x++)
                for (int y = 0; y < BOARD_SIZE && !hasEmpty; y++)
                    if (_boardState[x, y] == 0) hasEmpty = true;

            if (!hasEmpty)
            {
                ResetGameState();
                StartTurnTimer();
                _boardPanel.Invalidate();
                return;
            }

            int me = _currentPlayer;
            int other = (me == 1) ? 2 : 1;

            var helperAI = new OmokAI(_boardState, me, other);
            var move = helperAI.GetNextMove(false);

            int gx = move.X;
            int gy = move.Y;

            if (gx < 0 || gy < 0 || gx >= BOARD_SIZE || gy >= BOARD_SIZE || _boardState[gx, gy] != 0)
            {
                _currentPlayer = other;
                StartTurnTimer();
                return;
            }

            _boardState[gx, gy] = me;
            _lastMoveX = gx;
            _lastMoveY = gy;
            _boardPanel.LastMoveX = gx;
            _boardPanel.LastMoveY = gy;

            if (OmokRules.CheckForWin(gx, gy, me, _boardState))
            {
                string winnerName = (me == 1)
                    ? (_currentUser?.Nickname ?? "Player 1")
                    : (_isAiMode ? "AI" : "Player 2");

                MessageBox.Show($"{winnerName} 승리!", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetGameState();
                StartTurnTimer();
                _boardPanel.Invalidate();
                return;
            }

            _currentPlayer = other;

            OmokRules.CalculateForbiddenSpots(_boardState, _forbiddenSpots);
            _boardPanel.Invalidate();

            if (_isAiMode && _currentPlayer == 2)
            {
                AITurn();
            }
            else
            {
                StartTurnTimer();
            }
        }

        private void HandleTimeOver()
        {
            StopTurnTimer();
            AutoMoveOnTimeout();
        }

        // =================================================================
        // 오목 게임 상태 / 클릭 처리
        // =================================================================
        private void ResetGameState()
        {
            Array.Clear(_boardState, 0, _boardState.Length);
            Array.Clear(_forbiddenSpots, 0, _forbiddenSpots.Length);
            _currentPlayer = 1;
            _lastMoveX = _lastMoveY = -1;

            _boardPanel.BoardState = _boardState;
            _boardPanel.ForbiddenSpots = _forbiddenSpots;
            _boardPanel.LastMoveX = _lastMoveX;
            _boardPanel.LastMoveY = _lastMoveY;

            if (_isAiMode)
            {
                _ai = new OmokAI(_boardState, 2, 1);
            }
            else
            {
                _ai = null;
            }

            OmokRules.CalculateForbiddenSpots(_boardState, _forbiddenSpots);
            UpdateTimeBars();
            _boardPanel.Invalidate();
        }

        private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (_pausePanel != null && _pausePanel.Visible) return;
            if (_currentPlayer == 2 && _isAiMode) return;

            Rectangle r = _boardPanel.ClientRectangle;
            r.Width -= 4; r.Height -= 4;
            int m = 20;
            Rectangle gr = new Rectangle(r.X + m, r.Y + m, r.Width - m * 2, r.Height - m * 2);
            if (gr.Width <= 0 || gr.Height <= 0) return;

            float s = gr.Width / 18f;

            float relX = e.X - gr.X;
            float relY = e.Y - gr.Y;

            int gx = (int)Math.Round(relX / s);
            int gy = (int)Math.Round(relY / s);

            if (gx < 0 || gx >= BOARD_SIZE || gy < 0 || gy >= BOARD_SIZE)
                return;

            if (_boardState[gx, gy] != 0)
                return;

            if (_currentPlayer == 1)
            {
                int geumsuType = OmokRules.CheckForForbiddenMove(_boardState, gx, gy);
                if (geumsuType != OmokRules.GEUMSU_NONE)
                {
                    OmokRules.CalculateForbiddenSpots(_boardState, _forbiddenSpots);
                    _boardPanel.Invalidate();
                    return;
                }
            }

            _boardState[gx, gy] = _currentPlayer;
            _lastMoveX = gx;
            _lastMoveY = gy;
            _boardPanel.LastMoveX = gx;
            _boardPanel.LastMoveY = gy;

            _boardPanel.Invalidate();
            _boardPanel.Update();

            if (OmokRules.CheckForWin(gx, gy, _currentPlayer, _boardState))
            {
                StopTurnTimer();

                string winnerName = (_currentPlayer == 1)
                    ? (_currentUser?.Nickname ?? "Player 1")
                    : (_isAiMode ? "AI" : "Player 2");

                MessageBox.Show($"{winnerName} 승리!", "게임 종료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetGameState();
                StartTurnTimer();
                _boardPanel.Invalidate();
                return;
            }

            _currentPlayer = (_currentPlayer == 1) ? 2 : 1;

            if (_currentPlayer == 1)
            {
                OmokRules.CalculateForbiddenSpots(_boardState, _forbiddenSpots);
            }
            else
            {
                Array.Clear(_forbiddenSpots, 0, _forbiddenSpots.Length);
            }

            _boardPanel.Invalidate();

            if (_isAiMode && _currentPlayer == 2)
            {
                StopTurnTimer();
                AITurn();
            }
            else
            {
                StartTurnTimer();
            }
        }

        private void AITurn()
        {
            if (_ai == null)
                _ai = new OmokAI(_boardState, 2, 1);

            var move = _ai.GetNextMove(true);
            int x = move.X;
            int y = move.Y;

            if (x < 0 || y < 0 || x >= BOARD_SIZE || y >= BOARD_SIZE)
            {
                _currentPlayer = 1;
                StartTurnTimer();
                return;
            }

            _boardState[x, y] = 2;
            _lastMoveX = x;
            _lastMoveY = y;
            _boardPanel.LastMoveX = x;
            _boardPanel.LastMoveY = y;

            _boardPanel.Invalidate();
            _boardPanel.Update();

            if (OmokRules.CheckForWin(x, y, 2, _boardState))
            {
                StopTurnTimer();
                MessageBox.Show("AI 승리!", "게임 종료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                ResetGameState();
                StartTurnTimer();
                _boardPanel.Invalidate();
                return;
            }

            _currentPlayer = 1;

            OmokRules.CalculateForbiddenSpots(_boardState, _forbiddenSpots);
            _boardPanel.Invalidate();
            StartTurnTimer();
        }


        // =================================================================
        // PAUSE 메뉴
        // =================================================================
        private void InitPausePanel()
        {
            _pausePanel = new PausePanel { Dock = DockStyle.Fill, Visible = false };
            this.Controls.Add(_pausePanel);

            var menuBox = new RoundedPanel { Size = new Size(300, 320), BackColor = Color.White };
            _pausePanel.Controls.Add(menuBox);

            _lblPauseTitle = new Label { Text = "PAUSE", Dock = DockStyle.Top, Height = 70, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 20, FontStyle.Bold), BackColor = Color.Transparent };

            _btnPauseResume = new SimpleRoundedButton { Text = "계속하기", Size = new Size(240, 55), Location = new Point(30, 80) };
            _btnPauseRetry = new SimpleRoundedButton { Text = "처음부터", Size = new Size(240, 55), Location = new Point(30, 150) };
            _btnPauseQuit = new SimpleRoundedButton { Text = "나가기", Size = new Size(240, 55), Location = new Point(30, 220) };

            _btnPauseResume.Click += (s, e) =>
            {
                _pausePanel.Visible = false;
                StartTurnTimer();
            };

            _btnPauseRetry.Click += (s, e) =>
            {
                _pausePanel.Visible = false;
                ResetGameState();
                StartTurnTimer();
            };

            _btnPauseQuit.Click += (s, e) =>
            {
                _pausePanel.Visible = false;
                StopTurnTimer();
                _pnlGame.Visible = false;
                _pnlIntro.Visible = true;
                _pnlIntro.BringToFront();

                _introSeq = IntroSeq.ShowButtons;
                _introLoadingLbl.Visible = false;
                _introProgressBar.Visible = false;
                _btnLocal.Visible = true;
                _btnAI.Visible = true;
                _btnExit.Visible = true;
                _menuStrip.Visible = true;
            };

            menuBox.Controls.Add(_btnPauseQuit);
            menuBox.Controls.Add(_btnPauseRetry);
            menuBox.Controls.Add(_btnPauseResume);
            menuBox.Controls.Add(_lblPauseTitle);

            _pausePanel.Resize += (s, e) =>
            {
                menuBox.Location = new Point((_pausePanel.Width - menuBox.Width) / 2, (_pausePanel.Height - menuBox.Height) / 2);
            };
        }

        private void ShowPauseMenu()
        {
            StopTurnTimer();
            _pausePanel.Visible = true;
            _pausePanel.BringToFront();
        }

        private void ShowScreen(Panel target)
        {
            _pnlIntro.Visible = target == _pnlIntro;
            _pnlLogin.Visible = target == _pnlLogin;
            _pnlGameLoading.Visible = target == _pnlGameLoading;
            _pnlGame.Visible = target == _pnlGame;
            if (target.Visible) target.BringToFront();
        }

        private Image LoadImageSafe(string path) { try { return Image.FromFile(Path.Combine(Application.StartupPath, path)); } catch { return null; } }
        private void ShowMessage(string msg, string title = "알림") { MessageBox.Show(msg, title); }

        public static GraphicsPath GetRoundedPath(Rectangle r, int d)
        {
            GraphicsPath p = new GraphicsPath(); int dia = d * 2;
            p.AddArc(r.X, r.Y, dia, dia, 180, 90); p.AddArc(r.Right - dia, r.Y, dia, dia, 270, 90);
            p.AddArc(r.Right - dia, r.Bottom - dia, dia, dia, 0, 90); p.AddArc(r.X, r.Bottom - dia, dia, dia, 90, 90); p.CloseFigure(); return p;
        }
    }

    // =================================================================
    // [커스텀 컨트롤]
    // =================================================================
    public class ModernTextBox : Panel
    {
        private TextBox _tb;
        private Color _borderColor = Color.FromArgb(218, 220, 224);
        private string _placeholderText = "";
        private bool _isPassword = false;
        private bool _isPlaceholderActive = true;

        public override string Text { get => _isPlaceholderActive ? "" : _tb.Text; }

        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value;
                if (_isPlaceholderActive) SetPlaceholder();
            }
        }

        public bool IsPassword
        {
            set { _isPassword = value; if (_isPlaceholderActive) _tb.PasswordChar = '\0'; }
        }

        public void Clear()
        {
            _tb.Text = "";
            SetPlaceholder();
        }

        public ModernTextBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Padding = new Padding(10, 12, 10, 10);

            _tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Top,
                Font = new Font("맑은 고딕", 10),
                ForeColor = Color.Gray
            };

            _tb.Enter += RemovePlaceholder;
            _tb.Leave += SetPlaceholder;

            this.Controls.Add(_tb);
        }

        private void RemovePlaceholder(object sender, EventArgs e)
        {
            if (_isPlaceholderActive)
            {
                _isPlaceholderActive = false;
                _tb.Text = "";
                _tb.ForeColor = Color.Black;
                if (_isPassword) _tb.PasswordChar = '●';
            }
        }

        private void SetPlaceholder(object sender = null, EventArgs e = null)
        {
            if (string.IsNullOrWhiteSpace(_tb.Text))
            {
                _isPlaceholderActive = true;
                _tb.Text = _placeholderText;
                _tb.ForeColor = Color.Silver;
                _tb.PasswordChar = '\0';
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, _borderColor, ButtonBorderStyle.Solid);
        }
    }

    public class RoundedActionButton : Control
    {
        public Color BaseColor { get; set; } = Color.Blue;
        public Color HoverColor { get; set; } = Color.DarkBlue;
        private bool _isHover = false;
        public RoundedActionButton() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true); Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { _isHover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; Invalidate(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = UnifiedMainForm.GetRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 5))
            using (SolidBrush b = new SolidBrush(_isHover ? HoverColor : BaseColor))
            {
                e.Graphics.FillPath(b, p);
                TextRenderer.DrawText(e.Graphics, Text, new Font("맑은 고딕", 12, FontStyle.Bold), ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class DiagonalSlideButton : Control
    {
        public string Title { get; set; } = "Button";
        private bool _isHover = false; private Timer _tmr; private float _prog = 0f;

        public DiagonalSlideButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent; Cursor = Cursors.Hand;
            _tmr = new Timer { Interval = 15 };
            _tmr.Tick += (s, e) =>
            {
                float t = _isHover ? 1f : 0f;
                if (Math.Abs(_prog - t) < 0.05f) { _prog = t; _tmr.Stop(); } else { _prog += (t - _prog) * 0.2f; }
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { _isHover = true; _tmr.Start(); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; _tmr.Start(); }
        protected override void OnMouseUp(MouseEventArgs e) { if (ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle r = ClientRectangle;
            r.Width -= 4; r.Height -= 4;

            Rectangle shadowRect = r; shadowRect.Offset(3, 3);
            using (GraphicsPath sp = UnifiedMainForm.GetRoundedPath(shadowRect, 15))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                g.FillPath(sb, sp);

            using (GraphicsPath p = UnifiedMainForm.GetRoundedPath(r, 15))
            {
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                    g.FillPath(whiteBrush, p);

                if (_prog > 0.01f)
                {
                    g.SetClip(p);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(220, 220, 225)))
                    {
                        float w = r.Width * 1.5f, sx = -w + (w + r.Width) * _prog;
                        g.FillPolygon(b, new PointF[]{
                            new PointF(sx, 0), new PointF(sx+w, 0),
                            new PointF(sx+w-60, r.Height), new PointF(sx-60, r.Height)
                        });
                    }
                    g.ResetClip();
                }

                using (Pen pn = new Pen(Color.LightGray, 1.5f)) g.DrawPath(pn, p);

                using (Font f = new Font("맑은 고딕", 14, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (Brush b = new SolidBrush(Color.FromArgb(50, 50, 60)))
                    g.DrawString(Title, f, b, r, sf);
            }
        }
    }

    public class PausePanel : Panel
    {
        public PausePanel() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true); BackColor = Color.FromArgb(150, 0, 0, 0); }
        protected override void OnPaint(PaintEventArgs e) { using (SolidBrush b = new SolidBrush(BackColor)) e.Graphics.FillRectangle(b, ClientRectangle); }
    }

    public class GhostButton : Control
    {
        private bool _isHover = false; private bool _isDown = false;
        public GhostButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; Cursor = Cursors.Hand; Size = new Size(100, 36); }
        protected override void OnMouseEnter(EventArgs e) { _isHover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; _isDown = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _isDown = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { _isDown = false; Invalidate(); if (ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = ClientRectangle; r.Width -= 1; r.Height -= 1;
            using (GraphicsPath p = UnifiedMainForm.GetRoundedPath(r, 18))
            {
                Color bg = Color.Transparent; if (_isDown) bg = Color.FromArgb(220, 220, 220); else if (_isHover) bg = Color.FromArgb(240, 240, 245);
                using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, p);
                using (Pen pn = new Pen(_isHover ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180), 1.2f)) g.DrawPath(pn, p);
                Color tc = _isHover ? Color.Black : Color.FromArgb(80, 80, 80); if (_isDown) r.Offset(1, 1);
                TextRenderer.DrawText(g, Text, new Font("맑은 고딕", 10, FontStyle.Bold), r, tc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class SimpleRoundedButton : Control
    {
        private bool _isHover = false;
        public SimpleRoundedButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { _isHover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { if (ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = ClientRectangle; r.Width -= 1; r.Height -= 1;
            using (GraphicsPath p = UnifiedMainForm.GetRoundedPath(r, 15))
            {
                using (SolidBrush b = new SolidBrush(_isHover ? Color.FromArgb(235, 235, 240) : Color.White)) g.FillPath(b, p);
                using (Pen pn = new Pen(Color.Silver, 1)) g.DrawPath(pn, p);
                TextRenderer.DrawText(g, Text, new Font("맑은 고딕", 11, FontStyle.Bold), r, Color.FromArgb(60, 60, 60), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    public class ZenLoadingBar : Control
    {
        public int Value { get; set; } = 0; public int Maximum { get; set; } = 100; public Color BarColor { get; set; } = Color.Blue; public Color TrackColor { get; set; } = Color.LightGray;
        public ZenLoadingBar() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = ClientRectangle;
            using (SolidBrush b = new SolidBrush(TrackColor)) e.Graphics.FillRectangle(b, r);
            if (Value > 0 && Maximum > 0)
            {
                float pct = (float)Value / Maximum; int w = (int)(r.Width * pct);
                if (w > 0) using (SolidBrush b = new SolidBrush(BarColor)) e.Graphics.FillRectangle(b, 0, 0, w, r.Height);
            }
        }
    }

    public class CustomMenuStrip : MenuStrip
    {
        public CustomMenuStrip() { Renderer = new ToolStripProfessionalRenderer(new CustomColorTable()); BackColor = Color.FromArgb(245, 247, 250); Font = new Font("맑은 고딕", 9); }
        class CustomColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => Color.FromArgb(245, 247, 250);
            public override Color MenuItemSelected => Color.FromArgb(230, 230, 235);
            public override Color MenuItemBorder => Color.Transparent;
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 230, 235);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 230, 235);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(220, 220, 225);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(220, 220, 225);
        }
    }

    public class IntroPanel : Panel { public IntroPanel() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); } }

    public class RoundedPanel : Panel
    {
        public RoundedPanel() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath p = UnifiedMainForm.GetRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 20)) using (SolidBrush b = new SolidBrush(Color.White)) e.Graphics.FillPath(b, p);
        }
    }

    public class BoardPanel : Panel
    {
        public Color BoardColor { get; set; } = Color.Orange;

        public int[,] BoardState { get; set; }
        public int[,] ForbiddenSpots { get; set; }
        public int LastMoveX { get; set; } = -1;
        public int LastMoveY { get; set; } = -1;

        public BoardPanel() { SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = ClientRectangle; r.Width -= 4; r.Height -= 4;

            using (SolidBrush b = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillRectangle(b, r.X + 4, r.Y + 4, r.Width, r.Height);

            using (SolidBrush b = new SolidBrush(BoardColor))
                g.FillRectangle(b, r);

            using (Pen p = new Pen(Color.FromArgb(100, 0, 0, 0)))
                g.DrawRectangle(p, r);

            int m = 20;
            Rectangle gr = new Rectangle(r.X + m, r.Y + m, r.Width - m * 2, r.Height - m * 2);
            if (gr.Width <= 0 || gr.Height <= 0) return;
            float s = gr.Width / 18f;

            using (Pen p = new Pen(Color.FromArgb(180, 0, 0, 0)))
            {
                for (int i = 0; i < 19; i++)
                {
                    float v = gr.X + i * s;
                    g.DrawLine(p, v, gr.Y, v, gr.Bottom);
                    float h = gr.Y + i * s;
                    g.DrawLine(p, gr.X, h, gr.Right, h);
                }
            }

            using (Brush b = new SolidBrush(Color.Black))
            {
                int[] pts = { 3, 9, 15 };
                foreach (int y in pts)
                    foreach (int x in pts)
                        g.FillEllipse(b, gr.X + x * s - 3, gr.Y + y * s - 3, 6, 6);
            }

            if (BoardState == null) return;

            float stoneRadius = s * 0.45f;
            float markerRadius = stoneRadius * 0.3f;

            Brush blackStone = Brushes.Black;
            Brush whiteStone = Brushes.White;
            Brush lastMoveRedBrush = Brushes.Red;
            Brush lastMoveWhiteBrush = Brushes.White;

            Pen gridPen = new Pen(Color.Black, 1f);

            for (int x = 0; x < 19; x++)
            {
                for (int y = 0; y < 19; y++)
                {
                    float cx = gr.X + x * s;
                    float cy = gr.Y + y * s;

                    if (BoardState[x, y] == 1)
                    {
                        g.FillEllipse(blackStone, cx - stoneRadius, cy - stoneRadius, stoneRadius * 2, stoneRadius * 2);
                    }
                    else if (BoardState[x, y] == 2)
                    {
                        g.FillEllipse(whiteStone, cx - stoneRadius, cy - stoneRadius, stoneRadius * 2, stoneRadius * 2);
                        g.DrawEllipse(gridPen, cx - stoneRadius, cy - stoneRadius, stoneRadius * 2, stoneRadius * 2);
                    }

                    if (x == LastMoveX && y == LastMoveY && BoardState[x, y] != 0)
                    {
                        if (BoardState[x, y] == 1)
                        {
                            g.FillEllipse(lastMoveWhiteBrush,
                                          cx - markerRadius, cy - markerRadius,
                                          markerRadius * 2, markerRadius * 2);
                        }
                        else if (BoardState[x, y] == 2)
                        {
                            g.FillEllipse(lastMoveRedBrush,
                                          cx - markerRadius, cy - markerRadius,
                                          markerRadius * 2, markerRadius * 2);
                        }
                    }
                }
            }

            if (ForbiddenSpots != null)
            {
                using (Pen forbiddenPen = new Pen(Color.Red, 2f))
                {
                    float halfStep = s / 4f;

                    for (int x = 0; x < 19; x++)
                    {
                        for (int y = 0; y < 19; y++)
                        {
                            if (ForbiddenSpots[x, y] != 0 && BoardState[x, y] == 0)
                            {
                                float cx = gr.X + x * s;
                                float cy = gr.Y + y * s;

                                g.DrawLine(forbiddenPen, cx - halfStep, cy - halfStep, cx + halfStep, cy + halfStep);
                                g.DrawLine(forbiddenPen, cx + halfStep, cy - halfStep, cx - halfStep, cy + halfStep);
                            }
                        }
                    }
                }
            }
        }
    }

    public class RoundProgressBar : Control
    {
        private double _currentValue = 0.0;

        private double _startValue = 0.0;
        private double _targetValue = 0.0;

        private DateTime _animStartTime;

        private const int ANIM_DURATION_MS = 1000;

        private Timer _animTimer;

        public Color BarColor { get; set; } = Color.Blue;

        public int Value
        {
            get => (int)Math.Round(_targetValue);
            set
            {
                int v = value;
                if (v < 0) v = 0;
                if (v > 100) v = 100;

                if (v >= _currentValue)
                {
                    _currentValue = v;
                    _startValue = v;
                    _targetValue = v;

                    if (_animTimer != null && _animTimer.Enabled)
                        _animTimer.Stop();

                    Invalidate();
                    return;
                }

                _startValue = _currentValue;
                _targetValue = v;
                _animStartTime = DateTime.Now;

                if (_animTimer == null)
                {
                    _animTimer = new Timer();
                    _animTimer.Interval = 16;
                    _animTimer.Tick += AnimTimer_Tick;
                }

                if (!_animTimer.Enabled)
                    _animTimer.Start();
            }
        }
        public RoundProgressBar()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            BackColor = Color.Transparent;
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            double elapsed = (DateTime.Now - _animStartTime).TotalMilliseconds;
            double t = elapsed / ANIM_DURATION_MS;

            if (t >= 1.0)
            {
                _currentValue = _targetValue;
                _animTimer.Stop();
            }
            else
            {
                _currentValue = _startValue + (_targetValue - _startValue) * t;
            }

            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;

            r.Height--;

            using (SolidBrush b = new SolidBrush(Color.LightGray))
            {
                e.Graphics.FillEllipse(b, r.X, r.Y, r.Height, r.Height);
                e.Graphics.FillRectangle(b, r.X + r.Height / 2, r.Y, r.Width - r.Height, r.Height);
                e.Graphics.FillEllipse(b, r.Right - r.Height - 1, r.Y, r.Height, r.Height);
            }

            if (_currentValue <= 0.0)
                return;

            double p = _currentValue / 100.0;
            if (p < 0.0) p = 0.0;
            if (p > 1.0) p = 1.0;

            int w = (int)(r.Width * p);
            if (w < r.Height) w = r.Height;

            using (SolidBrush b = new SolidBrush(BarColor))
            {
                e.Graphics.FillEllipse(b, r.X, r.Y, r.Height, r.Height);
                e.Graphics.FillRectangle(b, r.X + r.Height / 2, r.Y, w - r.Height, r.Height);
                e.Graphics.FillEllipse(b, r.X + w - r.Height, r.Y, r.Height, r.Height);
            }
        }
    }
}