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
using System.Net;
using System.Text;
using Darkages.Network.ClientFormats;
using Darkages.Network.ServerFormats;
using Darkages.Storage;
using Darkages.Types;

namespace Darkages.Network.Login
{
    public class LoginServer : NetworkServer<LoginClient>
    {
        public LoginServer(int capacity)
            : base(capacity)
        {
            MServerTable = MServerTable.FromFile("MServerTable.xml");
            Notification = Notification.FromFile("notification.txt");
        }

        public static MServerTable MServerTable { get; set; }
        public static Notification Notification { get; set; }


        /// <summary>
        ///     Send Encryption Parameters.
        /// </summary>
        protected override void Format00Handler(LoginClient client, ClientFormat00 format)
        {
            if (format.Version == 718)
                client.Send(new ServerFormat00
                {
                    Type = 0x00,
                    Hash = MServerTable.Hash,
                    Parameters = client.Encryption.Parameters
                });
        }

        /// <summary>
        ///     Login Client - Create New Aisling, Choose Username/password.
        /// </summary>
        protected override void Format02Handler(LoginClient client, ClientFormat02 format)
        {
            //save information to memory.
            client.CreateInfo = format;

            var _aisling = StorageManager.AislingBucket.Load(format.AislingUsername);

            if (_aisling == null)
            {
                client.SendMessageBox(0x00, "\0");
            }
            else
            {
                client.SendMessageBox(0x03, "Character Already Exists.\0");
                client.CreateInfo = null;
                return;
            }
        }

        /// <summary>
        ///     Login Client - Save Character Template.
        /// </summary>
        protected override void Format04Handler(LoginClient client, ClientFormat04 format)
        {
            //make sure the first step was done first.
            if (client.CreateInfo == null)
            {
                ClientDisconnected(client);
                return;
            }

            //create aisling from default template.
            var template = Aisling.Create();
            template.Display = (BodySprite) (format.Gender * 16);
            template.Username = client.CreateInfo.AislingUsername;
            template.Password = client.CreateInfo.AislingPassword;
            template.Gender = (Gender) format.Gender;
            template.HairColor = format.HairColor;
            template.HairStyle = format.HairStyle;

            ServerContext.Log("New character Created: " + template.Username);

            StorageManager.AislingBucket.Save(template);
            client.SendMessageBox(0x00, "\0");
        }

        /// <summary>
        ///     Login - Check username/password. Proceed to Game Server.
        /// </summary>
        protected override void Format03Handler(LoginClient client, ClientFormat03 format)
        {
            Aisling _aisling = null;

            try
            {
                _aisling = StorageManager.AislingBucket.Load(format.Username);

                if (_aisling != null)
                {
                    if (_aisling.Password != format.Password)
                    {
                        client.SendMessageBox(0x02, "Sorry, Incorrect Password.");
                        return;
                    }
                }
                else
                {
                    client.SendMessageBox(0x02,
                        string.Format(
                            "{0} does not exist in this world. You can make this hero by clicking on 'Create'.",
                            format.Username));
                    return;
                }
            }
            catch (Exception e)
            {
                client.SendMessageBox(0x02,
                    string.Format(
                        "{0} is not supported by the new server. Please remake your character. This will not happen when the server goes to beta.",
                        format.Username));

                ServerContext.Report(e);

                return;
            }

            if (!ServerContext.Config.MultiUserLogin)
            {
                var aislings = GetObjects<Aisling>(null,
                    i => i.Username.ToLower() == format.Username.ToLower()
                    && format.Password == i.Password);

                foreach (var aisling in aislings)
                    aisling.Client.Server.ClientDisconnected(aisling.Client);
            }

            LoginAsAisling(client, _aisling);
        }

