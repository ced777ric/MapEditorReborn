﻿namespace MapEditorReborn.Events.EventArgs
{
    using System;
    using Exiled.API.Features;
    using global::MapEditorReborn.API.Features.Components;

    /// <summary>
    /// Contains all information before a <see cref="MapEditorObject"/> is deleted.
    /// </summary>
    public class DeletingObjectEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeletingObjectEventArgs"/> class.
        /// </summary>
        /// <param name="player"><inheritdoc cref="Player"/></param>
        /// <param name="mapEditorObject"><inheritdoc cref="Object"/></param>
        /// <param name="isAllowed"><inheritdoc cref="IsAllowed"/></param>
        public DeletingObjectEventArgs(Player player, MapEditorObject mapEditorObject, bool isAllowed = true)
        {
            Player = player;
            Object = mapEditorObject;
            IsAllowed = isAllowed;
        }

        /// <summary>
        /// Gets the <see cref="Exiled.API.Features.Player"/> who's deleting the <see cref="MapEditorObject"/>.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets or sets the <see cref="MapEditorObject"/> which is being deleted.
        /// </summary>
        public MapEditorObject Object { get; set; }

        /// <summary>
        /// Gets or sets the response to be displayed if the event cannot be executed.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MapEditorObject"/> can be deleted.
        /// </summary>
        public bool IsAllowed { get; set; }
    }
}
