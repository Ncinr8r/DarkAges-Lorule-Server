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
using Darkages.Common;
using Darkages.Network;
using Darkages.Network.Game;
using Darkages.Network.ServerFormats;
using Darkages.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Darkages
{
    /// <summary>The Main Aisling Class, Contains all Coupled Fields and Members for Interacting with the Aisling Sprite Object.</summary>
    /// <seealso cref="Darkages.Types.Sprite" />
    public class Aisling : Sprite
    {
        /// <summary>Initializes a new instance of the <see cref="Aisling"/> class.</summary>
        public Aisling()
        {
            OffenseElement = ElementManager.Element.None;
            DefenseElement = ElementManager.Element.None;
            Clan = string.Empty;
            Flags = AislingFlags.Normal;
            LegendBook = new Legend();
            ClanTitle = string.Empty;
            ClanRank = string.Empty;
            ActiveSpellInfo = null;
            LoggedIn = false;
            ActiveStatus = ActivityStatus.Awake;
            PortalSession = new PortalSession();
            Quests = new List<Quest>();
            PartyStatus = GroupStatus.AcceptingRequests;
            InvitePrivleges = true;
            LeaderPrivleges = false;
            Remains = new CursedSachel(this);
            ActiveReactor = null;
            DiscoveredMaps = new List<int>();
            Popups = new List<Popup>();
        }

        /// <summary>Gets or sets a value indicating whether The Aisling is a [game master].</summary>
        /// <value>
        /// <c>true</c> if [game master]; otherwise, <c>false</c>.</value>
        public bool GameMaster { get; set; }

        /// <summary>Gets or sets the game settings.</summary>
        /// <value>The game settings.</value>
        public List<ClientGameSettings> GameSettings { get; set; }

        /// <summary>Gets or sets the <strong>bank</strong> manager.</summary>
        /// <value>The bank manager.</value>
        public Bank BankManager { get; set; }

        public int CurrentWeight { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int AbpLevel { get; set; }
        public int AbpTotal { get; set; }
        public int AbpNext { get; set; }
        public int ExpLevel { get; set; }
        public int ExpTotal { get; set; }
        public int ExpNext { get; set; }
        public int Title { get; set; }
        public int ClassID { get; set; }
        public int GamePoints { get; set; }
        public int GoldPoints { get; set; }
        public int StatPoints { get; set; }
        public int BodyColor { get; set; }
        public int BodyStyle { get; set; }
        public int FaceColor { get; set; }
        public int FaceStyle { get; set; }
        public byte HairColor { get; set; }
        public byte HairStyle { get; set; }
        public byte Boots { get; set; }
        public int Helmet { get; set; }
        public byte Shield { get; set; }
        public ushort Weapon { get; set; }
        public ushort Armor { get; set; }
        public byte OverCoat { get; set; }
        public byte Pants { get; set; }
        public byte[] PictureData { get; set; }
        public string ProfileMessage { get; set; }
        public bool LoggedIn { get; set; }
        public byte Nation { get; set; }
        public string Clan { get; set; }
        public byte Resting { get; set; }
        public bool TutorialCompleted { get; set; }
        public byte Blind { get; set; }
        public byte HeadAccessory1 { get; set; }
        public byte HeadAccessory2 { get; set; }
        public string ClanTitle { get; set; }
        public string ClanRank { get; set; }
        public byte BootColor { get; set; }
        public byte NameColor { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastLogged { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AislingFlags Flags { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStatus PartyStatus { get; set; }
        public List<Quest> Quests { get; set; }
        public ClassStage Stage { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Class Path { get; set; }
        public Legend LegendBook { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BodySprite Display { get; set; }
        public SkillBook SkillBook { get; set; }
        public SpellBook SpellBook { get; set; }
        public Inventory Inventory { get; set; }
        public EquipmentManager EquipmentManager { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ActivityStatus ActiveStatus { get; set; }
        public List<int> DiscoveredMaps { get; set; }

        public Dictionary<string, int> MonsterKillCounters = new Dictionary<string, int>();

        public Dictionary<string, DateTime> Reactions      = new Dictionary<string, DateTime>();

        public PortalSession PortalSession { get; set; }

        public Dictionary<string, EphemeralReactor> ActiveReactors = new Dictionary<string, EphemeralReactor>();

        [JsonIgnore] public int MaximumWeight => (int)(_Str * ServerContext.Config.WeightIncreaseModifer);

        [JsonIgnore]
        public List<Popup> Popups = new List<Popup>();

        [JsonIgnore] public bool ProfileOpen { get; set; }

        [JsonIgnore] public int AreaID => CurrentMapId;

        [JsonIgnore] public bool Skulled => HasDebuff("skulled");

        [JsonIgnore] public Party GroupParty { get; set; }

        [JsonIgnore] public bool IsCastingSpell { get; set; }

        [JsonIgnore] public CastInfo ActiveSpellInfo { get; set; }

        [JsonIgnore] public List<Aisling> PartyMembers => GroupParty?.Members;

        [JsonIgnore] public int FieldNumber { get; set; } = 1;

        [JsonIgnore] public int LastMapId { get; set; }

        [JsonIgnore] public bool LeaderPrivleges { get; set; }

        [JsonIgnore] public bool InvitePrivleges { get; set; }

        [JsonIgnore] public int DamageCounter = 0;

        [JsonIgnore] public bool UsingTwoHanded { get; set; }

        [JsonIgnore] [Browsable(false)] public new Position Position => new Position(XPos, YPos);

        [JsonIgnore] public bool Dead => Flags.HasFlag(AislingFlags.Dead);

        [JsonIgnore] public bool Invisible => Flags.HasFlag(AislingFlags.Invisible);

        [JsonIgnore] public CursedSachel Remains { get; set; }

        [JsonIgnore] public ExchangeSession Exchange { get; set; }


        [JsonIgnore]
        public Reactor ActiveReactor { get; set; }

        [JsonIgnore]
        public List<Trap> MyTraps => Trap.Traps.Select(i => i.Value).Where(i => i.Owner.Serial == this.Serial).ToList();

        #region Reactor Stuff
        [JsonIgnore]
        public bool ReactorActive { get; set; }

        [JsonIgnore]
        public bool CanReact { get; set; }

        [JsonIgnore]
        public DialogSequence ActiveSequence { get; set; }

        public AnimalForm AnimalForm { get; set; }

        /// <summary>
        /// Used for Internal Server Authentication
        /// </summary>
        public Redirect Redirect { get; set; }

        #endregion

        [JsonIgnore]
        public HashSet<Sprite> View = new HashSet<Sprite>();

        /// <summary>Assail, (Space-Bar) Activating all Assails.</summary>
        public void Assail()
        {
            if (Client != null)
            {
                GameServer.ActivateAssails(Client);
            }
        }

        /// <summary>Makes the reactor.</summary>
        /// <param name="lpName">The Yaml Script Name</param>
        /// <param name="lpTimeout">Number of seconds until the Reactor expires.</param>
        /// <returns>void</returns>
        public void MakeReactor(string lpName, int lpTimeout)
        {
            ActiveReactors[lpName] = new EphemeralReactor(lpName, lpTimeout);
        }

        /// <summary>Determines whether the specified lp name has quest.</summary>
        /// <param name="lpName">  The Quest key to check for.</param>
        /// <returns>
        /// <c>true</c> if the specified Quest Exists; otherwise, <c>false</c>.
        /// </returns>
        public bool HasQuest(string lpName)
        {
            return Quests.Any(i => i.Name == lpName);
        }

        /// <summary>Determines whether [has completed quest] [the specified lp name].</summary>
        /// <param name="lpName">The Quest key to check for Completion.</param>
        /// <returns>
        ///   <c>true</c> if [has completed quest] [lpName]; otherwise, <c>false</c>.</returns>
        /// <example>
        /// Aisling.HasCompletedQuest("Quest Name");
        /// <code>
        /// </code>
        /// </example>
        public bool HasCompletedQuest(string lpName)
        {
            return Quests.Any(i => i.Name == lpName && i.Completed);
        }

        /// <summary>Forcefully Completes a Quest</summary>
        /// <param name="lpName">Name of the Quest to Complete</param>
        /// <example>Aisling.CompleteQuest("Quest Name");
        /// <code>
        /// </code>
        /// </example>
        public void CompleteQuest(string lpName)
        {
            var obj = Quests.Find(i => i.Name == lpName);
            obj.Completed = true;
        }

        /// <summary>Gets the quest.</summary>
        /// <param name="name">The name.</param>
        /// <returns>Returns the Quest, or returns null if no Quest was retrieved.</returns>
        public Quest GetQuest(string name) => Quests.FirstOrDefault(i => i.Name == name);

        /// <summary>Accepts the quest.</summary>
        /// <param name="lpQuest">The Quest to Accept.</param>
        /// <returns>True if the quest was accepted, and the Aisling does not already have it accepted. Returns false if the Quest has already been Accepted.</returns>
        /// <example>
        /// <code>
        /// if (client.Aisling.AcceptQuest(quest))
        /// {
        ///     //quest has been added to interpreter.
        /// }
        /// </code>
        /// </example>
        public bool AcceptQuest(Quest lpQuest)
        {
            lock (Quests)
            {
                if (!Quests.Any(i => i.Name == lpQuest.Name))
                {
                    Quests.Add(lpQuest);

                    return true;
                }

            }

            return false;
        }
        /// <summary>Nearests trap.</summary>
        /// <returns>Returns the Nearest Trap Closest to the Aisling</returns>
        public Trap NearestTrap()
        {
            return MyTraps.OrderBy(i => this.Position.DistanceFrom(i.Location)).FirstOrDefault();
        }

        public void Recover()
        {
            CurrentHp = MaximumHp;
            CurrentMp = MaximumMp;

            Client?.SendStats(StatusFlags.All);
        }

        public void GoHome()
        {
            var DestinationMap = ServerContext.Config.TransitionZone;

            if (ServerContext.GlobalMapCache.ContainsKey(DestinationMap))
            {
                var targetMap = ServerContext.GlobalMapCache[DestinationMap];

                Client.LeaveArea(true, true);
                Client.Aisling.XPos = ServerContext.Config.TransitionPointX;
                Client.Aisling.YPos = ServerContext.Config.TransitionPointY;
                Client.Aisling.CurrentMapId = DestinationMap;
                Client.EnterArea();
                Client.Refresh();
            }
            Client.CloseDialog();
        }


        public void CastSpell(Spell spell)
        {
            if (!spell.CanUse())
            {
                spell.InUse = false;
                return;
            }

            if (spell.InUse)
                return;

            var info = Client.Aisling.ActiveSpellInfo;

            if (info != null)
            {

                if (!string.IsNullOrEmpty(info.Data))
                    spell.Script.Arguments = info.Data;

                var target = GetObject(Map, i => i.Serial == info.Target, Get.Monsters | Get.Aislings | Get.Mundanes);
                spell.InUse = true;


                if (spell.Script != null)
                {
                    if (target != null)
                    {
                        if (target is Aisling)
                            spell.Script.OnUse(this, target as Aisling);
                        if (target is Monster)
                            spell.Script.OnUse(this, target as Monster);
                        if (target is Mundane)
                            spell.Script.OnUse(this, target as Mundane);
                    }
                    else
                    {
                        spell.Script.OnUse(this, this);
                    }
                }
            }

            spell.NextAvailableUse = DateTime.UtcNow.AddSeconds(info.SpellLines > 0 ? 1 : 0.3);
            spell.InUse = false;
        }

        public void DestroyReactor(Reactor Actor)
        {
            if (Reactions.ContainsKey(Actor.Name))
                Reactions.Remove(Actor.Name);

            ActiveReactor = null;
            ReactorActive = false;
            Actor = null;
        }

        public bool HasKilled(string value, int number)
        {
            if (MonsterKillCounters.ContainsKey(value))
            {
                return MonsterKillCounters[value] >= number;
            }

            return false;
        }

        public static Aisling Create()
        {
            var fractions = Enum.GetValues(typeof(Fraction));
            var randomFraction = (int)fractions.GetValue(Generator.Random.Next(fractions.Length));

            var result = new Aisling
            {
                Username = string.Empty,
                Password = string.Empty,
                AbpLevel = 0,
                AbpTotal = 0,
                AbpNext = 0,
                ExpLevel = 1,
                ExpTotal = 1,
                ExpNext = 600,
                Gender = 0,
                Title = 0,
                Flags = 0,
                CurrentMapId = ServerContext.Config.StartingMap,
                ClassID = 0,
                Stage = ClassStage.Class,
                Path = Class.Peasant,
                CurrentHp = 60,
                CurrentMp = 30,
                _MaximumHp = 60,
                _MaximumMp = 30,
                _Str = 3,
                _Int = 3,
                _Wis = 3,
                _Con = 3,
                _Dex = 3,
                GamePoints = 0,
                GoldPoints = 0,
                StatPoints = 0,
                BodyColor = 0,
                BodyStyle = 0,
                FaceColor = 0,
                FaceStyle = 0,
                HairColor = 0,
                HairStyle = 0,
                NameColor = 1,
                BootColor = 0,
                Amplified = 0,
                TutorialCompleted = false,
                SkillBook = new SkillBook(),
                SpellBook = new SpellBook(),
                Inventory = new Inventory(),
                EquipmentManager = new EquipmentManager(null),
                BankManager = new Bank(),
                Created = DateTime.UtcNow,
                LastLogged = DateTime.UtcNow,
                XPos = ServerContext.Config.StartingPosition.X,
                YPos = ServerContext.Config.StartingPosition.Y,
                Nation = (byte)randomFraction,
                AnimalForm = AnimalForm.None,
            };

            Skill.GiveTo(result, "Assail", 1);
            Spell.GiveTo(result, "Create Item", 1);
            Spell.GiveTo(result, "Gem Polishing", 1);

            if (DateTime.UtcNow.Year <= 2020)
            {
                result.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Event",
                    Color = (byte)LegendColor.DarkPurple,
                    Icon = (byte)LegendIcon.Victory,
                    Value = string.Format("Aisling Age of Aquarius")
                });
            }

            if (result.Nation == 1)
            {
                result.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Event",
                    Color = (byte)LegendColor.Orange,
                    Icon = (byte)LegendIcon.Community,
                    Value = string.Format("Lorule Citizen")
                });
            }
            else if (result.Nation == 2)
            {
                result.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Event",
                    Color = (byte)LegendColor.LightGreen,
                    Icon = (byte)LegendIcon.Community,
                    Value = string.Format("Lividia Citizen")
                });
            }
            else if (result.Nation == 3)
            {
                result.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Event",
                    Color = (byte)LegendColor.Darkgreen,
                    Icon = (byte)LegendIcon.Community,
                    Value = string.Format("Amongst the Exile.")
                });
            }

            return result;
        }

        /// <summary>
        ///     Removes Aisling, Sends Remove packet to nearby aislings
        ///     and removes itself from the ObjectManager.
        /// </summary>
        public void Remove(bool update = false, bool delete = true)
        {
            if (Map != null)
            {
                Map.Update(XPos, YPos, this, true);
            }

            if (update)
                Show(Scope.NearbyAislingsExludingSelf, new ServerFormat0E(Serial));

            try
            {
                if (delete)
                {
                    var objs = GetObjects(Map, i => i.WithinRangeOf(this) && i.Target != null && i.Target.Serial == Serial, Get.Monsters | Get.Mundanes);
                    if (objs != null)
                    {
                        foreach (var obj in objs)
                            obj.Target = null;
                    }
                }
            }
            finally
            {
                if (delete)
                    DelObject(this);
            }
        }

        public bool HasSkill<T>(SkillScope scope) where T : Template, new()
        {
            var obj = new T();

            if (obj is SkillTemplate)
                if ((scope & SkillScope.Assail) == SkillScope.Assail)
                    return SkillBook.Get(i => i != null && i.Template != null
                                                        && i.Template.Type == SkillScope.Assail).Length > 0;

            return false;
        }


        public Skill[] GetAssails()
        {
            return SkillBook.Get(i => i != null && i.Template != null
                                                && i.Template.Type == SkillScope.Assail).ToArray();
        }

        public void UpdateStats()
        {
            Client?.SendStats(StatusFlags.All);
        }


        public bool CastDeath()
        {
            if (!Client.Aisling.Flags.HasFlag(AislingFlags.Dead))
            {
                LastMapId = CurrentMapId;
                LastPosition = Position;

                Client.CloseDialog();
                Client.Aisling.Flags = AislingFlags.Dead;
                Client.HpRegenTimer.Disabled = true;
                Client.MpRegenTimer.Disabled = true;

                Client.LeaveArea(true, false);
                Client.EnterArea();

                return true;
            }

            return false;
        }

        public void SendToHell()
        {
            if (!ServerContext.GlobalMapCache.ContainsKey(ServerContext.Config.DeathMap))
                return;

            if (CurrentMapId == ServerContext.Config.DeathMap)
                return;

            Remains.Owner = this;

            var reepStack = Remains;
            var items = reepStack.Items;

            if (items.Count > 0)
            {
                Remains.ReepItems(items.ToList());
            }
            else
            {
                if (Inventory.Length > 0 || EquipmentManager.Length > 0)
                {
                    Remains.ReepItems();
                }
            }

            for (int i = 0; i < 2; i++)
                RemoveBuffsAndDebuffs();

            Client.LeaveArea(true, true);
            XPos = 21;
            YPos = 21;
            Direction = 0;
            Client.Aisling.CurrentMapId = ServerContext.Config.DeathMap;
            Client.EnterArea();

            UpdateStats();
        }

        public void CancelExchange()
        {
            var trader = Exchange.Trader;

            var exchangeA = Exchange;
            var exchangeB = trader.Exchange;

            var itemsA = exchangeA.Items.ToArray();
            var itemsB = exchangeB.Items.ToArray();

            var goldA = exchangeA.Gold;
            var goldB = exchangeB.Gold;

            Exchange = null;
            trader.Exchange = null;

            foreach (var item in itemsB)
                if (item.GiveTo(trader, true)) { }

            foreach (var item in itemsA)
                if (item.GiveTo(this, true)) { }


            GoldPoints += goldA;
            trader.GoldPoints += goldB;



            if (trader.GoldPoints > ServerContext.Config.MaxCarryGold)
                trader.GoldPoints = ServerContext.Config.MaxCarryGold;
            if (GoldPoints > ServerContext.Config.MaxCarryGold)
                GoldPoints = ServerContext.Config.MaxCarryGold;

            trader.Client.SendStats(StatusFlags.StructC);
            Client.SendStats(StatusFlags.StructC);


            var packet = new NetworkPacketWriter();
            packet.Write((byte)0x42);
            packet.Write((byte)0x00);

            packet.Write((byte)0x04);
            packet.Write((byte)0x00);
            packet.WriteStringA("Trade was aborted.");
            Client.Send(packet);

            packet = new NetworkPacketWriter();
            packet.Write((byte)0x42);
            packet.Write((byte)0x00);

            packet.Write((byte)0x04);
            packet.Write((byte)0x01);
            packet.WriteStringA("Trade was aborted.");
            trader.Client.Send(packet);
        }

        public void FinishExchange()
        {
            var trader = Exchange.Trader;
            var exchangeA = Exchange;
            var exchangeB = trader.Exchange;
            var itemsA = exchangeA.Items.ToArray();
            var itemsB = exchangeB.Items.ToArray();
            var goldA = exchangeA.Gold;
            var goldB = exchangeB.Gold;

            Exchange = null;
            trader.Exchange = null;

            foreach (var item in itemsB)
            {
                if (item.GiveTo(this, true))
                {

                }
            }

            foreach (var item in itemsA)
            {
                if (item.GiveTo(trader, true))
                {

                }
            }


            GoldPoints        += goldB;
            trader.GoldPoints += goldA;

            if (trader.GoldPoints > ServerContext.Config.MaxCarryGold)
                trader.GoldPoints = ServerContext.Config.MaxCarryGold;
            if (GoldPoints > ServerContext.Config.MaxCarryGold)
                GoldPoints = ServerContext.Config.MaxCarryGold;

            exchangeA.Items.Clear();
            exchangeB.Items.Clear();

            trader.Client?.SendStats(StatusFlags.All);
            Client?.SendStats(StatusFlags.All);

        }

        public bool ReactedWith(string str) => Reactions.ContainsKey(str);

        public void ReviveInFront()
        {
            var infront = GetInfront(1).OfType<Aisling>();

            var action = new ServerFormat1A
            {
                Serial = this.Serial,
                Number = 0x01,
                Speed = 30
            };

            foreach (var obj in infront)
            {
                if (obj.Serial == Serial)
                    continue;

                if (!obj.LoggedIn)
                    continue;

                obj.RemoveDebuff("skulled", true);
                obj.Client.Revive();
                obj.Animate(5);
            }

            ApplyDamage(this, 0, true, 8);
            Show(Scope.NearbyAislings, action);

        }

        public bool GiveGold(int offer, bool SendClientUpdate = true)
        {
            if (GoldPoints + offer < ServerContext.Config.MaxCarryGold)
            {
                GoldPoints += offer;
                return true;
            }

            if (SendClientUpdate)
            {
                Client?.SendStats(StatusFlags.StructC);
            }

            return false;
        }

        public override string ToString()
        {
            return Username;
        }
    }
}
