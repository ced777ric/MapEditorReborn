﻿namespace MapEditorReborn.API.Features
{
    using System;
    using AdminToys;
    using Components.ObjectComponents;
    using Enums;
    using Exiled.API.Extensions;
    using Exiled.API.Features.Items;
    using Exiled.CustomItems.API.Features;
    using Extensions;
    using Features.Components;
    using MEC;
    using Mirror;
    using Mirror.LiteNetLib4Mirror;
    using RemoteAdmin;
    using UnityEngine;

    using static API;

    using Object = UnityEngine.Object;

    /// <summary>
    /// A tool used to mark and save positions.
    /// </summary>
    public static class Indicator
    {
        /// <summary>
        /// Spawns a <see cref="IndicatorObjectComponent"/> given a specified <see cref="ItemSpawnPointComponent"/>.
        /// </summary>
        /// <param name="itemSpawnPoint">The specified <see cref="PlayerSpawnPointComponent"/>.</param>
        /// <param name="indicator">The <see cref="IndicatorObjectComponent"/> attached to the specified <see cref="PlayerSpawnPointComponent"/>.</param>
        public static void SpawnObjectIndicator(ItemSpawnPointComponent itemSpawnPoint, IndicatorObjectComponent indicator = null)
        {
            ItemType parsedItem;

            if (CustomItem.TryGet(itemSpawnPoint.Base.Item, out CustomItem custom))
            {
                parsedItem = custom.Type;
            }
            else
            {
                parsedItem = (ItemType)Enum.Parse(typeof(ItemType), itemSpawnPoint.Base.Item, true);
            }

            if (indicator != null)
            {
                if (indicator.TryGetComponent(out InventorySystem.Items.Pickups.ItemPickupBase ipb) && ipb.Info.ItemId == parsedItem)
                {
                    indicator.transform.position = itemSpawnPoint.transform.position;
                    return;
                }
                else
                {
                    SpawnedObjects.Remove(indicator);
                    indicator.Destroy();
                }
            }

            Vector3 scale = parsedItem.IsWeapon() ? new Vector3(0.25f, 0.25f, 0.25f) : Vector3.one;

            Pickup pickup = new Item(parsedItem).Create(itemSpawnPoint.transform.position + (Vector3.up * 0.1f * scale.y), Quaternion.identity, scale);
            pickup.Locked = true;

            GameObject pickupGameObject = pickup.Base.gameObject;

            if (pickupGameObject.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true;

            pickupGameObject.AddComponent<ItemSpiningComponent>();

            SpawnedObjects.Add(pickupGameObject.AddComponent<IndicatorObjectComponent>().Init(itemSpawnPoint));
            NetworkServer.Spawn(pickupGameObject);
        }

        /// <summary>
        /// Spawns a <see cref="IndicatorObjectComponent"/> given a specified <see cref="PlayerSpawnPointComponent"/>.
        /// </summary>
        /// <param name="playerSpawnPoint">The specified <see cref="PlayerSpawnPointComponent"/>.</param>
        /// <param name="indicator">The <see cref="IndicatorObjectComponent"/> attached to the specified <see cref="PlayerSpawnPointComponent"/>.</param>
        public static void SpawnObjectIndicator(PlayerSpawnPointComponent playerSpawnPoint, IndicatorObjectComponent indicator = null)
        {
            if (indicator != null)
            {
                SpawnedObjects.Remove(indicator);
                NetworkServer.Destroy(indicator.gameObject);
            }

            Vector3 position = playerSpawnPoint.transform.position;

            GameObject dummyObject = Object.Instantiate(LiteNetLib4MirrorNetworkManager.singleton.playerPrefab);
            dummyObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            dummyObject.transform.position = position;

            if (dummyObject.TryGetComponent(out QueryProcessor processor))
            {
                processor.NetworkPlayerId = QueryProcessor._idIterator++;
                processor._ipAddress = "127.0.0.WAN";
            }

            if (dummyObject.TryGetComponent(out CharacterClassManager ccm))
            {
                // ccm.CurClass = playerSpawnPoint.tag.ConvertToSpawnableTeam();
                ccm.CurClass = RoleType.Tutorial;
                ccm.GodMode = true;
            }

            string dummyNickname = playerSpawnPoint.Base.SpawnableTeam.ToString();

            if (dummyObject.TryGetComponent(out NicknameSync nicknameSync))
            {
                nicknameSync.Network_myNickSync = "PLAYER SPAWNPOINT";
                nicknameSync.CustomPlayerInfo = $"{dummyNickname}\nSPAWN POINT";
                nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
            }

            SpawnedObjects.Add(dummyObject.AddComponent<IndicatorObjectComponent>().Init(playerSpawnPoint));
            NetworkServer.Spawn(dummyObject);

            if (dummyObject.TryGetComponent(out ReferenceHub rh))
                Timing.CallDelayed(0.1f, () =>
                {
                    rh.playerMovementSync.OverridePosition(position, 0f);
                });
        }

        /// <summary>
        /// Spawns a <see cref="IndicatorObjectComponent"/> given a specified <see cref="RagdollSpawnPointComponent"/>.
        /// </summary>
        /// <param name="ragdollSpawnPoint">The specified <see cref="RagdollSpawnPointComponent"/>.</param>
        /// <param name="indicator">The <see cref="IndicatorObjectComponent"/> attached to the specified <see cref="RagdollSpawnPointComponent"/>.</param>
        public static void SpawnObjectIndicator(RagdollSpawnPointComponent ragdollSpawnPoint, IndicatorObjectComponent indicator = null)
        {
            if (indicator != null)
            {
                SpawnedObjects.Remove(indicator);
                NetworkServer.Destroy(indicator.gameObject);
            }

            Vector3 position = ragdollSpawnPoint.transform.position;

            GameObject dummyObject = Object.Instantiate(LiteNetLib4MirrorNetworkManager.singleton.playerPrefab);

            dummyObject.transform.localScale = new Vector3(-0.2f, -0.2f, -0.2f);
            dummyObject.transform.position = position;

            RoleType roleType = ragdollSpawnPoint.Base.RoleType;

            if (dummyObject.TryGetComponent(out QueryProcessor processor))
            {
                processor.NetworkPlayerId = QueryProcessor._idIterator++;
                processor._ipAddress = "127.0.0.WAN";
            }

            if (dummyObject.TryGetComponent(out CharacterClassManager ccm))
            {
                ccm.CurClass = roleType;
                ccm.GodMode = true;
            }

            string dummyNickname = roleType.ToString();

            switch (roleType)
            {
                case RoleType.NtfPrivate:
                    dummyNickname = "MTF";
                    break;

                case RoleType.ChaosRifleman:
                    dummyNickname = "CI";
                    break;

                case RoleType.Scp93953:
                    dummyNickname = "SCP939";
                    break;
            }

            if (dummyObject.TryGetComponent(out NicknameSync nicknameSync))
            {
                nicknameSync.Network_myNickSync = "RAGDOLL SPAWNPOINT";
                nicknameSync.CustomPlayerInfo = $"{dummyNickname} RAGDOLL\nSPAWN POINT";
                nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
            }

            SpawnedObjects.Add(dummyObject.AddComponent<IndicatorObjectComponent>().Init(ragdollSpawnPoint));
            NetworkServer.Spawn(dummyObject);

            if (dummyObject.TryGetComponent(out ReferenceHub rh))
                Timing.CallDelayed(0.1f, () =>
                {
                    rh.playerMovementSync.OverridePosition(position, 0f);
                });
        }

        /// <summary>
        /// Spawns a <see cref="IndicatorObjectComponent"/> given a specified <see cref="LightSourceComponent"/>.
        /// </summary>
        /// <param name="lightSource">The specified <see cref="LightSourceComponent"/>.</param>
        /// <param name="indicator">The <see cref="IndicatorObjectComponent"/> attached to the specified <see cref="LightSourceComponent"/>.</param>
        public static void SpawnObjectIndicator(LightSourceComponent lightSource, IndicatorObjectComponent indicator = null)
        {
            if (indicator != null)
            {
                indicator.transform.position = lightSource.transform.position;
                return;
            }

            Pickup pickup = new Item(ItemType.SCP2176).Create(lightSource.transform.position, Quaternion.Euler(180f, 0f, 0f), Vector3.one * 2f);
            pickup.Locked = true;

            GameObject pickupGameObject = pickup.Base.gameObject;
            if (pickupGameObject.gameObject.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true;

            SpawnedObjects.Add(pickupGameObject.AddComponent<IndicatorObjectComponent>().Init(lightSource));
            NetworkServer.Spawn(pickupGameObject);
        }

        /// <summary>
        /// Spawns a <see cref="IndicatorObjectComponent"/> given a specified <see cref="TeleportComponent"/>.
        /// </summary>
        /// <param name="teleport">The specified <see cref="TeleportComponent"/>.</param>
        /// <param name="indicator">The <see cref="IndicatorObjectComponent"/> attached to the specified <see cref="TeleportComponent"/>.</param>
        public static void SpawnObjectIndicator(TeleportComponent teleport, IndicatorObjectComponent indicator = null)
        {
            PrimitiveObjectToy primitive;

            if (indicator != null)
            {
                if (indicator.TryGetComponent(out primitive))
                {
                    primitive.transform.position = teleport.transform.position;
                    primitive.transform.localScale = teleport.transform.localScale;
                    primitive.UpdatePositionServer();
                }

                return;
            }

            if (Object.Instantiate(ObjectType.Primitive.GetObjectByMode(), teleport.transform.position, Quaternion.identity).TryGetComponent(out primitive))
            {
                primitive.NetworkPrimitiveType = PrimitiveType.Cube;
                primitive.NetworkMaterialColor = teleport.IsEntrance ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
                primitive.NetworkScale = -teleport.transform.localScale;
            }

            SpawnedObjects.Add(primitive.gameObject.AddComponent<IndicatorObjectComponent>().Init(teleport));
            NetworkServer.Spawn(primitive.gameObject);
        }
    }
}
