﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos
{
    public partial class SubmitSiteReport : Form
    {
        public string SiteName { get; set; }

        public SubmitSiteReport()
        {
            InitializeComponent();

            Text = "Submit a report for site: " + SiteName;
            cbType.Items.AddRange(Enum.GetNames(typeof(OnlineVideosWebservice.ReportType)));
            cbType.SelectedItem = OnlineVideosWebservice.ReportType.Broken.ToString();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (tbxMessage.Text.Trim().Length < 10)
            {
                MessageBox.Show("You must enter a short text!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                OnlineVideosWebservice.ReportType type = (OnlineVideosWebservice.ReportType)Enum.Parse(typeof(OnlineVideosWebservice.ReportType), cbType.SelectedItem.ToString());
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                string msg = "";
                bool success = ws.SubmitReport(SiteName, tbxMessage.Text, type, out msg);
                MessageBox.Show(msg, success ? "Success" : "Error", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
