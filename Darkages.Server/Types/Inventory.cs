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
using System.Collections.Generic;
using System.Linq;
using Darkages.Network.Game;
using Darkages.Network.Object;
using Darkages.Network.ServerFormats;
using Newtonsoft.Json;

namespace Darkages.Types
{
    public class Inventory : ObjectManager
    {
        public static readonly int LENGTH = 59;

        public Dictionary<int, Item> Items = new Dictionary<int, Item>();


        public Inventory()
        {
            for (var i = 0; i < LENGTH; i++) Items[i + 1] = null;
        }

        [JsonIgnore]
        public IEnumerable<byte> BankList => (Items?.Where(i =>
                i.Value != null && i.Value.Template != null && i.Value.Template.Flags.HasFlag(ItemFlags.Bankable)))
            .Select(i => i.Value.Slot);

        public int Length => Items.Count;

        public Item FindInSlot(int Slot)
        {
            if (Items.ContainsKey(Slot))
            {
                return Items[Slot];
            }

            return null;
        }

        public void Assign(Item Item)
        {
            if (Item != null)
            {
                Set(Item);
            }
        }

        public new Item[] Get(Predicate<Item> prediate)
        {
            return Items.Values.Where(i => i != null && prediate(i)).ToArray();
        }

        public Item Has(Predicate<Item> prediate)
        {
            return Items.Values.FirstOrDefault(i => i != null && prediate(i));
        }

        public void Set(Item s)
        {
            if (s == null)
                return;

            if (Items.ContainsKey(s.Slot))
            {
                Items[s.Slot] = Clone<Item>(s);
            }
        }

        public byte FindEmpty()
        {
            byte idx = 1;

            foreach (var slot in Items)
            {
                if (slot.Value == null)
                    return idx;

                idx++;
            }

            return byte.MaxValue;
        }

        public void Set(Item s, bool clone = false)
        {
            if (s == null)
                return;

            if (Items.ContainsKey(s.Slot))
            {
                Items[s.Slot] = clone ? Clone<Item>(s) : s;
            }
        }

        public void Remove(GameClient client, Item item)
        {
            if (item == null)
                return;

            if (Remove(item.Slot) != null)
            {
                client.Send(new ServerFormat10(item.Slot));
            }
        }

        public Item Remove(byte movingFrom)
        {
            if (Items.ContainsKey(movingFrom))
            {
                var copy = Items[movingFrom];
                Items[movingFrom] = null;
                return copy;
            }

            return null;
        }

        public int Has(Template templateContext)
        {
            var items = Items.Where(i => i.Value != null && i.Value.Template.Name == templateContext.Name)
                .Select(i => i.Value).ToList();

            var anyItem = items.FirstOrDefault();

            if (anyItem?.Template == null)
                return 0;

            var result = anyItem.Template.CanStack ? items.Sum(i => i.Stacks) : items.Count;

            return result;
        }


        public int HasCount(Template templateContext)
        {
            var items = Items.Where(i => i.Value != null && i.Value.Template.Name == templateContext.Name)
                .Select(i => i.Value).ToList();

            return items.Count;
        }

        public bool CanPickup(Aisling player, Item LpItem)
        {
            if (player == null || LpItem == null)
                return false;

            if (LpItem.Template == null)
                return false;

            return player.CurrentWeight + LpItem.Template.CarryWeight < player.MaximumWeight &&
                   FindEmpty() != byte.MaxValue;
        }

        public void RemoveRange(GameClient client, Item item, int range)
        {
            var remaining = item.Stacks - range;

            if (remaining <= 0)
            {
                Remove(item.Slot);
                client.Send(new ServerFormat10(item.Slot));

                client.Aisling.CurrentWeight -= item.Template.CarryWeight;

                if (client.Aisling.CurrentWeight < 0)
                    client.Aisling.CurrentWeight = 0;

                client.SendStats(StatusFlags.StructA);
            }
            else
            {
                item.Stacks = (byte) remaining;
                client.Aisling.Inventory.Set(item, false);

                client.Send(new ServerFormat0F(item));
            }
        }

        public void UpdateSlot(GameClient client, Item item)
        {
            client.Send(new ServerFormat0F(item));
        }
    }
}