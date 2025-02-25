﻿using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using System.IO;
using TouchSocket.Sockets;
using TouchSocket.Core;
using StringExtension = SocketHelper.helper.StringExtension;
using SocketHelper.Models;
using SocketHelper.helper;
using Masuit.Tools;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;

namespace SocketHelper
{
    public partial class MainForm : DevExpress.XtraEditors.XtraForm
    {

        // 是否打开关闭标志
        private bool _isOpen = false;

        // 生成日志文件的路径
        private string logFilePath = "";
        private Thread CycleThread;

        // 是否循坏发送
        private bool _isCycle = false;
        private TcpClient tcpClient = new TcpClient();
        private TcpService tcpServer = new TcpService();

        public MainForm()
        {
            InitializeComponent();
            InitData();
            InitThread();
        }

        private void InitThread()
        {
            // 保存日志
            var saveLogThread = new Thread(() =>
            {
                while (true)
                {
                    if (recSaveFile.Checked && !string.IsNullOrEmpty(logFilePath))
                    {
                        File.WriteAllText(logFilePath, dataLog.Text);
                    }

                    Thread.Sleep(10000);
                }
            });

            saveLogThread.Start();
        }
        private void InitData()
        {
            // 填充默认值
            agreeType.Properties.Items.AddRange(Faker.GetTypes());
            IPAddress.Properties.Items.AddRange(Faker.GetIps());
            // 默认选中
            agreeType.SelectedIndex = 0;
            IPAddress.SelectedIndex = 0;
            dataLog.AppendText(Environment.NewLine);
            sendInfo.AppendText(Environment.NewLine);
        }

        /// <summary>
        /// 清除接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearRec_Click(object sender, EventArgs e)
        {
            dataLog.Text = "";
        }

        /// <summary>
        ///  是否可以选中
        /// </summary>
        /// <param name="enbale"></param>
        private void ChangeComboxEnable(bool enbale)
        {
            agreeType.Enabled = enbale;
            IPAddress.Enabled = enbale;
        }

        /// <summary>
        ///  是否自动滚屏
        /// </summary>
        private void ChangeMemoEditLastend()
        {
            if (autoScroll.Checked)
            {
                dataLog.SelectionStart = dataLog.Text.Length;
                dataLog.ScrollToCaret();
            }
        }

        /// <summary>
        ///  是否启用日志模式显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recLog_CheckedChanged(object sender, EventArgs e)
        {
            StringExtension.recLog = recLog.Checked;
        }

        /// <summary>
        /// 点击发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendInfoBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var result = sendInfo.Text.Trim();
                if (result.Length == 0 && !_isCycle)
                {
                    MessageBox.Show("输入不能为空");
                    return;
                }

