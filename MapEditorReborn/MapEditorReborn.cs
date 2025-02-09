﻿namespace MapEditorReborn
{
    using System;
    using System.IO;
    using Exiled.API.Features;
    using HarmonyLib;

    using EventHandler = Events.Handlers.Internal.EventHandler;
    using MapEvent = Exiled.Events.Handlers.Map;
    using PlayerEvent = Exiled.Events.Handlers.Player;
    using ServerEvent = Exiled.Events.Handlers.Server;

    /// <summary>
    /// The main <see cref="MapEditorReborn"/> plugin class.
    /// </summary>
    public class MapEditorReborn : Plugin<Config, Translation>
    {
        /// <summary>
        /// The <see langword="static"/> instance of the <see cref="MapEditorReborn"/>.
        /// </summary>
        public static MapEditorReborn Singleton;

        /// <summary>
        /// Gets the MapEditorReborn parent folder path.
        /// </summary>
        public static string PluginDir { get; } = Path.Combine(Paths.Configs, "MapEditorReborn");

        /// <summary>
        /// Gets the folder path in which the maps are stored.
        /// </summary>
        public static string MapsDir { get; } = Path.Combine(PluginDir, "Maps");

        /// <summary>
        /// Gets the folder path in which the schematics are stored.
        /// </summary>
        public static string SchematicsDir { get; } = Path.Combine(PluginDir, "Schematics");

        private Harmony _harmony;

        private FileSystemWatcher _fileSystemWatcher;

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Singleton = this;

            if (!Directory.Exists(PluginDir))
            {
                Log.Warn("MapEditorReborn parent directory does not exist. Creating...");
                Directory.CreateDirectory(PluginDir);
            }

            if (!Directory.Exists(MapsDir))
            {
                Log.Warn("Maps directory does not exist. Creating...");
                Directory.CreateDirectory(MapsDir);
            }

            if (!Directory.Exists(SchematicsDir))
            {
                Log.Warn("Schematics directory does not exist. Creating...");
                Directory.CreateDirectory(SchematicsDir);
            }

            _harmony = new Harmony($"michal78900.mapEditorReborn-{DateTime.Now.Ticks}");
            _harmony.PatchAll();

            ServerEvent.WaitingForPlayers += EventHandler.OnWaitingForPlayers;
            ServerEvent.RoundStarted += EventHandler.OnRoundStarted;

            PlayerEvent.DroppingItem += EventHandler.OnDroppingItem;
            PlayerEvent.Shooting += EventHandler.OnShooting;
            PlayerEvent.InteractingShootingTarget += EventHandler.OnInteractingShootingTarget;
            PlayerEvent.AimingDownSight += EventHandler.OnAimingDownSight;
            PlayerEvent.DamagingShootingTarget += EventHandler.OnDamagingShootingTarget;
            PlayerEvent.TogglingWeaponFlashlight += EventHandler.OnTogglingWeaponFlashlight;
            PlayerEvent.UnloadingWeapon += EventHandler.OnUnloadingWeapon;

            MapEvent.Generated += EventHandler.OnGenerated;
            MapEvent.ChangingIntoGrenade += EventHandler.OnChangingIntoGrenade;

            if (Config.EnableFileSystemWatcher)
            {
                _fileSystemWatcher = new FileSystemWatcher(MapsDir)
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.yml",
                    EnableRaisingEvents = true,
                };

                _fileSystemWatcher.Changed += EventHandler.OnFileChanged;

                Log.Debug("FileSystemWatcher enabled!", Config.Debug);
            }

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Singleton = null;
            _harmony.UnpatchAll();

            ServerEvent.WaitingForPlayers -= EventHandler.OnWaitingForPlayers;
            ServerEvent.RoundStarted += EventHandler.OnRoundStarted;

            PlayerEvent.DroppingItem -= EventHandler.OnDroppingItem;
            PlayerEvent.Shooting -= EventHandler.OnShooting;
            PlayerEvent.InteractingShootingTarget -= EventHandler.OnInteractingShootingTarget;
            PlayerEvent.AimingDownSight -= EventHandler.OnAimingDownSight;
            PlayerEvent.DamagingShootingTarget -= EventHandler.OnDamagingShootingTarget;
            PlayerEvent.TogglingWeaponFlashlight -= EventHandler.OnTogglingWeaponFlashlight;
            PlayerEvent.UnloadingWeapon -= EventHandler.OnUnloadingWeapon;

            MapEvent.Generated -= EventHandler.OnGenerated;
            MapEvent.ChangingIntoGrenade -= EventHandler.OnChangingIntoGrenade;

            if (_fileSystemWatcher != null)
                _fileSystemWatcher.Changed -= EventHandler.OnFileChanged;

            base.OnDisabled();
        }

        /// <inheritdoc/>
        public override string Name => "MapEditorReborn";

        /// <inheritdoc/>
        public override string Author => "Michal78900 (original idea by Killers0992)";

        /// <inheritdoc/>
        public override Version Version => new Version(2, 0, 0);

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(4, 0, 0);
    }
}
