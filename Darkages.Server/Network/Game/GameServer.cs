﻿///************************************************************************
//Project Lorule: A Dark Ages Server (http://darkages.creatorlink.net/index/)
//Copyright(C) 2018 TrippyInc Pty Ltd
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//*************************************************************************/
using Darkages.Network.Game.Components;
using Darkages.Network.Object;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darkages.Network.Game
{
    public partial class GameServer
    {


        DateTime lastHeavyUpdate  = DateTime.UtcNow;

        TimeSpan HeavyUpdateSpan;



        public ObjectService ObjectFactory = new ObjectService();

        public Dictionary<Type, GameServerComponent> Components;

        public GameServer(int capacity) : base(capacity)
        {
            HeavyUpdateSpan  = TimeSpan.FromSeconds(1.0 / 30);

            InitializeGameServer();
        }

        ReaderWriterLock _writerLock = new ReaderWriterLock();

        private void AutoSave(GameClient client)
        {
            lock (Clients)
            {
                if ((DateTime.UtcNow - client.LastSave).TotalSeconds > ServerContext.Config.SaveRate)
                {
                    _writerLock.AcquireWriterLock(Timeout.Infinite);
                    {
                        client.Save();
                    }

                    if (_writerLock.IsWriterLockHeld)
                    {
                        _writerLock.ReleaseWriterLock();
                    }
                }
            }
        }

        /// <summary>
        ///   <para>
        /// Gets the Value True or False That represents if the Server is running Healthy.</para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if [server healthy]; otherwise, <c>false</c>.</value>
        /// <remarks>This is done by Checking that the lastUpdate took less then the specified length of time. By Default this is one second.</remarks>
        public bool ServerHealthy => DateTime.UtcNow - lastHeavyUpdate < new TimeSpan(0, 0, 0, 1);

        private readonly ManualResetEvent __msync = new ManualResetEvent(true);

        private void Update()
        {
            lastHeavyUpdate       = DateTime.UtcNow;
            ServerContext.Running = true;

            while (ServerContext.Running)
            {
                try
                {
                    var delta = DateTime.UtcNow - lastHeavyUpdate;

                    if (ServerContext.Paused)
                        continue;

                    ExecuteClientWork(delta);
                    ExecuteServerWork(delta);
                    ExecuteObjectWork(delta);
                }
                catch (Exception error)
                {
                    ServerContext.ILog?.Error("Error In Heavy Worker", error);
                }
                finally
                {
                    lastHeavyUpdate = DateTime.UtcNow;
                    Thread.Sleep(HeavyUpdateSpan);
                }
            }
        }

        public void InitializeGameServer()
        {
            InitComponentCache();

            ServerContext.ILog?.Info(string.Format("[Lorule] {0} Server Components loaded.", Components.Count));
        }

        private void InitComponentCache()
        {
            Components = new Dictionary<Type, GameServerComponent>
            {
                [typeof(MonolithComponent)] = new MonolithComponent(this),
                [typeof(DaytimeComponent)]  = new DaytimeComponent(this),
                [typeof(MundaneComponent)]  = new MundaneComponent(this),
                [typeof(MessageComponent)]  = new MessageComponent(this),
                [typeof(PingComponent)]     = new PingComponent(this),
                [typeof(Save)]              = new Save(this),
                [typeof(ObjectComponent)]   = new ObjectComponent(this),
            };
        }

        public void ExecuteClientWork(TimeSpan elapsedTime)
        {
            try
            {
                UpdateClients(elapsedTime);
            }
            catch (Exception err)
            {
                ServerContext.ILog.Error("Error: ExecuteClientWork", err);
            }
        }

        public void ExecuteServerWork(TimeSpan elapsedTime)
        {
            try
            {
                UpdateComponents(elapsedTime);
            }
            catch (Exception err)
            {
                ServerContext.ILog.Error("Error: ExecuteServerWork", err);
            }
        }

        public void ExecuteObjectWork(TimeSpan elapsedTime)
        {
            try
            {
                UpdateAreas(elapsedTime);
            }
            catch (Exception err)
            {
                ServerContext.ILog.Error("Error: ExecuteObjectWork", err);
            }
        }

        private void UpdateComponents(TimeSpan elapsedTime)
        {
            try
            {
                lock (Components)
                {
                    foreach (var component in Components.Values)
                    {
                        component.Update(elapsedTime);
                    }
                }
            }
            catch (Exception err)
            {
                ServerContext.ILog.Error("Error: UpdateComponents", err);
            }
        }

        private void UpdateAreas(TimeSpan elapsedTime)
        {
            lock (Clients)
            {
                foreach (var area in ServerContext.GlobalMapCache.Values)
                {
                    area.Update(elapsedTime);
                }
            }
        }

        public void UpdateClients(TimeSpan elapsedTime)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client != null && client.Aisling != null)
                    {
                        __msync.WaitOne();
                        __msync.Reset();

                        try
                        {
                            if (!client.IsWarping)
                            {
                                client.Update(elapsedTime);
                                client.FlushBuffers();
                            }
                            else
                            {
                                if (client.CanSendLocation)
                                    client.SendLocation();
                            }
                        }
                        catch (Exception err)
                        {
                            ServerContext.ILog.Error("Error: UpdateClients", err);
                        }
                        finally
                        {
                            __msync.Set();
                        }
                    }
                }
            }
        }

        public override void ClientDisconnected(GameClient client)
        {
            lock (Clients)
            {
                if (client == null || client.Aisling == null)
                    return;

                try
                {
                    client.Save();
                    ServerContext.ILog.Warning("{0} has disconnected from server.", client.Aisling.Username);

                    client.Aisling.LoggedIn = false;
                    client.Aisling.Remove(true);
                }
                catch (Exception)
                {
                    //Ignore
                }
                finally
                {
                    base.ClientDisconnected(client);
                }
            }
        }

        public override void Abort()
        {
            base.Abort();
        }

        
        public override async void StartAsync(int port)
        {
            base.StartAsync(port);

            await new TaskFactory().StartNew(ServerGuard);
        }

        private Thread _thread = null;

        public void Launch()
        {
            var thread = _thread;
            Thread.MemoryBarrier();

            if (thread == null || thread.ThreadState == ThreadState.Stopped)
            {
                var __tmpl = new Thread(Update)
                {
                    IsBackground = true,
                    Name         = ServerContext.Config.SERVER_TITLE
                };

                __tmpl.Start();


                Thread.MemoryBarrier();
                _thread = __tmpl;
            }
        }

        private void ServerGuard()
        {
            while (true)
            {
                if (!ServerHealthy)
                {
                    ServerContext.ILog.Warning("Starting Main Server Threads.");
                    Launch();
                }

                Thread.Sleep(5000);
            }
        }
    }
}
