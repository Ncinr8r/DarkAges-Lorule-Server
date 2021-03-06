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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darkages.Common;
using Darkages.Network.ClientFormats;
using Darkages.Network.ServerFormats;
using Darkages.Script.Context;
using Darkages.Scripting;
using Darkages.Storage;
using Darkages.Storage.locales.Scripts.Mundanes;
using Darkages.Types;
using MenuInterpreter;
using MenuInterpreter.Parser;
using ServiceStack.Text;

namespace Darkages.Network.Game
{
    /*
        The main Packet Handler Class
        Contains all methods handling received packets from the Game Client.
    */

    /// <summary>
    ///     The main <c>Packet Handler Class</c> class.
    ///     Contains all methods handling received packets from the Game Client.
    /// </summary>
    public partial class GameServer : NetworkServer<GameClient>
    {
        // CreateInterpreterFromMenuFile
        /// <summary>
        ///     Creates a yaml Interpreter from a <paramref name="lpClient" /> and <paramref name="lpName" />
        /// </summary>
        /// <returns>
        ///     void
        /// </returns>
        /// <example>
        ///     <code>
        /// if (client.MenuInterpter == null)
        /// {
        ///     CreateInterpreterFromMenuFile(client, popup.Template.YamlKey);
        /// 
        ///     if (client.MenuInterpter != null)
        ///     {
        ///         client.MenuInterpter.Start();
        ///         client.ShowCurrentMenu(popup, null, client.MenuInterpter.GetCurrentStep());
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when The lpName yaml File could not be created.
        /// </exception>
        /// <param name="lpClient">A valid GameClient</param>
        /// <param name="lpName">The yaml Script excluding the .yaml extension.</param>
        public void CreateInterpreterFromMenuFile(GameClient lpClient, string lpName, Sprite obj = null)
        {
            var parser = new YamlMenuParser();
            var yamlPath = ServerContext.StoragePath +
                           string.Format(CultureInfo.CurrentCulture, @"\Scripts\Menus\{0}.yaml", lpName);

            if (File.Exists(yamlPath))
            {

                var ycontent = File.ReadAllText(yamlPath);

                ServerContext.Log("[Interpreter] => ({0})", ycontent);

                try
                {
                    if (lpClient.MenuInterpter == null)
                    {
                        lpClient.MenuInterpter = parser.CreateInterpreterFromFile(yamlPath);
                        lpClient.MenuInterpter.Actor = obj;
                        lpClient.MenuInterpter.Client = lpClient;

                        lpClient.MenuInterpter.OnMovedToNextStep += MenuInterpter_OnMovedToNextStep;

                        lpClient.MenuInterpter.RegisterCheckpointHandler("QuestCompleted", (_client, res) =>
                        {
                            _Interop.Storage["client"] = lpClient;
                            _Interop.Storage["actor"] = lpClient.MenuInterpter.Actor;
                            _Interop.Storage["user"] = lpClient.Aisling;
                            {
                                "var client  = (GameClient) _Interop.Storage[\"client\"];".Run();
                                "var user    = (Aisling)    _Interop.Storage[\"user\"];".Run();
                                "var actor   = (Sprite)    _Interop.Storage[\"actor\"];".Run();
                            }



                            if (_client.Aisling.HasQuest(res.Value))
                                res.Result = _client.Aisling.HasCompletedQuest(res.Value);
                        });





                        lpClient.MenuInterpter.RegisterCheckpointHandler("CompleteQuest", (_client, res) =>
                        {
                            _Interop.Storage["client"] = lpClient;
                            _Interop.Storage["actor"] = lpClient.MenuInterpter.Actor;
                            _Interop.Storage["user"] = lpClient.Aisling;
                            {
                                "var client  = (GameClient) _Interop.Storage[\"client\"];".Run();
                                "var user    = (Aisling)    _Interop.Storage[\"user\"];".Run();
                                "var actor   = (Sprite)    _Interop.Storage[\"actor\"];".Run();
                            }

                            if (_client.Aisling.HasQuest(res.Value))
                            {
                                var q = _client.Aisling.GetQuest(res.Value);

                                if (q != null)
                                {
                                    if (!q.Completed)
                                    {
                                        q.HandleQuest(_client, null,
                                            completed =>
                                            {
                                                res.Result = completed;
                                            });
                                    }
                                }
                            }
                        });

                        lpClient.MenuInterpter.RegisterCheckpointHandler("Call", (_client, res) =>
                        {
                            _Interop.Storage["client"] = lpClient;
                            _Interop.Storage["actor"] = lpClient.MenuInterpter.Actor;
                            _Interop.Storage["user"] = lpClient.Aisling;
                            {
                                "var client  = (GameClient) _Interop.Storage[\"client\"];".Run();
                                "var user    = (Aisling)    _Interop.Storage[\"user\"];".Run();
                                "var actor   = (Sprite)     _Interop.Storage[\"actor\"];".Run();
                            }

                            res.Value.Run();
                            res.Result = (bool)ServerContext.EVALUATOR.Evaluate("result");
                        });

                        ServerContext.Log("Script Interpreter Created for Mundane: {0}", lpName);
                    }

                }
                catch (Exception err)
                {
                    ServerContext.Report(err);
                }
                return;
            }

            lpClient.MenuInterpter = null;
        }

        private void MenuInterpter_OnMovedToNextStep(GameClient client, MenuItem previous, MenuItem current)
        {
            if (client.MenuInterpter == null)
                return;

            if (client.MenuInterpter.IsFinished)
                client.MenuInterpter = null;
        }

        // ActivateAssails
        /// <summary>
        ///     Activates all available "Assails" for the provided <paramref name="lpClient" />
        /// </summary>
        /// <returns>
        ///     void
        /// </returns>
        /// <example>
        ///     <code>
        /// public void Assail()
        /// {
        ///     if (Client != null)
        ///     {
        ///         GameServer.ActivateAssails(Client);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="lpClient">A valid GameClient</param>
        public static void ActivateAssails(GameClient lpClient)
        {
            #region Sanity Checks

            if (lpClient?.Aisling == null)
                return;

            if (lpClient.Aisling.IsDead())
                return;

            #endregion

            if (lpClient.Aisling.IsSleeping || lpClient.Aisling.IsFrozen)
            {
                lpClient.Interupt();
                return;
            }

            lpClient.MenuInterpter = null;

            if (ServerContext.Config.AssailsCancelSpells)
                CancelIfCasting(lpClient);

            var ready = DateTime.UtcNow > lpClient.LastScriptExecuted;

            var itemScript = lpClient.Aisling.EquipmentManager.Weapon?.Item?.WeaponScript;
            if (itemScript != null && ready)
                itemScript.OnUse(lpClient.Aisling,
                    targets =>
                    {
                        lpClient.LastScriptExecuted =
                            DateTime.UtcNow.AddMilliseconds(ServerContext.Config.GlobalBaseSkillDelay);
                    });


            if ((lpClient.LastAssail - DateTime.UtcNow).TotalMilliseconds >
                ServerContext.Config.GlobalBaseSkillDelay) return;

            var lastTemplate = string.Empty;
            foreach (var skill in lpClient.Aisling.GetAssails())
            {
                if (skill == null)
                    continue;

                if (!skill.CanUse())
                    continue;

                if (skill.Template == null)
                    continue;

                if (skill.Script == null)
                    continue;

                if (skill.InUse)
                    continue;

                if (lastTemplate != skill.Template.Name)
                {
                    ExecuteAbility(lpClient, skill);
                    lastTemplate = skill.Template.Name;
                }
            }

            lpClient.LastAssail = DateTime.UtcNow;
        }

        // ExecuteAbility
        /// <summary>
        ///     Activates A <paramref name="lpSkill" /> for the provided <paramref name="lpClient" />
        ///     With an optional parameter <paramref name="optExecuteScript" />
        /// </summary>
        /// <returns>
        ///     void
        /// </returns>
        /// <example>
        ///     <code>
        /// foreach (var assail in client.Aisling.GetAssails())
        /// {
        ///     if (assail.Template.Name == skill.Template.Name)
        ///         continue;
        /// 
        ///     ExecuteAbility(client, assail, false);
        /// }
        /// </code>
        /// </example>
        /// <param name="lpClient">A valid GameClient</param>
        /// <param name="lpSkill">The Skill to Execute</param>
        /// <param name="optExecuteScript">
        ///     [Optional] Default: True, Execute the Script linked to <paramref name="lpSkill" />
        ///     set False to "Fake" the ability.
        /// </param>
        /// <seealso cref="ActivateAssails(GameClient)" />
        public static void ExecuteAbility(GameClient lpClient, Skill lpSkill, bool optExecuteScript = true)
        {
            lpSkill.InUse = true;

            if (optExecuteScript)
                lpSkill.Script.OnUse(lpClient.Aisling);


            if (lpSkill.Template.Cooldown > 0)
                lpSkill.NextAvailableUse = DateTime.UtcNow.AddSeconds(lpSkill.Template.Cooldown);
            else
                lpSkill.NextAvailableUse = DateTime.UtcNow.AddMilliseconds(ServerContext.Config.GlobalBaseSkillDelay);

            lpSkill.InUse = false;
        }

