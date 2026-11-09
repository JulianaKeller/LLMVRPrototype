using LLMVRPrototype.Assets;
using LLMVRPrototype.Assets.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public static class AssetDefinitionTypeResolver
    {
        public static readonly List<Type> AvailableDefinitionTypes = new()
        {
            typeof(ObjectAssetDefinition),
            typeof(ParticleSystemAssetDefinition),
            typeof(LightAssetDefinition),
            typeof(TextureAssetDefinition),
            typeof(MaterialAssetDefinition),
            typeof(AudioAssetDefinition),
            typeof(GenericAssetDefinition)
        };

        public static Type Resolve(UnityEngine.Object asset)
        {
            if (asset == null)
                return typeof(GenericAssetDefinition);

            if (asset is Material)
                return typeof(MaterialAssetDefinition);

            if (asset is Texture)
                return typeof(TextureAssetDefinition);

            if (asset is AudioClip)
                return typeof(AudioAssetDefinition);

            if (asset is GameObject gameObject)
            {
                ParticleSystem particleSystem =
                    gameObject.GetComponentInChildren<ParticleSystem>(true);

                if (particleSystem != null)
                    return typeof(ParticleSystemAssetDefinition);

                Light light =
                    gameObject.GetComponentInChildren<Light>(true);

                if (light != null)
                    return typeof(LightAssetDefinition);

                return typeof(ObjectAssetDefinition);
            }

            if (asset is Mesh || asset is AnimationClip)
                return typeof(ObjectAssetDefinition);

            return typeof(GenericAssetDefinition);
        }

        public static string GetDisplayName(Type definitionType)
        {
            if (definitionType == typeof(ObjectAssetDefinition))
                return "Object";

            if (definitionType == typeof(ParticleSystemAssetDefinition))
                return "Particle System";

            if (definitionType == typeof(LightAssetDefinition))
                return "Light";

            if (definitionType == typeof(TextureAssetDefinition))
                return "Texture";

            if (definitionType == typeof(MaterialAssetDefinition))
                return "Material";

            if (definitionType == typeof(AudioAssetDefinition))
                return "Audio";

            if (definitionType == typeof(GenericAssetDefinition))
                return "Generic";

            return definitionType.Name;
        }
    }
}