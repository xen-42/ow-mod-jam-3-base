using OWML.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ModJam3
{
    public interface INewHorizons
    {
        [Obsolete("Create(Dictionary<string, object> config) is deprecated, please use LoadConfigs(IModBehaviour mod) instead")]
        void Create(Dictionary<string, object> config);

        [Obsolete("Create(Dictionary<string, object> config) is deprecated, please use LoadConfigs(IModBehaviour mod) instead")]
        void Create(Dictionary<string, object> config, IModBehaviour mod);

        /// <summary>
        /// Will load all configs in the regular folders (planets, systems, translations, etc) for this mod.
        /// The NH addon config template is just a single call to this API method.
        /// </summary>
        void LoadConfigs(IModBehaviour mod);

        /// <summary>
        /// Retrieve the root GameObject of a custom planet made by creating configs. 
        /// Will only work if the planet has been created (see GetStarSystemLoadedEvent)
        /// </summary>
        GameObject GetPlanet(string name);

        /// <summary>
        /// The name of the current star system loaded.
        /// </summary>
        string GetCurrentStarSystem();

        /// <summary>
        /// An event invoked when the player begins loading the new star system, before the scene starts to load.
        /// Gives the name of the star system being switched to.
        /// </summary>
        UnityEvent<string> GetChangeStarSystemEvent();

        /// <summary>
        /// An event invoked when NH has finished generating all planets for a new star system.
        /// Gives the name of the star system that was just loaded.
        /// </summary>
        UnityEvent<string> GetStarSystemLoadedEvent();

        /// <summary>
        /// An event invoked when NH has finished a planet for a star system.
        /// Gives the name of the planet that was just loaded.
        /// </summary>
        UnityEvent<string> GetBodyLoadedEvent();

        /// <summary>
        /// Uses JSONPath to query a body
        /// </summary>
        object QueryBody(Type outType, string bodyName, string path);

        /// <summary>
        /// Uses JSONPath to query a system
        /// </summary>
        object QuerySystem(Type outType, string path);

        /// <summary>
        /// Allows you to overwrite the default system. This is where the player is respawned after dying.
        /// </summary>
        bool SetDefaultSystem(string name);

        /// <summary>
        /// Allows you to instantly begin a warp to a new star system.
        /// Will return false if that system does not exist (cannot be warped to).
        /// </summary>
        bool ChangeCurrentStarSystem(string name);

        /// <summary>
        /// Returns the uniqueIDs of each installed NH addon.
        /// </summary>
        string[] GetInstalledAddons();

        /// <summary>
        /// Allows you to spawn a copy of a prop by specifying its path.
        /// This is the same as using Props->details in a config, but also returns the spawned gameObject to you.
        /// </summary>
        GameObject SpawnObject(IModBehaviour mod, GameObject planet, Sector sector, string propToCopyPath, Vector3 position, Vector3 eulerAngles,
            float scale, bool alignWithNormal);

        (CharacterDialogueTree, RemoteDialogueTrigger) SpawnDialogue(IModBehaviour mod, GameObject root, string xmlFile, float radius = 1f,
            float range = 1f, string blockAfterPersistentCondition = null, float lookAtRadius = 1f, string pathToAnimController = null,
            float remoteTriggerRadius = 0f);

        /// <summary>
        /// Allows creation of dialogue by directly passing the xml and dialogueInfo json contents as strings
        /// </summary>
        /// <param name="textAssetID">TextAsset name used for compatibility with voice mod. Just has to be a unique identifier.</param>
        /// <param name="xml">The contents of the dialogue xml file as a string</param>
        /// <param name="dialogueInfo">The json dialogue info as a string. See the documentation/schema for what this can contain.</param>
        /// <param name="planetGO">The root planet rigidbody that this dialogue is attached to. Any paths in the dialogueInfo are relative to this body.</param>
        /// <returns></returns>
        (CharacterDialogueTree, RemoteDialogueTrigger) CreateDialogueFromXML(string textAssetID, string xml, string dialogueInfo, GameObject planetGO);

        /// <summary>
        /// Allows the creation of Nomai text by directly passing the xml and translatorTextInfo json contents as strings
        /// </summary>
        /// <param name="xml">The contents of the translator text file as a string</param>
        /// <param name="textInfo">The json translator text info as a string. See the documentation/schema for what this can contain.</param>
        /// <param name="planetGO">The root planet rigidbody that this text is attached to. Any paths in the translatorTextInfo are relative to this body.</param>
        /// <returns></returns>
        GameObject CreateNomaiText(string xml, string textInfo, GameObject planetGO);
    }
}