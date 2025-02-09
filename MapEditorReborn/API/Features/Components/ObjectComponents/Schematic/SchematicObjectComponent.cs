﻿namespace MapEditorReborn.API.Features.Components.ObjectComponents
{
    using System;
    using System.Collections.Generic;
    using AdminToys;
    using Enums;
    using Events.EventArgs;
    using Events.Handlers;
    using Exiled.API.Enums;
    using Exiled.API.Features.Items;
    using Extensions;
    using MEC;
    using Objects;
    using Objects.Schematics;
    using UnityEngine;

    using static API;

    using MapEditorObject = MapEditorObject;

    /// <summary>
    /// Component added to SchematicObject. Is is used for easier idendification of the object and it's variables.
    /// </summary>
    public class SchematicObjectComponent : MapEditorObject
    {
        /// <summary>
        /// Initializes the <see cref="SchematicObjectComponent"/>.
        /// </summary>
        /// <param name="schematicObject">The <see cref="SchematicObject"/> to instantiate.</param>
        /// <param name="data">The object data from a file.</param>
        /// <returns>Instance of this compoment.</returns>
        public SchematicObjectComponent Init(SchematicObject schematicObject, SaveDataObjectList data)
        {
            Base = schematicObject;
            SchematicData = data;
            ForcedRoomType = schematicObject.RoomType != RoomType.Unknown ? schematicObject.RoomType : FindRoom().Type;

            foreach (PrimitiveObject primitive in data.Primitives)
            {
                if (Instantiate(ObjectType.Primitive.GetObjectByMode(), transform.TransformPoint(primitive.Position), transform.rotation * Quaternion.Euler(primitive.Rotation)).TryGetComponent(out PrimitiveObjectToy primitiveObject))
                {
                    primitiveObject.transform.localScale = Vector3.Scale(primitive.Scale, schematicObject.Scale);

                    primitiveObject.name = $"CustomSchematicBlock-Primitive{primitive.PrimitiveType}";

                    primitiveObject.gameObject.AddComponent<PrimitiveObjectComponent>().Init(primitive, false);
                    AttachedBlocks.Add(primitiveObject.gameObject.AddComponent<SchematicBlockComponent>().Init(this, primitive.Position, primitive.Rotation, primitive.Scale));
                }
            }

            foreach (LightSourceObject lightSource in data.LightSources)
            {
                if (Instantiate(ObjectType.LightSource.GetObjectByMode(), transform.TransformPoint(lightSource.Position), Quaternion.identity).TryGetComponent(out LightSourceToy lightSourceToy))
                {
                    lightSourceToy.name = "CustomSchematicBlock-LightSource";
                    lightSourceToy.gameObject.AddComponent<LightSourceComponent>().Init(lightSource, false);
                    AttachedBlocks.Add(lightSourceToy.gameObject.AddComponent<SchematicBlockComponent>().Init(this, lightSource.Position, Vector3.zero, Vector3.one));
                }
            }

            foreach (ItemSpawnPointObject item in data.Items)
            {
                Pickup pickup = new Item((ItemType)Enum.Parse(typeof(ItemType), item.Item)).Create(transform.TransformPoint(item.Position), transform.rotation * Quaternion.Euler(item.Rotation), Vector3.Scale(item.Scale, schematicObject.Scale));

                pickup.Locked = true;

                if (pickup.Base.TryGetComponent(out Rigidbody rb))
                    rb.isKinematic = true;

                pickup.Base.name = $"CustomSchematicBlock-Item{pickup.Type}";

                AttachedBlocks.Add(pickup.Base.gameObject.AddComponent<SchematicBlockComponent>().Init(this, item.Position, item.Rotation, item.Scale));
            }

            foreach (var workStation in data.WorkStations)
            {
                GameObject gameObject = Instantiate(ObjectType.WorkStation.GetObjectByMode(), transform.TransformPoint(workStation.Position), transform.rotation * Quaternion.Euler(workStation.Rotation));
                gameObject.transform.localScale = Vector3.Scale(workStation.Scale, schematicObject.Scale);

                if (gameObject.TryGetComponent(out InventorySystem.Items.Firearms.Attachments.WorkstationController workstationController))
                    workstationController.NetworkStatus = 4;

                gameObject.name = "CustomSchematicBlock-Workstation";

                AttachedBlocks.Add(gameObject.AddComponent<SchematicBlockComponent>().Init(this, workStation.Position, workStation.Rotation, workStation.Scale));
            }

            UpdateObject();
            Timing.RunCoroutine(UpdateAnimation());

            return this;
        }

        /// <summary>
        /// The base config of the object which contains its properties.
        /// </summary>
        public SchematicObject Base;

        /// <summary>
        /// Gets a <see cref="SaveDataObjectList"/> used to build a schematic.
        /// </summary>
        public SaveDataObjectList SchematicData { get; private set; }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="SchematicBlockComponent"/> which contains all attached blocks.
        /// </summary>
        public List<SchematicBlockComponent> AttachedBlocks { get; private set; } = new List<SchematicBlockComponent>();

        /// <summary>
        /// Gets the original position.
        /// </summary>
        public Vector3 OriginalPosition { get; private set; }

        /// <summary>
        /// Gets the original rotation.
        /// </summary>
        public Vector3 OriginalRotation { get; private set; }

        /// <inheritdoc cref="MapEditorObject.UpdateObject()"/>
        public override void UpdateObject()
        {
            if (Base.SchematicName != name.Split(new[] { '-' })[1])
            {
                var newObject = ObjectSpawner.SpawnSchematic(Base, transform.position, transform.rotation, transform.localScale);

                if (newObject != null)
                {
                    SpawnedObjects[SpawnedObjects.FindIndex(x => x == this)] = newObject;

                    Destroy();
                    return;
                }

                Base.SchematicName = name.Replace("CustomSchematic-", string.Empty);
            }

            OriginalPosition = RelativePosition;
            OriginalRotation = RelativeRotation;

            Timing.RunCoroutine(UpdateBlocks());
        }

        /// <summary>
        /// Plays one frame for each block in <see cref="AttachedBlocks"/>.
        /// </summary>
        public void PlayOneFrameForBlocks()
        {
            foreach (SchematicBlockComponent block in AttachedBlocks)
            {
                block.PlayOneFrame();
            }
        }

        /// <summary>
        /// Moves the <see cref="AttachedBlocks"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator{T}"/> which represents one frame delay.</returns>
        public IEnumerator<float> MoveBlocks()
        {
            foreach (SchematicBlockComponent block in AttachedBlocks)
            {
                block.UpdateObject();
            }

            yield return Timing.WaitForOneFrame;
        }

        private IEnumerator<float> UpdateBlocks()
        {
            foreach (SchematicBlockComponent block in AttachedBlocks)
            {
                block.UpdateObject();

                if (UpdateDelay >= 0f)
                    yield return UpdateDelay == 0f ? Timing.WaitForOneFrame : Timing.WaitForSeconds(UpdateDelay);
            }

            yield return Timing.WaitForOneFrame;
        }

        private IEnumerator<float> UpdateAnimation()
        {
            if (SchematicData.ParentAnimationFrames.Count == 0)
                yield break;

            StartingSchematicAnimationEventArgs startingEv = new StartingSchematicAnimationEventArgs(this, true);
            Schematic.OnStartingSchematicAnimation(startingEv);

            if (!startingEv.IsAllowed)
                yield break;

            foreach (AnimationFrame frame in SchematicData.ParentAnimationFrames)
            {
                Vector3 remainingPosition = frame.PositionAdded;
                Vector3 remainingRotation = frame.RotationAdded;
                Vector3 deltaPosition = remainingPosition / Mathf.Abs(frame.PositionRate);
                Vector3 deltaRotation = remainingRotation / Mathf.Abs(frame.RotationRate);

                yield return Timing.WaitForSeconds(frame.Delay);

                while (true)
                {
                    if (remainingPosition != Vector3.zero)
                    {
                        transform.position += deltaPosition;
                        remainingPosition -= deltaPosition;
                    }

                    if (remainingRotation != Vector3.zero)
                    {
                        transform.Rotate(deltaRotation, Space.World);
                        remainingRotation -= deltaRotation;
                    }

                    Timing.RunCoroutine(MoveBlocks());

                    if (remainingPosition.sqrMagnitude <= 1 && remainingRotation.sqrMagnitude <= 1)
                        break;

                    yield return Timing.WaitForSeconds(frame.FrameLength);
                }
            }

            var endingEv = new EndingSchematicAnimationEventArgs(this, SchematicData.AnimationEndAction);
            Schematic.OnEndingSchematicAnimation(endingEv);

            SchematicData.AnimationEndAction = endingEv.AnimationEndAction;

            if (SchematicData.AnimationEndAction == AnimationEndAction.Destroy)
            {
                Destroy();
            }
            else if (SchematicData.AnimationEndAction == AnimationEndAction.Loop)
            {
                transform.position = OriginalPosition;
                transform.eulerAngles = OriginalRotation;
                Timing.RunCoroutine(MoveBlocks());
                Timing.RunCoroutine(UpdateAnimation());
            }
        }

        private void OnDestroy()
        {
            foreach (SchematicBlockComponent block in AttachedBlocks)
            {
                block?.Destroy();
            }
        }

        private static readonly float UpdateDelay = MapEditorReborn.Singleton.Config.SchematicBlockSpawnDelay;
    }
}
