using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.IO;
using System.Windows.Forms;

namespace atyarisi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int atGenisligi, bitisuzakligi, konumbirinciat;
        int startLeft1, startLeft2, startLeft3, startLeft4;
        bool raceFinished;
        Random rastgeleathizi = new Random();
        List<string> lastWinners = new List<string>();
        List<string> raceHistory = new List<string>();
        int[] winCounts = new int[4];
        int[] luckBonus = new int[4];
        double weatherFactor = 1.0;
        bool isDarkTheme;
        StatusKind currentStatus = StatusKind.Ready;
        int currentLeaderLane;
        WeatherKind currentWeather = WeatherKind.Sunny;
        int pulseTick;

        private enum StatusKind
        {
            Ready,
            Running,
            Paused,
            Finished
        }

        private enum WeatherKind
        {
            Sunny,
            Rainy,
            Windy
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            atGenisligi = pictureBox1.Width;
            pictureBox2.Width = atGenisligi;
            pictureBox3.Width = atGenisligi;
            pictureBox4.Width = atGenisligi;
            timer1.Interval = 100;
            bitisuzakligi = label1.Left - atGenisligi;
            startLeft1 = pictureBox1.Left;
            startLeft2 = pictureBox2.Left;
            startLeft3 = pictureBox3.Left;
            startLeft4 = pictureBox4.Left;
            raceFinished = false;
            SetStatusText("Hazir", StatusKind.Ready);
            button1.Text = "Baslat";
            numericUpDown1.Value = 20;
            numericUpDown2.Value = 20;
            numericUpDown3.Value = 20;
            numericUpDown4.Value = 20;
            numericUpDownInterval.Value = 100;
            textBoxHorse1.Text = GetSettingOrDefault(Properties.Settings.Default.Horse1Name, "At 1");
            textBoxHorse2.Text = GetSettingOrDefault(Properties.Settings.Default.Horse2Name, "At 2");
            textBoxHorse3.Text = GetSettingOrDefault(Properties.Settings.Default.Horse3Name, "At 3");
            textBoxHorse4.Text = GetSettingOrDefault(Properties.Settings.Default.Horse4Name, "At 4");
            ApplyNewRaceSettings();
            UpdateWinLabels();
            isDarkTheme = Properties.Settings.Default.ThemeDark;
            currentLeaderLane = 0;
            UpdateLeaderArrows();
            pulseTick = 0;
            UpdateRaceProgress();
            ApplyTheme();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (raceFinished)
            {
                ResetRace();
            }

            if (timer1.Enabled == false)
            {
                timer1.Start();
                button1.Text = "Duraklat";
                SetStatusText("Yaris basladi", StatusKind.Running);
            }
            else
            {
                timer1.Stop();
                button1.Text = "Baslat";
                SetStatusText("Duraklatildi", StatusKind.Paused);
            }
        }
   
        private void timer1_Tick(object sender, EventArgs e)
        {
            pulseTick++;
            pictureBox1.Left += CalculateStep(0, (int)numericUpDown1.Value);
            pictureBox2.Left += CalculateStep(1, (int)numericUpDown2.Value);
            pictureBox3.Left += CalculateStep(2, (int)numericUpDown3.Value);
            pictureBox4.Left += CalculateStep(3, (int)numericUpDown4.Value);

            int birinciAtNo = atNoDondur();
            SetStatusText(birinciAtNo + " numarali at onde", StatusKind.Running);
            labelLeadValue.Text = GetHorseName(birinciAtNo);
            UpdateLeaderLane(birinciAtNo);
            UpdateLeaderArrowPulse();
            UpdateRaceProgress();
            konumbirinciat = EnIleriKonum();
            if (konumbirinciat > bitisuzakligi)
            {
                timer1.Enabled = false;
                raceFinished = true;
                button1.Text = "Baslat";
                MessageBox.Show(birinciAtNo + " numarali at yarisi kazandi");
                SystemSounds.Asterisk.Play();
                AddWinner(birinciAtNo);
                AddHistoryEntry(birinciAtNo);
                winCounts[birinciAtNo - 1]++;
                UpdateWinLabels();
                SetStatusText("Yaris bitti", StatusKind.Finished);
                progressBarRace.Value = 100;
            }
        }
        private int atNoDondur()
        {
            int maxLeft = EnIleriKonum();
            if (pictureBox1.Left == maxLeft) return 1;
            if (pictureBox2.Left == maxLeft) return 2;
            if (pictureBox3.Left == maxLeft) return 3;
            if (pictureBox4.Left == maxLeft) return 4;
            return 1;

        }

        private int EnIleriKonum()
        {
            int maxLeft = pictureBox1.Left;
            if (pictureBox2.Left > maxLeft) maxLeft = pictureBox2.Left;
            if (pictureBox3.Left > maxLeft) maxLeft = pictureBox3.Left;
            if (pictureBox4.Left > maxLeft) maxLeft = pictureBox4.Left;
            return maxLeft;
        }

        private void ResetRace()
        {
            pictureBox1.Left = startLeft1;
            pictureBox2.Left = startLeft2;
            pictureBox3.Left = startLeft3;
            pictureBox4.Left = startLeft4;
            raceFinished = false;
            SetStatusText("Hazir", StatusKind.Ready);
            currentLeaderLane = 0;
            labelLeadValue.Text = "At -";
            UpdateLeaderArrows();
            pulseTick = 0;
            progressBarRace.Value = 0;
            ApplyNewRaceSettings();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            button1.Text = "Baslat";
            ResetRace();
        }

        private void numericUpDownInterval_ValueChanged(object sender, EventArgs e)
        {
            timer1.Interval = (int)numericUpDownInterval.Value;
        }

        private void AddWinner(int horseNo)
        {
            lastWinners.Add(GetHorseName(horseNo));
            if (lastWinners.Count > 5)
            {
                lastWinners.RemoveAt(0);
            }
            listBoxWinners.Items.Clear();
            foreach (var winner in lastWinners)
            {
                listBoxWinners.Items.Add(winner);
            }
        }

        private int CalculateStep(int horseIndex, int maxStep)
        {
            int baseStep = rastgeleathizi.Next(1, maxStep + 1);
            int stepWithLuck = baseStep + luckBonus[horseIndex];
            int adjusted = (int)Math.Round(stepWithLuck * weatherFactor);
            if (adjusted < 1) adjusted = 1;
            return adjusted;
        }

        private void ApplyNewRaceSettings()
        {
            for (int i = 0; i < luckBonus.Length; i++)
            {
                luckBonus[i] = rastgeleathizi.Next(-2, 3);
            }
            int weatherPick = rastgeleathizi.Next(0, 3);
            if (weatherPick == 0)
            {
                weatherFactor = 1.1;
                labelWeatherValue.Text = "Gunesli (+10%)";
                currentWeather = WeatherKind.Sunny;
            }
            else if (weatherPick == 1)
            {
                weatherFactor = 0.9;
                labelWeatherValue.Text = "Yagmurlu (-10%)";
                currentWeather = WeatherKind.Rainy;
            }
            else
            {
                weatherFactor = 1.0;
                labelWeatherValue.Text = "Ruzgarli (0%)";
                currentWeather = WeatherKind.Windy;
            }
            UpdateWeatherBadge();
        }

        private void UpdateWinLabels()
        {
            labelWin1.Text = GetHorseName(1) + ": " + winCounts[0];
            labelWin2.Text = GetHorseName(2) + ": " + winCounts[1];
            labelWin3.Text = GetHorseName(3) + ": " + winCounts[2];
            labelWin4.Text = GetHorseName(4) + ": " + winCounts[3];
            UpdateWinBars();
        }

        private void SetStatusText(string text, StatusKind kind)
        {
            label3.Text = text;
            labelStatusBadge.Text = GetStatusBadgeText(kind, text);
            currentStatus = kind;
            UpdateStatusBadgeColor();
        }

        private void UpdateWinBars()
        {
            int maxWin = winCounts.Max();
            if (maxWin < 1) maxWin = 1;

            panelWinBar1.Width = 30 + (int)(150.0 * winCounts[0] / maxWin);
            panelWinBar2.Width = 30 + (int)(150.0 * winCounts[1] / maxWin);
            panelWinBar3.Width = 30 + (int)(150.0 * winCounts[2] / maxWin);
            panelWinBar4.Width = 30 + (int)(150.0 * winCounts[3] / maxWin);

            labelWinBar1.Text = GetHorseName(1) + " (" + winCounts[0] + ")";
            labelWinBar2.Text = GetHorseName(2) + " (" + winCounts[1] + ")";
            labelWinBar3.Text = GetHorseName(3) + " (" + winCounts[2] + ")";
            labelWinBar4.Text = GetHorseName(4) + " (" + winCounts[3] + ")";
        }

        private void AddHistoryEntry(int horseNo)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string entry = time + " - " + GetHorseName(horseNo);
            raceHistory.Add(entry);
            if (raceHistory.Count > 10)
            {
                raceHistory.RemoveAt(0);
            }
            listBoxHistory.Items.Clear();
            foreach (var item in raceHistory)
            {
                listBoxHistory.Items.Add(item);
            }
        }

        private string GetHorseName(int horseNo)
        {
            if (horseNo == 1) return GetTextOrDefault(textBoxHorse1, "At 1");
            if (horseNo == 2) return GetTextOrDefault(textBoxHorse2, "At 2");
            if (horseNo == 3) return GetTextOrDefault(textBoxHorse3, "At 3");
            if (horseNo == 4) return GetTextOrDefault(textBoxHorse4, "At 4");
            return "At -";
        }

        private string GetTextOrDefault(TextBox textBox, string fallback)
        {
            string value = textBox.Text == null ? "" : textBox.Text.Trim();
            return value.Length > 0 ? value : fallback;
        }

        private string GetSettingOrDefault(string value, string fallback)
        {
            if (value == null) return fallback;
            string trimmed = value.Trim();
            return trimmed.Length > 0 ? trimmed : fallback;
        }

        private void textBoxHorse1_TextChanged(object sender, EventArgs e)
        {
            UpdateWinLabels();
        }

        private void textBoxHorse2_TextChanged(object sender, EventArgs e)
        {
            UpdateWinLabels();
        }

        private void textBoxHorse3_TextChanged(object sender, EventArgs e)
        {
            UpdateWinLabels();
        }

        private void textBoxHorse4_TextChanged(object sender, EventArgs e)
        {
            UpdateWinLabels();
        }

        private void buttonRandomNames_Click(object sender, EventArgs e)
        {
            var names = new List<string>
            {
                "Sahin", "Firtina", "Yildirim", "Ruzgar", "Kasim", "Cesur",
                "Golge", "Poyraz", "Savasci", "Kara", "Akis", "Simsek"
            };

            textBoxHorse1.Text = PickRandomName(names);
            textBoxHorse2.Text = PickRandomName(names);
            textBoxHorse3.Text = PickRandomName(names);
            textBoxHorse4.Text = PickRandomName(names);
            UpdateWinLabels();
        }

        private string PickRandomName(List<string> pool)
        {
            if (pool.Count == 0) return "At";
            int index = rastgeleathizi.Next(0, pool.Count);
            string picked = pool[index];
            pool.RemoveAt(index);
            return picked;
        }

        private void buttonExportReport_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text File (*.txt)|*.txt";
                dialog.FileName = "atyarisi_rapor_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                dialog.Title = "Yaris Raporu Kaydet";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, BuildReportText());
                    MessageBox.Show("Rapor kaydedildi: " + dialog.FileName);
                }
            }
        }

        private string BuildReportText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("At Yarisi Raporu");
            sb.AppendLine("Tarih: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();
            sb.AppendLine("Atlar:");
            sb.AppendLine("1) " + GetHorseName(1));
            sb.AppendLine("2) " + GetHorseName(2));
            sb.AppendLine("3) " + GetHorseName(3));
            sb.AppendLine("4) " + GetHorseName(4));
            sb.AppendLine();
            sb.AppendLine("Kazanan Sayilari:");
            sb.AppendLine(GetHorseName(1) + ": " + winCounts[0]);
            sb.AppendLine(GetHorseName(2) + ": " + winCounts[1]);
            sb.AppendLine(GetHorseName(3) + ": " + winCounts[2]);
            sb.AppendLine(GetHorseName(4) + ": " + winCounts[3]);
            sb.AppendLine();
            sb.AppendLine("Son Kazananlar:");
            foreach (var item in lastWinners)
            {
                sb.AppendLine("- " + item);
            }
            sb.AppendLine();
            sb.AppendLine("Yaris Gecmisi:");
            foreach (var item in raceHistory)
            {
                sb.AppendLine("- " + item);
            }
            return sb.ToString();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Horse1Name = GetTextOrDefault(textBoxHorse1, "At 1");
            Properties.Settings.Default.Horse2Name = GetTextOrDefault(textBoxHorse2, "At 2");
            Properties.Settings.Default.Horse3Name = GetTextOrDefault(textBoxHorse3, "At 3");
            Properties.Settings.Default.Horse4Name = GetTextOrDefault(textBoxHorse4, "At 4");
            Properties.Settings.Default.ThemeDark = isDarkTheme;
            Properties.Settings.Default.Save();
        }

        private void UpdateRaceProgress()
        {
            if (bitisuzakligi <= 0)
            {
                progressBarRace.Value = 0;
                return;
            }

            int maxLeft = EnIleriKonum();
            int percent = (int)Math.Round(100.0 * maxLeft / bitisuzakligi);
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            progressBarRace.Value = percent;
        }

        private void buttonTheme_Click(object sender, EventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color bg = isDarkTheme ? Color.FromArgb(28, 30, 34) : Color.FromArgb(248, 250, 252);
            Color panelText = isDarkTheme ? Color.FromArgb(235, 235, 235) : Color.FromArgb(55, 55, 55);
            Color accent = Color.FromArgb(61, 106, 255);
            Color soft = isDarkTheme ? Color.FromArgb(45, 48, 54) : Color.FromArgb(245, 245, 245);
            Color softText = isDarkTheme ? Color.FromArgb(220, 220, 220) : Color.FromArgb(55, 55, 55);

            BackColor = bg;
            label2.ForeColor = accent;
            label3.ForeColor = panelText;
            label4.ForeColor = panelText;
            label5.ForeColor = panelText;
            label6.ForeColor = panelText;
            label7.ForeColor = panelText;
            labelWeatherTitle.ForeColor = panelText;
            labelWin1.ForeColor = panelText;
            labelWin2.ForeColor = panelText;
            labelWin3.ForeColor = panelText;
            labelWin4.ForeColor = panelText;
            labelSpeedTitle.ForeColor = panelText;
            labelSpeedHint.ForeColor = panelText;
            labelSpeed1.ForeColor = panelText;
            labelSpeed2.ForeColor = panelText;
            labelSpeed3.ForeColor = panelText;
            labelSpeed4.ForeColor = panelText;
            labelNameTitle.ForeColor = panelText;
            labelName1.ForeColor = panelText;
            labelName2.ForeColor = panelText;
            labelName3.ForeColor = panelText;
            labelName4.ForeColor = panelText;
            labelHistoryTitle.ForeColor = panelText;
            labelLeadTitle.ForeColor = panelText;
            labelLeadValue.ForeColor = panelText;
            labelWinBarTitle.ForeColor = panelText;
            labelWinBar1.ForeColor = panelText;
            labelWinBar2.ForeColor = panelText;
            labelWinBar3.ForeColor = panelText;
            labelWinBar4.ForeColor = panelText;
            labelProgressTitle.ForeColor = panelText;
            labelLeadArrow1.ForeColor = accent;
            labelLeadArrow2.ForeColor = accent;
            labelLeadArrow3.ForeColor = accent;
            labelLeadArrow4.ForeColor = accent;

            panelWinBar1.BackColor = Color.FromArgb(61, 106, 255);
            panelWinBar2.BackColor = Color.FromArgb(76, 175, 80);
            panelWinBar3.BackColor = Color.FromArgb(255, 152, 0);
            panelWinBar4.BackColor = Color.FromArgb(156, 39, 176);
            progressBarRace.BackColor = isDarkTheme ? Color.FromArgb(40, 44, 52) : Color.FromArgb(230, 235, 241);

            Color inputBg = isDarkTheme ? Color.FromArgb(45, 48, 54) : Color.White;
            Color inputText = isDarkTheme ? Color.FromArgb(230, 230, 230) : Color.FromArgb(30, 30, 30);
            textBoxHorse1.BackColor = inputBg;
            textBoxHorse2.BackColor = inputBg;
            textBoxHorse3.BackColor = inputBg;
            textBoxHorse4.BackColor = inputBg;
            textBoxHorse1.ForeColor = inputText;
            textBoxHorse2.ForeColor = inputText;
            textBoxHorse3.ForeColor = inputText;
            textBoxHorse4.ForeColor = inputText;

            buttonRandomNames.BackColor = soft;
            buttonRandomNames.ForeColor = softText;
            buttonExportReport.BackColor = soft;
            buttonExportReport.ForeColor = softText;

            button1.BackColor = accent;
            button1.ForeColor = Color.White;
            button2.BackColor = soft;
            button2.ForeColor = softText;
            buttonTheme.BackColor = soft;
            buttonTheme.ForeColor = softText;

            listBoxWinners.BackColor = isDarkTheme ? Color.FromArgb(35, 38, 44) : Color.White;
            listBoxWinners.ForeColor = panelText;
            listBoxWinners.BorderStyle = BorderStyle.FixedSingle;
            listBoxHistory.BackColor = isDarkTheme ? Color.FromArgb(35, 38, 44) : Color.White;
            listBoxHistory.ForeColor = panelText;
            listBoxHistory.BorderStyle = BorderStyle.FixedSingle;
            panelRight.BackColor = isDarkTheme ? Color.FromArgb(33, 36, 41) : Color.White;
            panelLeft.BackColor = isDarkTheme ? Color.FromArgb(33, 36, 41) : Color.White;
            UpdateLaneColors();
            UpdateStatusBadgeColor();
            UpdateWeatherBadge();
            UpdateLeaderArrowPulse();
        }

        private void UpdateLeaderLane(int leaderLane)
        {
            currentLeaderLane = leaderLane;
            UpdateLaneColors();
            UpdateLeaderArrows();
        }

        private void UpdateLaneColors()
        {
            panelLane1.BackColor = GetLaneBaseColor(1);
            panelLane2.BackColor = GetLaneBaseColor(2);
            panelLane3.BackColor = GetLaneBaseColor(3);
            panelLane4.BackColor = GetLaneBaseColor(4);

            Color highlight = GetLaneHighlightColor();
            if (currentLeaderLane == 1) panelLane1.BackColor = highlight;
            else if (currentLeaderLane == 2) panelLane2.BackColor = highlight;
            else if (currentLeaderLane == 3) panelLane3.BackColor = highlight;
            else if (currentLeaderLane == 4) panelLane4.BackColor = highlight;
        }

        private void UpdateWeatherBadge()
        {
            if (currentWeather == WeatherKind.Sunny)
            {
                labelWeatherValue.BackColor = isDarkTheme ? Color.FromArgb(76, 175, 80) : Color.FromArgb(210, 244, 224);
                labelWeatherValue.ForeColor = isDarkTheme ? Color.White : Color.FromArgb(28, 122, 62);
            }
            else if (currentWeather == WeatherKind.Rainy)
            {
                labelWeatherValue.BackColor = isDarkTheme ? Color.FromArgb(33, 150, 243) : Color.FromArgb(214, 233, 255);
                labelWeatherValue.ForeColor = isDarkTheme ? Color.White : Color.FromArgb(23, 92, 153);
            }
            else
            {
                labelWeatherValue.BackColor = isDarkTheme ? Color.FromArgb(96, 125, 139) : Color.FromArgb(232, 236, 238);
                labelWeatherValue.ForeColor = isDarkTheme ? Color.White : Color.FromArgb(66, 66, 66);
            }
        }

        private void UpdateLeaderArrows()
        {
            labelLeadArrow1.Visible = currentLeaderLane == 1;
            labelLeadArrow2.Visible = currentLeaderLane == 2;
            labelLeadArrow3.Visible = currentLeaderLane == 3;
            labelLeadArrow4.Visible = currentLeaderLane == 4;
        }

        private void UpdateLeaderArrowPulse()
        {
            if (currentStatus != StatusKind.Running)
            {
                SetLeaderArrowColor(Color.FromArgb(61, 106, 255));
                return;
            }

            bool pulseOn = (pulseTick / 3) % 2 == 0;
            Color baseColor = Color.FromArgb(61, 106, 255);
            Color pulseColor = isDarkTheme ? Color.FromArgb(120, 170, 255) : Color.FromArgb(120, 150, 255);
            SetLeaderArrowColor(pulseOn ? pulseColor : baseColor);
        }

        private void SetLeaderArrowColor(Color color)
        {
            labelLeadArrow1.ForeColor = color;
            labelLeadArrow2.ForeColor = color;
            labelLeadArrow3.ForeColor = color;
            labelLeadArrow4.ForeColor = color;
        }

        private Color GetLaneBaseColor(int laneIndex)
        {
            bool even = laneIndex % 2 == 0;
            if (isDarkTheme)
            {
                return even ? Color.FromArgb(33, 36, 41) : Color.FromArgb(36, 39, 45);
            }

            return even ? Color.FromArgb(233, 236, 242) : Color.FromArgb(240, 242, 246);
        }

        private Color GetLaneHighlightColor()
        {
            return isDarkTheme ? Color.FromArgb(50, 84, 160) : Color.FromArgb(214, 228, 255);
        }

        private string GetStatusBadgeText(StatusKind kind, string text)
        {
            string prefix;
            if (kind == StatusKind.Ready) prefix = "● READY";
            else if (kind == StatusKind.Running) prefix = "▶ RUN";
            else if (kind == StatusKind.Paused) prefix = "⏸ PAUSE";
            else prefix = "✔ DONE";

            return prefix + ": " + text;
        }

        private void UpdateStatusBadgeColor()
        {
            Color badgeColor;
            if (currentStatus == StatusKind.Ready)
            {
                badgeColor = isDarkTheme ? Color.FromArgb(90, 90, 90) : Color.FromArgb(120, 120, 120);
            }
            else if (currentStatus == StatusKind.Running)
            {
                badgeColor = Color.FromArgb(46, 166, 107);
            }
            else if (currentStatus == StatusKind.Paused)
            {
                badgeColor = Color.FromArgb(242, 153, 74);
            }
            else
            {
                badgeColor = Color.FromArgb(142, 68, 173);
            }

            labelStatusBadge.BackColor = badgeColor;
            labelStatusBadge.ForeColor = Color.White;
        }

        private void panelLane1_Paint(object sender, PaintEventArgs e)
        {
            DrawLaneShadow(e.Graphics);
        }

        private void panelLane2_Paint(object sender, PaintEventArgs e)
        {
            DrawLaneShadow(e.Graphics);
        }

        private void panelLane3_Paint(object sender, PaintEventArgs e)
        {
            DrawLaneShadow(e.Graphics);
        }

        private void panelLane4_Paint(object sender, PaintEventArgs e)
        {
            DrawLaneShadow(e.Graphics);
        }

        private void DrawLaneShadow(Graphics graphics)
        {
            using (var pen = new Pen(isDarkTheme ? Color.FromArgb(18, 20, 23) : Color.FromArgb(220, 223, 229)))
            {
                graphics.DrawLine(pen, 0, 1, panelLane1.Width, 1);
                graphics.DrawLine(pen, 0, panelLane1.Height - 2, panelLane1.Width, panelLane1.Height - 2);
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {
           
        }
        private void label3_Click(object sender, EventArgs e)
        {
           
        }

    }
}
