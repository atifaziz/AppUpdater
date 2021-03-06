﻿namespace TestApp
{
    #region Imports

    using System;
    using System.Threading;
    using System.Windows.Forms;
    using AppUpdater;
    using AppUpdater.Logging;

    #endregion

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Logger.LoggerProvider = (type) => new FormLog(DoLog, type);
        }

        void btnStart_Click(object sender, EventArgs e)
        {
            var setup = new AutoUpdater.Setup(UpdateManager.Default)
            {
                CheckInterval = TimeSpan.FromSeconds(10),
            };
            setup.Updated += autoUpdater_Updated;
            AutoUpdater.Start(setup);


            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnUpdate.Enabled = false;
        }

        void autoUpdater_Updated(object sender, EventArgs e)
        {
            MessageBox.Show("Updated");
        }

        void btnStop_Click(object sender, EventArgs e)
        {
            AutoUpdater.Stop();

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnUpdate.Enabled = true;
        }

        void btnUpdate_Click(object sender, EventArgs e)
        {
            var manager = UpdateManager.Default;

            manager.CheckForUpdateAsync(CancellationToken.None)
                   .ContinueEventHandlerWith(this, info =>
            {
                if (info.HasUpdate)
                {
                    manager.DoUpdateAsync(info, CancellationToken.None)
                           .ContinueEventHandlerWith(this, () => MessageBox.Show("Updated"));
                }
                else
                {
                    MessageBox.Show("No update available.");
                }
            });
        }

        void DoLog(string logLevel, Type type, string message, object[] values)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action<string, Type, string, object[]>(DoLog), logLevel, type, message, values);
            }
            else
            {
                var msg = String.Format(message, values);
                msg = String.Format("{0:HH:mm:ss} - [{1}] - {2} - {3}\r\n", DateTime.Now, logLevel, type.FullName, msg);
                txtLog.AppendText(msg);
            }
        }

        public class FormLog : ILog
        {
            Action<string, Type, string, object[]> doLog;
            Type type;

            public FormLog(Action<string, Type, string, object[]> doLog, Type type)
            {
                this.doLog = doLog;
                this.type = type;
            }

            public void Info(string message, params object[] values)
            {
                doLog("Info", type, message, values);
            }

            public void Warn(string message, params object[] values)
            {
                doLog("Warn", type, message, values);
            }

            public void Error(string message, params object[] values)
            {
                doLog("Error", type, message, values);
            }

            public void Debug(string message, params object[] values)
            {
                doLog("Debug", type, message, values);
            }
        }
    }
}
