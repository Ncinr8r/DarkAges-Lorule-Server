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

using System;
using System.Linq;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Storage.locales.debuffs;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Spells
{
    [Script("ao puinsein", "Dean")]
    public class ao_puinsein : SpellScript
    {
        private readonly Random rand = new Random();
        private readonly Debuff_poison Debuff = new Debuff_poison("poison", 10, 10, 0);

        public ao_puinsein(Spell spell) : base(spell)
        {
        }

        public override void OnFailed(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
                (sprite as Aisling)
                    .Client
                    .SendMessage(0x02, "failed.");
        }

        public override void OnSuccess(Sprite sprite, Sprite target)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                client.TrainSpell(Spell);

                var debuff = Clone<Debuff_poison>(Debuff);
                var curses = target.Debuffs.Values.OfType<Debuff_poison>().ToList();

                client.SendMessage(0x02, string.Format("you cast {0}", Spell.Template.Name));
                client.SendAnimation(Spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = (byte) (client.Aisling.Path == Class.Priest ? 0x80 :
                        client.Aisling.Path == Class.Wizard ? 0x88 : 0x06),
                    Speed = 30
                };

                var hpbar = new ServerFormat13
                {
                    Serial = client.Aisling.Serial,
                    Health = 255,
                    Sound = Spell.Template.Sound
                };

                client.Aisling.Show(Scope.NearbyAislings, action);
                client.Aisling.Show(Scope.NearbyAislings, hpbar);

                if (curses.Count > 0)
                {
                    var dn = target.GetDebuffName(i => i.Name.ToLower().Contains("puinsein"));

                    if (dn != string.Empty)
                        if (target.RemoveDebuff(dn, true))
                            if (sprite.Serial != target.Serial)
                                if (target is Aisling)
                                    (target as Aisling).Client
                                        .SendMessage(0x02,
                                            string.Format("{0} Removes {1} from you.", client.Aisling.Username,
                                                Spell.Template.Name));
                }
            }
            else
            {
                var debuff = Clone<Debuff_poison>(Debuff);
                var curses = target.Debuffs.Values.OfType<Debuff_poison>().ToList();

                if (curses.Count > 0)
                {
                    var dn = target.GetDebuffName(i => i.Name.ToLower().Contains("puinsein"));

                    if (dn != string.Empty)
                        if (target.RemoveDebuff(dn, true))
                            if (target is Aisling)
                                (target as Aisling).Client
                                    .SendMessage(0x02,
                                        string.Format("{0} Removes {1} from you.",
                                            (sprite is Monster
                                                ? (sprite as Monster).Template.Name
                                                : (sprite as Mundane).Template.Name) ?? "Monster",
                                            Spell.Template.Name));
                }

                target.SendAnimation(Spell.Template.Animation, target, sprite);

                var action = new ServerFormat1A
                {
                    Serial = sprite.Serial,
                    Number = 1,
                    Speed = 30
                };

                var hpbar = new ServerFormat13
                {
                    Serial = target.Serial,
                    Health = 255,
                    Sound = Spell.Template.Sound
                };

                sprite.Show(Scope.NearbyAislings, action);
                target.Show(Scope.NearbyAislings, hpbar);
            }
        }

        public override void OnUse(Sprite sprite, Sprite target)
        {
            if (sprite.CurrentMp - Spell.Template.ManaCost > 0)
            {
                sprite.CurrentMp -= Spell.Template.ManaCost;
            }
            else
            {
                if (sprite is Aisling) (sprite as Aisling).Client.SendMessage(0x02, ServerContext.Config.NoManaMessage);

                return;
            }

            if (sprite.CurrentMp < 0)
                sprite.CurrentMp = 0;

            var success = Spell.RollDice(rand);

            if (success)
                OnSuccess(sprite, target);
            else
                OnFailed(sprite, target);

            if (sprite is Aisling)
                (sprite as Aisling)
                    .Client
                    .SendStats(StatusFlags.StructB);
        }
    }
}