        /// <summary>
        ///     Enter Game
        /// </summary>
        private void EnterGame(GameClient client, ClientFormat10 format)
        {
            LoadPlayer(client, format);
        }

        private Aisling LoadPlayer(GameClient client, ClientFormat10 format)
        {
            var aisling = StorageManager.AislingBucket.Load(format.Name);

            if (aisling != null)
                client.Aisling = aisling;

            if (client.Aisling == null)
            {
                client.SendMessage(0x02, "Your have has been corrupted. Please report this bug to lorule staff.");
                base.ClientDisconnected(client);
                return null;
            }

            if (client.Aisling._Str <= 0 || client.Aisling.Ac > 200 || client.Aisling.ExpLevel > 99)
            {
                client.SendMessage(0x02, "Your have has been corrupted. Please report this bug to lorule staff.");
                base.ClientDisconnected(client);
                return null;
            }


            client.Encryption.Parameters = format.Parameters;
            client.Server = this;

            lock (Generator.Random)
            {
                client.Aisling.Serial = Generator.GenerateNumber();
            }

            client.Aisling.Client = client;
            client.Aisling.LoggedIn = false;

            client.Aisling.LastLogged = DateTime.UtcNow;
            client.LastScriptExecuted = DateTime.UtcNow;

            client.Aisling.CurrentMapId = client.Aisling.Map.ID;
            client.Aisling.EquipmentManager.Client = client;
            client.Aisling.CurrentWeight = 0;
            client.Aisling.ActiveStatus = ActivityStatus.Awake;
            client.Aisling.InvitePrivleges = true;
            client.Aisling.LeaderPrivleges = false;

            Party.Reform(client);

            return client.Load()
                .SendStats(StatusFlags.All)
                .SendMessage(0x02, ServerContext.Config.ServerWelcomeMessage)
                .EnterArea()
                .LoggedIn(true).Aisling;
        }

        /// <summary>
        ///     Leave Game
        /// </summary>
        private void LeaveGame(GameClient client, ClientFormat0B format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            #endregion

            if (format.Type == 0) ExitGame(client);

            if (format.Type == 1)
            {
                client.FlushAndSend(new ServerFormat4C());
            }
            else if (format.Type == 3)
            {
                client.LastSave = DateTime.UtcNow;
                client.Aisling.Remove();
            }
        }

        public void ExitGame(GameClient client)
        {
            var redirect = new Redirect
            {
                Serial = Convert.ToString(client.Serial, CultureInfo.CurrentCulture),
                Salt = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
                Seed = Convert.ToString(client.Encryption.Parameters.Seed, CultureInfo.CurrentCulture),
                Name = client.Aisling.Username,
                Type = "2"
            };

            client.Aisling.LoggedIn = false;

            if ((DateTime.UtcNow - client.LastSave).TotalSeconds > 2)
            {
                client.Save();
            }

            client.Aisling.Remove(true);

            client.FlushAndSend(new ServerFormat03
            {
                EndPoint = new IPEndPoint(Address, 2610),
                Redirect = redirect
            });
            client.FlushAndSend(new ServerFormat02(0x00, "\0"));
        }

        private static void SendMapData(GameClient client)
        {
            for (var i = 0; i < client.Aisling.Map.Rows; i++)
            {
                var response = new ServerFormat3C
                {
                    Line = (ushort) i,
                    Data = client.Aisling.Map.GetRowData(i)
                };
                client.Send(response);
            }

            client.Aisling.Map.OnLoaded();
        }

        /// <summary>
        ///     Request Map
        /// </summary>
        protected override void Format05Handler(GameClient client, ClientFormat05 format)
        {
            if (client?.Aisling?.Map == null)
                return;

            if (client.MapUpdating || client.Aisling.CurrentMapId == ServerContext.Config.TransitionZone)
            {
                SendMapData(client);
                client.MapUpdating = false;
            }
        }

        /// <summary>
        ///     Settings Requested
        /// </summary>
        protected override void Format1BHandler(GameClient client, ClientFormat1B format)
        {
            var settingKeys = client.Aisling.GameSettings.ToArray();

            if (settingKeys.Length == 0)
                return;

            var settingIdx = format.Index;

            if (settingIdx > 0)
            {
                settingIdx--;

                if (settingIdx < 0)
                    return;

                var setting = settingKeys[settingIdx];
                setting.Toggle();

                UpdateSettings(client);
            }
            else
            {
                UpdateSettings(client);
            }
        }

        public void UpdateSettings(GameClient client)
        {
            var msg = "\t";
            foreach (var setting in client.Aisling.GameSettings)
            {
                msg += setting.Enabled ? setting.EnabledSettingStr : setting.DisabledSettingStr;
                msg += "\t";
            }

            client.SendMessage(0x07, msg);
        }

        /// <summary>
        ///     Sprite Walk
        /// </summary>
        protected override void Format06Handler(GameClient client, ClientFormat06 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Map == null)
                return;

            if (!client.Aisling.Map.Ready)
                return;

            if (client.IsWarping)
                return;

            if (client.WasUpdatingMapRecently)
            {
                client.Refresh();
                return;
            }

            if (ServerContext.Config.CancelCastingWhenWalking && client.Aisling.IsCastingSpell ||
                client.Aisling.ActiveSpellInfo != null)
                CancelIfCasting(client);

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen || client.Aisling.IsParalyzed)
            {
                client.SendLocation();
                client.UpdateDisplay();
                return;
            }

            client.Aisling.CanReact = true;
            client.MenuInterpter = null;

            if (client.Aisling.Skulled)
            {
                if (!ServerContext.Config.CanMoveDuringReap)
                    client.SendLocation();

                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }


            if (client.IsRefreshing && ServerContext.Config.CancelWalkingIfRefreshing)
                return;

            if (client.Aisling.Direction != format.Direction)
                client.Aisling.Direction = format.Direction;


            client.Aisling.Walk();
            client.LastMovement = DateTime.UtcNow;

            if (client.Aisling.AreaID == ServerContext.Config.TransitionZone)
            {
                client.Aisling.PortalSession = new PortalSession { IsMapOpen = false };
                client.Aisling.PortalSession.TransitionToMap(client);
                return;
            }

