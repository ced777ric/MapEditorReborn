﻿namespace MapEditorReborn.API.Features.Components.ObjectComponents
{
    using AdminToys;
    using Exiled.API.Enums;
    using Features.Objects;
    using Mirror;
    using UnityEngine;

    /// <summary>
    /// The component added to <see cref="PrimitiveObject"/>.
    /// </summary>
    public class PrimitiveObjectComponent : MapEditorObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveObjectComponent"/> class.
        /// </summary>
        /// <param name="primitiveObject">The required <see cref="PrimitiveObject"/>.</param>
        /// <param name="spawn">A value indicating whether the component should be spawned.</param>
        /// <returns>The initialized <see cref="PrimitiveObjectComponent"/> instance.</returns>
        public PrimitiveObjectComponent Init(PrimitiveObject primitiveObject, bool spawn = true)
        {
            Base = primitiveObject;

            if (TryGetComponent(out PrimitiveObjectToy primitiveObjectToy))
                primitive = primitiveObjectToy;

            primitive.NetworkMovementSmoothing = 60;

            prevScale = transform.localScale;

            ForcedRoomType = primitiveObject.RoomType == RoomType.Unknown ? FindRoom().Type : primitiveObject.RoomType;
            UpdateObject();

            if (spawn)
                NetworkServer.Spawn(gameObject);

            return this;
        }

        /// <summary>
        /// The base <see cref="PrimitiveObject"/>.
        /// </summary>
        public PrimitiveObject Base;

        /// <inheritdoc cref="MapEditorObject.UpdateObject()"/>
        public override void UpdateObject()
        {
            primitive.UpdatePositionServer();
            primitive.NetworkPrimitiveType = Base.PrimitiveType;
            primitive.NetworkMaterialColor = GetColorFromString(Base.Color);

            if (prevScale != transform.localScale)
            {
                prevScale = transform.localScale;
                base.UpdateObject();
            }
        }

        private Vector3 prevScale;
        private PrimitiveObjectToy primitive;
    }
}
