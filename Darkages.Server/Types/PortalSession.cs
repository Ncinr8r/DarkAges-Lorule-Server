﻿using Darkages.Network.Game;
using Darkages.Network.ServerFormats;
using Darkages.Types;
using Newtonsoft.Json;
///************************************************************************
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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darkages
{
    public class PortalSession
    {
        public PortalSession()
        {
            IsMapOpen = false;
        }

        public bool IsMapOpen { get; set; }
        public int FieldNumber { get; set; } = 1;
        public DateTime DateOpened { get; set; }

        [JsonIgnore]
        public WorldMapTemplate Template
            => ServerContext.GlobalWorldMapTemplateCache[FieldNumber];

        public void ShowFieldMap(GameClient client)
        {
            client.InMapTransition = true;
            client.Send(new ServerFormat2E(client.Aisling));
            Task.Run(async () => await client.FlushBuffers());

            client.Aisling.PortalSession
                    = new PortalSession
                    {
                        FieldNumber = client.Aisling.World,
                        IsMapOpen = true,
                        DateOpened = DateTime.UtcNow
                    };

            client.DateMapOpened = DateTime.UtcNow;
        }

        public void TransitionToMap(GameClient client, short X = -1, short Y = -1, int DestinationMap = 0)
        {
            if (DestinationMap == 0)
            {
                client.LeaveArea(true, true);

                DestinationMap = ServerContext.Config.TransitionZone;

                client.Aisling.XPos = X >= 0 ? X : ServerContext.Config.TransitionPointX;
                client.Aisling.YPos = Y >= 0 ? Y : ServerContext.Config.TransitionPointY;
                client.Aisling.CurrentMapId = DestinationMap;

                client.Refresh();
                ShowFieldMap(client);
            }
            else
            {
                if (ServerContext.GlobalMapCache.ContainsKey(DestinationMap))
                {
                    client.Aisling.XPos = X >= 0 ? X : ServerContext.Config.TransitionPointX;
                    client.Aisling.YPos = Y >= 0 ? Y : ServerContext.Config.TransitionPointY;
                    client.Aisling.CurrentMapId = DestinationMap;
                    client.LeaveArea(true, true);
                    client.EnterArea();

                    client.Aisling.PortalSession = null;
                }
            }
        }    
    }
}