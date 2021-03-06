﻿using Darkages.Types;
using LiteDB;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Darkages.Network.Game.Components
{
    public class ClientTickComponent : GameServerComponent
    {
        private readonly GameServerTimer timer;

        public ClientTickComponent(GameServer server)
            : base(server)
        {
            timer = new GameServerTimer(
                TimeSpan.FromSeconds(30));
        }

        public bool IsUpdating { get; set; } = false;

        public override void Update(TimeSpan elapsedTime)
        {
            timer.Update(elapsedTime);

            if (timer.Elapsed)
            {
                timer.Reset();
            }
        }

        public class EntityObj
        {
            public Type RefType;
            public string Name;
            public string Data;

            public DateTime Updated;
            public string UserName;

            [BsonId] public int Hash { get; set; }
        }
    }
}