            CheckWalkOverPopups  (client);
            CheckWarpTransitions (client);
        }

        private static void CheckWarpTransitions(GameClient client)
        {
            foreach (var warps in ServerContext.GlobalWarpTemplateCache)
            {
                if (warps.ActivationMapId != client.Aisling.CurrentMapId)
                    continue;

                foreach (var o in warps.Activations)
                    if (o.Location.DistanceFrom(client.Aisling.Position) <= warps.WarpRadius)
                    {
                        if (warps.WarpType == WarpType.Map)
                        {
                            client.WarpTo(warps);
                            break;
                        }
                        else if (warps.WarpType == WarpType.World)
                        {
                            if (!ServerContext.GlobalWorldMapTemplateCache.ContainsKey(warps.To.PortalKey))
                                return;

                            client.Aisling.PortalSession = new PortalSession
                            {
                                FieldNumber = warps.To.PortalKey
                            };


                            if (client.Aisling.World != warps.To.PortalKey)
                            {
                                ServerContext.Log("Transition for {0}: World: {1}  / {2}", client.Aisling.Username, client.Aisling.World, o.PortalKey);
                                client.Aisling.World = warps.To.PortalKey;
                            }

                            ServerContext.Log("{0}: World: {1}  / {2}", client.Aisling.Username, client.Aisling.World, o.PortalKey);
                            client.Aisling.PortalSession.TransitionToMap(client);

                            break;

                        }
                    }
            }
        }

        private void CheckWalkOverPopups(GameClient client)
        {
            var popupTemplates = ServerContext.GlobalPopupCache
                .OfType<UserWalkPopup>().Where(i => i.MapId == client.Aisling.CurrentMapId);

            foreach (var popupTemplate in popupTemplates)
            {
                if (popupTemplate != null && client.Aisling.X == popupTemplate.X && client.Aisling.Y == popupTemplate.Y)
                {
                    popupTemplate.SpriteId = popupTemplate.SpriteId;

                    var popup = Popup.Create(client, popupTemplate);

                    if (popup != null)
                    {
                        if (client.MenuInterpter == null)
                        {
                            CreateInterpreterFromMenuFile(client, popup.Template.YamlKey);

                            if (client.MenuInterpter != null)
                            {
                                if (client.MenuInterpter != null)
                                {
                                    client.MenuInterpter.Start();

                                    var next = client.MenuInterpter?.GetCurrentStep();

                                    if (next != null)
                                        client.ShowCurrentMenu(popup, null, next);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Pickup Item / Gold (User Pressed B)
        /// </summary>
        protected override void Format07Handler(GameClient client, ClientFormat07 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion


            var objs = GetObjects(client.Aisling.Map, i => i.XPos == format.Position.X && i.YPos == format.Position.Y,
                Get.Items | Get.Money);

            if (objs == null)
                return;

            foreach (var obj in objs.Reverse())
            {
                if (obj?.CurrentMapId != client.Aisling.CurrentMapId)
                    continue;

                if (!(client.Aisling.Position.DistanceFrom(obj.Position) <= ServerContext.Config.ClickLootDistance))
                    continue;

                if (obj is Money money)
                    money.GiveTo(money.Amount, client.Aisling);
                else if (obj is Item item)
                {
                    if (item.Cursed)
                    {
                        if (item.AuthenticatedAislings.FirstOrDefault(i =>
                                i.Serial == client.Aisling.Serial) == null)
                        {
                            client.SendMessage(0x02, ServerContext.Config.CursedItemMessage);
                            break;
                        }

                        if (item.GiveTo(client.Aisling))
                        {
                            item.Remove<Item>();
                            break;
                        }

                        item.XPos = client.Aisling.XPos;
                        item.YPos = client.Aisling.YPos;
                        item.Show(Scope.NearbyAislings, new ServerFormat07(new[] {obj}));
                        break;
                    }

                    if (item.GiveTo(client.Aisling))
                    {
                        item.Remove<Item>();

                        var popupTemplate = ServerContext.GlobalPopupCache
                            .OfType<ItemPickupPopup>().FirstOrDefault(i => i.ItemName == (obj as Item)?.Template.Name);

                        if (popupTemplate != null && client.Aisling.ActiveReactors.ContainsKey(popupTemplate.YamlKey))
                            if (item.Owner == client.Aisling.Serial)
                            {
                                popupTemplate.SpriteId = item.Template.DisplayImage;

                                var popup = Popup.Create(client, popupTemplate);

                                if (popup != null)
                                    if (client.MenuInterpter == null)
                                    {
                                        CreateInterpreterFromMenuFile(client, popup.Template.YamlKey);

                                        if (client.MenuInterpter != null)
                                        {
                                            client.MenuInterpter.Start();
                                            client.ShowCurrentMenu(popup, null, client.MenuInterpter.GetCurrentStep());
                                        }
                                    }
                            }

                        break;
                    }

                    item.XPos = client.Aisling.XPos;
                    item.YPos = client.Aisling.YPos;
                    item.Show(Scope.NearbyAislings, new ServerFormat07(new[] {obj}));
                    break;
                }
            }
        }

        /// <summary>
        ///     Drop Item
        /// </summary>
        protected override void Format08Handler(GameClient client, ClientFormat08 format)
        {
            #region Sanity Checks (alot can go wrong if you remove this)

            if (client == null || client.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Map == null || !client.Aisling.Map.Ready)
                return;

            #endregion

            //do we have an item in this slot?
            var item = client.Aisling.Inventory.Get(i => i != null && i.Slot == format.ItemSlot).FirstOrDefault();

            //check if it's also a valid item?
            if (item?.Template == null)
                return;

            if (!item.Template.Flags.HasFlag(ItemFlags.Dropable))
            {
                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDropItemMsg);
                return;
            }

            var popupTemplate = ServerContext.GlobalPopupCache
                .OfType<ItemDropPopup>().FirstOrDefault(i => i.ItemName == item.Template.Name);

            if (popupTemplate != null && client.Aisling.ActiveReactors.ContainsKey(popupTemplate.YamlKey))
            {
                popupTemplate.SpriteId = item.Template.DisplayImage;

                var popup = Popup.Create(client, popupTemplate);

                if (popup != null)
                    if (client.MenuInterpter == null)
                    {
                        CreateInterpreterFromMenuFile(client, popup.Template.YamlKey);

                        if (client.MenuInterpter != null)
                        {
                            client.MenuInterpter.Start();
                            client.ShowCurrentMenu(popup, null, client.MenuInterpter.GetCurrentStep());
                        }
                    }
            }

            var item_position = new Position(format.X, format.Y);

            if (client.Aisling.Position.DistanceFrom(item_position.X, item_position.Y) > 2)
            {
                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDoThat);
                return;
            }

            //check position is available to drop.
            if (client.Aisling.Map.IsWall(client.Aisling, format.X, format.Y))
                if (client.Aisling.XPos != format.X || client.Aisling.YPos != format.Y)
                {
                    client.SendMessage(Scope.Self, 0x02, ServerContext.Config.CantDoThat);
                    return;
                }

            //if this item a stackable item?
            if ((item.Template.Flags & ItemFlags.Stackable) == ItemFlags.Stackable)
            {
                //remaining?
                var remaining = item.Stacks - format.ItemAmount;

                if (remaining <= 0) //none remaining / remove the item from inventory.
                {
                    //clone the item.
                    var copy = Clone<Item>(item);

                    //remove from inventory and release to world.
                    if (client.Aisling.EquipmentManager.RemoveFromInventory(item, true))
                    {
                        copy.Release(client.Aisling, new Position(format.X, format.Y));
                        client.SendStats(StatusFlags.StructA);
                    }
                }
                else // some remain, update inventory item.
                {
                    //clone and release item
                    var nitem = Clone<Item>(item);
                    nitem.Stacks = (byte) format.ItemAmount;
                    nitem.Release(client.Aisling, new Position(format.X, format.Y));

                    item.Stacks = (byte) remaining;
                    client.Aisling.Inventory.Set(item, false);

                    //send remove packet.
                    client.Send(new ServerFormat10(item.Slot));
                    //add it again with updated information.
                    client.Send(new ServerFormat0F(item));
                }
            }
            else // not stackable.
            {
                //clone item
                var copy = Clone<Item>(item);

                //remove from inventory
                if (client.Aisling.EquipmentManager.RemoveFromInventory(item, true))
                    //release the item back to the world.
                    copy.Release(client.Aisling, new Position(format.X, format.Y));
            }
        }

        /// <summary>
        ///     Log Out
        /// </summary>
        protected override void Format0BHandler(GameClient client, ClientFormat0B format)
        {
            LeaveGame(client, format);
        }

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="format"></param>
        protected override void Format0EHandler(GameClient client, ClientFormat0E format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            #endregion

            var response = new ServerFormat0D
            {
                Serial = client.Aisling.Serial,
                Type = format.Type,
                Text = string.Empty
            };

            IEnumerable<Aisling> audience;

            switch (format.Type)
            {
                case 0x00:
                    response.Text = $"{client.Aisling.Username}: {format.Text}";
                    audience      = client.GetObjects<Aisling>(client.Aisling.Map,
                                  n => client.Aisling.WithinRangeOf(n, false));
                    break;
                case 0x01:
                    response.Text = $"{client.Aisling.Username}! {format.Text}";
                    audience      = client.GetObjects<Aisling>(client.Aisling.Map,
                                  n => client.Aisling.CurrentMapId == n.CurrentMapId);
                    break;
                case 0x02:
                    response.Text = format.Text;
                    audience      = client.GetObjects<Aisling>(client.Aisling.Map,
                                  n => client.Aisling.WithinRangeOf(n, false));
                    break;
                default:
                    ClientDisconnected(client);
                    return;
            }

            var nearbyMundanes = client.Aisling.MundanesNearby();

            foreach (var npc in nearbyMundanes)
                npc?.Script?.OnGossip(this, client, format.Text);

            client.Aisling.Show(Scope.DefinedAislings, response, audience.ToArray());
        }

        /// <summary>
        ///     Use Spell
        /// </summary>
        protected override void Format0FHandler(GameClient client, ClientFormat0F format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            try
            {
                var spellReq = client.Aisling.SpellBook.Get(i => i != null && i.Slot == format.Index).FirstOrDefault();

                if (spellReq == null)
                    return;

                if (client.Aisling.IsSleeping ||
                    client.Aisling.IsFrozen && !(spellReq.Template.Name == "ao suain"))
                {
                    CancelIfCasting(client);
                    return;
                }

                if (client.Aisling.ActiveSpellInfo != null)
                {
                    client.Aisling.ActiveSpellInfo.Slot = format.Index;
                    client.Aisling.ActiveSpellInfo.Target = format.Serial;
                    client.Aisling.ActiveSpellInfo.Position = format.Point;
                    client.Aisling.ActiveSpellInfo.Data = format.Data;

                    var spell = client.Aisling.SpellBook
                        .Get(i => i != null && i.Slot == client.Aisling.ActiveSpellInfo.Slot)
                        .FirstOrDefault();

                    client.Aisling.IsCastingSpell = true;
                    client.Aisling.CastSpell(spell);
                }
                else
                {
                    client.Aisling.ActiveSpellInfo = new CastInfo
                    {
                        Position = format.Point,
                        Slot = format.Index,
                        SpellLines = 0,
                        Started = DateTime.UtcNow,
                        Target = format.Serial,
                        Data = format.Data
                    };

                    var spell = client.Aisling.SpellBook.Get(i =>
                        i != null
                        && i.Slot == format.Index).FirstOrDefault();

                    if (spell?.Script == null)
                        return;
                    if (spell.Template == null)
                        return;

                    client.Aisling.IsCastingSpell = true;
                    client.Aisling.CastSpell(spell);
                }
            }
            finally
            {
                CancelIfCasting(client);
            }
        }

        private uint GetSpellTarget(GameClient client, ClientFormat0F format)
        {
            var obj = GetObject(client.Aisling.Map, i => i.Serial == format.Serial, Get.Monsters | Get.Aislings);

            if (obj != null && obj.SpellReflect)
            {                
                var n = Generator.Random.Next(100) > 30;  // 70% chance to reflect a spell

                if (n)
                {
                    return (uint)obj.Serial;
                }
            }

            return format.Serial;
        }

        /// <summary>
        ///     Enter Game
        /// </summary>
        protected override void Format10Handler(GameClient client, ClientFormat10 format)
        {
            #region Sanity Checks

            if (client == null)
                return;

            #endregion

            EnterGame(client, format);
        }

        /// <summary>
        ///     Sprite Turn
        /// </summary>
        protected override void Format11Handler(GameClient client, ClientFormat11 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing && ServerContext.Config.DontTurnDuringRefresh)
                return;

            #endregion

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            if (client.Aisling.Direction != format.Direction)
            {
                client.Aisling.Direction = format.Direction;

                client.Aisling.Show(Scope.NearbyAislings, new ServerFormat11
                {
                    Direction = client.Aisling.Direction,
                    Serial = client.Aisling.Serial
                });
            }
        }

        /// <summary>
        ///     SpaceBar
        /// </summary>
        protected override void Format13Handler(GameClient client, ClientFormat13 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            ActivateAssails(client);
        }

        /// <summary>
        ///     User List
        /// </summary>
        protected override void Format18Handler(GameClient client, ClientFormat18 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            #endregion

            client.Aisling.Show(Scope.Self, new ServerFormat36(client));
        }

        protected override void Format19Handler(GameClient client, ClientFormat19 format)
        {
            if (client == null || client.Aisling == null)
                return;

            if (format == null)
                return;

            if (DateTime.UtcNow.Subtract(client.LastWhisperMessageSent).TotalSeconds < 0.30)
                return;

            if (format.Name.Length > 24)
                return;

            client.LastWhisperMessageSent = DateTime.UtcNow;

            var user = Clients.FirstOrDefault(i =>
                i != null && i.Aisling != null && i.Aisling.LoggedIn && i.Aisling.Username.ToLower() ==
                format.Name.ToLower(CultureInfo.CurrentCulture));

            if (user == null)
                client.SendMessage
                    (0x02, string.Format(CultureInfo.CurrentCulture, "{0} is nowhere to be found.", format.Name));

            if (user != null)
            {
                user.SendMessage
                (0x00,
                    string.Format(CultureInfo.CurrentCulture, "{0}\" {1}", client.Aisling.Username, format.Message));
                client.SendMessage
                (0x00,
                    string.Format(CultureInfo.CurrentCulture, "{0}> {1}", user.Aisling.Username, format.Message));
            }
        }

        /// <summary>
        ///     Use Item
        /// </summary>
        protected override void Format1CHandler(GameClient client, ClientFormat1C format)
        {
            #region Sanity Checks (alot can go wrong if you remove this)

            if (client == null || client.Aisling == null)
                return;

            if (client.Aisling.Map == null || !client.Aisling.Map.Ready)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Dead)
                return;

            #endregion

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            var slot = format.Index;
            var item = client.Aisling.Inventory.Get(i => i != null && i.Slot == slot).FirstOrDefault();

            if (item == null)
                return;

            client.LastActivatedLost = slot;

            var activated = false;

            if (item.Template != null)
            {
                if (!string.IsNullOrEmpty(item?.Template?.MiniScript))
                {
                    item.Template.RunMiniScript(client);
                    activated = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.Template.ScriptName))
                        if (item.Script == null)
                            item.Script = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);

                    if (!string.IsNullOrEmpty(item.Template.WeaponScript))
                        if (item.WeaponScript == null)
                            item.WeaponScript = ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);

                    if (item.Script == null)
                    {
                        client.SendMessage(0x02, ServerContext.Config.CantUseThat);
                    }
                    else
                    {
                        item.Script.OnUse(client.Aisling, slot);
                        activated = true;
                    }
                }

                if (activated)
                    if (item.Template.Flags.HasFlag(ItemFlags.Stackable))
                        if (item.Template.Flags.HasFlag(ItemFlags.Consumable))
                        {
                            var stack = item.Stacks - 1;

                            if (stack > 0)
                            {
                                //consume 1 unit, update stack and refresh item in inventory.
                                item.Stacks -= 1;
                                client.Aisling.Inventory.Set(item, false);

                                //send remove packet.
                                client.Send(new ServerFormat10(item.Slot));

                                //add it again with updated information.
                                client.Send(new ServerFormat0F(item));
                            }
                            else
                            {
                                //remove from inventory
                                client.Aisling.Inventory.Remove(item.Slot);
                                client.Send(new ServerFormat10(item.Slot));
                            }
                        }
            }
        }


        /// <summary>
        ///     Emotions
        /// </summary>
        protected override void Format1DHandler(GameClient client, ClientFormat1D format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.Aisling.IsDead())
                return;

            #endregion

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            var id = format.Number;

            if (id > 35)
                return;

            client.Aisling.Show(Scope.NearbyAislings,
                new ServerFormat1A(client.Aisling.Serial, (byte) (id + 9), 64));
        }

        /// <summary>
        ///     Drop Gold
        /// </summary>
        protected override void Format24Handler(GameClient client, ClientFormat24 format)
        {
            if (client?.Aisling == null)
                return;

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            if (client.Aisling.GoldPoints >= format.GoldAmount)
            {
                client.Aisling.GoldPoints -= format.GoldAmount;
                if (client.Aisling.GoldPoints <= 0)
                    client.Aisling.GoldPoints = 0;

                client.SendMessage(Scope.Self, 0x02, ServerContext.Config.YouDroppedGoldMsg);
                client.SendMessage(Scope.NearbyAislingsExludingSelf, 0x02,
                    ServerContext.Config.UserDroppedGoldMsg.Replace("noname", client.Aisling.Username));
                Money.Create(client.Aisling, format.GoldAmount, new Position(format.X, format.Y));
                client.SendStats(StatusFlags.StructC);
            }
            else
            {
                client.SendMessage(0x02, ServerContext.Config.NotEnoughGoldToDropMsg);
            }
        }

        protected override void Format29Handler(GameClient client, ClientFormat29 format)
        {
            client.Send(new ServerFormat4B(format.ID, 0));
            client.Send(new ServerFormat4B(format.ID, 1, format.ItemSlot));
        }

        protected override void Format2AHandler(GameClient client, ClientFormat2A format)
        {
        }

        /// <summary>
        ///     Get Profile
        /// </summary>
        protected override void Format2DHandler(GameClient client, ClientFormat2D format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            #endregion

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            client.Aisling.ProfileOpen = true;
            client.Send(new ServerFormat39(client.Aisling));
        }

        /// <summary>
        ///     Grouping
        /// </summary>
        protected override void Format2EHandler(GameClient client, ClientFormat2E format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (format.Type != 0x02)
                return;

            var player = GetObject<Aisling>(client.Aisling.Map, i =>
                i.Username.ToLower(CultureInfo.CurrentCulture) == format.Name
                && i.WithinRangeOf(client.Aisling));

            if (player == null)
            {
                client.SendMessage(0x02, ServerContext.Config.BadRequestMessage);
                return;
            }

            //does player have group open?
            if (player.PartyStatus != GroupStatus.AcceptingRequests)
            {
                client.SendMessage(0x02,
                    ServerContext.Config.GroupRequestDeclinedMsg.Replace("noname", player.Username));
                return;
            }

            if (client.Aisling.GroupParty.RequestUserToJoin(player))
            {
                client.Aisling.LeaderPrivleges = true;
                player.InvitePrivleges = true;
            }
        }

        protected override void Format2FHandler(GameClient client, ClientFormat2F format)
        {
            var mode = client.Aisling.PartyStatus;

            if (mode == GroupStatus.AcceptingRequests)
                mode = GroupStatus.NotAcceptingRequests;
            else if (mode == GroupStatus.NotAcceptingRequests)
                mode = GroupStatus.AcceptingRequests;

            client.Aisling.PartyStatus = mode;

            if (client.Aisling.PartyStatus == GroupStatus.NotAcceptingRequests)
                if (client.Aisling.LeaderPrivleges)
                {
                    Party.DisbandParty(client.Aisling.GroupParty);
                }
                else
                {
                    Party.WithDrawFromParty(client);
                    Party.Reform(client);
                }
        }


        protected override void Format32Handler(GameClient client, ClientFormat32 format)
        {
        }

        /// <summary>
        ///     Moving Slot
        /// </summary>
        protected override void Format30Handler(GameClient client, ClientFormat30 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen || client.Aisling.IsCastingSpell)
            {
                client.Interupt();
                return;
            }

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            switch (format.PaneType)
            {
                case Pane.Inventory:
                {
                    if (format.MovingTo - 1 > client.Aisling.Inventory.Length)
                        return;
                    if (format.MovingFrom - 1 > client.Aisling.Inventory.Length)
                        return;
                    if (format.MovingTo - 1 < 0)
                        return;
                    if (format.MovingFrom - 1 < 0)
                        return;

                    client.Send(new ServerFormat10(format.MovingFrom));
                    client.Send(new ServerFormat10(format.MovingTo));

                    var a = client.Aisling.Inventory.Remove(format.MovingFrom);
                    var b = client.Aisling.Inventory.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat0F(a));
                        client.Aisling.Inventory.Set(a, false);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat0F(b));
                        client.Aisling.Inventory.Set(b, false);
                    }
                }
                    break;
                case Pane.Skills:
                {
                    if (format.MovingTo - 1 > client.Aisling.SkillBook.Length)
                        return;
                    if (format.MovingFrom - 1 > client.Aisling.SkillBook.Length)
                        return;
                    if (format.MovingTo - 1 < 0)
                        return;
                    if (format.MovingFrom - 1 < 0)
                        return;

                    client.Send(new ServerFormat2D(format.MovingFrom));
                    client.Send(new ServerFormat2D(format.MovingTo));

                    var a = client.Aisling.SkillBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SkillBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat2C(a.Slot, a.Icon, a.Name));
                        client.Aisling.SkillBook.Set(a, false);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat2C(b.Slot, b.Icon, b.Name));
                        client.Aisling.SkillBook.Set(b, false);
                    }
                }
                    break;
                case Pane.Spells:
                {
                    if (format.MovingTo - 1 > client.Aisling.SpellBook.Length)
                        return;
                    if (format.MovingFrom - 1 > client.Aisling.SpellBook.Length)
                        return;
                    if (format.MovingTo - 1 < 0)
                        return;
                    if (format.MovingFrom - 1 < 0)
                        return;

                    client.Send(new ServerFormat18(format.MovingFrom));
                    client.Send(new ServerFormat18(format.MovingTo));

                    var a = client.Aisling.SpellBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SpellBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat17(a));
                        client.Aisling.SpellBook.Set(a, false);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat17(b));
                        client.Aisling.SpellBook.Set(b, false);
                    }
                }
                    break;
                case Pane.Tools:
                {
                    if (format.MovingTo - 1 > client.Aisling.SpellBook.Length)
                        return;
                    if (format.MovingFrom - 1 > client.Aisling.SpellBook.Length)
                        return;
                    if (format.MovingTo - 1 < 0)
                        return;
                    if (format.MovingFrom - 1 < 0)
                        return;

                    client.Send(new ServerFormat18(format.MovingFrom));
                    client.Send(new ServerFormat18(format.MovingTo));

                    var a = client.Aisling.SpellBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SpellBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat17(a));
                        client.Aisling.SpellBook.Set(a, false);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat17(b));
                        client.Aisling.SpellBook.Set(b, false);
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Refresh
        /// </summary>
        protected override void Format38Handler(GameClient client, ClientFormat38 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            //if (client.IsRefreshing)
            //    return;

            #endregion


            client.LeaveArea(true, false);
            client.EnterArea();

            client.LastClientRefresh = DateTime.UtcNow;
        }

        /// <summary>
        ///     Dialogs A
        /// </summary>
        protected override void Format39Handler(GameClient client, ClientFormat39 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);

            #endregion

                if (client.Aisling.Skulled)
                {
                    client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                    return;
                }

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }


            var objId = (uint) format.Serial;

            if (objId > 0 && objId < int.MaxValue)
            {
                var obj = GetObject<Mundane>(client.Aisling.Map, i => i.Serial == objId);

                if (obj != null)
                {
                    var menu = client.MenuInterpter;
                    if (menu != null)
                    {
                        var selected_answer = menu.GetCurrentStep()?.Answers.ElementAt(format.Step - 1);

                        if (selected_answer != null)
                        {
                            client.ShowCurrentMenu(obj, null, menu.Move(selected_answer.Id));
                        }
                    }
                }
                else
                {
                    var popup = Popup.GetById(objId);

                    if (popup != null)
                    {
                        var menu = client.MenuInterpter;

                        if (menu != null)
                        {
                            var selected_answer = menu.GetCurrentStep()?.Answers.ElementAt(format.Step - 1);
                            if (selected_answer != null)
                            {
                                client.ShowCurrentMenu(popup, null, menu.Move(selected_answer.Id));
                                return;
                            }
                        }
                    }
                }
            }


            if (format.Serial != ServerContext.Config.HelperMenuId)
            {
                var mundane = GetObject<Mundane>(client.Aisling.Map, i => i.Serial == format.Serial);
                mundane?.Script?.OnResponse(this, client, format.Step, format.Args);
            }
            else
            {
                if (format.Serial == ServerContext.Config.HelperMenuId &&
                    ServerContext.GlobalMundaneTemplateCache.ContainsKey(ServerContext.Config.HelperMenuTemplateKey))
                {
                    if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                        return;

                    var helper = new UserHelper(this, new Mundane
                    {
                        Serial = ServerContext.Config.HelperMenuId,
                        Template = ServerContext.GlobalMundaneTemplateCache[ServerContext.Config.HelperMenuTemplateKey]
                    });

                    client.SendSound(12);
                    helper.OnResponse(this, client, format.Step, format.Args);
                }
            }
        }


        /// <summary>
        ///     Dialogs B
        /// </summary>
        protected override void Format3AHandler(GameClient client, ClientFormat3A format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);

            #endregion

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }


            if (format.Step == 0 && format.ScriptId == ushort.MaxValue)
            {
                client.CloseDialog();
                return;
            }

            var objId = format.Serial;

            if (objId > 0 && objId < int.MaxValue)
            {
                var obj = GetObject<Mundane>(client.Aisling.Map, i => i.Serial == objId);

                if (obj != null)
                {
                    if (client.MenuInterpter == null)
                        return;

                    var interpreter = client.MenuInterpter;

                    if (format.Step > 2)
                    {
                        var back = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == "back");

                        if (back != null)
                            client.ShowCurrentMenu(obj, interpreter.GetCurrentStep(), interpreter.Move(back.Id));
                        else
                            client.CloseDialog();
                    }

                    if (format.Step == 1)
                    {
                        var next = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == "next");

                        if (next != null)
                        {
                            client.ShowCurrentMenu(obj, interpreter.GetCurrentStep(), interpreter.Move(next.Id));
                        }
                        else
                        {
                            var complete = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == null);

                            if (complete != null)
                            {
                                client.ShowCurrentMenu(obj, null, interpreter.Move(complete.Id));
                            }
                            else
                            {
                                var last = interpreter.GetCurrentStep().Answers
                                    .FirstOrDefault(i => i.Text == "complete");
                                if (last != null) client.ShowCurrentMenu(obj, null, interpreter.Move(last.Id));
                            }
                        }
                    }

                    if (format.Step < 1 || format.Step == 2)
                    {
                        var step = interpreter.GetCurrentStep();

                        if (step == null) return;

                        var close = step.Answers.FirstOrDefault(i => i.Text == "close");

                        if (close != null) client.CloseDialog();
                    }

                    return;
                }

                var popup = Popup.GetById(objId);

                if (popup != null)
                {
                    if (client.MenuInterpter == null)
                        return;

                    var interpreter = client.MenuInterpter;

                    if (format.Step > 2)
                    {
                        var back = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == "back");

                        if (back != null)
                            client.ShowCurrentMenu(popup, interpreter.GetCurrentStep(), interpreter.Move(back.Id));
                        else
                            client.CloseDialog();
                    }

                    if (format.Step == 1)
                    {
                        var next = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == "next");

                        if (next != null)
                        {
                            client.ShowCurrentMenu(popup, interpreter.GetCurrentStep(), interpreter.Move(next.Id));
                        }
                        else
                        {
                            var complete = interpreter.GetCurrentStep().Answers.FirstOrDefault(i => i.Text == null);

                            if (complete != null)
                            {
                                client.ShowCurrentMenu(popup, null, interpreter.Move(complete.Id));
                            }
                            else
                            {
                                var last = interpreter.GetCurrentStep().Answers
                                    .FirstOrDefault(i => i.Text == "complete");
                                if (last != null) client.ShowCurrentMenu(popup, null, interpreter.Move(last.Id));
                            }
                        }
                    }

                    if (format.Step < 1 || format.Step == 2)
                    {
                        var step = interpreter.GetCurrentStep();

                        if (step == null) return;

                        var close = step.Answers.FirstOrDefault(i => i.Text == "close");

                        if (close != null) client.CloseDialog();
                    }
                }
            }


            if (format.ScriptId == ushort.MaxValue)
            {
                if (client.Aisling.ActiveReactor == null || client.Aisling.ActiveReactor.Decorator == null)
                    return;

                switch (format.Step)
                {
                    case 0:
                        client.Aisling.ActiveReactor.Decorator.OnClose(client.Aisling);
                        break;
                    case 255:
                        client.Aisling.ActiveReactor.Decorator.OnBack(client.Aisling);
                        break;
                    case 0xFFFF:
                        client.Aisling.ActiveReactor.Decorator.OnBack(client.Aisling);
                        break;
                    case 2:
                        client.Aisling.ActiveReactor.Decorator.OnClose(client.Aisling);
                        break;
                    case 1:
                        client.Aisling.ActiveReactor.Decorator.OnNext(client.Aisling);
                        break;
                }
            }
            else
            {
                client.DlgSession?.Callback?.Invoke(this, client, format.Step, string.Empty);
            }
        }

        /// <summary>
        ///     Board Handling
        /// </summary>
        protected override void Format3BHandler(GameClient client, ClientFormat3B format)
        {
            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            try
            {
                if (format.Type == 0x01)
                {
                    client.Send(new BoardList(ServerContext.Community));
                    return;
                }

                if (format.Type == 0x02)
                {
                    if (format.BoardIndex == 0)
                    {
                        var clone = Clone<Board>(ServerContext.Community[format.BoardIndex]);
                        {
                            clone.Client = client;
                            client.Send(clone);
                        }
                        return;
                    }

                    var boards = ServerContext.GlobalBoardCache.Select(i => i.Value)
                        .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                        .FirstOrDefault();

                    if (boards != null)
                        client.Send(boards);


                    return;
                }

                if (format.Type == 0x03)
                {
                    var index = format.TopicIndex - 1;

                    var boards = ServerContext.GlobalBoardCache.Select(i => i.Value)
                        .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                        .FirstOrDefault();

                    if (boards != null &&
                        boards.Posts.Count > index)
                    {
                        var post = boards.Posts[index];
                        client.Send(post);
                        return;
                    }

                    client.Send(new ForumCallback("Unable to retrieve more.", 0x06, true));
                    return;
                }

                if (format.Type == 0x06)
                {
                    var boards = ServerContext.GlobalBoardCache.Select(i => i.Value)
                        .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                        .FirstOrDefault();

                    if (boards != null)
                    {
                        var np = new PostFormat(format.BoardIndex, format.TopicIndex)
                        {
                            DatePosted = DateTime.UtcNow,
                            Message = format.Message,
                            Subject = format.Title,
                            Read = false,
                            Sender = client.Aisling.Username,
                            Recipient = format.To,
                            PostId = (ushort) (boards.Posts.Count + 1)
                        };

                        np.Associate(client.Aisling.Username);
                        boards.Posts.Add(np);
                        ServerContext.SaveCommunityAssets();
                        client.Send(new ForumCallback("Message Delivered.", 0x06, true));
                    }

                    return;
                }

                if (format.Type == 0x04)
                {
                    var boards = ServerContext.GlobalBoardCache.Select(i => i.Value)
                        .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                        .FirstOrDefault();

                    if (boards != null)
                    {
                        var np = new PostFormat(format.BoardIndex, format.TopicIndex)
                        {
                            DatePosted = DateTime.UtcNow,
                            Message = format.Message,
                            Subject = format.Title,
                            Read = false,
                            Sender = client.Aisling.Username,
                            PostId = (ushort) (boards.Posts.Count + 1)
                        };

                        np.Associate(client.Aisling.Username);


                        boards.Posts.Add(np);
                        ServerContext.SaveCommunityAssets();
                        client.Send(new ForumCallback("Post Added.", 0x06, true));
                    }

                    return;
                }

                if (format.Type == 0x05)
                {
                    var community = ServerContext.GlobalBoardCache.Select(i => i.Value)
                        .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                        .FirstOrDefault();

                    if (community != null && community.Posts.Count > 0)
                    {
                        if ((format.BoardIndex == 0
                                ? community.Posts[format.TopicIndex - 1].Recipient
                                : community.Posts[format.TopicIndex - 1].Sender
                            ).Equals(client.Aisling.Username, StringComparison.OrdinalIgnoreCase))
                        {
                            community.Posts.RemoveAt(format.TopicIndex - 1);
                            ServerContext.SaveCommunityAssets();
                            client.Send(new ForumCallback("Post has been deleted.", 0x07, true));
                            return;
                        }

                        client.Send(new ForumCallback(ServerContext.Config.CantDoThat, 0x07, true));
                        return;
                    }

                    client.Send(new ForumCallback(ServerContext.Config.CantDoThat, 0x07, true));
                }
            }
            catch (Exception e)
            {
                ServerContext.Report(e);
                //ignore
            }
        }

        /// <summary>
        ///     Use Skill
        /// </summary>
        protected override void Format3EHandler(GameClient client, ClientFormat3E format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            var skill = client.Aisling.SkillBook.Get(i => i.Slot == format.Index).FirstOrDefault();
            if (skill?.Template == null || skill.Script == null)
                return;

            if (!skill.CanUse())
                return;

            skill.InUse = true;

            if (skill.Template.Type == SkillScope.Assail)
                //reset all other assail timers so that things work properly.
                foreach (var assail in client.Aisling.GetAssails())
                {
                    if (assail.Template.Name == skill.Template.Name)
                        continue;

                    ExecuteAbility(client, assail, false);
                }

            skill.Script.OnUse(client.Aisling);

            //define cooldown.
            if (skill.Template.Cooldown > 0)
                skill.NextAvailableUse = DateTime.UtcNow.AddSeconds(skill.Template.Cooldown);
            else
                skill.NextAvailableUse = DateTime.UtcNow.AddMilliseconds(ServerContext.Config.GlobalBaseSkillDelay);

            skill.InUse = false;
        }

        public static void HandleMapNodeSelection(GameClient client, int nodeIndex)
        {
            try
            {
                var worldmap = client.Aisling.PortalSession?.Template;

                if (worldmap == null)
                {
                    client.Aisling.PortalSession = new PortalSession { FieldNumber = client.Aisling.World, IsMapOpen = true };
                    worldmap = client.Aisling.PortalSession.Template;
                }

                if (worldmap == null)
                    return;

                var node = worldmap.Portals
                    .Find(i => i.Destination != null &&
                               i.Destination.AreaID == nodeIndex);

                if (node == null)
                    return;

                if (client.Aisling.PortalSession == null)
                {
                    client.Aisling.GoHome();
                    return;
                }


                if (client.Aisling.PortalSession != null)
                {
                    client.Aisling.PortalSession.DateOpened = DateTime.UtcNow;
                    client.Aisling.PortalSession.TransitionToMap(client,
                        (short)node.Destination.Location.X,
                        (short)node.Destination.Location.Y, node.Destination.AreaID);
                }
            }
            catch (Exception e)
            {
                ServerContext.Report(e);
                client.Aisling.GoHome();
            }
            finally
            {
                client.MapOpen           = false;
                client.SelectedNodeIndex = 0;
            }
        }

        /// <summary>
        ///     World Map
        /// </summary>
        protected override void Format3FHandler(GameClient client, ClientFormat3F format)
        {
            if (client.Aisling == null || !client.Aisling.LoggedIn)
                return;

            var maxIdx = format.Index;
            if (maxIdx <= 0)
                return;

            client.SelectedNodeIndex = maxIdx;
        }

        /// <summary>
        ///     Mouse Click
        /// </summary>
        protected override void Format43Handler(GameClient client, ClientFormat43 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            CancelIfCasting(client);

            #endregion


            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            if (format.Type == 3)
            {
                var popTemplate = ServerContext.GlobalPopupCache
                    .OfType<UserClickPopup>().FirstOrDefault(i => i.X == format.X && i.Y == format.Y && i.MapId == client.Aisling.CurrentMapId);

                if (popTemplate != null)
                {
                    var popup = Popup.Create(client, popTemplate);

                    if (popup != null)
                    {
                        try
                        {
                            if (client.MenuInterpter == null)
                            {
                                CreateInterpreterFromMenuFile(client, popup.Template.YamlKey);

                                if (client.MenuInterpter != null)
                                {
                                    client.MenuInterpter.Start();
                                    client.ShowCurrentMenu(popup, null, client.MenuInterpter.GetCurrentStep());
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            ServerContext.Report(e);
                            client.MenuInterpter = null;
                        }

                        return;
                    }

                    return;
                }
            }

            //Menu Helper Handler!
            if (format.Serial == ServerContext.Config.HelperMenuId &&
                ServerContext.GlobalMundaneTemplateCache.ContainsKey(ServerContext.Config.HelperMenuTemplateKey))
            {
                if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                    return;

                if (format.Type != 0x01)
                    return;

                var helper = new UserHelper(this, new Mundane
                {
                    Serial = ServerContext.Config.HelperMenuId,
                    Template = ServerContext.GlobalMundaneTemplateCache[ServerContext.Config.HelperMenuTemplateKey]
                });

                helper.OnClick(this, client);
                return;
            }

            if (format.Type == 1)
            {
                var obj = GetObject(client.Aisling.Map, i => i.Serial == format.Serial, Get.All);

                switch (obj)
                {
                    case null:
                        return;
                    case Aisling _:
                        client.Aisling.Show(Scope.Self, new ServerFormat34(obj as Aisling));
                        break;
                    case Monster _:
                        (obj as Monster)?.Script?.OnClick(client);
                        break;
                    case Mundane _:
                    {
                            try
                            {
                                //try and call script first
                                (obj as Mundane)?.Script?.OnClick(this, client);

                                if (client.MenuInterpter != null)
                                {
                                    client.MenuInterpter = null;
                                    client.CloseDialog();
                                }

                                //if call does not produce it's own interpreter. Assume default role.
                                if (client.MenuInterpter == null)
                                {
                                    CreateInterpreterFromMenuFile(client, (obj as Mundane).Template.Name, obj);

                                    if (client.MenuInterpter != null) client.MenuInterpter.Start();

                                    ServerContext.Log("Interpreter - Using Default Role: {0}",
                                        (obj as Mundane).Template.Name);
                                }
                                else
                                {
                                    ServerContext.Log("Interpreter - Using Defined Role: {0}",
                                        (obj as Mundane).Template.Name);
                                    return;
                                }

                                if (client.MenuInterpter != null)
                                    client.ShowCurrentMenu(obj as Mundane, null, client.MenuInterpter.GetCurrentStep());
                            }
                            catch (Exception e)
                            {
                                ServerContext.Log(
                                    string.Format(CultureInfo.CurrentCulture, "Error in Menu Handler : {0}",
                                        obj.GetType().FullName));
                                ServerContext.Report(e);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///     Remove equipment
        /// </summary>
        protected override void Format44Handler(GameClient client, ClientFormat44 format)
        {
            #region Sanity Checks

            if (client == null || client.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            CancelIfCasting(client);

            if (client.Aisling.Dead)
                return;

            #endregion

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

            if (client.Aisling.EquipmentManager.Equipment.ContainsKey(format.Slot))
                client.Aisling.EquipmentManager?.RemoveFromExisting(format.Slot);
        }

        /// <summary>
        ///     Keep-Alive Ping Response
        /// </summary>
        protected override void Format45Handler(GameClient client, ClientFormat45 format)
        {
            client.LastPingResponse = DateTime.UtcNow;
            AutoSave(client);
        }

        /// <summary>
        ///     Add Stat Point
        /// </summary>
        protected override void Format47Handler(GameClient client, ClientFormat47 format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.IsRefreshing)
                return;

            CancelIfCasting(client);

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
                return;

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            var attribute = (Stat) format.Stat;

            if (client.Aisling.StatPoints == 0)
            {
                client.SendMessage(0x02, ServerContext.Config.CantDoThat);
                return;
            }

            if ((attribute & Stat.Str) == Stat.Str)
            {
                client.Aisling._Str++;
                client.SendMessage(0x02, ServerContext.Config.StrAddedMessage);
            }

            if ((attribute & Stat.Int) == Stat.Int)
            {
                client.Aisling._Int++;
                client.SendMessage(0x02, ServerContext.Config.IntAddedMessage);
            }

            if ((attribute & Stat.Wis) == Stat.Wis)
            {
                client.Aisling._Wis++;
                client.SendMessage(0x02, ServerContext.Config.WisAddedMessage);
            }

            if ((attribute & Stat.Con) == Stat.Con)
            {
                client.Aisling._Con++;
                client.SendMessage(0x02, ServerContext.Config.ConAddedMessage);
            }

            if ((attribute & Stat.Dex) == Stat.Dex)
            {
                client.Aisling._Dex++;
                client.SendMessage(0x02, ServerContext.Config.DexAddedMessage);
            }

            if (client.Aisling._Wis > ServerContext.Config.StatCap)
                client.Aisling._Wis = ServerContext.Config.StatCap;
            if (client.Aisling._Str > ServerContext.Config.StatCap)
                client.Aisling._Str = ServerContext.Config.StatCap;
            if (client.Aisling._Int > ServerContext.Config.StatCap)
                client.Aisling._Int = ServerContext.Config.StatCap;
            if (client.Aisling._Con > ServerContext.Config.StatCap)
                client.Aisling._Con = ServerContext.Config.StatCap;
            if (client.Aisling._Dex > ServerContext.Config.StatCap)
                client.Aisling._Dex = ServerContext.Config.StatCap;

            if (client.Aisling._Wis <= 0)
                client.Aisling._Wis = ServerContext.Config.StatCap;
            if (client.Aisling._Str <= 0)
                client.Aisling._Str = ServerContext.Config.StatCap;
            if (client.Aisling._Int <= 0)
                client.Aisling._Int = ServerContext.Config.StatCap;
            if (client.Aisling._Con <= 0)
                client.Aisling._Con = ServerContext.Config.StatCap;
            if (client.Aisling._Dex <= 0)
                client.Aisling._Dex = ServerContext.Config.StatCap;

            client.Aisling.StatPoints--;

            if (client.Aisling.StatPoints < 0)
                client.Aisling.StatPoints = 0;


            client.Aisling.Show(Scope.Self, new ServerFormat08(client.Aisling, StatusFlags.All));
        }

        /// <summary>
        ///     This entire exchange routine was shamelessly copy pasted from Kojasou's Server Project.
        ///     (Yes I'm way to lazy to write this myself when it's already been done correctly.)
        ///     Credits: https://github.com/kojasou/wewladh
        /// </summary>
        protected override void Format4AHandler(GameClient client, ClientFormat4A format)
        {
            if (format == null)
                return;

            if (client == null || !client.Aisling.LoggedIn)
                return;

            if (client.Aisling.Skulled)
            {
                client.SystemMessage(ServerContext.Config.ReapMessageDuringAction);
                return;
            }

            var trader = GetObject<Aisling>(client.Aisling.Map, i => i.Serial == format.Id);
            var player = client.Aisling;


            if (player == null || trader == null)
                return;

            if (!player.WithinRangeOf(trader))
                return;

            switch (format.Type)
            {
                case 0x00:
                {
                    player.Exchange = new ExchangeSession(trader);
                    trader.Exchange = new ExchangeSession(player);

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);
                    packet.Write((byte) 0x00);
                    packet.Write((uint) trader.Serial);
                    packet.WriteStringA(trader.Username);
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);
                    packet.Write((byte) 0x00);
                    packet.Write((uint) player.Serial);
                    packet.WriteStringA(player.Username);
                    trader.Client.Send(packet);
                }
                    break;
                case 0x01:
                    var slot = format.ItemSlot;
                    var item = client.Aisling.Inventory.FindInSlot(slot);

                    if (player.Exchange == null)
                        return;

                    if (trader.Exchange == null)
                        return;

                    if (player.Exchange.Trader != trader)
                        return;

                    if (trader.Exchange.Trader != player)
                        return;

                    if (player.Exchange.Confirmed)
                        return;

                    if (item == null || item.Template == null)
                        return;

                    if (item != null && trader.Exchange != null)
                        if (player.EquipmentManager.RemoveFromInventory(item, true))
                            if (trader.CurrentWeight + item.Template.CarryWeight < trader.MaximumWeight)
                            {
                                player.Exchange.Items.Add(item);
                                player.Exchange.Weight += item.Template.CarryWeight;

                                var packet = new NetworkPacketWriter();
                                packet.Write((byte) 0x42);
                                packet.Write((byte) 0x00);

                                packet.Write((byte) 0x02);
                                packet.Write((byte) 0x00);
                                packet.Write((byte) player.Exchange.Items.Count);
                                packet.Write(item.DisplayImage);
                                packet.Write(item.Color);
                                packet.WriteStringA(item.DisplayName);
                                client.Send(packet);

                                packet = new NetworkPacketWriter();
                                packet.Write((byte) 0x42);
                                packet.Write((byte) 0x00);

                                packet.Write((byte) 0x02);
                                packet.Write((byte) 0x01);
                                packet.Write((byte) player.Exchange.Items.Count);
                                packet.Write(item.DisplayImage);
                                packet.Write(item.Color);
                                packet.WriteStringA(item.DisplayName);
                                trader.Client.Send(packet);
                            }
                            else
                            {
                                trader.Client.SendMessage(0x02, "You can't hold this.");
                                client.SendMessage(0x02, "They can't hold that.");
                            }

                    break;
                case 0x03:
                {
                    if (player.Exchange == null)
                        return;

                    if (trader.Exchange == null)
                        return;

                    if (player.Exchange.Trader != trader)
                        return;

                    if (trader.Exchange.Trader != player)
                        return;

                    if (player.Exchange.Confirmed)
                        return;

                    var gold = format.Gold;

                    if (gold > player.GoldPoints)
                        return;
                    if (player.Exchange.Gold != 0)
                        return;

                    player.GoldPoints -= gold;
                    player.Exchange.Gold = gold;
                    player.Client.SendStats(StatusFlags.StructC);

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);

                    packet.Write((byte) 0x03);
                    packet.Write((byte) 0x00);
                    packet.Write((uint) gold);
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);

                    packet.Write((byte) 0x03);
                    packet.Write((byte) 0x01);
                    packet.Write(gold);
                    trader.Client.Send(packet);
                }
                    break;
                case 0x04:
                    if (player.Exchange == null)
                        return;

                    if (trader.Exchange == null)
                        return;

                    if (player.Exchange.Trader != trader)
                        return;

                    if (trader.Exchange.Trader != player)
                        return;

                    player.CancelExchange();
                    break;
                case 0x05:
                {
                    if (player.Exchange == null)
                        return;

                    if (trader.Exchange == null)
                        return;

                    if (player.Exchange.Trader != trader)
                        return;

                    if (trader.Exchange.Trader != player)
                        return;

                    if (player.Exchange.Confirmed)
                        return;

                    player.Exchange.Confirmed = true;

                    if (trader.Exchange.Confirmed)
                        player.FinishExchange();

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);

                    packet.Write((byte) 0x05);
                    packet.Write((byte) 0x00);
                    packet.WriteStringA("Trade was completed.");
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte) 0x42);
                    packet.Write((byte) 0x00);

                    packet.Write((byte) 0x05);
                    packet.Write((byte) 0x01);
                    packet.WriteStringA("Trade was completed.");
                    trader.Client.Send(packet);
                }
                    break;
            }
        }

        /// <summary>
        ///     Start Spell Cast
        /// </summary>
        protected override void Format4DHandler(GameClient client, ClientFormat4D format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }

            var lines = format.Lines;

            if (lines <= 0)
            {
                CancelIfCasting(client);
                return;
            }

            if (client.Aisling.ActiveSpellInfo != null)
                client.Aisling.ActiveSpellInfo = null;

            client.Aisling.ActiveSpellInfo = new CastInfo
            {
                SpellLines = format.Lines,
                Started = DateTime.UtcNow
            };
            client.Aisling.IsCastingSpell = true;
        }

        /// <summary>
        ///     Spell/Skill Chant Information
        /// </summary>
        protected override void Format4EHandler(GameClient client, ClientFormat4E format)
        {
            #region Sanity Checks

            if (client?.Aisling == null)
                return;

            if (!client.Aisling.LoggedIn)
                return;

            if (client.Aisling.IsDead())
                return;

            #endregion

            if (client.Aisling.IsSleeping || client.Aisling.IsFrozen)
            {
                client.Interupt();
                return;
            }


            var chant = format.Message;
            var subject = chant.IndexOf(" Lev", StringComparison.Ordinal);

            if (subject > 0)
            {
                var message = chant.Substring(subject, chant.Length - subject);
                client.Say(
                    ServerContext.Config.ChantPrefix + chant.Replace(message, string.Empty).Trim() +
                    ServerContext.Config.ChantSuffix, 0x02);
                return;
            }

            client.Say(chant, 0x02);
        }

        /// <summary>
        ///     Profile Picture
        /// </summary>
        protected override void Format4FHandler(GameClient client, ClientFormat4F format)
        {
            client.Aisling.ProfileMessage = format.Words;
            client.Aisling.PictureData = format.Image;
        }

        /// <summary>
        ///     Save
        /// </summary>
        protected override void Format75Handler(GameClient client, ClientFormat75 format)
        {
            AutoSave(client);
        }


        protected override void Format79Handler(GameClient client, ClientFormat79 format)
        {
            if (client == null || client.Aisling == null)
                return;

            client.Aisling.ActiveStatus = format.Status;
        }

        /// <summary>
        ///     Meta data
        /// </summary>
        protected override void Format7BHandler(GameClient client, ClientFormat7B format)
        {
            if (format.Type == 0x00)
            {
                Console.WriteLine("Client Requested Metafile: {0}", format.Name);

                client.FlushAndSend(new ServerFormat6F
                {
                    Type = 0x00,
                    Name = format.Name
                });
            }

            if (format.Type == 0x01)
            {
                client.FlushAndSend(new ServerFormat6F
                {
                    Type = 0x01
                });
            }
        }

        /// <summary>
        ///     CancelIfCasting - Use this Method to check and or abort if casting.
        /// </summary>
        public static void CancelIfCasting(GameClient client)
        {
            if (!client.Aisling.LoggedIn)
            {
                client.Aisling.ActiveSpellInfo = null;
                client.Aisling.IsCastingSpell = false;
                client.Send(new ServerFormat48());
                return;
            }

            client.Aisling.ActiveSpellInfo = null;
            client.Aisling.IsCastingSpell = false;
            client.Send(new ServerFormat48());
        }
    }
}