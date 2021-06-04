﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CameraRigController
{
    class ArduinoConnectionManager : IDisposable
    {
        ArduinoRecievePacket _lastPacket;
        private bool _newPacket;
        public string ComPort 
        {
            get => _comPort;
            set
            {
                _comPort = value;
                Port.Dispose();
            }
        }
        public SerialPort Port { get; private set; }
        Thread _arduinoPlay;
        private List<AnimChannel> _data;
        Dispatcher Dispatcher;
        private bool _abort;
        private bool _run;
        private bool _running;
        private StringBuilder _buffer = new StringBuilder();
        private string _comPort;

        public ArduinoConnectionManager()
        {
            OpenPort();
            _arduinoPlay = new Thread(PlayRoutine);
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var s = Port.ReadLine();
            var status = new ArduinoRecievePacket(s);
            if (status.Status == ArduinoStatusCode.Debug)
            {
                Debug.WriteLine(Port.ReadLine());
            }
            else
            {
                _lastPacket = status;
                _newPacket = true;
            }
        }

        private bool AttemptConnection()
        {
            if (!Port.IsOpen)
            {
                try
                {
                    Port.Open();
                }
                catch (UnauthorizedAccessException e)
                {
                    _run = false;
                    MessageBox.Show(e.Message);
                    return false;
                }
                catch (InvalidOperationException e)
                {
                    _run = false;
                    MessageBox.Show(e.Message);
                    return false;
                }
                catch (IOException e)
                {
                    _run = false;
                    MessageBox.Show("Invalid port. Please select a valid port.\n" + e.Message);
                    return false;
                }
            }
            return true;
        }

        public void Load(List<AnimChannel> channels)
        {
            _data = channels;
            if (!_arduinoPlay.IsAlive)
            {
                _arduinoPlay.Start();
            }
            _run = false;
            while (_running) Thread.Sleep(50);
            _run = true;
        }

        public bool TryConnect()
        {
            if (!Port.IsOpen)
            {
                if (AttemptConnection())
                {
                    var timeout = DateTime.Now + TimeSpan.FromSeconds(2);
                    while (DateTime.Now < timeout)
                    {
                        //if (Port.BytesToRead > 0)
                        //{
                        //    _lastPacket = new ArduinoRecievePacket(Port.ReadLine());
                        //    return _lastPacket.Status == ArduinoStatusCode.Ready;
                        //}
                        if (_newPacket)
                        {
                            _newPacket = false;
                            return _lastPacket.Status == ArduinoStatusCode.Ready;
                        }
                    }
                }
            }
            else
            {
                Port.Close();
                return false;
            }
            return false;
        }

        public bool ResetArduino()
        {
            if (Port.IsOpen) Port.Dispose();
            return TryConnect();
        }

        public void Play()
        {
            //if (_data == null)
            //{
            //    MessageBox.Show("The data needs to be loaded before playing.");
            //    return;
            //}
            //SendDataPacket(ArduinoSendRequestPacket.StartRequest.ToString());
        }

        private void OpenPort()
        {
            Port = new SerialPort();
            if (!string.IsNullOrEmpty(ComPort)) Port.PortName = ComPort;
            Port.BaudRate = 115200;
            Port.DtrEnable = true;
            Port.RtsEnable = true;
            Port.DataReceived += Port_DataReceived;
            Port.Disposed += Port_Disposed;
        }

        private void Port_Disposed(object sender, EventArgs e)
        {
            Port.DataReceived -= Port_DataReceived;
            Port.Disposed -= Port_Disposed;
            OpenPort();
        }

        public ArduinoStatusCode SendStatusRequest()
        {
            SendDataPacket(ArduinoSendRequestPacket.StatusRequest.ToString());
            var res = ReadNextLine();
            if (res == null) return ArduinoStatusCode.None;
            return new ArduinoRecievePacket(res).Status;
        }

        private void SendKeyframeData(Keyframe keyframe, UInt16 id)
        {
            SendDataPacket(new ArduinoSendKeyframePacket(id, keyframe.MS, keyframe.Value).ToString());
        }

        private string ReadNextLine()
        {
            if (Port.IsOpen)
            {
                return Port.ReadLine();
            }
            return null;
        }

        private void SendDataPacket(string packet)
        {
            if (Port.IsOpen)
            {
                //if (_buffer.Length != 0)_buffer.Append(' ');
                //_buffer.Append(packet);
                Port.WriteLine(packet);
                Debug.WriteLine(packet);
            }
        }

        private void FlushBuffer()
        {
            //if (Port.IsOpen)
            //{
            //    Port.WriteLine(_buffer.ToString());
            //    Debug.WriteLine($"SerialPort: {_buffer}");
            //    _buffer.Clear();
            //}
        }

        private void PlayRoutine()
        {
            while (!_abort)
            {
                _running = false;
                while (!_run)
                {
                    if (_abort) return;
                    Thread.Sleep(50);
                }
                _running = true;
                if (ResetArduino())
                {
                    Debug.WriteLine("Arduino connected");
                    while (_run)
                    {
                        //var status = SendStatusRequest();
                        //if (status == ArduinoStatusCode.Ready)
                        //{
                        foreach (var channel in _data)
                        {
                            foreach (var keyframe in channel.Keyframes)
                            {
                                if (!_run) break;
                                if (_abort) return;
                                SendKeyframeData(keyframe, channel.MotorInfo.MotorID);
                                Thread.Sleep(10);
                            }
                            if (!_run) break;
                            if (_abort) return;
                        }
                        if (!_run) break;
                        if (_abort) return;
                        //}
                        //status = SendStatusRequest();
                        //if (status == ArduinoStatusCode.Ready)
                        //{
                        //FlushBuffer();
                        //}
                        SendDataPacket(ArduinoSendRequestPacket.StartRequest.ToString());
                        //while (status != ArduinoStatusCode.Done && !_abort)
                        //{
                        //    Thread.Sleep(1000);
                        //    status = SendStatusRequest();
                        //}
                        Debug.WriteLine("Upload complete");
                        _run = false;
                    }
                }
                Thread.Sleep(1000);
                if (Port.IsOpen)
                {
                    Port.Dispose();
                    //while (!_abort)
                    //{
                    //    if (_newPacket)
                    //    {
                    //        if (_lastPacket.Status == ArduinoStatusCode.Done) break;
                    //    }
                    //}
                    //Debug.WriteLine("Playback done");
                }
            }
        }

        public void Dispose()
        {
            _abort = true;
            _arduinoPlay.Abort();
            Port.Dispose();
        }
    }
}
