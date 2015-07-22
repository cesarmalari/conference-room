﻿using System;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    public class StatusMonitor
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        ArduinoSender _sender = new ArduinoSender();
        private Timer _timer;
        private TimeSpan _statusInterval = Config.StatusInterval;

        public StatusMonitor()
        {
            _timer = new Timer(UpdateState, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _timer.Change(TimeSpan.FromSeconds(1), _statusInterval);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void UpdateState(object state)
        {
            try
            {
                var status = GetStatus();
                if (status.Status == RoomStatus.Free)
                {
                    _sender.SetColor(Color.Green);
                }
                else
                {
                    _sender.SetColor(Color.Red);
                }

                if (status.NextChangeSeconds.HasValue && status.NextChangeSeconds.Value < _statusInterval.TotalSeconds)
                {
                    _timer.Change(TimeSpan.FromSeconds(status.NextChangeSeconds.Value), _statusInterval);
                }
            }
            catch (Exception ex)
            {
                log.WarnFormat("Error updating state", ex);
            }
        }

        private RoomStatusInfo GetStatus()
        {
            var c = new HttpClient();
            var uri = new Uri(new Uri(Config.ApiServer), "room/" + Config.RoomAddress + "/status");
            var data = c.GetStringAsync(uri).Result;
            var status = JsonConvert.DeserializeObject<RoomStatusInfo>(data);
            return status;
        }
    }
}