                // 关闭线程
                if (_isCycle)
                {
                    CycleThread?.Abort();
                    sendBtn.Text = "发送";
                    _isCycle = false;
                    DisableControls(true);
                    return;
                }
                if (result.Length > 0)
                {
                    // 是否生成随机数
                    if (randomCheck.Checked)
                    {
                        result += new Random().Next(1, 999).ToString();
                    }
                    // 选择循环周期
                    if (selectCycle.Checked)
                    {
                        CycleThread = new Thread(() =>
                        {
                            while (_isCycle)
                            {
                                sendMessage(result);
                                Thread.Sleep(cycleTime.Text.ToString().ToInt32());
                            }
                        });
                        CycleThread?.Start();
                        _isCycle = true;
                        DisableControls(false);
                        // 将发送选项设置为 disbale
                        sendBtn.Text = "停止发送";
                    }
                    else
                    {
                        sendMessage(result);
                    }
                }
            }
            catch (Exception ex)
            {
                dataLog.AppendText(ex.ToString());
            }
        }

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="result"></param>
        private void sendMessage(string result)
        {
            Invoke(new Action(() =>
            {
                // ascii 发送
                if (sendASCII.Checked)
                {
                    dataLog.AppendText(result.FormatStringLog());
                    if (agreeType.Text == "TCP Server")
                    {
                        tcpServer.GetClients().ToList().ForEach(client =>
                        {
                            client.Send(result);
                        });
                    }
                    else
                    {
                        tcpClient.Send(result);
                    }
                }
                else
                {
                    byte[] sendBytes = Encoding.UTF8.GetBytes(result);

                    if (sendBytes != null)
                    {
                        BitConverter.ToString(sendBytes).Replace("-", " ");
                        // 打印字节数组的十六进制表示
                        dataLog.AppendText(String.Join(" ", sendBytes.Select(b => b.ToString("X2"))).FormatStringLog());
                    }

                    if (agreeType.Text == "TCP Server")
                    {
                        tcpServer.GetClients().ToList().ForEach(client =>
                        {
                            client.Send(sendBytes);
                        });
                    }
                    else
                    {
                        tcpClient.Send(sendBytes);
                    }
                }
                sendInfo.Text = "";
            }));
        }

        /// <字符串转16进制格式,不够自动前面补零>
        /// 
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] TranStrToHexByte(string hexString)
        {
            int i;
            hexString = hexString.Replace(" ", "");//清除空格
            if ((hexString.Length % 2) != 0)//奇数个
            {
                byte[] returnBytes = new byte[(hexString.Length + 1) / 2];

                try
                {
                    for (i = 0; i < (hexString.Length - 1) / 2; i++)
                    {
                        returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                    }
                    returnBytes[returnBytes.Length - 1] = Convert.ToByte(hexString.Substring(hexString.Length - 1, 1).PadLeft(2, '0'), 16);
                }
                catch
                {
                    dataLog.AppendText("含有非16进制字符".FormatStringLog());
                    return null;
                }

                return returnBytes;
            }
            else
            {
                byte[] returnBytes = new byte[(hexString.Length) / 2];
                try
                {
                    for (i = 0; i < returnBytes.Length; i++)
                    {
                        returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                    }
                }
                catch
                {
                    dataLog.AppendText("含有非16进制字符".FormatStringLog());
                    return null;
                }
                return returnBytes;
            }
        }

        private void DisableControls(bool enable)
        {
            sendASCII.Enabled = enable;
            sendHEX.Enabled = enable;
            randomCheck.Enabled = enable;
            cycleTime.Enabled = enable;
            selectCycle.Enabled = enable;
        }

        private void recSaveFile_Click(object sender, EventArgs e)
        {
            if (recSaveFile.Checked)
            {
                // 弹出保存文件对话框
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    logFilePath = saveFileDialog.FileName;
                }
            }
        }
        /// <summary>
        /// 打开关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSwitch_Click(object sender, EventArgs e)
        {
            try
            {
                if (agreeType.Text == "TCP Server")
                {
                    StartTcpServer();
                }
                else if (agreeType.Text == "TCP Client")
                {
                    StartTcpClient();
                }

            }
            catch (Exception exception)
            {
                switchBtn.Text = "打开";
                dataLog.AppendText(exception.Message.FormatStringLog());
            }
        }

        private void StartTcpClient()
        {
            if (_isOpen)
            {
                tcpClient?.Close();
                switchBtn.Text = "打开";
                ChangeComboxEnable(true);
                dataLog.AppendText("连接关闭".FormatStringLog());
            }
            else
            {
                tcpClient.Connecting += (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 正在连接服务器".FormatStringLog());
                    }));

                    return EasyTask.CompletedTask;
                };

                tcpClient.Connected += (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 连接服务器成功".FormatStringLog());
                    }));

                    return EasyTask.CompletedTask;
                };

                tcpClient.Disconnecting += (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 连接正在断开".FormatStringLog());
                    }));

                    return EasyTask.CompletedTask;
                };
                tcpClient.Disconnecting += (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 连接已经断开".FormatStringLog());
                    }));

                    return EasyTask.CompletedTask;
                };

                tcpClient.Received = (client, args) =>
                {
                    //从服务器收到信息。但是一般byteBlock和requestInfo会根据适配器呈现不同的值。
                    var msg = Encoding.UTF8.GetString(args.ByteBlock.Buffer, 0, args.ByteBlock.Len);

                    client.Logger.Info($"已经从 {client.IP}:{client.Port} 接收到消息了{msg}");

                    if (recASCII.Checked)
                    {
                        Invoke(new Action(() =>
                        {
                            dataLog.AppendText($"已经从 {client.IP}:{client.Port} 接收到消息了{msg}".FormatStringLog());
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            dataLog.AppendText($"已经从 {client.IP}:{client.Port} 接收到消息了{string.Join(" ", args.ByteBlock.Buffer.Take(args.ByteBlock.Len).Select(b => b.ToString("X2")))}".FormatStringLog());
                        }));
                    }

                    client.Send($"已经收到消息了:{msg}");

                    return EasyTask.CompletedTask;
                };

                var ip = IPAddress.Text;
                var port = ipPort.Text.ToInt32();

                tcpClient.Setup(new TouchSocketConfig()
                    .SetRemoteIPHost($"{ip}:{port}")
                    .ConfigureContainer(a => a.AddConsoleLogger())
                    .ConfigurePlugins(a =>
                    {
                        a.UseReconnection(5, true, 1000);
                    }));

                tcpClient.Connect();
                tcpClient.Send("已经连接上了");

                ChangeComboxEnable(false);
                switchBtn.Text = "关闭";
                dataLog.AppendText("连接打开".FormatStringLog());
            }

            _isOpen = !_isOpen;
        }

        /// <summary>
        /// 启动tcpserve
        /// </summary>
        private void StartTcpServer()
        {
            if (_isOpen)
            {
                tcpServer.Stop();
                switchBtn.Text = "打开";
                ChangeComboxEnable(true);
                dataLog.AppendText("连接关闭".FormatStringLog());
            }
            else
            {
                tcpServer.Connecting = (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"正在连接{client.IP}:{client.Port} 客户端".FormatStringLog());
                    }));
                    return EasyTask.CompletedTask;
                };
                tcpServer.Connected = (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 客户端连接成功".FormatStringLog());
                    }));
                    return EasyTask.CompletedTask;
                };
                tcpServer.Disconnecting = (client, args) =>
                {
                    // 只有当主动断开时才有效
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 客户端正在断开连接".FormatStringLog());
                    }));
                    return EasyTask.CompletedTask;
                };
                tcpServer.Disconnected = (client, args) =>
                {
                    Invoke(new Action(() =>
                    {
                        dataLog.AppendText($"{client.IP}:{client.Port} 客户端断开连接".FormatStringLog());
                    }));
                    return EasyTask.CompletedTask;
                };

                tcpServer.Received = (client, args) =>
                {
                    // 从客户端收到信息:注意：数据长度是byteBlock.Len
                    var mes = Encoding.UTF8.GetString(args.ByteBlock.Buffer, 0, args.ByteBlock.Len);
                    client.Logger.Info($"已从{client.IP}:{client.Port} 接收到信息：{mes}");

                    if (recASCII.Checked)
                    {
                        Invoke(new Action(() =>
                        {
                            dataLog.AppendText($"已从{client.IP}:{client.Port}接收到信息：{mes}".FormatStringLog());
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            dataLog.AppendText($@"已从{client.IP}:{client.Port}接收到信息：{string.Join(" ", args.ByteBlock.Buffer.Take(args.ByteBlock.Len).Select(b => b.ToString("X2")).ToArray())}"
                                .FormatStringLog());
                        }));
                    }

                    // 将收到的信息直接返回给发送方
                    client.Send($"已收到信息：{mes}");

                    return EasyTask.CompletedTask;
                };

                var ip = IPAddress.Text;
                var port = ipPort.Text.ToInt32();

                tcpServer.Setup(new TouchSocketConfig()//载入配置
                   .SetListenIPHosts($"tcp://{ip}:{port}")//同时监听两个地址
                   .ConfigureContainer(a =>//容器的配置顺序应该在最前面
                   {
                       a.AddConsoleLogger();//添加一个控制台日志注入
                   })
                   .ConfigurePlugins(a =>
                   {
                       // 开启断线重连
                       // 如需永远尝试连接，tryCount 设置为 -1 即可。
                       a.UseReconnection(5, true, 1000);
                   }));

                tcpServer.Start();//启动

                ChangeComboxEnable(false);
                switchBtn.Text = "关闭";
                dataLog.AppendText("连接打开".FormatStringLog());
            }

            _isOpen = !_isOpen;
            ChangeMemoEditLastend();
        }
    }
}
