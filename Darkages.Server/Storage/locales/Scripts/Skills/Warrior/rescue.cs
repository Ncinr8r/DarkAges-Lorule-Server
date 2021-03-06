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

using System.Collections.Concurrent;
using Darkages.Network.ServerFormats;
using Darkages.Types;

namespace Darkages.Scripting.Scripts.Skills
{
    [Script("Rescue", "Dean")]
    public class Rescue : SkillScript
    {
        public Skill _skill;
        public Sprite Target;

        public Rescue(Skill skill) : base(skill)
        {
            _skill = skill;
        }

        public override void OnFailed(Sprite sprite)
        {
            if (Target != null)
                if (sprite is Aisling)
                {
                    var client = (sprite as Aisling).Client;
                    client.Aisling.Show(Scope.NearbyAislings,
                        new ServerFormat29(Skill.Template.MissAnimation, (ushort) Target.XPos, (ushort) Target.YPos));
                }
        }

        public override void OnSuccess(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                var action = new ServerFormat1A
                {
                    Serial = client.Aisling.Serial,
                    Number = 0x01,
                    Speed = 20
                };


                var enemy = client.Aisling.GetInfront();

                if (enemy != null)
                    foreach (var i in enemy)
                    {
                        if (i == null)
                            continue;

                        if (client.Aisling.Serial == i.Serial)
                            continue;
                        if (i is Money)
                            continue;
                        if (!i.Attackable)
                            continue;

                        //this should ensure we remove everyone from the tag list except me or group members.
                        if (i is Monster _monster)
                        {
                            if (sprite is Aisling _player)
                            {
                                if (_player.GroupParty.MembersExcludingSelfMapWide.Count > 0)
                                {
                                    var nearbyTags = _player.GroupParty.MembersExcludingSelfMapWide.ToArray();
                                    var removed    = 0;

                                    foreach (var obj in nearbyTags)
                                    {
                                        removed += _monster.TaggedAislings.RemoveWhere(n => n != obj.Serial);
                                    }
                                }
                            }
                        }

                        Target = i;
                        i.ApplyDamage(sprite, 0, true, Skill.Template.Sound, null, true);

                        if (i is Aisling)
                        {
                            if ((i as Aisling).Skulled)
                            {
                                (i as Aisling).RemoveDebuff("skulled", true);
                                (i as Aisling).Client.Revive();
                            }


                            (i as Aisling).Client.Aisling.Show(Scope.NearbyAislings,
                                new ServerFormat29((uint) client.Aisling.Serial, (uint) i.Serial, byte.MinValue,
                                    Skill.Template.TargetAnimation, 100));
                            (i as Aisling).Client.Send(new ServerFormat08(i as Aisling, StatusFlags.All));
                        }

                        if (i is Monster || i is Mundane || i is Aisling)
                            client.Aisling.Show(Scope.NearbyAislings,
                                new ServerFormat29((uint) client.Aisling.Serial, (uint) i.Serial,
                                    Skill.Template.TargetAnimation, 0, 100));
                    }

                client.Aisling.Show(Scope.NearbyAislings, action);
            }
        }

        public override void OnUse(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                if (Skill.Ready)
                {
                    client.TrainSkill(Skill);
                    OnSuccess(sprite);
                }
            }
        }
    }
}