        public void LoginAsAisling(LoginClient client, Aisling _aisling)
        {
            if (_aisling != null)
            {
                var redirect = new Redirect
                {
                    Serial = Convert.ToString(client.Serial),
                    Salt = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
                    Seed = Convert.ToString(client.Encryption.Parameters.Seed),
                    Name = _aisling.Username
                };

                if (_aisling.Username.Equals(ServerContext.Config.GameMaster, StringComparison.OrdinalIgnoreCase))
                {
                    _aisling.GameMaster = true;
                    ServerContext.Log("GameMaster Entering Game: {0}", _aisling.Username);
                }
                else
                {
                    ServerContext.Log("Player Entering Game: {0}", _aisling.Username);
                }

                _aisling.Redirect = redirect;

                StorageManager.AislingBucket.Save(_aisling);

                client.SendMessageBox(0x00, "\0");
                client.Send(new ServerFormat03
                {
                    EndPoint = new IPEndPoint(Address, ServerContext.DefaultPort),
                    Redirect = redirect
                });
            }
        }

        /// <summary>
        ///     Client Closed Connection (Closed Darkages.exe), Remove them.
        /// </summary>
        protected override void Format0BHandler(LoginClient client, ClientFormat0B format)
        {
            RemoveClient(client.Serial);
        }

        /// <summary>
        ///     Redirect Client from Lobby Server to Login Server Automatically.
        /// </summary>
        protected override void Format10Handler(LoginClient client, ClientFormat10 format)
        {
            client.Encryption.Parameters = format.Parameters;
            client.Send(new ServerFormat60
            {
                Type = 0x00,
                Hash = Notification.Hash
            });
        }

        /// <summary>
        ///     Login Client - Update Password.
        /// </summary>
        protected override void Format26Handler(LoginClient client, ClientFormat26 format)
        {
            var _aisling = StorageManager.AislingBucket.Load(format.Username);
            if (_aisling == null)
            {
                client.SendMessageBox(0x02, "Incorrect Information provided.");
                return;
            }

            if (_aisling.Password != format.Password)
            {
                client.SendMessageBox(0x02, "Incorrect Information provided.");
                return;
            }

            if (string.IsNullOrEmpty(format.NewPassword) || format.NewPassword.Length < 3)
            {
                client.SendMessageBox(0x02, "new password not accepted.");
                return;
            }

            //Update new password.
            _aisling.Password = format.NewPassword;
            //Update and Store Information.
            StorageManager.AislingBucket.Save(_aisling);

            client.SendMessageBox(0x00, "\0");
        }

        protected override void Format4BHandler(LoginClient client, ClientFormat4B format)
        {
            client.Send(new ServerFormat60
            {
                Type = 0x01,
                Size = Notification.Size,
                Data = Notification.Data
            });
        }

        protected override void Format66Handler(LoginClient client, ClientFormat66 format)
        {
        }

        protected override void Format57Handler(LoginClient client, ClientFormat57 format)
        {
            if (format.Type == 0x00)
            {
                var redirect = new Redirect
                {
                    Serial = Convert.ToString(client.Serial),
                    Salt   = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
                    Seed   = Convert.ToString(client.Encryption.Parameters.Seed),
                    Name   = "socket[" + client.Serial + "]"
                };

                client.Send(new ServerFormat03
                {
                    EndPoint = new IPEndPoint(MServerTable.Servers[0].Address, MServerTable.Servers[0].Port),
                    Redirect = redirect
                });
            }
            else
            {
                client.Send(new ServerFormat56
                {
                    Size = MServerTable.Size,
                    Data = MServerTable.Data
                });
            }
        }

        protected override void Format68Handler(LoginClient client, ClientFormat68 format)
        {
            client.Send(new ServerFormat66());
        }

        protected override void Format7BHandler(LoginClient client, ClientFormat7B format)
        {
            if (format.Type == 0x00)
            {
                Console.WriteLine("Client Requested Metafile: {0}", format.Name);

                client.Send(new ServerFormat6F
                {
                    Type = 0x00,
                    Name = format.Name
                });
            }

            if (format.Type == 0x01)
            {
                client.Send(new ServerFormat6F
                {
                    Type = 0x01
                });
            }
        }

        public override bool AddClient(LoginClient client)
        {
            return base.AddClient(client);
        }

        public override void ClientConnected(LoginClient client)
        {
            client.Send(new ServerFormat7E());
        }
    }
}